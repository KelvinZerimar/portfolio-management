using Application.Common.Security;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Portfolios.Query;
using Contracts.Portfolios;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Portfolios.Query;

public class GetPortfolioAllocationQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private GetPortfolioAllocationQueryHandler CreateHandler() => new(
        _portfolioRepository,
        _portfolioEntryRepository,
        _currentUserProvider);

    public GetPortfolioAllocationQueryHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    private (Portfolio Portfolio, PortfolioEntry BtcOnBinance, PortfolioEntry EthOnKraken) CreateHoldings()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var recordedAt = DateTime.UtcNow.AddDays(-1);

        var btcEntry = PortfolioEntry.Create(portfolio.Id, 1, 1, quantity: 2m, pricePerUnit: 100m, recordedAt);
        btcEntry.CryptoCurrency = CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin");
        btcEntry.Exchange = Exchange.Create("Binance", "api-key");

        var ethEntry = PortfolioEntry.Create(portfolio.Id, 2, 2, quantity: 10m, pricePerUnit: 20m, recordedAt);
        ethEntry.CryptoCurrency = CryptoCurrency.Create("ETH", "Ethereum", "ethereum");
        ethEntry.Exchange = Exchange.Create("Kraken", "api-key");

        return (portfolio, btcEntry, ethEntry);
    }

    [Fact]
    public async Task Handle_GroupedByAsset_ReturnsItemsWithCorrectPercentages()
    {
        var (portfolio, btcEntry, ethEntry) = CreateHoldings();
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([btcEntry, ethEntry]);

        var result = await CreateHandler().Handle(
            new GetPortfolioAllocationQuery(PortfolioId, PortfolioAllocationGroupBy.Asset, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.GroupBy.Should().Be(PortfolioAllocationGroupBy.Asset);
        result.Value.Items.Should().HaveCount(2);
        // BTC: 200, ETH: 200 -> total 400, 50% each
        result.Value.Items[0].Value.Should().Be(200m);
        result.Value.Items[0].Percentage.Should().Be(50m);
        result.Value.Items[1].Value.Should().Be(200m);
        result.Value.Items[1].Percentage.Should().Be(50m);
    }

    [Fact]
    public async Task Handle_GroupedByExchange_ReturnsItemsLabeledByExchangeName()
    {
        var (portfolio, btcEntry, ethEntry) = CreateHoldings();
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([btcEntry, ethEntry]);

        var result = await CreateHandler().Handle(
            new GetPortfolioAllocationQuery(PortfolioId, PortfolioAllocationGroupBy.Exchange, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().HaveCount(2);
        result.Value.Items.Should().Contain(i => i.Label == "Binance" && i.Value == 200m);
        result.Value.Items.Should().Contain(i => i.Label == "Kraken" && i.Value == 200m);
    }

    [Fact]
    public async Task Handle_WithNoHoldings_ReturnsEmptyItemsWithoutDivisionByZero()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(
            new GetPortfolioAllocationQuery(PortfolioId, PortfolioAllocationGroupBy.Asset, null),
            CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(
            new GetPortfolioAllocationQuery(PortfolioId, PortfolioAllocationGroupBy.Asset, null),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(
            new GetPortfolioAllocationQuery(PortfolioId, PortfolioAllocationGroupBy.Asset, null),
            CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }
}
