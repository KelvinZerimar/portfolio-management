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

public class UpdatePortfolioEntryCommandHandlerTests
{
    private readonly IPortfolioEntryRepository _portfolioEntryRepository = Substitute.For<IPortfolioEntryRepository>();
    private readonly IPortfolioRepository _portfolioRepository = Substitute.For<IPortfolioRepository>();
    private readonly ICryptoCurrencyRepository _cryptoCurrencyRepository = Substitute.For<ICryptoCurrencyRepository>();
    private readonly IExchangeRepository _exchangeRepository = Substitute.For<IExchangeRepository>();
    private readonly ICurrentUserProvider _currentUserProvider = Substitute.For<ICurrentUserProvider>();

    private const long UserId = 1L;
    private const long EntryId = 5L;
    private const long PortfolioId = 10L;
    private const long CryptoCurrencyId = 20L;
    private const long ExchangeId = 30L;

    private UpdatePortfolioEntryCommandHandler CreateHandler() => new(
        Substitute.For<ILogger<UpdatePortfolioEntryCommandHandler>>(),
        _portfolioEntryRepository,
        _portfolioRepository,
        _cryptoCurrencyRepository,
        _exchangeRepository,
        _currentUserProvider);

    private static UpdatePortfolioEntryRequest CreateRequest() => new()
    {
        CryptoCurrencyId = CryptoCurrencyId,
        ExchangeId = ExchangeId,
        Quantity = 3m,
        PricePerUnit = 200m,
        RecordedAt = DateTime.UtcNow
    };

    public UpdatePortfolioEntryCommandHandlerTests()
    {
        _currentUserProvider.UserId.Returns(UserId);
    }

    [Fact]
    public async Task Handle_WithValidData_UpdatesEntryAndReturnsResponse()
    {
        var entry = PortfolioEntry.Create(PortfolioId, 1, 1, quantity: 1m, pricePerUnit: 50m, DateTime.UtcNow.AddDays(-1));
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns(entry);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));
        _cryptoCurrencyRepository.GetByIdAsync(CryptoCurrencyId, Arg.Any<CancellationToken>())
            .Returns(CryptoCurrency.Create("ETH", "Ethereum", "ethereum"));
        _exchangeRepository.GetByIdAsync(ExchangeId, Arg.Any<CancellationToken>())
            .Returns(Exchange.Create("Kraken", "api-key"));

        var result = await CreateHandler().Handle(new UpdatePortfolioEntryCommand(EntryId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeFalse();
        result.Value.CryptoCurrencyId.Should().Be(CryptoCurrencyId);
        result.Value.ExchangeId.Should().Be(ExchangeId);
        result.Value.Quantity.Should().Be(3m);
        result.Value.PricePerUnit.Should().Be(200m);
        entry.CryptoCurrencyId.Should().Be(CryptoCurrencyId);
        entry.ExchangeId.Should().Be(ExchangeId);
        _portfolioEntryRepository.Received(1).Update(entry);
    }

    [Fact]
    public async Task Handle_WithNonExistentEntry_ReturnsNotFoundWithoutCheckingPortfolio()
    {
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns((PortfolioEntry?)null);

        var result = await CreateHandler().Handle(new UpdatePortfolioEntryCommand(EntryId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PortfolioEntry.NotFound");
        await _portfolioRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithEntryOwnedByAnotherUser_ReturnsNotFound()
    {
        var entry = PortfolioEntry.Create(PortfolioId, 1, 1, quantity: 1m, pricePerUnit: 50m, DateTime.UtcNow);
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns(entry);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(userId: 999, name: "Other user's portfolio", description: string.Empty));

        var result = await CreateHandler().Handle(new UpdatePortfolioEntryCommand(EntryId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("PortfolioEntry.NotFound");
    }

    [Fact]
    public async Task Handle_WithNonExistentCryptoCurrency_ReturnsNotFoundWithoutCheckingExchange()
    {
        var entry = PortfolioEntry.Create(PortfolioId, 1, 1, quantity: 1m, pricePerUnit: 50m, DateTime.UtcNow);
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns(entry);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));
        _cryptoCurrencyRepository.GetByIdAsync(CryptoCurrencyId, Arg.Any<CancellationToken>()).Returns((CryptoCurrency?)null);

        var result = await CreateHandler().Handle(new UpdatePortfolioEntryCommand(EntryId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("CryptoCurrency.NotFound");
        await _exchangeRepository.DidNotReceive().GetByIdAsync(Arg.Any<long>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithNonExistentExchange_ReturnsNotFoundWithoutUpdatingEntry()
    {
        var entry = PortfolioEntry.Create(PortfolioId, 1, 1, quantity: 1m, pricePerUnit: 50m, DateTime.UtcNow);
        _portfolioEntryRepository.GetByIdAsync(EntryId, Arg.Any<CancellationToken>()).Returns(entry);
        _portfolioRepository.GetByIdAsync(PortfolioId, Arg.Any<CancellationToken>())
            .Returns(Portfolio.Create(UserId, "My Portfolio", string.Empty));
        _cryptoCurrencyRepository.GetByIdAsync(CryptoCurrencyId, Arg.Any<CancellationToken>())
            .Returns(CryptoCurrency.Create("ETH", "Ethereum", "ethereum"));
        _exchangeRepository.GetByIdAsync(ExchangeId, Arg.Any<CancellationToken>()).Returns((Exchange?)null);

        var result = await CreateHandler().Handle(new UpdatePortfolioEntryCommand(EntryId, CreateRequest()), CancellationToken.None);

        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Exchange.NotFound");
        _portfolioEntryRepository.DidNotReceive().Update(Arg.Any<PortfolioEntry>());
    }
}
