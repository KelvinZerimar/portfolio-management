using Application.Common.Messaging;
using Application.CryptoCurrencies.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Reports.Interfaces;
using Application.Users.Interfaces;
using Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Reports.Command;

public sealed record SendPortfolioStatusReportCommand(long PortfolioId, DateTime PeriodStart, DateTime PeriodEnd)
    : IRequest<ErrorOr<Success>>, ICommand;

public sealed class SendPortfolioStatusReportCommandHandler(
    ILogger<SendPortfolioStatusReportCommandHandler> logger,
    IPortfolioRepository portfolioRepository,
    IPortfolioEntryRepository portfolioEntryRepository,
    IUserRepository userRepository,
    ICoinGeckoClient coinGeckoClient,
    IEmailSender emailSender,
    TimeProvider timeProvider
    ) : IRequestHandler<SendPortfolioStatusReportCommand, ErrorOr<Success>>
{
    public async Task<ErrorOr<Success>> Handle(SendPortfolioStatusReportCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(command.PortfolioId, cancellationToken);
        if (portfolio is null)
        {
            return Error.NotFound("Portfolio.NotFound", $"Portfolio with ID '{command.PortfolioId}' was not found.");
        }

        var user = await userRepository.GetByIdAsync(portfolio.UserId, cancellationToken);
        if (user is null)
        {
            return Error.NotFound("User.NotFound", $"User with ID '{portfolio.UserId}' was not found.");
        }

        var entries = await portfolioEntryRepository.GetByPortfolioIdAsync(command.PortfolioId, cancellationToken);
        var report = portfolio.GetStatusReport(entries, command.PeriodStart, command.PeriodEnd);
        var marketRows = await BuildMarketRowsAsync(command, report, entries, cancellationToken);

        var subject = PortfolioStatusReportEmailTemplate.RenderSubject(portfolio.Name, report);
        var body = PortfolioStatusReportEmailTemplate.RenderBody(portfolio.Name, report, marketRows);
        await emailSender.SendAsync(user.Email, subject, body, cancellationToken);

        portfolio.LastStatusReportSentAt = timeProvider.GetUtcNow().UtcDateTime;
        portfolioRepository.Update(portfolio);

        logger.LogInformation(
            "Sent status report email for portfolio {PortfolioId} to {Email}", command.PortfolioId, user.Email);

        return Result.Success;
    }

    // Best-effort: a missing CoinGeckoId mapping or a CoinGecko failure just means fewer (or no)
    // market rows in the email, never a reason to fail the whole send - this section is
    // supplementary context, not core to the report.
    private async Task<IReadOnlyList<PortfolioMarketRow>> BuildMarketRowsAsync(
        SendPortfolioStatusReportCommand command, PortfolioStatusReport report, List<PortfolioEntry> entries, CancellationToken cancellationToken)
    {
        var coinGeckoIdByCryptoCurrencyId = entries
            .GroupBy(e => e.CryptoCurrencyId)
            .ToDictionary(g => g.Key, g => g.First().CryptoCurrency.CoinGeckoId);

        var holdingsWithCoinGeckoId = report.Holdings
            .Select(h => (Holding: h, CoinGeckoId: coinGeckoIdByCryptoCurrencyId.GetValueOrDefault(h.CryptoCurrencyId)))
            .Where(x => !string.IsNullOrWhiteSpace(x.CoinGeckoId))
            .ToList();

        if (holdingsWithCoinGeckoId.Count == 0)
        {
            return [];
        }

        var idsToQuery = holdingsWithCoinGeckoId.Select(x => x.CoinGeckoId!).Distinct().ToList();
        var marketResult = await coinGeckoClient.GetMarketSummaryAsync(idsToQuery, command.PeriodStart, command.PeriodEnd, cancellationToken);
        if (marketResult.IsError)
        {
            logger.LogWarning(
                "Could not fetch CoinGecko market summary for portfolio {PortfolioId}: {Error}",
                command.PortfolioId, marketResult.FirstError.Code);
            return [];
        }

        var summaries = marketResult.Value;
        return holdingsWithCoinGeckoId
            .Where(x => summaries.ContainsKey(x.CoinGeckoId!))
            .Select(x =>
            {
                var summary = summaries[x.CoinGeckoId!];
                return new PortfolioMarketRow(x.Holding.Symbol, x.CoinGeckoId!, summary.ChangePercentage, summary.High, summary.Low);
            })
            .ToList();
    }
}
