using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace WebApi.MinimalAPI.Logging;

public static class OpenTelemetryExtensions
{
    // Skipped entirely when debugging locally or when no connection string is configured —
    // same guard as the (now-removed) Serilog Application Insights sink it replaces.
    // This is the only channel to Azure Monitor: traces, metrics and logs together, instead
    // of just forwarding ILogger events as Serilog did.
    public static WebApplicationBuilder AddAzureMonitorObservability(this WebApplicationBuilder builder)
    {
        var appInsightsConnectionString = builder.Configuration["ApplicationInsights:ConnectionString"];
        if (Debugger.IsAttached || string.IsNullOrEmpty(appInsightsConnectionString))
        {
            return builder;
        }

        var samplingRatio = builder.Configuration.GetValue("ApplicationInsights:SamplingRatio", 1.0f);
        var roleName = typeof(OpenTelemetryExtensions).Assembly.GetName().Name ?? "WebApi.MinimalAPI";

        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(serviceName: roleName))
            .UseAzureMonitor(options =>
            {
                options.ConnectionString = appInsightsConnectionString;
                options.SamplingRatio = samplingRatio;
            })
            // Dependencias Npgsql/EF Core no vienen instrumentadas por defecto en el distro de
            // Azure Monitor (solo ASP.NET Core + HttpClient) — se agregan explícitamente para
            // tener visibilidad de queries. El SamplingRatio es la palanca principal de costo
            // si este instrumentador resulta demasiado "chatty".
            .WithTracing(tracing => tracing.AddEntityFrameworkCoreInstrumentation());

        return builder;
    }
}
