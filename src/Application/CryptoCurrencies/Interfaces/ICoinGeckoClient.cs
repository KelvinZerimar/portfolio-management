using ErrorOr;

namespace Application.CryptoCurrencies.Interfaces;

public sealed record CoinGeckoMarketSummary(
    string CoinGeckoId,
    decimal Open,
    decimal Close,
    decimal High,
    decimal Low,
    decimal? ChangePercentage);

public interface ICoinGeckoClient
{
    Task<ErrorOr<IReadOnlyDictionary<string, decimal>>> GetEurPricesAsync(
        IReadOnlyCollection<string> coinGeckoIds, CancellationToken cancellationToken);

    // "from"/"to" are whole-day boundaries (matches PortfolioStatusReportPeriod): "to" is treated
    // as inclusive of that entire day, not just its midnight instant. Ids that fail or have no
    // data are simply absent from the result - this is presented as supplementary market context,
    // not core functionality, so a partial/empty result is never an error by itself.
    Task<ErrorOr<IReadOnlyDictionary<string, CoinGeckoMarketSummary>>> GetMarketSummaryAsync(
        IReadOnlyCollection<string> coinGeckoIds, DateTime from, DateTime to, CancellationToken cancellationToken);
}
