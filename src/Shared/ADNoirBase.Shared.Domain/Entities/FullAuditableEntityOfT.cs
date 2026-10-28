namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>Katman 3: Değiştirme izi. UpdatedAt ve UpdatedBy AuditInterceptor tarafından doldurulur.</summary>
public abstract class FullAuditableEntity<TId> : AuditableEntity<TId>, IFullAuditableEntity
    where TId : IEquatable<TId>
{
    public DateTime? UpdatedAt { get; private set; }

    /// <summary>Son güncelleyen kullanıcı. AuditInterceptor tarafından doldurulur.</summary>
    public string? UpdatedBy { get; private set; }

    void IFullAuditableEntity.MarkUpdated(DateTime updatedAt, string updatedBy)
    {
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }
}
