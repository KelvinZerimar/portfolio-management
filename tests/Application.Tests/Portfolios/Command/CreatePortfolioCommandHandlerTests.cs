using Application.Common.Security;
using Application.Portfolios.Command;
using Application.Portfolios.Interfaces;
using Contracts.Portfolios;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.Portfolios.Command;

public class CreatePortfolioCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;

    private CreatePortfolioCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<CreatePortfolioCommandHandler>>(),
        _portfolioRepository,
        _currentUserProvider);

    private static CreatePortfolioRequest CreateRequest(string name = "My Portfolio") => new()
    {
        Name = name,
        Description = "A description"
    };

    public CreatePortfolioCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithNewName_CreatesPortfolioAndReturnsResponse()
    {
        var request = CreateRequest();
        _portfolioRepository.GetPortfolioByNameAsync(UserId, request.Name, Arg.Any<CancellationToken>())
            .Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new CreatePortfolioCommand(request), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(request.Name);
        await _portfolioRepository.Received(1).AddAsync(
            Arg.Is<Portfolio>(p => p.UserId == UserId && p.Name == request.Name && p.Description == request.Description),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithDuplicateName_ReturnsConflictWithoutCreatingPortfolio()
    {
        var request = CreateRequest();
        var existing = Portfolio.Create(UserId, request.Name, "existing");
        _portfolioRepository.GetPortfolioByNameAsync(UserId, request.Name, Arg.Any<CancellationToken>())
            .Returns(existing);

        var result = await CreateHandler().Handle(new CreatePortfolioCommand(request), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.AlreadyExists");
        await _portfolioRepository.DidNotReceive().AddAsync(Arg.Any<Portfolio>(), Arg.Any<CancellationToken>());
    }
}
