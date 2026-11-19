namespace Infrastructure.Common.Options;

public enum AiProviderType
{
    OpenAI,
    AzureOpenAI,
    Ollama
}

public sealed class ModelOptions
{
    public string ModelId { get; set; } = string.Empty;
    public string? DeploymentName { get; set; }
    public string? Endpoint { get; set; }
    public string? ApiKey { get; set; }
}

public sealed class MafAgentOptions
{
    // false: the MAF agent is read-only. No Propose* tool or write rule reaches the model, which also keeps
    // the prompt small for local models. Config key: Ai:MafAgent:EnableWriteProposals.
    public bool EnableWriteProposals { get; set; } = true;
}

public sealed class AiOptions
{
    public const string SectionName = "Ai";

    public AiProviderType Provider { get; set; } = AiProviderType.Ollama;
    public ModelOptions FastModel { get; set; } = new();
    public ModelOptions ReasoningModel { get; set; } = new();
    public int MaxAllowedToolLoops { get; set; } = 5;
    public int RequestTimeoutSeconds { get; set; } = 100;
    public MafAgentOptions MafAgent { get; set; } = new();
}
