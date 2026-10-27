using ADNoirBase.Shared.Application.Validation;
using FluentValidation;
using Identity.Application.Dtos.User;
using Identity.Application.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADNoirBase.API.Controllers.Identity;

[Tags("User")]
[Authorize]
public sealed class UserController : BaseApiController
{
    private readonly IUserService _userService;
    private readonly IValidator<UpdateProfileRequest> _updateProfileValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator;

    public UserController(
        IUserService userService,
        IValidator<UpdateProfileRequest> updateProfileValidator,
        IValidator<ChangePasswordRequest> changePasswordValidator)
    {
        _userService = userService;
        _updateProfileValidator = updateProfileValidator;
        _changePasswordValidator = changePasswordValidator;
    }

    [HttpGet("me")]
    public async Task<IActionResult> GetProfile(CancellationToken ct)
    {
        var result = await _userService.GetProfileAsync(ct);
        return ToHttpResponse(result);
    }

    [HttpPut("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var validation = await _updateProfileValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ToHttpResponse(ValidationHelper.ToValidationFailedResponse(validation));

        var result = await _userService.UpdateProfileAsync(request, ct);
        return ToHttpResponse(result);
    }

    [HttpPut("me/change-password")]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var validation = await _changePasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
            return ToHttpResponse(ValidationHelper.ToValidationFailedResponse(validation));

        var result = await _userService.ChangePasswordAsync(request, ct);
        return ToHttpResponse(result);
    }
}
