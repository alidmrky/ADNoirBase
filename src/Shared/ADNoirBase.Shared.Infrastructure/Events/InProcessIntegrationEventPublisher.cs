using ADNoirBase.Shared.Application.Events;
using ADNoirBase.Shared.Contracts.IntegrationEvents;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ADNoirBase.Shared.Infrastructure.Events;

/// <summary>
/// Olayları aynı süreç içinde, kayıtlı handler'lara sırayla iletir.
/// Handler'lar kendi scope'unda çalışır: yayıncının scope'u kapanmış olabilir
/// (örneğin olay bir arka plan işinden geliyorsa).
/// </summary>
public sealed class InProcessIntegrationEventPublisher : IIntegrationEventPublisher
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<InProcessIntegrationEventPublisher> _logger;

    public InProcessIntegrationEventPublisher(
        IServiceScopeFactory scopeFactory,
        ILogger<InProcessIntegrationEventPublisher> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    public async Task PublishAsync<TEvent>(TEvent integrationEvent, CancellationToken ct = default)
        where TEvent : IIntegrationEvent
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);

        using var scope = _scopeFactory.CreateScope();
        var handlers = scope.ServiceProvider.GetServices<IIntegrationEventHandler<TEvent>>();

        foreach (var handler in handlers)
        {
            try
            {
                await handler.HandleAsync(integrationEvent, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                // Yan etki, onu tetikleyen işlemi bozmamalı: kullanıcı kaydı başarılıyken
                // hoş geldin maili kuyruğa alınamadı diye kayıt geri alınmaz.
                _logger.LogError(
                    ex,
                    "Integration event handler {Handler} failed for {EventType} {EventId}",
                    handler.GetType().Name,
                    typeof(TEvent).Name,
                    integrationEvent.EventId);
            }
        }
    }
}
