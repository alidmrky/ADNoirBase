using Notification.Domain.Entities;

namespace Notification.Application.Delivery;

public interface INotificationDeliveryService
{
    /// <summary>
    /// Sahiplenilmiş bir mesajı gönderir ve sonucuna göre durumunu günceller.
    /// İstisna fırlatmaz: worker döngüsü tek bir bozuk mesaj yüzünden durmamalıdır.
    /// </summary>
    Task ProcessAsync(NotificationMessage message, CancellationToken ct = default);
}
