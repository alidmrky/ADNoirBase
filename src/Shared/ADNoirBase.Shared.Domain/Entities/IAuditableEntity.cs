namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>
/// Id tipinden bağımsız oluşturma izi. AuditInterceptor ve MongoRepository
/// <see cref="AuditableEntity{TId}"/> türevlerini Guid, int veya string fark etmeksizin bu arayüzle tanır.
/// </summary>
public interface IAuditableEntity
{
    DateTime CreatedAt { get; }

    string CreatedBy { get; }

    /// <summary>Yalnızca persistence katmanı çağırır; açık implementasyon sayesinde entity API'sinde görünmez.</summary>
    void MarkCreated(DateTime createdAt, string createdBy);
}
