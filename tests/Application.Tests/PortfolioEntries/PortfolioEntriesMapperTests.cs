using Application.PortfolioEntries;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.PortfolioEntries;

public class PortfolioEntriesMapperTests
{
    private static PortfolioEntry CreatePortfolioEntry() =>
        PortfolioEntry.Create(portfolioId: 1, cryptoCurrencyId: 2, exchangeId: 3, quantity: 2.5m, pricePerUnit: 100m, recordedAt: new DateTime(2026, 1, 10));

    [Fact]
    public void ToCreatePortfolioEntryResponse_MapsAllFields()
    {
        var entry = CreatePortfolioEntry();

        var response = entry.ToCreatePortfolioEntryResponse();

        response.Id.Should().Be(entry.Id);
        response.PortfolioId.Should().Be(entry.PortfolioId);
        response.CryptoCurrencyId.Should().Be(entry.CryptoCurrencyId);
        response.ExchangeId.Should().Be(entry.ExchangeId);
        response.Quantity.Should().Be(entry.Quantity);
        response.PricePerUnit.Should().Be(entry.PricePerUnit);
        response.RecordedAt.Should().Be(entry.RecordedAt);
    }

    [Fact]
    public void ToUpdatePortfolioEntryResponse_MapsAllFields()
    {
        var entry = CreatePortfolioEntry();

        var response = entry.ToUpdatePortfolioEntryResponse();

        response.Id.Should().Be(entry.Id);
        response.PortfolioId.Should().Be(entry.PortfolioId);
        response.CryptoCurrencyId.Should().Be(entry.CryptoCurrencyId);
        response.ExchangeId.Should().Be(entry.ExchangeId);
        response.Quantity.Should().Be(entry.Quantity);
        response.PricePerUnit.Should().Be(entry.PricePerUnit);
        response.RecordedAt.Should().Be(entry.RecordedAt);
    }

    [Fact]
    public void ToPortfolioEntryResponse_MapsAllFields()
    {
        var entry = CreatePortfolioEntry();

        var response = entry.ToPortfolioEntryResponse();

        response.Id.Should().Be(entry.Id);
        response.PortfolioId.Should().Be(entry.PortfolioId);
        response.CryptoCurrencyId.Should().Be(entry.CryptoCurrencyId);
        response.ExchangeId.Should().Be(entry.ExchangeId);
        response.Quantity.Should().Be(entry.Quantity);
        response.PricePerUnit.Should().Be(entry.PricePerUnit);
        response.RecordedAt.Should().Be(entry.RecordedAt);
    }
}
