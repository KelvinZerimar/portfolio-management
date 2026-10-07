using Application.Common.Security;
using Application.CryptoCurrencies.Interfaces;
using Application.Portfolios.Command;
using Application.Portfolios.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Application.Tests.Portfolios.Command;

public class RefreshPortfolioPricesCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly ICoinGeckoClient _coinGeckoClient = Substitute.For<ICoinGeckoClient>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    // Fixed at noon UTC on an arbitrary day, far from the Europe/Madrid local-midnight boundary,
    // so tests that don't care about the boundary itself stay fully deterministic.
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero));

    private const long UserId = 1L;

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    private RefreshPortfolioPricesCommandHandler CreateHandler(TimeProvider? timeProvider = null) => new(
        Substitute.For<ILogger<RefreshPortfolioPricesCommandHandler>>(),
        _portfolioRepository,
        _portfolioEntryRepository,
        _coinGeckoClient,
        _currentUserProvider,
        timeProvider ?? _timeProvider);

    private static PortfolioEntry CreateHolding(
        long cryptoCurrencyId, long exchangeId, decimal quantity, decimal pricePerUnit,
        string symbol, string? coinGeckoId, DateTime recordedAt)
    {
        var entry = PortfolioEntry.Create(0, cryptoCurrencyId, exchangeId, quantity, pricePerUnit, recordedAt);
        entry.CryptoCurrency = CryptoCurrency.Create(symbol, symbol, coinGeckoId);
        return entry;
    }

    public RefreshPortfolioPricesCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
        _coinGeckoClient.GetEurPricesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal>());
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithHoldingMappedToCoinGecko_CreatesNewEntryWithRefreshedPriceAndSameQuantity()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var entry = CreateHolding(1, 1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: "bitcoin", Now.AddDays(-1));
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([entry]);
        _coinGeckoClient.GetEurPricesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["bitcoin"] = 150m });

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Updated.Should().ContainSingle();
        var updated = result.Value.Updated.Single();
        updated.CryptoCurrencyId.Should().Be(1);
        updated.PreviousPrice.Should().Be(100m);
        updated.NewPrice.Should().Be(150m);
        result.Value.Skipped.Should().BeEmpty();

        await _portfolioEntryRepository.Received(1).AddRangeAsync(
            Arg.Is<IEnumerable<PortfolioEntry>>(e => e.Count() == 1
                && e.Single().Quantity == 2m
                && e.Single().PricePerUnit == 150m
                && e.Single().CryptoCurrencyId == 1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithHoldingWithoutCoinGeckoId_SkipsItWithoutCallingCoinGecko()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var entry = CreateHolding(1, 1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: null, Now);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([entry]);
        _coinGeckoClient.GetEurPricesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal>());

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Updated.Should().BeEmpty();
        result.Value.Skipped.Should().ContainSingle()
            .Which.Reason.Should().Be("NoCoinGeckoIdMapped");
        await _coinGeckoClient.Received(1).GetEurPricesAsync(
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCoinGeckoDoesNotReturnPriceForId_SkipsThatHoldingButProcessesOthers()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var missingPriceEntry = CreateHolding(1, 1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: "bitcoin", Now);
        var pricedEntry = CreateHolding(2, 1, quantity: 5m, pricePerUnit: 10m, symbol: "ETH", coinGeckoId: "ethereum", Now);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([missingPriceEntry, pricedEntry]);
        _coinGeckoClient.GetEurPricesAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, decimal> { ["ethereum"] = 20m });

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.Value.Updated.Should().ContainSingle().Which.CryptoCurrencyId.Should().Be(2);
        result.Value.Skipped.Should().ContainSingle();
        var skipped = result.Value.Skipped.Single();
        skipped.CryptoCurrencyId.Should().Be(1);
        skipped.Reason.Should().Be("PriceNotReturnedByCoinGecko");
    }

    [Fact]
    public async Task Handle_WithNoHoldings_ReturnsEmptyResponseWithoutCallingCoinGeckoOrSaving()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.Value.Updated.Should().BeEmpty();
        result.Value.Skipped.Should().BeEmpty();
        await _coinGeckoClient.Received(1).GetEurPricesAsync(
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Count == 0), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSuccessfulRefresh_SetsLastPriceRefreshAtOnPortfolio()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        portfolio.LastPriceRefreshAt.Should().Be(Now);
        _portfolioRepository.Received(1).Update(portfolio);
    }

    [Fact]
    public async Task Handle_WhenAlreadyRefreshedToday_ReturnsConflictWithoutCallingCoinGecko()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        portfolio.LastPriceRefreshAt = Now;
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.PricesAlreadyRefreshedToday");
        await _coinGeckoClient.DidNotReceive().GetEurPricesAsync(
            Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLastRefreshWasOnAPreviousDay_AllowsRefreshingAgain()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        portfolio.LastPriceRefreshAt = Now.AddDays(-1);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }

    // Both instants below fall on the same UTC calendar day (Dec 31st), so a UTC-day comparison
    // would wrongly treat them as "already refreshed today". Europe/Madrid is UTC+1 in winter,
    // so 22:00 and 22:30 UTC both land on Dec 31st local time too - this is the non-boundary case.
    [Fact]
    public async Task Handle_WhenLastRefreshWasEarlierTheSameMadridLocalDay_ReturnsConflict()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        portfolio.LastPriceRefreshAt = new DateTime(2025, 12, 31, 22, 0, 0, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 12, 31, 22, 30, 0, TimeSpan.Zero));
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler(timeProvider).Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.PricesAlreadyRefreshedToday");
    }

    // 22:59:59 UTC and 23:00:01 UTC are both Dec 31st in UTC (2 seconds apart), but Europe/Madrid
    // is UTC+1 in winter, so locally they land on Dec 31st 23:59:59 and Jan 1st 00:00:01 -
    // different calendar days in Madrid. If the gate compared UTC dates instead of Madrid-local
    // dates, this would incorrectly report a conflict.
    [Fact]
    public async Task Handle_WhenLastRefreshWasJustBeforeMadridMidnightAndNowIsJustAfter_AllowsRefreshingAgain()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        portfolio.LastPriceRefreshAt = new DateTime(2025, 12, 31, 22, 59, 59, DateTimeKind.Utc);
        var timeProvider = new FakeTimeProvider(new DateTimeOffset(2025, 12, 31, 23, 0, 1, TimeSpan.Zero));
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler(timeProvider).Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }
}
