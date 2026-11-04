namespace Application.Agent.Interfaces;

public interface IAgentOrchestrator
{
    Task<AgentChatResult> SendMessageAsync(string userMessage, CancellationToken cancellationToken);
}

public sealed record AgentChatResult(string Reply, int ChatHistoryLength);
