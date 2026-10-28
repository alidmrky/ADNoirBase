namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>Katman 2: Oluşturma izi. CreatedAt ve CreatedBy ICurrentUserService tarafından doldurulur.</summary>
public abstract class AuditableEntity<TId> : BaseEntity<TId>, IAuditableEntity
    where TId : IEquatable<TId>
{
    public DateTime CreatedAt { get; private set; } = DateTime.UtcNow;

    /// <summary>UserId veya "system". AuditInterceptor tarafından doldurulur.</summary>
    public string CreatedBy { get; private set; } = string.Empty;

    void IAuditableEntity.MarkCreated(DateTime createdAt, string createdBy)
    {
        CreatedAt = createdAt;
        CreatedBy = createdBy;
    }
}
