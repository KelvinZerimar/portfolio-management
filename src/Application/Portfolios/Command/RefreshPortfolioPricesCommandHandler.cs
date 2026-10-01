using Application.Common.Messaging;
using Application.Common.Security;
using Application.CryptoCurrencies.Interfaces;
using Application.Portfolios.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Contracts.Portfolios;
using Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Command;

public sealed record RefreshPortfolioPricesCommand(long Id) : IRequest<ErrorOr<RefreshPortfolioPricesResponse>>, ICommand;

public sealed class RefreshPortfolioPricesCommandHandler(
    ILogger<RefreshPortfolioPricesCommandHandler> logger,
    IPortfolioRepository portfolioRepository,
    IPortfolioEntryRepository portfolioEntryRepository,
    ICoinGeckoClient coinGeckoClient,
    ICurrentUserProvider currentUserProvider
    ) : IRequestHandler<RefreshPortfolioPricesCommand, ErrorOr<RefreshPortfolioPricesResponse>>
{
    // The app targets Spain-based users (EUR/es-ES throughout); "today" for the once-a-day
    // refresh gate and for the recorded snapshot date must follow their local calendar day,
    // not the UTC one, otherwise a refresh run close to local midnight lands on the wrong date.
    private static readonly TimeZoneInfo SpainTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");

    public async Task<ErrorOr<RefreshPortfolioPricesResponse>> Handle(RefreshPortfolioPricesCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(command.Id, cancellationToken);
        if (portfolio is null || portfolio.UserId != currentUserProvider.UserId)
        {
            return Error.NotFound("Portfolio.NotFound", $"Portfolio with ID '{command.Id}' was not found.");
        }

        var now = DateTime.UtcNow;
        var localToday = TimeZoneInfo.ConvertTimeFromUtc(now, SpainTimeZone).Date;
        if (portfolio.LastPriceRefreshAt.HasValue &&
            TimeZoneInfo.ConvertTimeFromUtc(portfolio.LastPriceRefreshAt.Value, SpainTimeZone).Date == localToday)
        {
            return Error.Conflict("Portfolio.PricesAlreadyRefreshedToday", "Los precios de este portfolio ya se actualizaron hoy. Inténtalo de nuevo mañana.");
        }

        // Recorded as UTC midnight of the local calendar day, matching how manually-entered
        // dates are stored, so the snapshot's date always matches the day the refresh actually
        // ran in Spain instead of drifting a day near local midnight.
        var recordedAt = DateTime.SpecifyKind(localToday, DateTimeKind.Utc);

        var entries = await portfolioEntryRepository.GetByPortfolioIdAsync(command.Id, cancellationToken);
        var holdings = portfolio.GetHoldingsAsOf(entries, now);

        var mappable = holdings.Where(h => !string.IsNullOrWhiteSpace(h.CryptoCurrency.CoinGeckoId)).ToList();
        var skipped = holdings
            .Where(h => string.IsNullOrWhiteSpace(h.CryptoCurrency.CoinGeckoId))
            .Select(h => new SkippedHoldingItem(h.CryptoCurrencyId, h.CryptoCurrency.Symbol, "NoCoinGeckoIdMapped"))
            .ToList();

        var coinGeckoIds = mappable.Select(h => h.CryptoCurrency.CoinGeckoId!).Distinct().ToList();
        var pricesResult = await coinGeckoClient.GetEurPricesAsync(coinGeckoIds, cancellationToken);
        if (pricesResult.IsError)
        {
            logger.LogWarning("Failed to refresh prices for portfolio {PortfolioId}: {Error}", portfolio.Id, pricesResult.FirstError.Code);
            return pricesResult.Errors;
        }

        var prices = pricesResult.Value;

        var updated = new List<RefreshedHoldingItem>();
        var newEntries = new List<PortfolioEntry>();
        foreach (var holding in mappable)
        {
            if (!prices.TryGetValue(holding.CryptoCurrency.CoinGeckoId!, out var newPrice))
            {
                skipped.Add(new SkippedHoldingItem(holding.CryptoCurrencyId, holding.CryptoCurrency.Symbol, "PriceNotReturnedByCoinGecko"));
                continue;
            }

            newEntries.Add(PortfolioEntry.Create(
                portfolio.Id, holding.CryptoCurrencyId, holding.ExchangeId,
                holding.Quantity, newPrice, recordedAt));

            updated.Add(new RefreshedHoldingItem(
                holding.CryptoCurrencyId, holding.CryptoCurrency.Symbol, holding.ExchangeId,
                holding.Quantity, holding.PricePerUnit, newPrice));
        }

        if (newEntries.Count > 0)
        {
            await portfolioEntryRepository.AddRangeAsync(newEntries, cancellationToken);
        }

        portfolio.LastPriceRefreshAt = now;
        portfolioRepository.Update(portfolio);

        logger.LogInformation("Refreshed {Count} holdings for portfolio {PortfolioId}", updated.Count, portfolio.Id);

        return new RefreshPortfolioPricesResponse(portfolio.Id, now, updated, skipped);
    }
}
