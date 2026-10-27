using ADNoirBase.Shared.Application.Interfaces;

namespace ADNoirBase.DbMigrator.Services;

/// <summary>
/// Migrator için ICurrentUserService implementasyonu. AuditInterceptor'ın
/// seed kayıtlarını "system" kullanıcısına yazabilmesi için gereklidir.
/// </summary>
public sealed class MigratorCurrentUserService : ICurrentUserService
{
    public string? UserId => "system";

    public string? UserName => "system";

    public IReadOnlyList<string> Roles => [];

    public bool IsAuthenticated => false;
}
