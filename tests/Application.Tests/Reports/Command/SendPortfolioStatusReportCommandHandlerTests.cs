using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Reports.Command;
using Application.Reports.Interfaces;
using Application.Users.Interfaces;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Application.Tests.Reports.Command;

public class SendPortfolioStatusReportCommandHandlerTests
{
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero));

    private static readonly DateTime PeriodStart = new(2026, 1, 1);
    private static readonly DateTime PeriodEnd = new(2026, 1, 31);

    private SendPortfolioStatusReportCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<SendPortfolioStatusReportCommandHandler>>(),
        _portfolioRepository,
        _portfolioEntryRepository,
        _userRepository,
        _emailSender,
        _timeProvider);

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFoundWithoutSendingEmail()
    {
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new SendPortfolioStatusReportCommand(1, PeriodStart, PeriodEnd), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNoMatchingUser_ReturnsNotFoundWithoutSendingEmail()
    {
        var portfolio = Portfolio.Create(userId: 1, name: "My Portfolio", description: string.Empty);
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns((User?)null);

        var result = await CreateHandler().Handle(new SendPortfolioStatusReportCommand(1, PeriodStart, PeriodEnd), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("User.NotFound");
        await _emailSender.DidNotReceive().SendAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithValidPortfolioAndUser_SendsEmailToUserAndStampsLastStatusReportSentAt()
    {
        var portfolio = Portfolio.Create(userId: 1, name: "My Portfolio", description: string.Empty);
        var user = User.Create("owner@example.com", "Jane", null, "hash");
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([]);

        var result = await CreateHandler().Handle(new SendPortfolioStatusReportCommand(1, PeriodStart, PeriodEnd), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _emailSender.Received(1).SendAsync(
            "owner@example.com", Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
        portfolio.LastStatusReportSentAt.Should().Be(_timeProvider.GetUtcNow().UtcDateTime);
        _portfolioRepository.Received(1).Update(portfolio);
    }
}
