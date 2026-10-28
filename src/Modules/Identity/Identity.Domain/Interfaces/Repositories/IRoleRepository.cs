using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Identity.Domain.Entities;
using ADNoirBase.Shared.Contracts.Pagination;

namespace Identity.Domain.Interfaces.Repositories;

public interface IRoleRepository : IRepository<Role, Guid>
{
    Task<Role?> FindByCodeAsync(string code, CancellationToken ct = default);
    Task<bool> CodeExistsAsync(string code, CancellationToken ct = default);
    Task<PagedList<Role>> SearchAsync(int page, int pageSize, string? search, CancellationToken ct = default);
    Task<bool> TryInsertAsync(Role role, CancellationToken ct = default);
    Task<bool> TryDeleteUnassignedAsync(Guid roleId, CancellationToken ct = default);
}
