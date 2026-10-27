using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

public interface IUserRepository : IRepository<User, Guid>
{
    Task<User?> FindByEmailAsync(string email, CancellationToken ct = default);
    Task<bool> EmailExistsAsync(string email, CancellationToken ct = default);
    Task<User?> FindWithRolesAsync(Guid userId, CancellationToken ct = default);
}
