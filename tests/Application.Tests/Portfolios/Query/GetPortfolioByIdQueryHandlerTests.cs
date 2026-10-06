using Application.Common.Security;
using Application.Portfolios.Interfaces;
using Application.Portfolios.Query;
using Domain.Entities;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Portfolios.Query;

public class GetPortfolioByIdQueryHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private GetPortfolioByIdQueryHandler CreateHandler() => new(_portfolioRepository, _currentUserProvider);

    public GetPortfolioByIdQueryHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithExistingPortfolio_ReturnsResponse()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", "A description");
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new GetPortfolioByIdQuery(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("My Portfolio");
        result.Value.Description.Should().Be("A description");
        result.Value.UserId.Should().Be(UserId);
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new GetPortfolioByIdQuery(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new GetPortfolioByIdQuery(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }
}
