namespace Domain.Entities;

public sealed record PortfolioHoldingValue(long CryptoCurrencyId, string Symbol, decimal Quantity, decimal Value);

public sealed record PortfolioStatusReport(
    DateTime PeriodStart,
    DateTime PeriodEnd,
    decimal ValueAtPeriodStart,
    decimal ValueAtPeriodEnd,
    decimal ChangeAmount,
    decimal? ChangePercentage,
    List<PortfolioHoldingValue> Holdings);
