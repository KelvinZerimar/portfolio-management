using Domain.Entities;
using FluentAssertions;

namespace Domain.Tests.Entities;

public class PortfolioEntryTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsPortfolioEntryWithExpectedProperties()
    {
        var portfolioId = 10L;
        var cryptoCurrencyId = 5L;
        var exchangeId = 3L;
        var quantity = 1.5m;
        var pricePerUnit = 25000m;
        var recordedAt = new DateTime(2026, 1, 1);

        var entry = PortfolioEntry.Create(portfolioId, cryptoCurrencyId, exchangeId, quantity, pricePerUnit, recordedAt);

        entry.PortfolioId.Should().Be(portfolioId);
        entry.CryptoCurrencyId.Should().Be(cryptoCurrencyId);
        entry.ExchangeId.Should().Be(exchangeId);
        entry.Quantity.Should().Be(quantity);
        entry.PricePerUnit.Should().Be(pricePerUnit);
        entry.RecordedAt.Should().Be(recordedAt);
    }

    [Fact]
    public void Create_SetsCreatedAtToCurrentUtcTime()
    {
        var before = DateTime.UtcNow;

        var entry = PortfolioEntry.Create(1, 1, 1, 1m, 1m, DateTime.UtcNow);

        var after = DateTime.UtcNow;
        entry.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_DoesNotPopulateNavigationProperties()
    {
        var entry = PortfolioEntry.Create(1, 1, 1, 1m, 1m, DateTime.UtcNow);

        entry.CryptoCurrency.Should().BeNull();
        entry.Exchange.Should().BeNull();
    }

    [Fact]
    public void Create_WithZeroQuantity_AllowsZeroQuantity()
    {
        var entry = PortfolioEntry.Create(1, 1, 1, quantity: 0m, pricePerUnit: 100m, recordedAt: DateTime.UtcNow);

        entry.Quantity.Should().Be(0m);
    }

    [Fact]
    public void Create_WithNegativeQuantity_AllowsNegativeQuantity()
    {
        var entry = PortfolioEntry.Create(1, 1, 1, quantity: -5m, pricePerUnit: 100m, recordedAt: DateTime.UtcNow);

        entry.Quantity.Should().Be(-5m);
    }
}
