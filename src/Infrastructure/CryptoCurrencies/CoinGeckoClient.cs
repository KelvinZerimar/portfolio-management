using Application.CryptoCurrencies.Interfaces;
using System.Net.Http.Json;

namespace Infrastructure.CryptoCurrencies;

internal sealed class CoinGeckoClient(HttpClient httpClient) : ICoinGeckoClient
{
    public async Task<IReadOnlyDictionary<string, decimal>> GetEurPricesAsync(
        IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken)
    {
        if (coinGeckoIds.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        var ids = string.Join(',', coinGeckoIds.Select(Uri.EscapeDataString));
        var response = await httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(
            $"simple/price?ids={ids}&vs_currencies=eur", cancellationToken);

        if (response is null)
        {
            return new Dictionary<string, decimal>();
        }

        return response.ToDictionary(kv => kv.Key, kv => kv.Value["eur"]);
    }
}
