namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>
/// Katman 4: Soft delete. SoftDelete() yalnızca bu entity üzerinde çağrılır —
/// repository doğrudan IsDeleted set edemez.
/// </summary>
public abstract class SoftDeletableEntity<TId> : FullAuditableEntity<TId>
    where TId : IEquatable<TId>
{
    public bool IsDeleted { get; private set; }
    public DateTime? DeletedAt { get; private set; }
    public string? DeletedBy { get; private set; }

    /// <summary>Soft delete işlemini gerçekleştirir. Repository bu metodu çağırır.</summary>
    public void SoftDelete(string deletedBy)
    {
        IsDeleted = true;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }

    /// <summary>Soft delete'i geri alır (restore).</summary>
    public void Restore()
    {
        IsDeleted = false;
        DeletedAt = null;
        DeletedBy = null;
    }
}
