using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Persistence.Postgres;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;
using ADNoirBase.Shared.Contracts.Pagination;
using Npgsql;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : PostgresRepository<IdentityDbContext, Role, Guid>, IRoleRepository
{
    public RoleRepository(IdentityDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService) { }

    public async Task<Role?> FindByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = Normalize(code);
        return await DbSet.FirstOrDefaultAsync(r => r.Code == normalized, ct);
    }

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default)
    {
        var normalized = Normalize(code);
        return await DbSet.AnyAsync(r => r.Code == normalized, ct);
    }

    /// <summary>Rol kodları her zaman büyük harfle saklanır.</summary>
    private static string Normalize(string code) => code.Trim().ToUpperInvariant();

    public async Task<PagedList<Role>> SearchAsync(int page, int pageSize, string? search, CancellationToken ct = default)
    {
        var query = DbSet.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = "%" + search.Trim().Replace("\\", "\\\\", StringComparison.Ordinal)
                .Replace("%", "\\%", StringComparison.Ordinal).Replace("_", "\\_", StringComparison.Ordinal) + "%";
            query = query.Where(role => EF.Functions.ILike(role.Code, term, "\\") || EF.Functions.ILike(role.Name, term, "\\"));
        }
        var count = await query.CountAsync(ct);
        var roles = await query.OrderBy(role => role.Code).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);
        return new PagedList<Role>(roles, count, page, pageSize);
    }

    public async Task<bool> TryInsertAsync(Role role, CancellationToken ct = default)
    {
        DbSet.Add(role);
        try
        {
            await Context.SaveChangesAsync(ct);
            return true;
        }
        catch (DbUpdateException ex) when (ex.InnerException is PostgresException
            { SqlState: PostgresErrorCodes.UniqueViolation, ConstraintName: "IX_Roles_Code" })
        {
            Context.Entry(role).State = EntityState.Detached;
            return false;
        }
    }

    public async Task<bool> TryDeleteUnassignedAsync(Guid roleId, CancellationToken ct = default)
    {
        try
        {
            return await DbSet.Where(role => role.Id == roleId && !role.IsSystem && !role.UserRoles.Any()).ExecuteDeleteAsync(ct) > 0;
        }
        catch (PostgresException ex) when (ex.SqlState == PostgresErrorCodes.ForeignKeyViolation)
        {
            // A role may be assigned after the check; the foreign key remains authoritative.
            return false;
        }
    }
}
