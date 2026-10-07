using Application.Common.Security;
using Application.CryptoCurrencies.Interfaces;
using Application.Exchanges.Interfaces;
using Application.PortfolioEntries.Command;
using Application.PortfolioEntries.Interfaces;
using Application.Portfolios.Interfaces;
using Contracts.PortfolioEntries;
using Domain.Entities;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Application.Tests.PortfolioEntries.Command;

public class CreatePortfolioEntryCommandHandlerTests
{
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICryptoCurrencyRepository _cryptoCurrencyRepository = Substitute.For<ICryptoCurrencyRepository>();
    private readonly IExchangeRepository _exchangeRepository = Substitute.For<IExchangeRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long PortfolioId = 10L;
    private const long CryptoCurrencyId = 20L;
    private const long ExchangeId = 30L;

    private CreatePortfolioEntryCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<CreatePortfolioEntryCommandHandler>>(),
        _portfolioEntryRepository,
        _portfolioRepository,
        _cryptoCurrencyRepository,
        _exchangeRepository,
        _currentUserProvider);

    private static CreatePortfolioEntryRequest CreateRequest() => new()
    {
        PortfolioId = PortfolioId,
        CryptoCurrencyId = CryptoCurrencyId,
        ExchangeId = ExchangeId,
        Quantity = 2m,
        PricePerUnit = 100m,
        RecordedAt = DateTime.UtcNow
    };

    public CreatePortfolioEntryCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithValidData_CreatesEntryAndReturnsResponse()
    {
        var request = CreateRequest();
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));
        _cryptoCurrencyRepository.GetByIdAsync(CryptoCurrencyId, Arg.Any<CancellationToken>())
            .Returns(CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin"));
        _exchangeRepository.GetByIdAsync(ExchangeId, Arg.Any<CancellationToken>())
            .Returns(Exchange.Create("Binance", "api-key"));

        var result = await CreateHandler().Handle(new CreatePortfolioEntryCommand(request), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.PortfolioId.Should().Be(PortfolioId);
        result.Value.CryptoCurrencyId.Should().Be(CryptoCurrencyId);
        result.Value.ExchangeId.Should().Be(ExchangeId);
        await _portfolioEntryRepository.Received(1).AddAsync(
            Arg.Is<PortfolioEntry>(e => e.PortfolioId == PortfolioId
                && e.CryptoCurrencyId == CryptoCurrencyId
                && e.ExchangeId == ExchangeId
                && e.Quantity == 2m),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentPortfolio_ReturnsNotFoundWithoutCheckingCryptoCurrencyOrExchange()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>()).Returns((Portfolio?)null);

        var result = await CreateHandler().Handle(new CreatePortfolioEntryCommand(CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
        await _cryptoCurrencyRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
        await _exchangeRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPortfolioOwnedByAnotherUser_ReturnsNotFound()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty));

        var result = await CreateHandler().Handle(new CreatePortfolioEntryCommand(CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Portfolio.NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentCryptoCurrency_ReturnsNotFoundWithoutCheckingExchange()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));
        _cryptoCurrencyRepository.GetByIdAsync(CryptoCurrencyId, Arg.Any<CancellationToken>()).Returns((CryptoCurrency?)null);

        var result = await CreateHandler().Handle(new CreatePortfolioEntryCommand(CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CryptoCurrency.NotFound");
        await _exchangeRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentExchange_ReturnsNotFoundWithoutCreatingEntry()
    {
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));
        _cryptoCurrencyRepository.GetByIdAsync(CryptoCurrencyId, Arg.Any<CancellationToken>())
            .Returns(CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin"));
        _exchangeRepository.GetByIdAsync(ExchangeId, Arg.Any<CancellationToken>()).Returns((Exchange?)null);

        var result = await CreateHandler().Handle(new CreatePortfolioEntryCommand(CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Exchange.NotFound");
        await _portfolioEntryRepository.DidNotReceive().AddAsync(Arg.Any<PortfolioEntry>(), Arg.Any<CancellationToken>());
    }
}
