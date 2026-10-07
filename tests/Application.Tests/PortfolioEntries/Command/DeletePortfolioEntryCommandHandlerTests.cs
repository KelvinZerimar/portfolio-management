using Application.Common.Security;
using Application.PortfolioEntries.Command;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.PortfolioEntries.Command;

public class DeletePortfolioEntryCommandHandlerTests
{
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long EntryId = 5L;
    private const long PortfolioId = 10L;

    private DeletePortfolioEntryCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<DeletePortfolioEntryCommandHandler>>(),
        _portfolioEntryRepository,
        _portfolioRepository,
        _currentUserProvider);

    public DeletePortfolioEntryCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithExistingEntry_RemovesEntryAndReturnsDeleted()
    {
        var entry = PortfolioEntry.Create(PortfolioId, 1, 1, quantity: 1m, pricePerUnit: 50m, DateTime.UtcNow);
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns(entry);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));

        var result = await CreateHandler().Handle(new DeletePortfolioEntryCommand(EntryId), CancellationToken.None);

        result.IsError.Should().BeFalse();
        _portfolioEntryRepository.Received(1).RemoveRange(
            Arg.Is<List<PortfolioEntry>>(entries => entries.Count == 1 && entries[0] == entry));
    }

    [Fact]
    public async Task Handle_WithNonExistentEntry_ReturnsNotFoundWithoutCheckingPortfolio()
    {
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns((PortfolioEntry?)null);

        var result = await CreateHandler().Handle(new DeletePortfolioEntryCommand(EntryId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PortfolioEntry.NotFound");
        await _portfolioRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEntryOwnedByAnotherUser_ReturnsNotFoundWithoutRemoving()
    {
        var entry = PortfolioEntry.Create(PortfolioId, 1, 1, quantity: 1m, pricePerUnit: 50m, DateTime.UtcNow);
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns(entry);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty));

        var result = await CreateHandler().Handle(new DeletePortfolioEntryCommand(EntryId), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PortfolioEntry.NotFound");
        _portfolioEntryRepository.DidNotReceive().RemoveRange(Arg.Any<List<PortfolioEntry>>());
    }
}
