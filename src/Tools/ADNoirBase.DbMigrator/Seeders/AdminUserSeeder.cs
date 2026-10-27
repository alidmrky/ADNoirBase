using BCrypt.Net;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ADNoirBase.DbMigrator.Seeders;

public sealed class AdminUserSeeder
{
    private readonly IdentityDbContext _context;
    private readonly ILogger<AdminUserSeeder> _logger;

    public AdminUserSeeder(IdentityDbContext context, ILogger<AdminUserSeeder> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var adminEmail = Environment.GetEnvironmentVariable("ADMIN_EMAIL") ?? "admin@adnoirbase.com";
        var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123456";

        if (await _context.Users.AnyAsync(u => u.Email == adminEmail, ct))
        {
            _logger.LogInformation("Admin user already exists, skipping.");
            return;
        }

        var adminRole = await _context.Roles.FirstOrDefaultAsync(r => r.Code == "ADMIN", ct);
        if (adminRole is null)
        {
            _logger.LogWarning("ADMIN role not found. Run DefaultRolesSeeder first.");
            return;
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(adminPassword);
        var admin = User.Create(adminEmail, passwordHash, "System", "Admin");

        await _context.Users.AddAsync(admin, ct);
        await _context.SaveChangesAsync(ct);

        var userRole = UserRole.Create(admin.Id, adminRole.Id);
        await _context.UserRoles.AddAsync(userRole, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Admin user created: {Email}", adminEmail);
    }
}
