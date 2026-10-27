using ADNoirBase.Shared.Application.Common;
using Identity.Application.Dtos.User;
using Identity.Application.Services.User;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADNoirBase.API.Controllers.Identity;

[Tags("User Management")]
[Authorize(Roles = "ADMIN")]
[Route("api/admin/users")]
public sealed class UserManagementController : BaseApiController
{
    private readonly IUserManagementService _userManagementService;

    public UserManagementController(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _userManagementService.GetUsersAsync(request, ct);
        return ToHttpResponse(result);
    }

    [HttpGet("{userId:guid}")]
    public async Task<IActionResult> GetById(Guid userId, CancellationToken ct)
    {
        var result = await _userManagementService.GetByIdAsync(userId, ct);
        return ToHttpResponse(result);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var result = await _userManagementService.CreateUserAsync(request, ct);
        return ToHttpResponse(result);
    }

    [HttpPut("{userId:guid}")]
    public async Task<IActionResult> UpdateUser(Guid userId, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var result = await _userManagementService.UpdateUserAsync(userId, request, ct);
        return ToHttpResponse(result);
    }

    [HttpPost("{userId:guid}/deactivate")]
    public async Task<IActionResult> Deactivate(Guid userId, CancellationToken ct)
    {
        var result = await _userManagementService.DeactivateUserAsync(userId, ct);
        return ToHttpResponse(result);
    }

    [HttpPost("{userId:guid}/activate")]
    public async Task<IActionResult> Activate(Guid userId, CancellationToken ct)
    {
        var result = await _userManagementService.ActivateUserAsync(userId, ct);
        return ToHttpResponse(result);
    }

    [HttpPost("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> AssignRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        var result = await _userManagementService.AssignRoleAsync(userId, roleId, ct);
        return ToHttpResponse(result);
    }

    [HttpDelete("{userId:guid}/roles/{roleId:guid}")]
    public async Task<IActionResult> RemoveRole(Guid userId, Guid roleId, CancellationToken ct)
    {
        var result = await _userManagementService.RemoveRoleAsync(userId, roleId, ct);
        return ToHttpResponse(result);
    }
}
