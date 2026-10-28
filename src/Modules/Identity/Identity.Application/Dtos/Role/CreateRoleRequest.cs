namespace Identity.Application.Dtos.Role;
public sealed record CreateRoleRequest(string Name, string Code, string? Description = null);
