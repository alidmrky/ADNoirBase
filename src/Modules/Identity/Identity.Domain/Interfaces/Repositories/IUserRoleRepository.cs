using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

public interface IUserRoleRepository
{
    Task<bool> TryAssignAsync(UserRole assignment, CancellationToken ct = default);
    Task<bool> RemoveAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task<IReadOnlyDictionary<Guid, int>> CountByRoleAsync(IReadOnlyList<Guid> roleIds, CancellationToken ct = default);
}
