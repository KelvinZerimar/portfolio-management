namespace Application.CryptoCurrencies.Interfaces;

public interface ICoinGeckoClient
{
    Task<IReadOnlyDictionary<string, decimal>> GetEurPricesAsync(
        IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken);
}
