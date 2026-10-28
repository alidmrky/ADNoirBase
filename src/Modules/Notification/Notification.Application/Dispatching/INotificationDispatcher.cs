namespace Notification.Application.Dispatching;

public interface INotificationDispatcher
{
    /// <summary>
    /// Bildirimi doğrular, şablonu render eder, tercihleri kontrol eder ve kuyruğa yazar.
    /// Gönderimin kendisi arka plan worker'ının işidir; bu çağrı ağ erişimi yapmaz.
    /// </summary>
    Task<EnqueueResult> EnqueueAsync(NotificationRequest request, CancellationToken ct = default);
}
