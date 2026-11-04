namespace Domain.Common.Exceptions;

public sealed class AgentLoopExhaustedException(int maxIterations)
    : Exception($"Agent execution halted: exceeded maximum allowed tool loops ({maxIterations}).")
{
    public int MaxIterations { get; } = maxIterations;
}
