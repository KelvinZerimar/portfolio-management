using Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace Domain.Entities;

public sealed class Portfolio : Entity
{
    public long UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastPriceRefreshAt { get; set; }
    public DateTime? LastStatusReportSentAt { get; set; }

    // Property navigation for related PortfolioEntry entities
    public List<PortfolioEntry> Entries { get; set; } = new List<PortfolioEntry>();


    public Portfolio() { }
    public static Portfolio Create(long userId, string name, string description)
    {
        return new Portfolio
        {
            UserId = userId,
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
    }
    public decimal GetTotalValue(List<PortfolioEntry> entries)
    {
        return entries
            .Where(e => e.PortfolioId == Id)
            .Sum(e => e.Quantity * e.PricePerUnit);
    }

    public decimal GetPortfolioValueByDate(List<PortfolioEntry> entries, DateTime date)
    {
        return GetHoldingsAsOf(entries, date).Sum(e => e.Quantity * e.PricePerUnit);
    }

    public List<PortfolioEntry> GetCryptoCurrencyHistory(List<PortfolioEntry> entries, long cryptoCurrencyId)
    {
        return entries
            .Where(e => e.PortfolioId == Id && e.CryptoCurrencyId == cryptoCurrencyId)
            .OrderByDescending(e => e.RecordedAt)
            .ToList();
    }

    /// <summary>
    /// Summarizes how the portfolio's value moved over a period, for status-report purposes:
    /// value at each end of the period (per <see cref="GetPortfolioValueByDate"/>) plus a
    /// per-asset breakdown of the closing holdings.
    /// </summary>
    public PortfolioStatusReport GetStatusReport(List<PortfolioEntry> entries, DateTime periodStart, DateTime periodEnd)
    {
        var valueAtStart = GetPortfolioValueByDate(entries, periodStart);
        var valueAtEnd = GetPortfolioValueByDate(entries, periodEnd);
        var changeAmount = valueAtEnd - valueAtStart;
        var changePercentage = valueAtStart == 0m ? (decimal?)null : Math.Round(changeAmount / valueAtStart * 100m, 2);

        var holdings = GetHoldingsAsOf(entries, periodEnd)
            .Select(e => new PortfolioHoldingValue(e.CryptoCurrencyId, e.CryptoCurrency.Symbol, e.Quantity, e.Quantity * e.PricePerUnit))
            .OrderByDescending(h => h.Value)
            .ToList();

        return new PortfolioStatusReport(periodStart.Date, periodEnd.Date, valueAtStart, valueAtEnd, changeAmount, changePercentage, holdings);
    }

    /// <summary>
    /// The current snapshot as of a date: one entry per (CryptoCurrency, Exchange) pair,
    /// the latest recorded one on or before the date. Backs both the holdings table and allocation views.
    /// </summary>
    public List<PortfolioEntry> GetHoldingsAsOf(List<PortfolioEntry> entries, DateTime date)
    {
        return entries
            .Where(e => e.PortfolioId == Id && e.RecordedAt.Date <= date.Date)
            .GroupBy(e => new { e.CryptoCurrencyId, e.ExchangeId })
            .Select(g => g.OrderByDescending(e => e.RecordedAt).First())
            .ToList();
    }

    public List<PortfolioValuationPoint> GetValueHistory(List<PortfolioEntry> entries)
    {
        var relevant = entries.Where(e => e.PortfolioId == Id).ToList();

        return relevant
            .Select(e => e.RecordedAt.Date)
            .Distinct()
            .OrderBy(date => date)
            .Select(date => new PortfolioValuationPoint(date, GetPortfolioValueByDate(relevant, date)))
            .ToList();
    }

    /// <summary>
    /// One valuation series per cryptocurrency, sampled on the same dates as <see cref="GetValueHistory"/>
    /// so every asset line shares the total line's x-axis (0 before the asset's first recorded entry).
    /// </summary>
    public List<PortfolioAssetValuationSeries> GetValueHistoryByAsset(List<PortfolioEntry> entries)
    {
        var relevant = entries.Where(e => e.PortfolioId == Id).ToList();
        var dates = relevant.Select(e => e.RecordedAt.Date).Distinct().OrderBy(date => date).ToList();

        return relevant
            .GroupBy(e => new { e.CryptoCurrencyId, e.CryptoCurrency.Symbol })
            .Select(assetGroup =>
            {
                var assetEntries = assetGroup.ToList();
                var points = dates
                    .Select(date => new PortfolioValuationPoint(
                        date,
                        GetHoldingsAsOf(assetEntries, date).Sum(e => e.Quantity * e.PricePerUnit)))
                    .ToList();
                return new PortfolioAssetValuationSeries(assetGroup.Key.CryptoCurrencyId, assetGroup.Key.Symbol, points);
            })
            .OrderBy(series => series.Symbol)
            .ToList();
    }
}
