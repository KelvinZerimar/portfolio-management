using Application.Agent.Interfaces;
using Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

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
            "that a tool did not return.");
        chatHistory.AddUserMessage(userMessage);

        var executionSettings = new PromptExecutionSettings
        {
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
    }
}
