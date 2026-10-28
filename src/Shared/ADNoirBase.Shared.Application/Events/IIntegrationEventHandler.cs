using ADNoirBase.Shared.Contracts.IntegrationEvents;

namespace ADNoirBase.Shared.Application.Events;

public interface IIntegrationEventHandler<in TEvent>
    where TEvent : IIntegrationEvent
{
    Task HandleAsync(TEvent integrationEvent, CancellationToken ct = default);
}
