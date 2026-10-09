using System.Diagnostics;
using Serilog;

namespace WebApi.MinimalAPI.Logging;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        // Default .NET providers (Console, Debug, EventSource) cleared first so that
        // writeToProviders below only forwards to the OpenTelemetry provider that
        // AddAzureMonitorObservability registers — not also to those defaults.
        builder.Logging.ClearProviders();

        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.Conditional(static _ => Debugger.IsAttached, static writeTo => writeTo.Console()),
            writeToProviders: true);

        return builder;
    }
}
