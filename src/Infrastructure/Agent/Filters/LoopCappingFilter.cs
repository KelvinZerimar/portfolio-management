using Domain.Common.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Infrastructure.Agent.Filters;

/// <summary>
/// Hard cap on tool-call loops. This is a security/stability control, not a workflow-tuning
/// knob - if an agent workflow needs more iterations, fix the planning/tool logic rather than
/// raising this cap to make a symptom disappear.
/// </summary>
public sealed class LoopCappingFilter(
    ILogger<LoopCappingFilter> logger,
    int maxAllowedIterations) : IAutoFunctionInvocationFilter
{
    public async Task OnAutoFunctionInvocationAsync(
        AutoFunctionInvocationContext context,
        Func<AutoFunctionInvocationContext, Task> next)
    {
        if (context.RequestSequenceIndex >= maxAllowedIterations)
        {
            context.Terminate = true;
            logger.LogWarning(
                "Agent execution loop capped: reached {RequestSequenceIndex} iterations (limit: {MaxIterations})",
                context.RequestSequenceIndex,
                maxAllowedIterations);

            throw new AgentLoopExhaustedException(maxAllowedIterations);
        }

        await next(context);
    }
}
