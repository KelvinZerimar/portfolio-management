using Application.Common.Security;
using Application.Portfolios.Command;
using Application.Portfolios.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Portfolios.Command;

public class DeletePortfolioCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private DeletePortfolioCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<DeletePortfolioCommandHandler>>(),
        _portfolioRepository,
        _currentUserProvider);

    public DeletePortfolioCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithExistingPortfolio_RemovesPortfolioAndReturnsDeleted()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new DeletePortfolioCommand(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _portfolioRepository.Received(1).RemoveRange(
            Arg.Is<List<Portfolio>>(portfolios => portfolios.Count == 1 && portfolios[0] == portfolio));
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new DeletePortfolioCommand(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFoundWithoutRemoving()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new DeletePortfolioCommand(PortfolioId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
        _portfolioRepository.DidNotReceive().RemoveRange(Arg.Any<List<Portfolio>>());
    }
}
