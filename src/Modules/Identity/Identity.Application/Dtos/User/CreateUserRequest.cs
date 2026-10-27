namespace Identity.Application.Dtos.User;

public sealed record CreateUserRequest(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    IReadOnlyList<Guid>? RoleIds = null);
