using Domain.Entities;
using FluentAssertions;

namespace Domain.Tests.Entities;

public class CryptoCurrencyTests
{
    [Fact]
    public void Create_WithValidParameters_ReturnsCryptoCurrencyWithExpectedProperties()
    {
        var symbol = "BTC";
        var name = "Bitcoin";

        var before = DateTime.UtcNow;
        var cryptoCurrency = CryptoCurrency.Create(symbol, name, coinGeckoId: "bitcoin");
        var after = DateTime.UtcNow;

        cryptoCurrency.Symbol.Should().Be(symbol);
        cryptoCurrency.Name.Should().Be(name);
        cryptoCurrency.CoinGeckoId.Should().Be("bitcoin");
        cryptoCurrency.CreatedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
    }

    [Fact]
    public void Create_WithoutCoinGeckoId_LeavesItNull()
    {
        var cryptoCurrency = CryptoCurrency.Create("ETH", "Ethereum");

        cryptoCurrency.CoinGeckoId.Should().BeNull();
    }
}
