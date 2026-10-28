using ADNoirBase.API.Configuration;
using ADNoirBase.Shared.Application.Validation;
using FluentValidation;
using Identity.Application.Dtos.Auth;
using Identity.Application.Services.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace ADNoirBase.API.Controllers.Identity;

[Tags("Auth")]
// Kimlik uçları brute-force ve credential stuffing'in ilk hedefidir.
[EnableRateLimiting(RateLimitPolicies.Authentication)]
public sealed class AuthController : BaseApiController
{
    private readonly IAuthService _authService;
    private readonly IValidator<LoginRequest> _loginValidator;
    private readonly IValidator<RegisterRequest> _registerValidator;
    private readonly IValidator<RefreshTokenRequest> _refreshValidator;

    public AuthController(
        IAuthService authService,
        IValidator<LoginRequest> loginValidator,
        IValidator<RegisterRequest> registerValidator,
        IValidator<RefreshTokenRequest> refreshValidator)
    {
        _authService = authService;
        _loginValidator = loginValidator;
        _registerValidator = registerValidator;
        _refreshValidator = refreshValidator;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var validation = await _loginValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ToHttpResponse(ValidationHelper.ToValidationFailedResponse(validation));
        }

        var result = await _authService.LoginAsync(request, ct);
        return ToHttpResponse(result);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        var validation = await _registerValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ToHttpResponse(ValidationHelper.ToValidationFailedResponse(validation));
        }

        var result = await _authService.RegisterAsync(request, ct);
        return ToHttpResponse(result);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var validation = await _refreshValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            return ToHttpResponse(ValidationHelper.ToValidationFailedResponse(validation));
        }

        var result = await _authService.RefreshTokenAsync(request, ct);
        return ToHttpResponse(result);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout([FromBody] RefreshTokenRequest request, CancellationToken ct)
    {
        var result = await _authService.LogoutAsync(request.RefreshToken, ct);
        return ToHttpResponse(result);
    }
}
