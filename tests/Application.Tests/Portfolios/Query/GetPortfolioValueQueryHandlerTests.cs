using Application.Common.Security;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Portfolios.Query;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Portfolios.Query;

public class GetPortfolioValueQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private GetPortfolioValueQueryHandler CreateHandler() => new(
        _portfolioRepository,
        _portfolioEntryRepository,
        _currentUserProvider);

    public GetPortfolioValueQueryHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithExplicitDate_ReturnsValueAsOfThatDate()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        var date = new DateTime(2026, 1, 15);
        var entry = PortfolioEntry.Create(portfolio.Id, 1, 1, quantity: 2m, pricePerUnit: 100m, date.AddDays(-1));
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([entry]);

        var result = await CreateHandler().Handle(new GetPortfolioValueQuery(PortfolioId, date), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.PortfolioId.Should().Be(portfolio.Id);
        result.Value.Date.Should().Be(date.Date);
        result.Value.Value.Should().Be(200m);
    }

    [Fact]
    public async Task Handle_WithoutDate_DefaultsToToday()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioEntryRepository.GetByPortfolioIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new GetPortfolioValueQuery(PortfolioId, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Date.Should().Be(DateTime.UtcNow.Date);
        result.Value.Value.Should().Be(0m);
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new GetPortfolioValueQuery(PortfolioId, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new GetPortfolioValueQuery(PortfolioId, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }
}
