using ADNoirBase.Shared.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class RoleRepository : BasePostgresRepository<Role, Guid>, IRoleRepository
{
    public RoleRepository(IdentityDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService) { }

    public async Task<Role?> FindByCodeAsync(string code, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(r => r.Code == code.ToUpperInvariant(), ct);

    public async Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) =>
        await DbSet.AnyAsync(r => r.Code == code.ToUpperInvariant(), ct);
}
