namespace Notification.Application.Delivery;

/// <summary>
/// Canlı bağlantı katmanı kurulu değilken kullanılan varsayılan. API, SignalR tabanlı
/// implementasyonu kaydederek bunun yerini alır.
/// </summary>
public sealed class NullInAppNotificationPublisher : IInAppNotificationPublisher
{
    public Task PublishAsync(
        Guid userId, InAppNotificationPayload payload, CancellationToken ct = default) =>
        Task.CompletedTask;
}
