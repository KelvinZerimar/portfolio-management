
using Application.PortfolioEntries.Interfaces;
using Contracts.Common;
using Domain.Entities;
using Infrastructure.Common.Persistence;
using Infrastructure.Common.Persistence.Contexts;
using Infrastructure.Common.Repository;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.PortfolioEntries;

internal sealed class PortfolioEntryRepository(AppDbContext dbContext) : Repository<PortfolioEntry>(dbContext), IPortfolioEntryRepository
{
    public Task<PortfolioEntry?> GetByIdAsync(long id, CancellationToken cancellationToken)
        => dbContext.Set<PortfolioEntry>().FirstOrDefaultAsync(e => e.Id == id, cancellationToken);

    public Task<List<PortfolioEntry>> GetByPortfolioIdAsync(long portfolioId, CancellationToken cancellationToken)
        => dbContext.Set<PortfolioEntry>()
            .AsNoTracking()
            .Include(e => e.CryptoCurrency)
            .Include(e => e.Exchange)
            .Where(e => e.PortfolioId == portfolioId)
            .ToListAsync(cancellationToken);

    public Task<PaginatorResponse<PortfolioEntry>> GetPagedAsync(
        long portfolioId,
        int page,
        int limit,
        long? cryptoCurrencyId,
        DateTime? fromDate,
        DateTime? toDate,
        CancellationToken cancellationToken)
    {
        // Postgres stores RecordedAt as "timestamp with time zone" (UTC). Query-string dates
        // are bound with Kind=Unspecified, which Npgsql refuses to compare against it, so the
        // date-only filter is normalized to UTC here (matching how RecordedAt is written).
        var from = fromDate.HasValue ? DateTime.SpecifyKind(fromDate.Value.Date, DateTimeKind.Utc) : (DateTime?)null;
        var to = toDate.HasValue
            ? DateTime.SpecifyKind(toDate.Value.Date.AddDays(1), DateTimeKind.Utc)
            : (DateTime?)null;

        return dbContext.Set<PortfolioEntry>()
            .AsNoTracking()
            .Where(e => e.PortfolioId == portfolioId)
            .Where(e => !cryptoCurrencyId.HasValue || e.CryptoCurrencyId == cryptoCurrencyId.Value)
            .Where(e => !from.HasValue || e.RecordedAt >= from.Value)
            .Where(e => !to.HasValue || e.RecordedAt < to.Value)
            .OrderByDescending(e => e.RecordedAt)
            .ThenByDescending(e => e.Id)
            .PaginateAsync(page, limit, cancellationToken);
    }
}
