namespace Identity.Application.Dtos.User;

public sealed record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string? PhoneNumber = null);
