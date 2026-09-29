
namespace Contracts.CryptoCurrencies;

public sealed record UpdateCryptoCurrencyResponse(
    long Id,
    string Symbol,
    string Name,
    string? CoinGeckoId,
    string? Image,
    DateTime CreatedAt
    );
