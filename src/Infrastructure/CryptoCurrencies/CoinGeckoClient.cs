using Application.CryptoCurrencies.Interfaces;
using ErrorOr;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.CryptoCurrencies;

internal sealed class CoinGeckoClient(HttpClient httpClient) : ICoinGeckoClient
{
    public async Task<ErrorOr<IReadOnlyDictionary<string, decimal>>> GetEurPricesAsync(
        IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken)
    {
        if (coinGeckoIds.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        var ids = string.Join(',', coinGeckoIds.Select(Uri.EscapeDataString));

        Dictionary<string, Dictionary<string, decimal>>? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(
                $"simple/price?ids={ids}&vs_currencies=eur", cancellationToken);
        }
        catch (HttpRequestException)
        {
            return Error.Failure("CoinGecko.Unavailable", "Unable to retrieve current prices from CoinGecko. Please try again later.");
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Error.Failure("CoinGecko.Timeout", "The request to CoinGecko timed out. Please try again later.");
        }
        catch (JsonException)
        {
            return Error.Failure("CoinGecko.InvalidResponse", "CoinGecko returned an unexpected response.");
        }

        if (response is null)
        {
            return new Dictionary<string, decimal>();
        }

        return response.ToDictionary(kv => kv.Key, kv => kv.Value["eur"]);
    }
}
