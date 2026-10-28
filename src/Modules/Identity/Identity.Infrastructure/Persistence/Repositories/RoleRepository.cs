using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Persistence.Postgres;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

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
}
