using Application.Agent.Interfaces;
using Contracts.Agent;
using Domain.Common.Exceptions;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Agent.Command;

public sealed record SendAgentMessageCommand(SendAgentMessageRequest Request) : IRequest<ErrorOr<SendAgentMessageResponse>>;

public sealed class SendAgentMessageCommandHandler(
    ILogger<SendAgentMessageCommandHandler> logger,
    IAgentOrchestrator agentOrchestrator) : IRequestHandler<SendAgentMessageCommand, ErrorOr<SendAgentMessageResponse>>
{
    public async Task<ErrorOr<SendAgentMessageResponse>> Handle(SendAgentMessageCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await agentOrchestrator.SendMessageAsync(command.Request.Message, cancellationToken);
            return new SendAgentMessageResponse(result.Reply);
        }
        catch (AgentLoopExhaustedException ex)
        {
            logger.LogWarning(ex, "Agent chat halted: tool loop cap exceeded");
            return Error.Failure("Agent.LoopExhausted", ex.Message);
        }
    }
}
