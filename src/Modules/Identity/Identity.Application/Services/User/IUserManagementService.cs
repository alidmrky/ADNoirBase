using ADNoirBase.Shared.Application.Common;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.User;
using Identity.Application.Dtos.Role;

namespace Identity.Application.Services.User;

public interface IUserManagementService
{
    Task<BaseResponse<IReadOnlyList<RoleDto>>> GetRolesAsync(Guid userId, CancellationToken ct = default);
    Task<BaseResponse<PagedList<UserListDto>>> GetUsersAsync(PagedRequest request, CancellationToken ct = default);
    Task<BaseResponse<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default);
    Task<BaseResponse<UserDto>> CreateUserAsync(CreateUserRequest request, CancellationToken ct = default);
    Task<BaseResponse> UpdateUserAsync(Guid userId, UpdateUserRequest request, CancellationToken ct = default);
    Task<BaseResponse> DeactivateUserAsync(Guid userId, CancellationToken ct = default);
    Task<BaseResponse> ActivateUserAsync(Guid userId, CancellationToken ct = default);
    Task<BaseResponse> AssignRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
    Task<BaseResponse> RemoveRoleAsync(Guid userId, Guid roleId, CancellationToken ct = default);
}
