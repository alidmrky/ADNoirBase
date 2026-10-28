using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.User;

namespace Identity.Application.Services.User;

public interface IUserService
{
    Task<BaseResponse<UserDto>> GetProfileAsync(CancellationToken ct = default);
    Task<BaseResponse> UpdateProfileAsync(UpdateProfileRequest request, CancellationToken ct = default);
    Task<BaseResponse> ChangePasswordAsync(ChangePasswordRequest request, CancellationToken ct = default);
}
