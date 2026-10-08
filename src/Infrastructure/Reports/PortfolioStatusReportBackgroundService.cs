using Application.Portfolios.Interfaces;
using Application.Reports;
using Application.Reports.Command;
using Infrastructure.Common.Options;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Infrastructure.Reports;

// Polls at ReportSchedulingOptions.PollIntervalMinutes; only acts once per local calendar day,
// and only when that day matches ReportSchedulingOptions.DayOfMonth. Each eligible portfolio's
// own LastStatusReportSentAt gates it from being emailed twice on the same send day, the same
// way RefreshPortfolioPricesCommandHandler gates its once-a-day price refresh.
internal sealed class PortfolioStatusReportBackgroundService(
    IServiceScopeFactory scopeFactory,
    IOptions<ReportSchedulingOptions> options,
    ILogger<PortfolioStatusReportBackgroundService> logger,
    TimeProvider timeProvider
    ) : BackgroundService
{
    private static readonly TimeZoneInfo SpainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollInterval = TimeSpan.FromMinutes(Math.Max(1, options.Value.PollIntervalMinutes));
        using var timer = new PeriodicTimer(pollInterval);

        do
        {
            try
            {
                await RunOnceAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Portfolio status report sweep failed");
            }
        }
        while (await timer.WaitForNextTickAsync(stoppingToken));
    }

    private async Task RunOnceAsync(CancellationToken cancellationToken)
    {
        var nowUtc = timeProvider.GetUtcNow().UtcDateTime;
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(nowUtc, SpainTimeZone).Date;
        if (localToday.Day != options.Value.DayOfMonth)
        {
            return;
        }

        var (periodStart, periodEnd) = PortfolioStatusReportPeriod.PreviousCalendarMonth(localToday);

        using var scope = scopeFactory.CreateScope();
        var portfolioRepository = scope.ServiceProvider.GetRequiredService<IPortfolioRepository>();
        var sender = scope.ServiceProvider.GetRequiredService<ISender>();

        var portfolios = await portfolioRepository.GetAllAsync(cancellationToken);
        foreach (var portfolio in portfolios)
        {
            if (portfolio.LastStatusReportSentAt.HasValue &&
                TimeZoneInfo.ConvertTimeFromUtc(portfolio.LastStatusReportSentAt.Value, SpainTimeZone).Date == localToday)
            {
                continue;
            }

            var result = await sender.Send(new SendPortfolioStatusReportCommand(portfolio.Id, periodStart, periodEnd), cancellationToken);
            if (result.IsError)
            {
                logger.LogWarning(
                    "Failed to send status report for portfolio {PortfolioId}: {Error}",
                    portfolio.Id, result.FirstError.Code);
            }
        }
    }
}
