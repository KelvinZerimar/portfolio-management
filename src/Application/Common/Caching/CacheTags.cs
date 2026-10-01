namespace Application.Common.Caching;

// HybridCache tags: shared between the query handlers that cache a list and the
// command handlers that must invalidate it after a mutation.
public static class CacheTags
{
    public const string CryptoCurrencies = "cryptocurrencies";
    public const string Exchanges = "exchanges";
}
