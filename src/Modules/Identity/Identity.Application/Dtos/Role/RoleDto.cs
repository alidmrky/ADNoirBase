namespace Identity.Application.Dtos.Role;
public sealed record RoleDto(Guid Id, string Name, string Code, string? Description, bool IsSystem, int UserCount);
