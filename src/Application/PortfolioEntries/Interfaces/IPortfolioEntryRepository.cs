using Application.Common.Repository;
using Contracts.Common;
using Domain.Entities;

namespace Application.PortfolioEntries.Interfaces;

public interface IPortfolioEntryRepository : IRepository<PortfolioEntry>
{
    Task<PortfolioEntry?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<List<PortfolioEntry>> GetByPortfolioIdAsync(long portfolioId, CancellationToken cancellationToken);

    // An entry for the same asset on the same exchange at exactly the same
    // RecordedAt. Valuation keeps the latest RecordedAt per asset + exchange with
    // no tie-break, so such a pair would make the portfolio value arbitrary.
    // `excludingEntryId` lets an edit ignore the entry being edited.
    Task<PortfolioEntry?> FindDuplicateAsync(
        long portfolioId,
        long cryptoCurrencyId,
        long exchangeId,
        DateTime recordedAt,
        long? excludingEntryId,
        CancellationToken cancellationToken);

    Task<PaginatorResponse<PortfolioEntry>> GetPagedAsync(
        long portfolioId,
        int page,
        int limit,
        long? cryptoCurrencyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);
}
