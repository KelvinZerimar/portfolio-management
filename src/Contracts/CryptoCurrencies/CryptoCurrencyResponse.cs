
namespace Contracts.CryptoCurrencies;

public sealed record CryptoCurrencyResponse(
    long Id,
    string Symbol,
    string Name,
    string? CoinGeckoId,
    DateTime CreatedAt
    );
