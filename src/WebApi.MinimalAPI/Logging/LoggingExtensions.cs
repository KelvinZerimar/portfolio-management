using System.Diagnostics;
using Serilog;

namespace WebApi.MinimalAPI.Logging;

public static class LoggingExtensions
{
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((context, loggerConfig) => loggerConfig
            .ReadFrom.Configuration(context.Configuration)
            .WriteTo.Conditional(static _ => Debugger.IsAttached, static writeTo => writeTo.Console()));

        return builder;
    }
}
