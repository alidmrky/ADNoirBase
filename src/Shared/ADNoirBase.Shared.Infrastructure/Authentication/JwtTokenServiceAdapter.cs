using System.Security.Claims;
using ADNoirBase.Shared.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ADNoirBase.Shared.Infrastructure.Authentication;

public sealed class JwtTokenServiceAdapter : ITokenService
{
    private readonly JwtTokenService _inner;

    public JwtTokenServiceAdapter(IOptions<JwtSettings> settings)
    {
        _inner = new JwtTokenService(settings);
    }

    public string GenerateAccessToken(IEnumerable<Claim> claims) =>
        _inner.GenerateAccessToken(claims);

    public string GenerateRefreshToken() =>
        JwtTokenService.GenerateRefreshToken();

    public DateTime RefreshTokenExpiry =>
        _inner.RefreshTokenExpiry;
}
