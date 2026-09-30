using Application.CryptoCurrencies.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Caching.Hybrid;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.CryptoCurrencies;

internal sealed class CoinGeckoClient(HttpClient httpClient, HybridCache cache) : ICoinGeckoClient
{
    private const string CacheTag = "coingecko-prices";

    // Prices barely move minute to minute and refreshing a portfolio is already gated to once
    // a day, so a short TTL is purely about collapsing near-simultaneous refresh calls (same or
    // overlapping coin sets) into fewer CoinGecko requests, not about serving stale data.
    private static readonly HybridCacheEntryOptions CacheOptions = new()
    {
        Expiration = TimeSpan.FromMinutes(5),
        LocalCacheExpiration = TimeSpan.FromMinutes(5),
    };

    public async Task<ErrorOr<IReadOnlyDictionary<string, decimal>>> GetEurPricesAsync(
        IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken)
    {
        if (coinGeckoIds.Count == 0)
        {
            return new Dictionary<string, decimal>();
        }

        // Keyed by the exact set of ids: a retried refresh for the same portfolio (or two
        // portfolios holding the identical coin set) reuses the cached quote, and concurrent
        // callers for the same key collapse into a single in-flight CoinGecko request.
        var sortedIds = coinGeckoIds.Distinct().OrderBy(id => id, StringComparer.Ordinal).ToArray();
        var cacheKey = $"{CacheTag}:{string.Join(',', sortedIds)}";

        try
        {
            var prices = await cache.GetOrCreateAsync(
                cacheKey,
                (httpClient, sortedIds),
                static async (state, ct) => await FetchAsync(state.httpClient, state.sortedIds, ct),
                CacheOptions,
                tags: [CacheTag],
                cancellationToken: cancellationToken);

            return new Dictionary<string, decimal>(prices);
        }
        catch (CoinGeckoRequestException ex)
        {
            return ex.Error;
        }
    }

    private static async Task<Dictionary<string, decimal>> FetchAsync(
        HttpClient httpClient, IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken)
    {
        var ids = string.Join(',', coinGeckoIds.Select(Uri.EscapeDataString));

        Dictionary<string, Dictionary<string, decimal>>? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<Dictionary<string, Dictionary<string, decimal>>>(
                $"simple/price?ids={ids}&vs_currencies=eur", cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.Unavailable", "Unable to retrieve current prices from CoinGecko. Please try again later."));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.Timeout", "The request to CoinGecko timed out. Please try again later."));
        }
        catch (JsonException)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.InvalidResponse", "CoinGecko returned an unexpected response."));
        }

        if (response is null)
        {
            return [];
        }

        return response.ToDictionary(kv => kv.Key, kv => kv.Value["eur"]);
    }

    // Internal-only: lets a failed fetch cross HybridCache's factory boundary as an exception
    // (so the failure is never cached) while the public method still returns ErrorOr, matching
    // the rest of the codebase's error-handling convention.
    private sealed class CoinGeckoRequestException(Error error) : Exception
    {
        public Error Error { get; } = error;
    }
}
