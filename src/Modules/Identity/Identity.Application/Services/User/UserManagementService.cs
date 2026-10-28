using ADNoirBase.Shared.Application.Common;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Identity.Application.Dtos.User;
using Identity.Application.Dtos.Role;
using Identity.Application.Messages;
using Identity.Domain.Interfaces.Repositories;
using IdentityEntities = Identity.Domain.Entities;

namespace Identity.Application.Services.User;

public sealed class UserManagementService : IUserManagementService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUserRoleRepository _userRoleRepository;
    private readonly ICurrentUserService _currentUser;

    public UserManagementService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IPasswordHasher passwordHasher,
        IUserRoleRepository userRoleRepository,
        ICurrentUserService currentUser)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _passwordHasher = passwordHasher;
        _userRoleRepository = userRoleRepository;
        _currentUser = currentUser;
    }

    public async Task<BaseResponse<PagedList<UserListDto>>> GetUsersAsync(
        PagedRequest request, CancellationToken ct = default)
    {
        var paged = await _userRepository.ListPagedAsync(request.Page, request.PageSize, null, ct);
        var dtoItems = paged.Items
            .Select(u => new UserListDto(u.Id, u.Email, u.FullName, u.Status, u.CreatedAt, u.LastLoginAt))
            .ToList();

        var result = new PagedList<UserListDto>(dtoItems, paged.TotalCount, paged.Page, paged.PageSize);
        return BaseResponse<PagedList<UserListDto>>.Success(result);
    }

    public async Task<BaseResponse<UserDto>> GetByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindWithRolesAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse<UserDto>.NotFound(IdentityMessageCodes.User.NotFound);
        }

        var roles = user.UserRoles.Select(ur => ur.Role.Code).ToList();
        return BaseResponse<UserDto>.Success(
            new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
                user.FullName, user.PhoneNumber, user.Status, user.CreatedAt, user.LastLoginAt, roles));
    }

    public async Task<BaseResponse<UserDto>> CreateUserAsync(
        CreateUserRequest request, CancellationToken ct = default)
    {
        if (await _userRepository.EmailExistsAsync(request.Email, ct))
        {
            return BaseResponse<UserDto>.Conflict(IdentityMessageCodes.User.EmailConflict);
        }

        var passwordHash = _passwordHasher.Hash(request.Password);
        var user = IdentityEntities.User.Create(
            request.Email, passwordHash, request.FirstName, request.LastName, request.PhoneNumber);
        await _userRepository.InsertAsync(user, ct);

        var dto = new UserDto(user.Id, user.Email, user.FirstName, user.LastName,
            user.FullName, user.PhoneNumber, user.Status, user.CreatedAt, user.LastLoginAt, []);
        return BaseResponse<UserDto>.Created(dto, IdentityMessageCodes.User.Created);
    }

    public async Task<BaseResponse> UpdateUserAsync(
        Guid userId, UpdateUserRequest request, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);
        }

        user.UpdateProfile(request.FirstName, request.LastName, request.PhoneNumber);
        await _userRepository.UpdateAsync(user, ct);
        return BaseResponse.Success(IdentityMessageCodes.User.Updated);
    }

    public async Task<BaseResponse> DeactivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);
        }

        user.Deactivate();
        await _userRepository.UpdateAsync(user, ct);
        return BaseResponse.Success(IdentityMessageCodes.User.Deactivated);
    }

    public async Task<BaseResponse> ActivateUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindByIdAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);
        }

        user.Activate();
        await _userRepository.UpdateAsync(user, ct);
        return BaseResponse.Success(IdentityMessageCodes.User.Activated);
    }

    public async Task<BaseResponse> AssignRoleAsync(
        Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindWithRolesAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);
        }

        var role = await _roleRepository.FindByIdAsync(roleId, ct);
        if (role is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.Role.NotFound);
        }

        var userRole = IdentityEntities.UserRole.Create(userId, roleId);
        if (!await _userRoleRepository.TryAssignAsync(userRole, ct))
        {
            return BaseResponse.Conflict(IdentityMessageCodes.Role.AlreadyAssigned);
        }

        return BaseResponse.Success(IdentityMessageCodes.Role.Assigned);
    }

    public async Task<BaseResponse<IReadOnlyList<RoleDto>>> GetRolesAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindWithRolesAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse<IReadOnlyList<RoleDto>>.NotFound(IdentityMessageCodes.User.NotFound);
        }
        var counts = await _userRoleRepository.CountByRoleAsync(user.UserRoles.Select(x => x.RoleId).ToList(), ct);
        var roles = user.UserRoles.Select(x => x.Role).OrderBy(role => role.Code)
            .Select(role => new RoleDto(role.Id, role.Name, role.Code, role.Description, role.IsSystem, counts.GetValueOrDefault(role.Id))).ToList();
        return BaseResponse<IReadOnlyList<RoleDto>>.Success(roles);
    }

    public async Task<BaseResponse> RemoveRoleAsync(
        Guid userId, Guid roleId, CancellationToken ct = default)
    {
        var user = await _userRepository.FindWithRolesAsync(userId, ct);
        if (user is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.User.NotFound);
        }

        var role = await _roleRepository.FindByIdAsync(roleId, ct);
        if (role is null)
        {
            return BaseResponse.NotFound(IdentityMessageCodes.Role.NotFound);
        }

        if (role.Code == "ADMIN" && Guid.TryParse(_currentUser.UserId, out var currentId) && currentId == userId)
        {
            return BaseResponse.BusinessRuleViolation(IdentityMessageCodes.Role.CannotRemoveOwnAdmin);
        }

        if (!await _userRoleRepository.RemoveAsync(userId, roleId, ct))
        {
            return BaseResponse.Conflict(IdentityMessageCodes.Role.NotAssigned);
        }

        return BaseResponse.Success(IdentityMessageCodes.Role.Removed);
    }
}
