using System.Security.Claims;

namespace ADNoirBase.Shared.Application.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(IEnumerable<Claim> claims);
    string GenerateRefreshToken();
    DateTime RefreshTokenExpiry { get; }
}
