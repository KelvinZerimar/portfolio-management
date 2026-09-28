
namespace Contracts.CryptoCurrencies;

public sealed record CreateCryptoCurrencyResponse(
    long Id,
    string Symbol,
    string Name,
    string? CoinGeckoId,
    DateTime CreatedAt
    );
