namespace Identity.Application.Dtos.User;

public sealed record UpdateUserRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber = null);
