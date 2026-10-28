using ADNoirBase.Shared.Contracts.IntegrationEvents;

namespace ADNoirBase.Shared.Application.Events;

public interface IIntegrationEventPublisher
{
    /// <summary>
    /// Olayı tüm dinleyicilere iletir. Bir dinleyicinin hatası yayıncıya yansımaz;
    /// bildirim gönderilemedi diye kullanıcı kaydı geri alınmamalıdır.
    /// </summary>
    Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent;
}
