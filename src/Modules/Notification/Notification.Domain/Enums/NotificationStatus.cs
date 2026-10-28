namespace Notification.Domain.Enums;

public enum NotificationStatus
{
    /// <summary>Kuyrukta, gönderilmeyi bekliyor.</summary>
    Pending = 1,

    /// <summary>Bir worker tarafından sahiplenildi.</summary>
    Processing = 2,

    Sent = 3,

    /// <summary>Tüm denemeler tükendi.</summary>
    Failed = 4,

    /// <summary>Kampanya iptal edildiği veya kullanıcı tarafından durdurulduğu için gönderilmedi.</summary>
    Cancelled = 5,

    /// <summary>Alıcının tercihleri izin vermediği ya da iletişim bilgisi olmadığı için atlandı.</summary>
    Skipped = 6
}
