namespace ADNoirBase.Shared.Contracts.IntegrationEvents;

public sealed record UserPasswordChangedIntegrationEvent(
    Guid UserId,
    string Email,
    string FirstName,
    string LastName,
    bool ChangedByAdministrator) : IIntegrationEvent
{
    public Guid EventId { get; } = Guid.NewGuid();

    public DateTime OccurredAt { get; } = DateTime.UtcNow;
}
