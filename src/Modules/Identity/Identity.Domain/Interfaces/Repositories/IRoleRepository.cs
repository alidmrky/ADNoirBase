using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
}
