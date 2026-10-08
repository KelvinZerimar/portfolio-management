using System.Diagnostics;
using Microsoft.ApplicationInsights.Channel;
using Microsoft.ApplicationInsights.Extensibility;
using Serilog;
using Serilog.Sinks.ApplicationInsights.TelemetryConverters;

namespace WebApi.MinimalAPI.Logging;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) =>
        {
            loggerConfig
                .ReadFrom.Configuration(context.Configuration)
                .WriteTo.Conditional(static _ => Debugger.IsAttached, static writeTo => writeTo.Console());

            var appInsightsConnectionString = context.Configuration["ApplicationInsights:ConnectionString"];
            if (!Debugger.IsAttached && !string.IsNullOrEmpty(appInsightsConnectionString))
            {
                var telemetryConfiguration = new TelemetryConfiguration { ConnectionString = appInsightsConnectionString };
                telemetryConfiguration.TelemetryInitializers.Add(new CloudRoleNameTelemetryInitializer());

                loggerConfig.WriteTo.ApplicationInsights(telemetryConfiguration, new TraceTelemetryConverter());
            }
        });

        return builder;
    }
}

internal sealed class CloudRoleNameTelemetryInitializer : ITelemetryInitializer
{
    private static readonly string RoleName = typeof(LoggingExtensions).Assembly.GetName().Name ?? "WebApi.MinimalAPI";

    public void Initialize(ITelemetry telemetry) => telemetry.Context.Cloud.RoleName = RoleName;
}
