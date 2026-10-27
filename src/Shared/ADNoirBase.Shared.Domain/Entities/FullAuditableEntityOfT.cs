namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>Katman 3: Değiştirme izi. UpdatedAt ve UpdatedBy AuditInterceptor tarafından doldurulur.</summary>
public abstract class FullAuditableEntity<TId> : AuditableEntity<TId>
    where TId : IEquatable<TId>
{
    public DateTime? UpdatedAt { get; set; }

    /// <summary>Son güncelleyen kullanıcı. AuditInterceptor tarafından doldurulur.</summary>
    public string? UpdatedBy { get; set; }
}
