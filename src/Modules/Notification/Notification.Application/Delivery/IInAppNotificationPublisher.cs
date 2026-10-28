namespace Notification.Application.Delivery;

/// <summary>
/// Uygulama içi bildirimi açık istemcilere anlık iletir. Kayıt zaten veritabanına yazıldığı
/// için bu çağrının başarısız olması bildirimi kaybettirmez, yalnızca sayfa yenilenene kadar geciktirir.
/// </summary>
public interface IInAppNotificationPublisher
{
    Task PublishAsync(Guid userId, InAppNotificationPayload payload, CancellationToken ct = default);
}
