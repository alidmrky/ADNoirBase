using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ADNoirBase.DbMigrator.Seeders;

public sealed class DefaultRolesSeeder
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<DefaultRolesSeeder> _logger;

    public DefaultRolesSeeder(IdentityDbContext context, ILogger<DefaultRolesSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var roles = new[]
        {
            Role.Create("Administrator", "ADMIN", "Tam yetkili sistem yöneticisi", isSystem: true),
            Role.Create("User", "USER", "Standart kullanıcı", isSystem: true)
        };

        foreach (var role in roles)
        {
            if (!await _context.Roles.AnyAsync(r => r.Code == role.Code, ct))
            {
                await _context.Roles.AddAsync(role, ct);
                _logger.LogInformation("Role seeded: {Code}", role.Code);
            }
        }

        await _context.SaveChangesAsync(ct);
    }
}
