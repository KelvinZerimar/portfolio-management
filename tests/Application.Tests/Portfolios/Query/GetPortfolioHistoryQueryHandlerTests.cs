using Application.Common.Security;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Portfolios.Query;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Portfolios.Query;

public class GetPortfolioHistoryQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private GetPortfolioHistoryQueryHandler CreateHandler() => new(
        _portfolioRepository,
        _portfolioEntryRepository,
        _currentUserProvider);

    public GetPortfolioHistoryQueryHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithEntriesOnTwoDates_ReturnsTotalAndPerAssetHistory()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var day1 = new DateTime(2026, 1, 10);
        var day2 = new DateTime(2026, 1, 11);

        var entryDay1 = PortfolioEntry.Create(portfolio.Id, 1, 1, quantity: 2m, pricePerUnit: 100m, day1);
        entryDay1.CryptoCurrency = CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin");
        var entryDay2 = PortfolioEntry.Create(portfolio.Id, 1, 1, quantity: 3m, pricePerUnit: 100m, day2);
        entryDay2.CryptoCurrency = CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin");

        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([entryDay1, entryDay2]);

        var result = await CreateHandler().Handle(new GetPortfolioHistoryQuery(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Points.Should().HaveCount(2);
        result.Value.Points[0].Date.Should().Be(day1);
        result.Value.Points[0].Value.Should().Be(200m);
        result.Value.Points[1].Date.Should().Be(day2);
        result.Value.Points[1].Value.Should().Be(300m);

        result.Value.ByAsset.Should().ContainSingle();
        var btcSeries = result.Value.ByAsset.Single();
        btcSeries.Symbol.Should().Be("BTC");
        btcSeries.Points.Should().HaveCount(2);
        btcSeries.Points[1].Value.Should().Be(300m);
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new GetPortfolioHistoryQuery(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new GetPortfolioHistoryQuery(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }
}
