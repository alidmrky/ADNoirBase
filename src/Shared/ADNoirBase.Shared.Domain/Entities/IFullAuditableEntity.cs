namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>Id tipinden bağımsız değiştirme izi. Bkz. <see cref="IAuditableEntity"/>.</summary>
public interface IFullAuditableEntity : IAuditableEntity
{
    DateTime? UpdatedAt { get; }

    string? UpdatedBy { get; }

    /// <summary>Yalnızca persistence katmanı çağırır; açık implementasyon sayesinde entity API'sinde görünmez.</summary>
    void MarkUpdated(DateTime updatedAt, string updatedBy);
}
