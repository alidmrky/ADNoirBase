using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Identity.Domain.Entities;

namespace Identity.Domain.Interfaces.Repositories;

public interface IRefreshTokenRepository : IRepository<RefreshToken, Guid>
{
    Task<RefreshToken?> FindActiveByTokenAsync(string token, CancellationToken ct = default);
    Task RevokeAllUserTokensAsync(Guid userId, CancellationToken ct = default);
}
