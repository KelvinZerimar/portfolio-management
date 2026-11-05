using Application.Agent.Interfaces;
using Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Polly.Timeout;

namespace Infrastructure.Agent;

public sealed class AgentOrchestrator(
    IKernelFactory kernelFactory,
    ILogger<AgentOrchestrator> logger) : IAgentOrchestrator
{
    public async Task<AgentChatResult> SendMessageAsync(string userMessage, CancellationToken cancellationToken)
    {
        var kernel = kernelFactory.CreateKernel();
        var chatService = kernel.GetRequiredService<IChatCompletionService>(KernelFactory.ReasoningModelServiceId);

        var chatHistory = new ChatHistory();
        chatHistory.AddSystemMessage(
            "You are a portfolio management assistant. Use the available tools to answer questions " +
            "about the authenticated user's own portfolios only. Never invent portfolio ids or values " +
            "that a tool did not return. Holdings lists returned by tools are already sorted from " +
            "highest to lowest value - the first item is the one with the most value and the last is " +
            "the one with the least. Report the Symbol and Value fields exactly as given, and never " +
            "add a currency symbol or name, since none is provided by the tools.\n\n" +
            "Tool selection rules - pick the tool by what the question asks, not by keyword matching " +
            "on the word 'value':\n" +
            "- Question has no specific asset and no date (e.g. 'how much is my portfolio worth') -> " +
            "GetPortfolioValueAsync.\n" +
            "- Question compares assets against each other as of today, no date mentioned (e.g. " +
            "'which asset is worth the most right now') -> GetPortfolioHoldingsAsync.\n" +
            "- Question names a specific asset AND asks about a date, 'when'/'cuándo', or a historical " +
            "peak/trough (e.g. 'when was BTC worth the most') -> GetAssetPeakOrTroughDateAsync. Never " +
            "use GetPortfolioValueAsync or GetPortfolioHoldingsAsync for these - they only know the " +
            "current value, not history.");
        chatHistory.AddUserMessage(userMessage);

        // Low temperature: this assistant relays tool data verbatim, it does not write creative
        // prose - a higher temperature measurably increases the odds of a small local model
        // inventing a value/currency instead of repeating what the tool actually returned.
        var executionSettings = new OpenAIPromptExecutionSettings
        {
            Temperature = 0.1,
            FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
        };

        try
        {
            var response = await chatService.GetChatMessageContentAsync(
                chatHistory,
                executionSettings,
                kernel,
                cancellationToken);

            return new AgentChatResult(response.Content ?? string.Empty, chatHistory.Count);
        }
        catch (AgentLoopExhaustedException ex)
        {
            logger.LogError(ex, "Agent loop exhausted while answering a chat message");
            throw;
        }
        catch (TimeoutRejectedException ex)
        {
            logger.LogError(ex, "Inference request timed out while answering a chat message");
            throw new AgentTimeoutException();
        }
    }
}
