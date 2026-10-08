using Application.Common.Security;
using Application.Portfolios.Interfaces;
using Application.Reports.Command;
using Domain.Entities;
using ErrorOr;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Application.Tests.Reports.Command;

public class TriggerPortfolioStatusReportCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();
    private readonly ISender _sender = Substitute.For<ISender>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 2, 15, 12, 0, 0, TimeSpan.Zero));

    private const long UserId = 1L;

    private TriggerPortfolioStatusReportCommandHandler CreateHandler() => new(
        _portfolioRepository, _currentUserProvider, _sender, _timeProvider);

    public TriggerPortfolioStatusReportCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFoundWithoutDispatchingSendCommand()
    {
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new TriggerPortfolioStatusReportCommand(1, null, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
        await _sender.DidNotReceive().Send(Arg.Any<SendPortfolioStatusReportCommand>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        var portfolio = Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);

        var result = await CreateHandler().Handle(new TriggerPortfolioStatusReportCommand(1, null, null), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithoutExplicitPeriod_DispatchesSendCommandForThePreviousCalendarMonth()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _sender.Send(Arg.Any<SendPortfolioStatusReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success);

        var result = await CreateHandler().Handle(new TriggerPortfolioStatusReportCommand(1, null, null), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _sender.Received(1).Send(
            Arg.Is<SendPortfolioStatusReportCommand>(c =>
                c.PortfolioId == portfolio.Id &&
                c.PeriodStart == new DateTime(2026, 1, 1) &&
                c.PeriodEnd == new DateTime(2026, 1, 31)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithExplicitPeriod_DispatchesSendCommandForThatPeriodInstead()
    {
        var portfolio = Portfolio.Create(UserId, "My Portfolio", string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _sender.Send(Arg.Any<SendPortfolioStatusReportCommand>(), Arg.Any<CancellationToken>())
            .Returns(Result.Success);
        var customStart = new DateTime(2026, 2, 1);
        var customEnd = new DateTime(2026, 2, 15);

        await CreateHandler().Handle(new TriggerPortfolioStatusReportCommand(1, customStart, customEnd), CancellationToken.None);

        await _sender.Received(1).Send(
            Arg.Is<SendPortfolioStatusReportCommand>(c => c.PeriodStart == customStart && c.PeriodEnd == customEnd),
            Arg.Any<CancellationToken>());
    }
}
