using ADNoirBase.Shared.Domain.Entities;

namespace Identity.Domain.Entities;

public sealed class Role : AuditableEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;
    public string Code { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public bool IsSystem { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = [];

    private Role() { }

    public static Role Create(string name, string code, string? description = null, bool isSystem = false)
    {
        return new Role
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Code = code.Trim().ToUpperInvariant(),
            Description = description,
            IsSystem = isSystem
        };
    }

    public void Update(string name, string? description)
    {
        Name = name.Trim();
        Description = description;
    }
}
