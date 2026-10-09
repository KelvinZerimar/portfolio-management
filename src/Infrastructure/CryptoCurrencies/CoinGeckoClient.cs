using Application.CryptoCurrencies.Interfaces;
using ErrorOr;
using Microsoft.Extensions.Caching.Hybrid;
using System.Net.Http.Json;
using System.Text.Json;

namespace Infrastructure.CryptoCurrencies;

internal sealed class CoinGeckoClient(HttpClient httpClient, HybridCache cache) : ICoinGeckoClient
{
    private const string CacheTag = "coingecko-prices";
    private const string MarketSummaryCacheTag = "coingecko-market-summary";

    // A past month's price history never changes once the month is over, so this can be cached
    // far longer than current prices - mainly to avoid re-fetching per-coin history every time
    // the monthly report job (or a manual re-send) runs for the same period.
    private static readonly HybridCacheEntryOptions MarketSummaryCacheOptions = new()
    {
        Expiration = TimeSpan.FromHours(24),
        LocalCacheExpiration = TimeSpan.FromHours(24),
    };

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

    public async Task<ErrorOr<IReadOnlyDictionary<string, CoinGeckoMarketSummary>>> GetMarketSummaryAsync(
        IReadOnlyCollection<string> coinGeckoIds, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        var results = new Dictionary<string, CoinGeckoMarketSummary>();

        // CoinGecko's market_chart/range endpoint takes a single coin id - there's no batched
        // equivalent of simple/price for historical ranges, so this is one request per asset.
        foreach (var id in coinGeckoIds.Distinct())
        {
            var cacheKey = $"{MarketSummaryCacheTag}:{id}:{from:yyyyMMdd}:{to:yyyyMMdd}";
            try
            {
                var summary = await cache.GetOrCreateAsync(
                    cacheKey,
                    (httpClient, id, from, to),
                    static async (state, ct) => await FetchMarketSummaryAsync(state.httpClient, state.id, state.from, state.to, ct),
                    MarketSummaryCacheOptions,
                    tags: [MarketSummaryCacheTag],
                    cancellationToken: cancellationToken);

                results[id] = summary;
            }
            catch (CoinGeckoRequestException)
            {
                // Best-effort: skip this asset's market context rather than failing the whole
                // report over one coin's history being unavailable.
            }
        }

        return results;
    }

    private static async Task<CoinGeckoMarketSummary> FetchMarketSummaryAsync(
        HttpClient httpClient, string coinGeckoId, DateTime from, DateTime to, CancellationToken cancellationToken)
    {
        // "to" is a whole-day boundary (midnight) - extend to the end of that day so its own
        // trading range is included, not just the single midnight instant.
        var fromUnix = new DateTimeOffset(DateTime.SpecifyKind(from, DateTimeKind.Utc)).ToUnixTimeSeconds();
        var toUnix = new DateTimeOffset(DateTime.SpecifyKind(to, DateTimeKind.Utc)).AddDays(1).AddSeconds(-1).ToUnixTimeSeconds();

        Dictionary<string, List<List<decimal>>>? response;
        try
        {
            response = await httpClient.GetFromJsonAsync<Dictionary<string, List<List<decimal>>>>(
                $"coins/{Uri.EscapeDataString(coinGeckoId)}/market_chart/range?vs_currency=eur&from={fromUnix}&to={toUnix}",
                cancellationToken);
        }
        catch (HttpRequestException)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.Unavailable", "Unable to retrieve market history from CoinGecko. Please try again later."));
        }
        catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.Timeout", "The request to CoinGecko timed out. Please try again later."));
        }
        catch (JsonException)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.InvalidResponse", "CoinGecko returned an unexpected response."));
        }

        if (response is null || !response.TryGetValue("prices", out var prices) || prices.Count == 0)
        {
            throw new CoinGeckoRequestException(Error.Failure("CoinGecko.NoMarketData", $"CoinGecko returned no price history for '{coinGeckoId}' in the requested period."));
        }

        var open = prices[0][1];
        var close = prices[^1][1];
        var high = prices.Max(p => p[1]);
        var low = prices.Min(p => p[1]);
        var changePercentage = open == 0m ? (decimal?)null : Math.Round((close - open) / open * 100m, 2);

        return new CoinGeckoMarketSummary(coinGeckoId, open, close, high, low, changePercentage);
    }

    // Internal-only: lets a failed fetch cross HybridCache's factory boundary as an exception
    // (so the failure is never cached) while the public method still returns ErrorOr, matching
    // the rest of the codebase's error-handling convention.
    private sealed class CoinGeckoRequestException(Error error) : Exception
    {
        public Error Error { get; } = error;
    }
}
