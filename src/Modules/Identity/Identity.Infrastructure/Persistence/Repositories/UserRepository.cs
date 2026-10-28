using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Persistence.Postgres;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : PostgresRepository<IdentityDbContext, User, Guid>, IUserRepository
{
    public UserRepository(IdentityDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService) { }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default)
    {
        var normalized = Normalize(email);
        return await DbSet.FirstOrDefaultAsync(u => u.Email == normalized, ct);
    }

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default)
    {
        var normalized = Normalize(email);
        return await DbSet.AnyAsync(u => u.Email == normalized, ct);
    }

    public async Task<User?> FindWithRolesAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

    /// <summary>E-postalar her zaman trim'lenmiş ve küçük harfe çevrilmiş olarak saklanır.</summary>
    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
