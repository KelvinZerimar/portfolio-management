using Application.Common.Repository;
using Domain.Entities;

namespace Application.Users.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken);
    Task<User?> GetByIdAsync(long id, CancellationToken cancellationToken);
}
