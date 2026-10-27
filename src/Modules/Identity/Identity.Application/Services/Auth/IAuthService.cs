using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.Auth;

namespace Identity.Application.Services.Auth;

public interface IAuthService
{
    Task<BaseResponse<LoginResponse>> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<BaseResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<BaseResponse<RefreshTokenResponse>> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken ct = default);
    Task<BaseResponse> LogoutAsync(string refreshToken, CancellationToken ct = default);
}
