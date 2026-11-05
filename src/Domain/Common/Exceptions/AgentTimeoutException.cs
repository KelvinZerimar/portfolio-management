namespace Domain.Common.Exceptions;

public sealed class AgentTimeoutException()
    : Exception("Agent execution halted: the inference request took too long to complete.");
