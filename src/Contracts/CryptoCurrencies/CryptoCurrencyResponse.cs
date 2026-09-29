
namespace Contracts.CryptoCurrencies;

public sealed record CryptoCurrencyResponse(
    long Id,
    string Symbol,
    string Name,
    string? CoinGeckoId,
    string? Image,
    DateTime CreatedAt
    );
