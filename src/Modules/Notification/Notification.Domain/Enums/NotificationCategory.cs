namespace Notification.Domain.Enums;

/// <summary>
/// Kullanıcı tercihlerinin hangi bildirimlere uygulanacağını belirler.
/// Yalnızca <see cref="Marketing"/> kapatılabilir; diğerleri hesabın işleyişine ait olduğu için
/// abonelikten çıkma kapsamı dışındadır.
/// </summary>
public enum NotificationCategory
{
    /// <summary>Kullanıcının bir eylemine karşılık gelen bildirim: parola değişikliği, sipariş onayı.</summary>
    Transactional = 1,

    /// <summary>Kampanya, duyuru, bülten. Kullanıcı kapatabilir.</summary>
    Marketing = 2,

    /// <summary>Operasyonel uyarı: bakım bildirimi, güvenlik uyarısı.</summary>
    System = 3
}
