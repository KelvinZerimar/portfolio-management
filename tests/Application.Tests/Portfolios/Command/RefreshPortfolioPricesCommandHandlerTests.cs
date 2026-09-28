using Application.Common.Security;
using Application.Common.UnitOfWork;
using Application.CryptoCurrencies.Interfaces;
using Application.Portfolios.Command;
using Application.Portfolios.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Portfolios.Command;

public class RefreshPortfolioPricesCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly ICoinGeckoClient _coinGeckoClient = Substitute.For<ICoinGeckoClient>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;

    private RefreshPortfolioPricesCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<RefreshPortfolioPricesCommandHandler>>(),
        _portfolioRepository,
        _portfolioEntryRepository,
        _coinGeckoClient,
        _unitOfWork,
        _currentUserProvider);

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
        var entry = CreateHolding(1, 1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: "bitcoin", DateTime.UtcNow.AddDays(-1));
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
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithHoldingWithoutCoinGeckoId_SkipsItWithoutCallingCoinGecko()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var entry = CreateHolding(1, 1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: null, DateTime.UtcNow);
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
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCoinGeckoDoesNotReturnPriceForId_SkipsThatHoldingButProcessesOthers()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var missingPriceEntry = CreateHolding(1, 1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: "bitcoin", DateTime.UtcNow);
        var pricedEntry = CreateHolding(2, 1, quantity: 5m, pricePerUnit: 10m, symbol: "ETH", coinGeckoId: "ethereum", DateTime.UtcNow);
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
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithSuccessfulRefresh_SetsLastPriceRefreshAtOnPortfolio()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        portfolio.LastPriceRefreshAt.Should().NotBeNull();
        portfolio.LastPriceRefreshAt!.Value.Date.Should().Be(DateTime.UtcNow.Date);
        _portfolioRepository.Received(1).Update(portfolio);
    }

    [Fact]
    public async Task Handle_WhenAlreadyRefreshedToday_ReturnsConflictWithoutCallingCoinGecko()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        portfolio.LastPriceRefreshAt = DateTime.UtcNow;
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.PricesAlreadyRefreshedToday");
        await _coinGeckoClient.DidNotReceive().GetEurPricesAsync(
            Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>());
        await _unitOfWork.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenLastRefreshWasOnAPreviousDay_AllowsRefreshingAgain()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        portfolio.LastPriceRefreshAt = DateTime.UtcNow.AddDays(-1);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new RefreshPortfolioPricesCommand(1), CancellationToken.None);

        result.IsError.Should().BeFalse();
    }
}
