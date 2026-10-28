using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRoleRepository(IdentityDbContext context) : IUserRoleRepository
{
    public async Task<bool> TryAssignAsync(UserRole assignment, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(assignment);
        // A client-generated GUID must not cause EF to update a new relationship.
        context.UserRoles.Add(assignment);
        try
        {
            await context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_UserRoles_UserId_RoleId" })
        {
            context.Entry(assignment).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> RemoveAsync(Guid userId, Guid roleId, CancellationToken ct = default) =>
        await context.UserRoles.Where(x => x.UserId == userId && x.RoleId == roleId).ExecuteDeleteAsync(ct) > 0;

    public async Task<IReadOnlyDictionary<Guid, int>> CountByRoleAsync(IReadOnlyList<Guid> roleIds, CancellationToken ct = default) =>
        await context.UserRoles.Where(x => roleIds.Contains(x.RoleId)).GroupBy(x => x.RoleId)
            .Select(group => new { Id = group.Key, Count = group.Count() }).ToDictionaryAsync(x => x.Id, x => x.Count, ct);
}
