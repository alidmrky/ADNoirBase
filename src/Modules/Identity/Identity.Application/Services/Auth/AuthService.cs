using System.Security.Claims;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.Auth;
using Identity.Application.Messages;
using IdentityEntities = Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Domain.Interfaces.Repositories;

namespace Identity.Application.Services.Auth;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        ITokenService tokenService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
    }

    public async Task<BaseResponse<LoginResponse>> LoginAsync(
        LoginRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByEmailAsync(request.Email, ct);

        if (user is null || !_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            if (user is not null)
            {
                user.RecordFailedLogin();
                await _userRepository.UpdateAsync(user, ct);
            }
            return BaseResponse<LoginResponse>.Unauthorized(IdentityMessageCodes.Auth.CredentialsInvalid);
        }

        if (user.Status == UserStatus.Locked)
            return BaseResponse<LoginResponse>.Unauthorized(IdentityMessageCodes.Auth.AccountLocked);

        if (user.Status == UserStatus.Inactive)
            return BaseResponse<LoginResponse>.Unauthorized(IdentityMessageCodes.Auth.AccountInactive);

        var userWithRoles = await _userRepository.FindWithRolesAsync(user.Id, ct) ?? user;
        var roles = userWithRoles.UserRoles.Select(ur => ur.Role.Code).ToList();

        var claims = BuildClaims(user, roles);
        var accessToken = _tokenService.GenerateAccessToken(claims);
        var refreshTokenValue = _tokenService.GenerateRefreshToken();
        var refreshExpiry = _tokenService.RefreshTokenExpiry;

        var refreshToken = IdentityEntities.RefreshToken.Create(user.Id, refreshTokenValue, refreshExpiry);
        await _refreshTokenRepository.InsertAsync(refreshToken, ct);

        user.RecordSuccessfulLogin();
        await _userRepository.UpdateAsync(user, ct);

        var response = new LoginResponse(
            AccessToken: accessToken,
            RefreshToken: refreshTokenValue,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt: refreshExpiry,
            UserId: user.Id.ToString(),
            Email: user.Email,
            FullName: user.FullName,
            Roles: roles);

        return BaseResponse<LoginResponse>.Success(response, IdentityMessageCodes.Auth.LoginSuccess);
    }

    public async Task<BaseResponse> RegisterAsync(
        RegisterRequest request, CancellationToken ct = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, ct))
            return BaseResponse.Conflict(IdentityMessageCodes.User.EmailConflict);

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = IdentityEntities.User.Create(request.Email, passwordHash, request.FirstName, request.LastName);
        await _userRepository.InsertAsync(user, ct);

        return BaseResponse.Created(IdentityMessageCodes.Auth.RegisterSuccess);
    }

    public async Task<BaseResponse<RefreshTokenResponse>> RefreshTokenAsync(
        RefreshTokenRequest request, CancellationToken ct = default)
    {
        var existingToken = await _refreshTokenRepository
            .FindActiveByTokenAsync(request.RefreshToken, ct);

        if (existingToken is null || !existingToken.IsActive)
            return BaseResponse<RefreshTokenResponse>.Unauthorized(IdentityMessageCodes.Auth.RefreshTokenInvalid);

        var user = await _userRepository.FindWithRolesAsync(existingToken.UserId, ct);
        if (user is null)
            return BaseResponse<RefreshTokenResponse>.NotFound(IdentityMessageCodes.User.NotFound);

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        var newAccessToken = _tokenService.GenerateAccessToken(BuildClaims(user, roles));
        var newRefreshValue = _tokenService.GenerateRefreshToken();
        var newRefreshExpiry = _tokenService.RefreshTokenExpiry;

        existingToken.Revoke(newRefreshValue);
        await _refreshTokenRepository.UpdateAsync(existingToken, ct);

        var newToken = IdentityEntities.RefreshToken.Create(user.Id, newRefreshValue, newRefreshExpiry);
        await _refreshTokenRepository.InsertAsync(newToken, ct);

        var response = new RefreshTokenResponse(
            AccessToken: newAccessToken,
            RefreshToken: newRefreshValue,
            AccessTokenExpiresAt: DateTime.UtcNow.AddMinutes(15),
            RefreshTokenExpiresAt: newRefreshExpiry);

        return BaseResponse<RefreshTokenResponse>.Success(response, IdentityMessageCodes.Auth.RefreshSuccess);
    }

    public async Task<BaseResponse> LogoutAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await _refreshTokenRepository.FindActiveByTokenAsync(refreshToken, ct);
        if (token is not null)
        {
            token.Revoke();
            await _refreshTokenRepository.UpdateAsync(token, ct);
        }
        return BaseResponse.Success(IdentityMessageCodes.Auth.LogoutSuccess);
    }

    private static IEnumerable<Claim> BuildClaims(IdentityEntities.User user, IEnumerable<string> roles)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new(ClaimTypes.Email, user.Email),
            new(ClaimTypes.Name, user.FullName),
            new("sub", user.Id.ToString())
        };
        claims.AddRange(roles.Select(r => new Claim(ClaimTypes.Role, r)));
        return claims;
    }
}
