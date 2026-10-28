namespace ADNoirBase.Shared.Contracts.IntegrationEvents;

/// <summary>
/// Modüller arası olay sözleşmesi. Modüller birbirinin projesine referans veremediği için
/// yayıncı ve dinleyici yalnızca bu katmandaki tipleri paylaşır.
/// </summary>
public interface IIntegrationEvent
{
    Guid EventId { get; }

    DateTime OccurredAt { get; }
}
