namespace ADNoirBase.Shared.Application.Interfaces;

/// <summary>
/// O anki kimliği doğrulanmış kullanıcıyı sağlar.
/// Implementasyonu Shared.Infrastructure'da HttpContext üzerinden okur.
/// AuditInterceptor, CreatedBy/UpdatedBy/DeletedBy alanlarını bu servisten alır.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    string? UserName { get; }
    IReadOnlyList<string> Roles { get; }
    bool IsAuthenticated { get; }
}
