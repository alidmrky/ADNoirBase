using ADNoirBase.Shared.Application.Common;
using Identity.Application.Dtos.Role;
using Identity.Application.Services.Role;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ADNoirBase.API.Controllers.Identity;

[Authorize(Roles = "ADMIN")]
[Route("api/admin/roles")]
[Tags("Role Management")]
public sealed class RoleManagementController(IRoleManagementService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedRequest request, CancellationToken ct) =>
        ToHttpResponse(await service.ListAsync(request, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToHttpResponse(await service.GetAsync(id, ct));
    [HttpPost]
    public async Task<IActionResult> Create(CreateRoleRequest request, CancellationToken ct) => ToHttpResponse(await service.CreateAsync(request, ct));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateRoleRequest request, CancellationToken ct) => ToHttpResponse(await service.UpdateAsync(id, request, ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToHttpResponse(await service.DeleteAsync(id, ct));
}
