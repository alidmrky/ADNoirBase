namespace Identity.Application.Dtos.User;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string? PhoneNumber = null,
    IReadOnlyList<Guid>? RoleIds = null);
