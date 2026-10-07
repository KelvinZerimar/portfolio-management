using Application.CryptoCurrencies;
using Domain.Entities;
using FluentAssertions;

namespace Application.Tests.CryptoCurrencies;

public class CryptoCurrenciesMapperTests
{
    private static CryptoCurrency CreateCryptoCurrency() =>
        CryptoCurrency.Create("BTC", "Bitcoin", "bitcoin", "btc.png");

    [Fact]
    public void ToCreateCryptoCurrencyResponse_MapsAllFields()
    {
        var cryptoCurrency = CreateCryptoCurrency();

        var response = cryptoCurrency.ToCreateCryptoCurrencyResponse();

        response.Id.Should().Be(cryptoCurrency.Id);
        response.Symbol.Should().Be(cryptoCurrency.Symbol);
        response.Name.Should().Be(cryptoCurrency.Name);
        response.CoinGeckoId.Should().Be(cryptoCurrency.CoinGeckoId);
        response.Image.Should().Be(cryptoCurrency.Image);
        response.CreatedAt.Should().Be(cryptoCurrency.CreatedAt);
    }

    [Fact]
    public void ToUpdateCryptoCurrencyResponse_MapsAllFields()
    {
        var cryptoCurrency = CreateCryptoCurrency();

        var response = cryptoCurrency.ToUpdateCryptoCurrencyResponse();

        response.Id.Should().Be(cryptoCurrency.Id);
        response.Symbol.Should().Be(cryptoCurrency.Symbol);
        response.Name.Should().Be(cryptoCurrency.Name);
        response.CoinGeckoId.Should().Be(cryptoCurrency.CoinGeckoId);
        response.Image.Should().Be(cryptoCurrency.Image);
        response.CreatedAt.Should().Be(cryptoCurrency.CreatedAt);
    }

    [Fact]
    public void ToCryptoCurrencyResponse_MapsAllFields()
    {
        var cryptoCurrency = CreateCryptoCurrency();

        var response = cryptoCurrency.ToCryptoCurrencyResponse();

        response.Id.Should().Be(cryptoCurrency.Id);
        response.Symbol.Should().Be(cryptoCurrency.Symbol);
        response.Name.Should().Be(cryptoCurrency.Name);
        response.CoinGeckoId.Should().Be(cryptoCurrency.CoinGeckoId);
        response.Image.Should().Be(cryptoCurrency.Image);
        response.CreatedAt.Should().Be(cryptoCurrency.CreatedAt);
    }
}
