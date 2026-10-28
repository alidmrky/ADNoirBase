using ADNoirBase.Shared.Application.Common;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.Role;
using Identity.Application.Messages;
using Identity.Domain.Interfaces.Repositories;
using RoleEntity = Identity.Domain.Entities.Role;

namespace Identity.Application.Services.Role;

public sealed class RoleManagementService(IRoleRepository roles, IUserRoleRepository assignments) : IRoleManagementService
{
    public async Task<BaseResponse<PagedList<RoleDto>>> ListAsync(PagedRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var page = await roles.SearchAsync(request.Page, request.PageSize, request.SearchTerm, ct);
        var counts = await assignments.CountByRoleAsync(page.Items.Select(r => r.Id).ToList(), ct);
        return BaseResponse<PagedList<RoleDto>>.Success(new PagedList<RoleDto>(
            page.Items.Select(r => ToDto(r, counts.GetValueOrDefault(r.Id))).ToList(), page.TotalCount, page.Page, page.PageSize));
    }

    public async Task<BaseResponse<RoleDto>> GetAsync(Guid id, CancellationToken ct = default)
    {
        var role = await roles.FindByIdAsync(id, ct);
        if (role is null)
        {
            return BaseResponse<RoleDto>.NotFound(IdentityMessageCodes.Role.NotFound);
        }
        var counts = await assignments.CountByRoleAsync([id], ct);
        return BaseResponse<RoleDto>.Success(ToDto(role, counts.GetValueOrDefault(id)));
    }

    public async Task<BaseResponse<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var code = request.Code?.Trim().ToUpperInvariant();
        if (!ValidDetails(request.Name, request.Description) || string.IsNullOrEmpty(code) || code.Length > 50 ||
            !char.IsAsciiLetter(code[0]) || !code.All(c => char.IsAsciiLetterOrDigit(c) || c == '_'))
        {
            return BaseResponse<RoleDto>.BusinessRuleViolation(IdentityMessageCodes.Role.Invalid);
        }
        var role = RoleEntity.Create(request.Name, code, request.Description?.Trim());
        if (!await roles.TryInsertAsync(role, ct))
        {
            return BaseResponse<RoleDto>.Conflict(IdentityMessageCodes.Role.CodeConflict);
        }
        return BaseResponse<RoleDto>.Created(ToDto(role, 0), IdentityMessageCodes.Role.Created);
    }

    public async Task<BaseResponse> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var role = await roles.FindByIdAsync(id, ct);
        if (role is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.Role.NotFound);
        }
        if (role.IsSystem)
        {
            return BaseResponse.BusinessRuleViolation(IdentityMessageCodes.Role.SystemRoleImmutable);
        }
        if (!ValidDetails(request.Name, request.Description))
        {
            return BaseResponse.BusinessRuleViolation(IdentityMessageCodes.Role.Invalid);
        }
        role.Update(request.Name, request.Description?.Trim());
        await roles.UpdateAsync(role, ct);
        return BaseResponse.Success(IdentityMessageCodes.Role.Updated);
    }

    public async Task<BaseResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var role = await roles.FindByIdAsync(id, ct);
        if (role is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.Role.NotFound);
        }
        if (role.IsSystem)
        {
            return BaseResponse.BusinessRuleViolation(IdentityMessageCodes.Role.SystemRoleImmutable);
        }
        if (!await roles.TryDeleteUnassignedAsync(id, ct))
        {
            return BaseResponse.Conflict(IdentityMessageCodes.Role.InUse);
        }
        return BaseResponse.Success(IdentityMessageCodes.Role.Deleted);
    }

    private static bool ValidDetails(string? name, string? description) =>
        !string.IsNullOrWhiteSpace(name) && name.Trim().Length <= 100 && (description?.Length ?? 0) <= 500;
    private static RoleDto ToDto(RoleEntity role, int count) =>
        new(role.Id, role.Name, role.Code, role.Description, role.IsSystem, count);
}
