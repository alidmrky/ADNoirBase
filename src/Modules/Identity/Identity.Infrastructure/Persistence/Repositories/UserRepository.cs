using ADNoirBase.Shared.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : BasePostgresRepository<User, Guid>, IUserRepository
{
    public UserRepository(IdentityDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService) { }

    public async Task<User?> FindByEmailAsync(string email, CancellationToken ct = default) =>
        await DbSet
            .FirstOrDefaultAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<bool> EmailExistsAsync(string email, CancellationToken ct = default) =>
        await DbSet
            .AnyAsync(u => u.Email == email.Trim().ToLowerInvariant(), ct);

    public async Task<User?> FindWithRolesAsync(Guid userId, CancellationToken ct = default) =>
        await DbSet
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.Id == userId, ct);
}
