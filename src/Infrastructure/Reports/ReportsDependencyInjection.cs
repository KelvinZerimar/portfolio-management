using Infrastructure.Common.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Reports;

// Not called from the shared AddInfrastructure (which both the API and the Worker call) -
// the status-report hosted service must only run in the Worker process, so Worker.Program.cs
// calls this explicitly instead. IEmailSender itself is still registered by the shared
// AddInfrastructure (see AddAdapters), so that MediatR's handler for SendPortfolioStatusReportCommand
// - scanned from the shared Application assembly - stays structurally resolvable in the API process
// too, even though only the Worker ever actually dispatches that command.
public static class ReportsDependencyInjection
{
    public static IServiceCollection AddReportScheduling(this IServiceCollection services, IConfiguration configuration)
    {
        services
            .AddOptions<EmailOptions>()
            .Bind(configuration.GetRequiredSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.Configure<ReportSchedulingOptions>(configuration.GetSection(ReportSchedulingOptions.SectionName));

        services.AddHostedService<PortfolioStatusReportBackgroundService>();

        return services;
    }
}
