using Domain.Entities;
using FluentAssertions;

namespace Domain.Tests.Entities;

public class PortfolioTests
{
    private static PortfolioEntry CreateEntry(
        long portfolioId,
        long cryptoCurrencyId,
        long exchangeId,
        decimal quantity,
        decimal pricePerUnit,
        DateTime recordedAt,
        string symbol = "BTC")
    {
        var entry = PortfolioEntry.Create(portfolioId, cryptoCurrencyId, exchangeId, quantity, pricePerUnit, recordedAt);
        entry.CryptoCurrency = CryptoCurrency.Create(symbol, symbol);
        return entry;
    }

    [Fact]
    public void Create_WithValidParameters_ReturnsPortfolioWithExpectedProperties()
    {
        var userId = 42L;
        var name = "My Portfolio";
        var description = "Long term holdings";

        var before = DateTime.UtcNow;
        var portfolio = Portfolio.Create(userId, name, description);
        var after = DateTime.UtcNow;

        portfolio.UserId.Should().Be(userId);
        portfolio.Name.Should().Be(name);
        portfolio.Description.Should().Be(description);
        portfolio.UpdatedAt.Should().BeNull();
        portfolio.Entries.Should().BeEmpty();
        portfolio.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void GetTotalValue_WithEntriesBelongingToPortfolio_ReturnsSumOfQuantityTimesPrice()
    {
        var portfolio = new Portfolio();
        var entries = new List<PortfolioEntry>
        {
            CreateEntry(portfolio.Id, 1, 1, quantity: 2m, pricePerUnit: 100m, recordedAt: DateTime.UtcNow),
            CreateEntry(portfolio.Id, 2, 1, quantity: 3m, pricePerUnit: 50m, recordedAt: DateTime.UtcNow),
        };

        var total = portfolio.GetTotalValue(entries);

        total.Should().Be(2m * 100m + 3m * 50m);
    }

    [Fact]
    public void GetTotalValue_WithEntriesFromOtherPortfolios_ExcludesThem()
    {
        var portfolio = new Portfolio();
        var entries = new List<PortfolioEntry>
        {
            CreateEntry(portfolio.Id, 1, 1, 2m, 100m, DateTime.UtcNow),
            CreateEntry(999, 2, 1, 3m, 50m, DateTime.UtcNow),
        };

        var total = portfolio.GetTotalValue(entries);

        total.Should().Be(200m);
    }

    [Fact]
    public void GetTotalValue_WithNoEntries_ReturnsZero()
    {
        var portfolio = new Portfolio();

        var total = portfolio.GetTotalValue(new List<PortfolioEntry>());

        total.Should().Be(0m);
    }

    [Fact]
    public void GetPortfolioValueByDate_ReturnsValueOfLatestHoldingsOnOrBeforeDate()
    {
        var portfolio = new Portfolio();
        var targetDate = new DateTime(2026, 1, 10);
        var entries = new List<PortfolioEntry>
        {
            CreateEntry(portfolio.Id, 1, 1, 1m, 100m, new DateTime(2026, 1, 1)),
            CreateEntry(portfolio.Id, 1, 1, 2m, 120m, new DateTime(2026, 1, 5)),
            CreateEntry(portfolio.Id, 1, 1, 5m, 200m, new DateTime(2026, 1, 15)),
        };

        var value = portfolio.GetPortfolioValueByDate(entries, targetDate);

        value.Should().Be(2m * 120m);
    }

    [Fact]
    public void GetCryptoCurrencyHistory_ReturnsMatchingEntriesOrderedByRecordedAtDescending()
    {
        var portfolio = new Portfolio();
        var oldest = CreateEntry(portfolio.Id, 1, 1, 1m, 100m, new DateTime(2026, 1, 1));
        var newest = CreateEntry(portfolio.Id, 1, 1, 2m, 110m, new DateTime(2026, 1, 10));
        var otherCrypto = CreateEntry(portfolio.Id, 2, 1, 3m, 90m, new DateTime(2026, 1, 5));
        var otherPortfolio = CreateEntry(999, 1, 1, 4m, 80m, new DateTime(2026, 1, 8));
        var entries = new List<PortfolioEntry> { oldest, newest, otherCrypto, otherPortfolio };

        var history = portfolio.GetCryptoCurrencyHistory(entries, cryptoCurrencyId: 1);

        history.Should().Equal(newest, oldest);
    }

    [Fact]
    public void GetHoldingsAsOf_ReturnsLatestEntryPerCryptoCurrencyAndExchangePair()
    {
        var portfolio = new Portfolio();
        var date = new DateTime(2026, 1, 10);
        var btcBinanceOld = CreateEntry(portfolio.Id, 1, 1, 1m, 100m, new DateTime(2026, 1, 1));
        var btcBinanceNew = CreateEntry(portfolio.Id, 1, 1, 2m, 110m, new DateTime(2026, 1, 5));
        var btcCoinbase = CreateEntry(portfolio.Id, 1, 2, 3m, 105m, new DateTime(2026, 1, 3));
        var ethBinance = CreateEntry(portfolio.Id, 2, 1, 4m, 200m, new DateTime(2026, 1, 4));
        var entries = new List<PortfolioEntry> { btcBinanceOld, btcBinanceNew, btcCoinbase, ethBinance };

        var holdings = portfolio.GetHoldingsAsOf(entries, date);

        holdings.Should().BeEquivalentTo(new[] { btcBinanceNew, btcCoinbase, ethBinance });
    }

    [Fact]
    public void GetHoldingsAsOf_ExcludesEntriesRecordedAfterDate()
    {
        var portfolio = new Portfolio();
        var date = new DateTime(2026, 1, 5);
        var beforeDate = CreateEntry(portfolio.Id, 1, 1, 1m, 100m, new DateTime(2026, 1, 1));
        var afterDate = CreateEntry(portfolio.Id, 1, 1, 2m, 150m, new DateTime(2026, 1, 10));
        var entries = new List<PortfolioEntry> { beforeDate, afterDate };

        var holdings = portfolio.GetHoldingsAsOf(entries, date);

        holdings.Should().ContainSingle().Which.Should().Be(beforeDate);
    }

    [Fact]
    public void GetHoldingsAsOf_WithEntriesFromOtherPortfolios_ExcludesThem()
    {
        var portfolio = new Portfolio();
        var date = new DateTime(2026, 1, 10);
        var ownEntry = CreateEntry(portfolio.Id, 1, 1, 1m, 100m, new DateTime(2026, 1, 1));
        var otherPortfolioEntry = CreateEntry(999, 1, 1, 2m, 150m, new DateTime(2026, 1, 2));
        var entries = new List<PortfolioEntry> { ownEntry, otherPortfolioEntry };

        var holdings = portfolio.GetHoldingsAsOf(entries, date);

        holdings.Should().ContainSingle().Which.Should().Be(ownEntry);
    }

    [Fact]
    public void GetValueHistory_ReturnsOnePointPerDistinctRecordedDateOrderedAscending()
    {
        var portfolio = new Portfolio();
        var entries = new List<PortfolioEntry>
        {
            CreateEntry(portfolio.Id, 1, 1, 1m, 100m, new DateTime(2026, 1, 1)),
            CreateEntry(portfolio.Id, 2, 1, 2m, 50m, new DateTime(2026, 1, 1)),
            CreateEntry(portfolio.Id, 1, 1, 3m, 120m, new DateTime(2026, 1, 5)),
        };

        var history = portfolio.GetValueHistory(entries);

        history.Should().HaveCount(2);
        history[0].Date.Should().Be(new DateTime(2026, 1, 1));
        history[0].Value.Should().Be(1m * 100m + 2m * 50m);
        history[1].Date.Should().Be(new DateTime(2026, 1, 5));
        history[1].Value.Should().Be(3m * 120m + 2m * 50m);
    }

    [Fact]
    public void GetValueHistory_WithNoEntries_ReturnsEmptyList()
    {
        var portfolio = new Portfolio();

        var history = portfolio.GetValueHistory(new List<PortfolioEntry>());

        history.Should().BeEmpty();
    }

    [Fact]
    public void GetValueHistoryByAsset_ReturnsSeriesPerAssetOrderedBySymbolWithZeroBeforeFirstEntry()
    {
        var portfolio = new Portfolio();
        var jan1 = new DateTime(2026, 1, 1);
        var jan5 = new DateTime(2026, 1, 5);
        var entries = new List<PortfolioEntry>
        {
            CreateEntry(portfolio.Id, 1, 1, 1m, 100m, jan1, symbol: "ETH"),
            CreateEntry(portfolio.Id, 2, 1, 2m, 50m, jan1, symbol: "BTC"),
            CreateEntry(portfolio.Id, 1, 1, 3m, 120m, jan5, symbol: "ETH"),
            CreateEntry(portfolio.Id, 3, 1, 5m, 10m, jan5, symbol: "SOL"),
        };

        var series = portfolio.GetValueHistoryByAsset(entries);

        series.Select(s => s.Symbol).Should().Equal("BTC", "ETH", "SOL");

        var btc = series.Single(s => s.Symbol == "BTC");
        btc.CryptoCurrencyId.Should().Be(2);
        btc.Points.Should().Equal(
            new PortfolioValuationPoint(jan1, 100m),
            new PortfolioValuationPoint(jan5, 100m));

        var eth = series.Single(s => s.Symbol == "ETH");
        eth.CryptoCurrencyId.Should().Be(1);
        eth.Points.Should().Equal(
            new PortfolioValuationPoint(jan1, 100m),
            new PortfolioValuationPoint(jan5, 360m));

        var sol = series.Single(s => s.Symbol == "SOL");
        sol.CryptoCurrencyId.Should().Be(3);
        sol.Points.Should().Equal(
            new PortfolioValuationPoint(jan1, 0m),
            new PortfolioValuationPoint(jan5, 50m));
    }
}
