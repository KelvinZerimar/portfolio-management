namespace Infrastructure.Common.Options;

public sealed class CoinGeckoOptions
{
    public const string SectionName = "CoinGecko";

    public string BaseUrl { get; set; } = "https://api.coingecko.com/api/v3/";
    public string? ApiKey { get; set; }
}
