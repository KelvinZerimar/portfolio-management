using System.Reflection;
using Application.Common.Security;
using Application.Portfolios.Command;
using Application.Portfolios.Interfaces;
using Contracts.Portfolios;
using Domain.Common;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Portfolios.Command;

public class UpdatePortfolioCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    // Id is database-generated (private init via Entity), so two in-memory Portfolio.Create() instances
    // both default to Id 0. This mirrors how EF Core itself materializes private-init keys.
    private static void SetId(Entity entity, long id) =>
        typeof(Entity).GetProperty(nameof(Entity.Id), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(entity, id);

    private const long UserId = 1L;
    private const long PortfolioId = 10L;

    private UpdatePortfolioCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<UpdatePortfolioCommandHandler>>(),
        _portfolioRepository,
        _currentUserProvider);

    private static UpdatePortfolioRequest CreateRequest(string name = "Renamed Portfolio") => new()
    {
        Name = name,
        Description = "Updated description"
    };

    public UpdatePortfolioCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesPortfolioAndReturnsResponse()
    {
        var portfolio = Portfolio.Create(UserId, "Old Name", "Old description");
        var request = CreateRequest();
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioRepository.GetPortfolioByNameAsync(UserId, request.Name, Arg.Any<CancellationToken>())
            .Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new UpdatePortfolioCommand(PortfolioId, request), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(request.Name);
        result.Value.Description.Should().Be(request.Description);
        portfolio.UpdatedAt.Should().NotBeNull();
        _portfolioRepository.Received(1).Update(portfolio);
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new UpdatePortfolioCommand(PortfolioId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new UpdatePortfolioCommand(PortfolioId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithNameClashingAnotherPortfolio_ReturnsConflictWithoutUpdating()
    {
        var portfolio = Portfolio.Create(UserId, "Old Name", "Old description");
        SetId(portfolio, 1);
        var request = CreateRequest();
        var otherPortfolio = Portfolio.Create(UserId, request.Name, "unrelated");
        SetId(otherPortfolio, 2);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioRepository.GetPortfolioByNameAsync(UserId, request.Name, Arg.Any<CancellationToken>())
            .Returns(otherPortfolio);

        var result = await CreateHandler().Handle(new UpdatePortfolioCommand(PortfolioId, request), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.AlreadyExists");
        _portfolioRepository.DidNotReceive().Update(Arg.Any<Portfolio>());
    }

    [Fact]
    public async Task Handle_WhenDuplicateNameBelongsToSamePortfolio_UpdatesSuccessfully()
    {
        var portfolio = Portfolio.Create(UserId, "Same Name", "Old description");
        var request = CreateRequest(name: "Same Name");
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns(portfolio);
        _portfolioRepository.GetPortfolioByNameAsync(UserId, request.Name, Arg.Any<CancellationToken>())
            .Returns(portfolio);

        var result = await CreateHandler().Handle(new UpdatePortfolioCommand(PortfolioId, request), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _portfolioRepository.Received(1).Update(portfolio);
    }
}
