using Application.Common.Repository;
using Domain.Entities;

namespace Application.Portfolios.Interfaces;

public interface IPortfolioRepository : IRepository<Portfolio>
{
    Task<Portfolio?> GetByIdAsync(long id, CancellationToken cancellationToken);
    Task<Portfolio?> GetPortfolioByNameAsync(long userId, string name, CancellationToken cancellationToken);

    /// <summary>All portfolios across all users — used by the background report scheduler, which has no "current user" to scope by.</summary>
    Task<List<Portfolio>> GetAllAsync(CancellationToken cancellationToken);
}
