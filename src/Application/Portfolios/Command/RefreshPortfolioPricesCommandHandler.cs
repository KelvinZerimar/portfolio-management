using Application.Common.Security;
using Application.Common.UnitOfWork;
using Application.CryptoCurrencies.Interfaces;
using Application.Portfolios.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Contracts.Portfolios;
using Domain.Entities;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Application.Portfolios.Command;

public sealed record RefreshPortfolioPricesCommand(long Id) : IRequest<ErrorOr<RefreshPortfolioPricesResponse>>;

public sealed class RefreshPortfolioPricesCommandHandler(
    ILogger<RefreshPortfolioPricesCommandHandler> logger,
    IPortfolioRepository portfolioRepository,
    IPortfolioEntryRepository portfolioEntryRepository,
    ICoinGeckoClient coinGeckoClient,
    IUnitOfWork unitOfWork,
    ICurrentUserProvider currentUserProvider
    ) : IRequestHandler<RefreshPortfolioPricesCommand, ErrorOr<RefreshPortfolioPricesResponse>>
{
    public async Task<ErrorOr<RefreshPortfolioPricesResponse>> Handle(RefreshPortfolioPricesCommand command, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(command.Id, cancellationToken);
        if (portfolio is null || portfolio.UserId != currentUserProvider.UserId)
        {
            return Error.NotFound("Portfolio.NotFound", $"Portfolio with ID '{command.Id}' was not found.");
        }

        var now = DateTime.UtcNow;
        if (portfolio.LastPriceRefreshAt?.Date == now.Date)
        {
            return Error.Conflict("Portfolio.PricesAlreadyRefreshedToday", "Los precios de este portfolio ya se actualizaron hoy. Inténtalo de nuevo mañana.");
        }

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
                holding.Quantity, newPrice, now));

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
        await unitOfWork.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Refreshed {Count} holdings for portfolio {PortfolioId}", updated.Count, portfolio.Id);

        return new RefreshPortfolioPricesResponse(portfolio.Id, now, updated, skipped);
    }
}
