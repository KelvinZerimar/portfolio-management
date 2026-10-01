using Application.Common.Repository;
using Contracts.Common;
using Domain.Entities;

namespace Application.PortfolioEntries.Interfaces;

public interface IPortfolioEntryRepository : IRepository<PortfolioEntry>
{
    Task<PortfolioEntry?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<List<PortfolioEntry>> GetByPortfolioIdAsync(long portfolioId, CancellationToken cancellationToken);

    Task<PaginatorResponse<PortfolioEntry>> GetPagedAsync(
        long portfolioId,
        int page,
        int limit,
        long? cryptoCurrencyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken);
}
