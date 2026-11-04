using Infrastructure.Agent.Filters;
using Infrastructure.Agent.Plugins;
using Infrastructure.Common.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;

namespace Infrastructure.Agent;

public interface IKernelFactory
{
    Kernel CreateKernel();
}

/// <summary>
/// Builds a brand-new Kernel per call. Plugins/filters are resolved from the request-scoped
/// container on every CreateKernel() so they carry the authenticated user's identity
/// (ICurrentUserProvider) for exactly one request/conversation - never shared or reused as a
/// singleton across users.
/// </summary>
public sealed class KernelFactory(
    IServiceProvider serviceProvider,
    IOptions<AiOptions> options,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory) : IKernelFactory
{
    public const string FastUtilityModelServiceId = "FastUtilityModel";
    public const string ReasoningModelServiceId = "ReasoningModel";

    private readonly AiOptions _options = options.Value;

    public Kernel CreateKernel()
    {
        var builder = Kernel.CreateBuilder();
        builder.Services.AddSingleton(loggerFactory);

        var httpClient = httpClientFactory.CreateClient("InferenceClient");

        ConfigureModelConnector(builder, _options.FastModel, FastUtilityModelServiceId, httpClient);
        ConfigureModelConnector(builder, _options.ReasoningModel, ReasoningModelServiceId, httpClient);

        var kernel = builder.Build();

        var portfolioPlugin = serviceProvider.GetService<PortfolioAgentPlugin>();
        if (portfolioPlugin is not null)
        {
            kernel.Plugins.AddFromObject(portfolioPlugin, "PortfolioOperations");
        }

        var loopFilter = serviceProvider.GetService<LoopCappingFilter>();
        if (loopFilter is not null)
        {
            kernel.AutoFunctionInvocationFilters.Add(loopFilter);
        }

        var auditFilter = serviceProvider.GetService<ToolInvocationAuditFilter>();
        if (auditFilter is not null)
        {
            kernel.FunctionInvocationFilters.Add(auditFilter);
        }

        return kernel;
    }

    private void ConfigureModelConnector(
        IKernelBuilder builder,
        ModelOptions modelOptions,
        string serviceId,
        HttpClient httpClient)
    {
        var modelId = string.IsNullOrWhiteSpace(modelOptions.ModelId) ? "llama3.2" : modelOptions.ModelId;
        var apiKey = string.IsNullOrWhiteSpace(modelOptions.ApiKey) ? "unused" : modelOptions.ApiKey;

        if (_options.Provider == AiProviderType.AzureOpenAI)
        {
            builder.AddAzureOpenAIChatCompletion(
                deploymentName: modelOptions.DeploymentName ?? modelId,
                endpoint: modelOptions.Endpoint!,
                apiKey: apiKey,
                serviceId: serviceId,
                httpClient: httpClient);
            return;
        }

        // Works for OpenAI and Ollama (via Ollama's OpenAI-compatible /v1 endpoint).
        if (!string.IsNullOrWhiteSpace(modelOptions.Endpoint))
        {
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                endpoint: new Uri(modelOptions.Endpoint),
                apiKey: apiKey,
                serviceId: serviceId,
                httpClient: httpClient);
        }
        else
        {
            builder.AddOpenAIChatCompletion(
                modelId: modelId,
                apiKey: apiKey,
                serviceId: serviceId,
                httpClient: httpClient);
        }
    }
}
