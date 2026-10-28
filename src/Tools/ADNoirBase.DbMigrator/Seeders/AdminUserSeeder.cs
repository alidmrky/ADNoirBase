using ADNoirBase.Shared.Application.Interfaces;
using Identity.Domain.Entities;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ADNoirBase.DbMigrator.Seeders;

public sealed class AdminUserSeeder
{
    private const int MinimumPasswordLength = 12;

    private readonly IdentityDbContext _context;
    private readonly IPasswordHasher _passwordHasher;
    private readonly ILogger<AdminUserSeeder> _logger;

    public AdminUserSeeder(
        IdentityDbContext context,
        IPasswordHasher passwordHasher,
        ILogger<AdminUserSeeder> logger)
    {
        _context = context;
        _passwordHasher = passwordHasher;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        var adminEmail = ReadRequired("ADMIN_EMAIL");
        var adminPassword = ReadRequired("ADMIN_PASSWORD");

        if (adminPassword.Length < MinimumPasswordLength)
        {
            throw new InvalidOperationException(
                $"ADMIN_PASSWORD en az {MinimumPasswordLength} karakter olmalıdır.");
        }

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

        var passwordHash = _passwordHasher.Hash(adminPassword);
        var admin = User.Create(adminEmail, passwordHash, "System", "Admin");

        await _context.Users.AddAsync(admin, ct);
        await _context.SaveChangesAsync(ct);

        var userRole = UserRole.Create(admin.Id, adminRole.Id);
        await _context.UserRoles.AddAsync(userRole, ct);
        await _context.SaveChangesAsync(ct);

        _logger.LogInformation("Admin user created: {Email}", adminEmail);
    }

    /// <summary>
    /// Seed kimlik bilgileri için varsayılan değer yoktur. Eksik bir değişken,
    /// herkesin bildiği bir parolayla admin hesabı oluşturmaktan iyidir.
    /// </summary>
    private static string ReadRequired(string variableName)
    {
        var value = Environment.GetEnvironmentVariable(variableName);

        if (string.IsNullOrWhiteSpace(value))
        {
            throw new InvalidOperationException(
                $"{variableName} ortam değişkeni tanımlı değil. " +
                "Değerleri .env.example dosyasını örnek alarak ayarlayın.");
        }

        return value.Trim();
    }
}
