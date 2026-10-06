using Application.Common.Security;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Portfolios.Query;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Portfolios.Query;

public class GetPortfolioHoldingsQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private GetPortfolioHoldingsQueryHandler CreateHandler() => new(
        _portfolioRepository,
        _portfolioEntryRepository,
        _currentUserProvider);

    public GetPortfolioHoldingsQueryHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithHoldings_ReturnsMappedItemsOrderedByValueDescending()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var date = new DateTime(2026, 1, 15);

        var btcEntry = PortfolioEntry.Create(portfolio.Id, 1, 1, quantity: 2m, pricePerUnit: 100m, date.AddDays(-1));
        btcEntry.CryptoCurrency = CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin", "btc.png");
        btcEntry.Exchange = Exchange.Create("Binance", "api-key");

        var ethEntry = PortfolioEntry.Create(portfolio.Id, 2, 1, quantity: 10m, pricePerUnit: 50m, date.AddDays(-1));
        ethEntry.CryptoCurrency = CryptoCurrency.Create("ETH", "Ethereum", "ethereum");
        ethEntry.Exchange = Exchange.Create("Binance", "api-key");

        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([btcEntry, ethEntry]);

        var result = await CreateHandler().Handle(new GetPortfolioHoldingsQuery(PortfolioId, date), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Date.Should().Be(date.Date);
        result.Value.Holdings.Should().HaveCount(2);
        result.Value.Holdings[0].CryptoCurrencySymbol.Should().Be("ETH");
        result.Value.Holdings[0].Value.Should().Be(500m);
        result.Value.Holdings[1].CryptoCurrencySymbol.Should().Be("BTC");
        result.Value.Holdings[1].Value.Should().Be(200m);
        result.Value.Holdings[1].ExchangeName.Should().Be("Binance");
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new GetPortfolioHoldingsQuery(PortfolioId, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new GetPortfolioHoldingsQuery(PortfolioId, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }
}
