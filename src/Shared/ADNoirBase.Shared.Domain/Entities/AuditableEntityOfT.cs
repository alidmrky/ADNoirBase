namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>Katman 2: Oluşturma izi. CreatedAt ve CreatedBy ICurrentUserService tarafından doldurulur.</summary>
public abstract class AuditableEntity<TId> : BaseEntity<TId>
    where TId : IEquatable<TId>
{
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    /// <summary>UserId veya "system". AuditInterceptor tarafından doldurulur.</summary>
    public string CreatedBy { get; init; } = string.Empty;
}
