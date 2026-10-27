namespace Identity.Application.Dtos.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt,
    DateTime RefreshTokenExpiresAt,
    string UserId,
    string Email,
    string FullName,
    IReadOnlyList<string> Roles);
