
namespace Contracts.Portfolios;

public sealed record RefreshPortfolioPricesResponse(
    long PortfolioId,
    DateTime RefreshedAt,
    List<RefreshedHoldingItem> Updated,
    List<SkippedHoldingItem> Skipped
    );

public sealed record RefreshedHoldingItem(
    long CryptoCurrencyId,
    string Symbol,
    long ExchangeId,
    decimal Quantity,
    decimal PreviousPrice,
    decimal NewPrice
    );

public sealed record SkippedHoldingItem(
    long CryptoCurrencyId,
    string Symbol,
    string Reason
    );
