using ErrorOr;

namespace Application.CryptoCurrencies.Interfaces;

public interface ICoinGeckoClient
{
    Task<ErrorOr<IReadOnlyDictionary<string, decimal>>> GetEurPricesAsync(
        IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken);
}
