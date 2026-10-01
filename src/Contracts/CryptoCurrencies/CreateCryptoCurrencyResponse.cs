
namespace Contracts.CryptoCurrencies;

public sealed record CreateCryptoCurrencyResponse(
    long Id,
    string Symbol,
    string Name,
    string? CoinGeckoId,
    string? Image,
    DateTime CreatedAt
    );
