using ADNoirBase.Shared.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Domain.Entities;

/// <summary>
/// Kullanıcının bildirim tercihleri. Kayıt yoksa varsayılan "her şey açık" kabul edilir,
/// böylece mevcut kullanıcılar için toplu kayıt üretmek gerekmez.
/// </summary>
public sealed class NotificationPreference : FullAuditableEntity<Guid>
{
    public Guid UserId { get; private set; }

    public bool EmailMarketing { get; private set; } = true;

    public bool SmsMarketing { get; private set; } = true;

    public bool InAppMarketing { get; private set; } = true;

    /// <summary>
    /// Abonelikten çıkma linkindeki token'ın SHA-256 özeti. Token'ın kendisi saklanmaz;
    /// veritabanı sızarsa linkler kullanılamaz olmalıdır.
    /// </summary>
    public string? UnsubscribeTokenHash { get; private set; }

    public DateTime? UnsubscribedAt { get; private set; }

    private NotificationPreference() { }

    public static NotificationPreference CreateDefault(Guid userId) =>
        new()
        {
            Id = Guid.NewGuid(),
            UserId = userId
        };

    public void Update(bool emailMarketing, bool smsMarketing, bool inAppMarketing)
    {
        EmailMarketing = emailMarketing;
        SmsMarketing = smsMarketing;
        InAppMarketing = inAppMarketing;

        if (emailMarketing && smsMarketing && inAppMarketing)
        {
            UnsubscribedAt = null;
        }
    }

    public void SetUnsubscribeTokenHash(string tokenHash) => UnsubscribeTokenHash = tokenHash;

    /// <summary>Abonelikten çıkma linki yalnızca pazarlama iletişimini kapatır.</summary>
    public void UnsubscribeFromMarketing(DateTime unsubscribedAt)
    {
        EmailMarketing = false;
        SmsMarketing = false;
        InAppMarketing = false;
        UnsubscribedAt = unsubscribedAt;
    }

    /// <summary>
    /// Kategorisi Marketing olmayan bildirimler tercihlerden etkilenmez:
    /// parola değişikliği uyarısı kapatılabilir olmamalıdır.
    /// </summary>
    public bool Allows(NotificationChannel channel, NotificationCategory category)
    {
        if (category != NotificationCategory.Marketing)
        {
            return true;
        }

        return channel switch
        {
            NotificationChannel.Email => EmailMarketing,
            NotificationChannel.Sms => SmsMarketing,
            NotificationChannel.InApp => InAppMarketing,
            _ => true
        };
    }
}
