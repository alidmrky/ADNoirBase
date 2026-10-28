using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Persistence.Postgres;
using Identity.Domain.Entities;
using Identity.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

public sealed class RefreshTokenRepository
    : PostgresRepository<IdentityDbContext, RefreshToken, Guid>, IRefreshTokenRepository
{
    public RefreshTokenRepository(IdentityDbContext context, ICurrentUserService currentUserService)
        : base(context, currentUserService) { }

    public async Task<RefreshToken?> FindActiveByTokenAsync(string token, CancellationToken ct = default) =>
        await DbSet
            .FirstOrDefaultAsync(rt => rt.Token == token && !rt.IsRevoked && rt.ExpiresAt > DateTime.UtcNow, ct);

    public async Task<RefreshToken?> FindByTokenAsync(string token, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(rt => rt.Token == token, ct);

    public async Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default)
    {
        var tokens = await DbSet
            .Where(rt => rt.UserId == userId && !rt.IsRevoked)
            .ToListAsync(ct);

        foreach (var token in tokens)
        {
            token.Revoke();
        }

        await Context.SaveChangesAsync(ct);
    }
}
