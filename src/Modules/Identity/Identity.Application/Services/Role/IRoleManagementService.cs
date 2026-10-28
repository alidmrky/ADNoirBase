using ADNoirBase.Shared.Application.Common;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.Role;
namespace Identity.Application.Services.Role;
public interface IRoleManagementService
{
    Task<BaseResponse<PagedList<RoleDto>>> ListAsync(PagedRequest request, CancellationToken ct = default);
    Task<BaseResponse<RoleDto>> GetAsync(Guid id, CancellationToken ct = default);
    Task<BaseResponse<RoleDto>> CreateAsync(CreateRoleRequest request, CancellationToken ct = default);
    Task<BaseResponse> UpdateAsync(Guid id, UpdateRoleRequest request, CancellationToken ct = default);
    Task<BaseResponse> DeleteAsync(Guid id, CancellationToken ct = default);
}
