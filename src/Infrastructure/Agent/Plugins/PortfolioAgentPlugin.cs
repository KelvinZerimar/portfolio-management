using System.ComponentModel;
using Application.Common.Security;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Microsoft.SemanticKernel;

namespace Infrastructure.Agent.Plugins;

public sealed record PortfolioSummaryForAgent(long Id, string Name, string Description);

public sealed record PortfolioHoldingForAgent(string Symbol, decimal Quantity, decimal Value);

public sealed record AssetValuePointForAgent(string Symbol, DateTime Date, decimal Value);

/// <summary>
/// Kernel functions never take a userId parameter - that is exactly the injection vector the
/// zero-trust identity pattern avoids. Identity always comes from ICurrentUserProvider, which
/// resolves it from the authenticated JWT on the current request.
/// </summary>
public sealed class PortfolioAgentPlugin(
    IPortfolioRepository portfolioRepository,
    IPortfolioEntryRepository portfolioEntryRepository,
    ICurrentUserProvider currentUserProvider)
{
    [KernelFunction, Description("Lists the authenticated user's own portfolios (id, name, description).")]
    public async Task<IReadOnlyList<PortfolioSummaryForAgent>> ListMyPortfoliosAsync(
        CancellationToken cancellationToken = default)
    {
        var userId = currentUserProvider.UserId;

        var page = await portfolioRepository.GetAllAsync(
            page: 1,
            limit: 50,
            predicate: p => p.UserId == userId,
            cancellationToken);

        return page.Data
            .Select(p => new PortfolioSummaryForAgent(p.Id, p.Name, p.Description))
            .ToList();
    }

    [KernelFunction, Description("Gets the current total value (sum of ALL holdings combined) of one of the authenticated user's portfolios, by portfolio id. Use ONLY when the question has no specific asset symbol and no date - for a single asset or a historical date, use GetAssetPeakOrTroughDateAsync or GetPortfolioHoldingsAsync instead.")]
    public async Task<decimal?> GetPortfolioValueAsync(
        [Description("The portfolio id, as returned by ListMyPortfoliosAsync")] long portfolioId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetOwnedPortfolioOrNullAsync(portfolioId, cancellationToken);
        if (portfolio is null)
        {
            return null;
        }

        var entries = await portfolioEntryRepository.GetByPortfolioIdAsync(portfolioId, cancellationToken);
        return portfolio.GetTotalValue(entries);
    }

    [KernelFunction, Description("Gets the CURRENT (right now, today) per-asset breakdown (symbol, quantity, value) of one of the authenticated user's portfolios, ordered from highest to lowest value. Use this to compare assets against each other as of today (e.g. 'which asset is worth the most right now'). Do NOT use this if the question mentions a date or 'when'/'cuándo' - use GetAssetPeakOrTroughDateAsync for that instead.")]
    public async Task<IReadOnlyList<PortfolioHoldingForAgent>?> GetPortfolioHoldingsAsync(
        [Description("The portfolio id, as returned by ListMyPortfoliosAsync")] long portfolioId,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetOwnedPortfolioOrNullAsync(portfolioId, cancellationToken);
        if (portfolio is null)
        {
            return null;
        }

        var entries = await portfolioEntryRepository.GetByPortfolioIdAsync(portfolioId, cancellationToken);
        var holdings = portfolio.GetHoldingsAsOf(entries, DateTime.UtcNow);

        return holdings
            .Select(e => new PortfolioHoldingForAgent(e.CryptoCurrency.Symbol, e.Quantity, e.Quantity * e.PricePerUnit))
            .OrderByDescending(h => h.Value)
            .ToList();
    }

    [KernelFunction, Description("Finds the DATE when ONE specific named asset (by symbol) reached its historical highest or lowest value within one of the authenticated user's portfolios, optionally limited to a date range. Use this whenever the question names a specific asset AND mentions a date, 'when'/'cuándo', or a historical peak/trough (e.g. 'when was BTC worth the most'). Never use GetPortfolioValueAsync or GetPortfolioHoldingsAsync for these questions - those only cover the CURRENT value, not history.")]
    public async Task<AssetValuePointForAgent?> GetAssetPeakOrTroughDateAsync(
        [Description("The portfolio id, as returned by ListMyPortfoliosAsync")] long portfolioId,
        [Description("The asset symbol, e.g. BTC")] string symbol,
        [Description("'max' for the highest value, 'min' for the lowest value")] string extreme,
        [Description("Optional start date (inclusive, format yyyy-MM-dd) to limit the search")] string? from = null,
        [Description("Optional end date (inclusive, format yyyy-MM-dd) to limit the search")] string? to = null,
        CancellationToken cancellationToken = default)
    {
        var portfolio = await GetOwnedPortfolioOrNullAsync(portfolioId, cancellationToken);
        if (portfolio is null)
        {
            return null;
        }

        var entries = await portfolioEntryRepository.GetByPortfolioIdAsync(portfolioId, cancellationToken);
        var series = portfolio.GetValueHistoryByAsset(entries)
            .FirstOrDefault(s => string.Equals(s.Symbol, symbol, StringComparison.OrdinalIgnoreCase));

        var points = series?.Points.AsEnumerable() ?? [];
        if (DateTime.TryParse(from, out var fromDate))
        {
            points = points.Where(p => p.Date >= fromDate.Date);
        }

        if (DateTime.TryParse(to, out var toDate))
        {
            points = points.Where(p => p.Date <= toDate.Date);
        }

        var pointList = points.ToList();
        if (pointList.Count == 0)
        {
            return null;
        }

        // Anything other than an exact "min" is treated as "max" - the model only ever has two
        // real options here, and failing a whole tool call over a near-miss string like "maximum"
        // or "highest" would just cost another round trip for no benefit.
        var wantsMin = string.Equals(extreme.Trim(), "min", StringComparison.OrdinalIgnoreCase);
        var extremePoint = wantsMin ? pointList.MinBy(p => p.Value) : pointList.MaxBy(p => p.Value);

        return new AssetValuePointForAgent(series!.Symbol, extremePoint!.Date, extremePoint.Value);
    }

    // Ownership check: never trust an LLM-supplied id blindly, even though it only ever came
    // from this user's own ListMyPortfoliosAsync call in this conversation.
    private async Task<Domain.Entities.Portfolio?> GetOwnedPortfolioOrNullAsync(long portfolioId, CancellationToken cancellationToken)
    {
        var portfolio = await portfolioRepository.GetByIdAsync(portfolioId, cancellationToken);
        return portfolio is not null && portfolio.UserId == currentUserProvider.UserId ? portfolio : null;
    }
}
