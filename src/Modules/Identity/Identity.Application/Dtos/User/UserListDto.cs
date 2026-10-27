using Identity.Domain.Enums;

namespace Identity.Application.Dtos.User;

public sealed record UserListDto(
    Guid Id,
    string Email,
    string FullName,
    UserStatus Status,
    DateTime CreatedAt,
    DateTime? LastLoginAt);
