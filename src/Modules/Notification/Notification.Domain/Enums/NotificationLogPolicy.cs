namespace Notification.Domain.Enums;

/// <summary>Gönderim tamamlandıktan sonra kaydın ne kadarının saklanacağı.</summary>
public enum NotificationLogPolicy
{
    /// <summary>İçerik dahil her şey saklanır. Kişisel veri barındırabileceği için saklama süresi kısa tutulmalıdır.</summary>
    Full = 1,

    /// <summary>Alıcı, durum ve zaman saklanır; gövde ve ekler silinir.</summary>
    MetadataOnly = 2,

    /// <summary>Gönderim başarılı olduğu anda kayıt tamamen silinir.</summary>
    None = 3
}
