using System.Diagnostics;
using Application.Common.Security;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Infrastructure.Agent.Filters;

/// <summary>Per-tool-call audit log: who invoked which plugin function, and how long it took.</summary>
public sealed class ToolInvocationAuditFilter(
    ILogger<ToolInvocationAuditFilter> logger,
    ICurrentUserProvider currentUserProvider) : IFunctionInvocationFilter
{
    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();

        logger.LogInformation(
            "User {UserId} invoking tool {PluginName}.{FunctionName}",
            currentUserProvider.UserId,
            context.Function.PluginName,
            context.Function.Name);

        await next(context);

        stopwatch.Stop();
        logger.LogInformation(
            "Tool {PluginName}.{FunctionName} completed in {ElapsedMs}ms for user {UserId}",
            context.Function.PluginName,
            context.Function.Name,
            stopwatch.ElapsedMilliseconds,
            currentUserProvider.UserId);
    }
}
