using Application.CryptoCurrencies.Interfaces;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Application.Reports.Command;
using Application.Reports.Interfaces;
using Application.Users.Interfaces;
using Domain.Entities;
using ErrorOr;
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
    private readonly ICoinGeckoClient _coinGeckoClient = Substitute.For<ICoinGeckoClient>();
    private readonly IEmailSender _emailSender = Substitute.For<IEmailSender>();
    private readonly FakeTimeProvider _timeProvider = new(new DateTimeOffset(2026, 2, 1, 8, 0, 0, TimeSpan.Zero));

    private static readonly DateTime PeriodStart = new(2026, 1, 1);
    private static readonly DateTime PeriodEnd = new(2026, 1, 31);

    private SendPortfolioStatusReportCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<SendPortfolioStatusReportCommandHandler>>(),
        _portfolioRepository,
        _portfolioEntryRepository,
        _userRepository,
        _coinGeckoClient,
        _emailSender,
        _timeProvider);

    private static PortfolioEntry CreateEntry(
        long cryptoCurrencyId, decimal quantity, decimal pricePerUnit, string symbol, string? coinGeckoId, DateTime recordedAt)
    {
        // portfolioId 0 matches Portfolio.Create(...)'s default Id (never persisted in these
        // tests) - GetHoldingsAsOf/GetPortfolioValueByDate filter entries by portfolio.Id, so a
        // mismatched portfolioId here would silently filter the entry out entirely.
        var entry = PortfolioEntry.Create(0, cryptoCurrencyId, exchangeId: 1, quantity, pricePerUnit, recordedAt);
        entry.CryptoCurrency = CryptoCurrency.Create(symbol, symbol, coinGeckoId);
        return entry;
    }

    public SendPortfolioStatusReportCommandHandlerTests()
    {
        _coinGeckoClient.GetMarketSummaryAsync(
            Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CoinGeckoMarketSummary>());
    }

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

    [Fact]
    public async Task Handle_WithHoldingMappedToCoinGecko_IncludesMarketSectionInEmailBody()
    {
        var portfolio = Portfolio.Create(userId: 1, name: "My Portfolio", description: string.Empty);
        var user = User.Create("owner@example.com", "Jane", null, "hash");
        var entry = CreateEntry(1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: "bitcoin", PeriodStart.AddDays(1));
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([entry]);
        _coinGeckoClient.GetMarketSummaryAsync(
            Arg.Is<IReadOnlyCollection<string>>(ids => ids.Contains("bitcoin")), PeriodStart, PeriodEnd, Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, CoinGeckoMarketSummary>
            {
                ["bitcoin"] = new CoinGeckoMarketSummary("bitcoin", Open: 90m, Close: 100m, High: 110m, Low: 80m, ChangePercentage: 11.11m)
            });

        var result = await CreateHandler().Handle(new SendPortfolioStatusReportCommand(1, PeriodStart, PeriodEnd), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _emailSender.Received(1).SendAsync(
            "owner@example.com", Arg.Any<string>(),
            Arg.Is<string>(body => body.Contains("Mercado del mes anterior") && body.Contains("coingecko.com/en/coins/bitcoin")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenCoinGeckoMarketSummaryFails_StillSendsEmailWithoutMarketSection()
    {
        var portfolio = Portfolio.Create(userId: 1, name: "My Portfolio", description: string.Empty);
        var user = User.Create("owner@example.com", "Jane", null, "hash");
        var entry = CreateEntry(1, quantity: 2m, pricePerUnit: 100m, symbol: "BTC", coinGeckoId: "bitcoin", PeriodStart.AddDays(1));
        _portfolioRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(portfolio);
        _userRepository.GetByIdAsync(1, Arg.Any<CancellationToken>()).Returns(user);
        _portfolioEntryRepository.GetByPortfolioIdAsync(1, Arg.Any<CancellationToken>()).Returns([entry]);
        _coinGeckoClient.GetMarketSummaryAsync(
            Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(Error.Failure("CoinGecko.Unavailable", "down"));

        var result = await CreateHandler().Handle(new SendPortfolioStatusReportCommand(1, PeriodStart, PeriodEnd), CancellationToken.None);

        result.IsError.Should().BeFalse();
        await _emailSender.Received(1).SendAsync(
            "owner@example.com", Arg.Any<string>(),
            Arg.Is<string>(body => !body.Contains("Mercado del mes anterior")),
            Arg.Any<CancellationToken>());
    }
}
