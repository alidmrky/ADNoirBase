using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Messages;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.User;
using Identity.Application.Messages;
using Identity.Domain.Interfaces.Repositories;

namespace Identity.Application.Services.User;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ICurrentUserService _currentUserService;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(
        IUserRepository userRepository,
        ICurrentUserService currentUserService,
        IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _currentUserService = currentUserService;
        _passwordHasher = passwordHasher;
    }

    public async Task<BaseResponse<UserDto>> GetProfileAsync(CancellationToken ct = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            return BaseResponse<UserDto>.Unauthorized(MessageCodes.Common.Unauthorized);

        var userId = Guid.Parse(_currentUserService.UserId);
        var user = await _userRepository.FindWithRolesAsync(userId, ct);

        if (user is null)
            return BaseResponse<UserDto>.NotFound(IdentityMessageCodes.User.NotFound);

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        return BaseResponse<UserDto>.Success(
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
                user.FullName, user.Status, user.CreatedAt, user.LastLoginAt, roles));
    }

    public async Task<BaseResponse> UpdateProfileAsync(
        UpdateProfileRequest request, CancellationToken ct = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            return BaseResponse.Unauthorized();

        var userId = Guid.Parse(_currentUserService.UserId);
        var user = await _userRepository.FindByIdAsync(userId, ct);

        if (user is null)
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);

        user.UpdateProfile(request.FirstName, request.LastName);
        await _userRepository.UpdateAsync(user, ct);
        return BaseResponse.Success(IdentityMessageCodes.User.Updated);
    }

    public async Task<BaseResponse> ChangePasswordAsync(
        ChangePasswordRequest request, CancellationToken ct = default)
    {
        if (!_currentUserService.IsAuthenticated || _currentUserService.UserId is null)
            return BaseResponse.Unauthorized();

        var userId = Guid.Parse(_currentUserService.UserId);
        var user = await _userRepository.FindByIdAsync(userId, ct);

        if (user is null)
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
            return BaseResponse.Unauthorized(IdentityMessageCodes.Auth.CredentialsInvalid);

        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.ChangePassword(newHash);
        await _userRepository.UpdateAsync(user, ct);
        return BaseResponse.Success(IdentityMessageCodes.User.PasswordChanged);
    }
}
