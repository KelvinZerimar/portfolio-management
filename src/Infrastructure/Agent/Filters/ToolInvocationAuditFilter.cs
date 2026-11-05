using System.Diagnostics;
using System.Text.Json;
using Application.Common.Security;
using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel;

namespace Infrastructure.Agent.Filters;

/// <summary>
/// Per-tool-call audit log: who invoked which plugin function, with what arguments, how long it
/// took, and what it returned - the arguments/result are what the LLM actually saw, which is the
/// one piece of evidence needed to tell "the tool returned bad data" apart from "the model
/// misreported data the tool returned correctly" when a chat answer looks wrong.
/// </summary>
public sealed class ToolInvocationAuditFilter(
    ILogger<ToolInvocationAuditFilter> logger,
    ICurrentUserProvider currentUserProvider) : IFunctionInvocationFilter
{
    private const int MaxLoggedValueLength = 1000;

    public async Task OnFunctionInvocationAsync(
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, Task> next)
    {
        var stopwatch = Stopwatch.StartNew();
        var argumentsText = Truncate(string.Join(", ", context.Arguments.Select(a => $"{a.Key}={a.Value}")));

        logger.LogInformation(
            "User {UserId} invoking tool {PluginName}.{FunctionName}({Arguments})",
            currentUserProvider.UserId,
            context.Function.PluginName,
            context.Function.Name,
            argumentsText);

        await next(context);

        stopwatch.Stop();
        logger.LogInformation(
            "Tool {PluginName}.{FunctionName} completed in {ElapsedMs}ms for user {UserId}, returned: {Result}",
            context.Function.PluginName,
            context.Function.Name,
            stopwatch.ElapsedMilliseconds,
            currentUserProvider.UserId,
            Truncate(SerializeResult(context.Result)));
    }

    // FunctionResult.ToString() on a complex return type (e.g. a List<T>) prints the .NET type
    // name, not the data - JSON-serializing the actual value is what shows what the LLM saw.
    private static string SerializeResult(FunctionResult result)
    {
        var value = result.GetValue<object>();
        return value is null ? "null" : JsonSerializer.Serialize(value);
    }

    private static string Truncate(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value.Length <= MaxLoggedValueLength ? value : string.Concat(value.AsSpan(0, MaxLoggedValueLength), "...");
    }
}
