
namespace Contracts.CryptoCurrencies;

public sealed record UpdateCryptoCurrencyResponse(
    long Id,
    string Symbol,
    string Name,
    string? CoinGeckoId,
    DateTime CreatedAt
    );
