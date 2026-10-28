namespace Identity.Application.Dtos.Role;
public sealed record UpdateRoleRequest(string Name, string? Description = null);
