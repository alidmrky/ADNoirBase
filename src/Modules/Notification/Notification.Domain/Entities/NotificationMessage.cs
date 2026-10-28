using ADNoirBase.Shared.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Entities;

/// <summary>
/// Hem kuyruk kaydı hem gönderim kaydıdır. Worker bu dökümanı atomik olarak sahiplenir,
/// gönderir ve şablonun saklama politikasına göre içeriğini temizler.
/// </summary>
public sealed class NotificationMessage : FullAuditableEntity<Guid>
{
    public NotificationChannel Channel { get; private set; }

    public NotificationCategory Category { get; private set; }

    public NotificationPriority Priority { get; private set; } = NotificationPriority.Normal;

    public NotificationStatus Status { get; private set; } = NotificationStatus.Pending;

    // ─── Alıcılar ────────────────────────────────────────────────────────────

    /// <summary>E-postada adres, SMS'te telefon numarası, uygulama içi bildirimde kullanıcı kimliği.</summary>
    public IReadOnlyList<NotificationRecipient> To { get; private set; } = [];

    public IReadOnlyList<NotificationRecipient> Cc { get; private set; } = [];

    public IReadOnlyList<NotificationRecipient> Bcc { get; private set; } = [];

    public string? FromAddress { get; private set; }

    public string? FromDisplayName { get; private set; }

    public string? ReplyTo { get; private set; }

    // ─── İçerik ──────────────────────────────────────────────────────────────

    public string? Subject { get; private set; }

    public string? HtmlBody { get; private set; }

    public string? TextBody { get; private set; }

    /// <summary>Uygulama içi bildirimin tıklandığında gideceği adres.</summary>
    public string? Link { get; private set; }

    public IReadOnlyList<NotificationAttachmentRef> Attachments { get; private set; } = [];

    /// <summary>List-Unsubscribe gibi ek başlıklar.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; private set; } =
        new Dictionary<string, string>();

    // ─── Kuyruk durumu ───────────────────────────────────────────────────────

    public int Attempts { get; private set; }

    public DateTime NextAttemptAt { get; private set; } = DateTime.UtcNow;

    /// <summary>Sahiplenen worker'ın kilit süresi. Süre dolarsa mesaj tekrar alınabilir hale gelir.</summary>
    public DateTime? LockedUntil { get; private set; }

    public string? LockedBy { get; private set; }

    public string? LastError { get; private set; }

    public DateTime? SentAt { get; private set; }

    // ─── Bağlantılar ─────────────────────────────────────────────────────────

    public Guid? CampaignId { get; private set; }

    public string? TemplateCode { get; private set; }

    public NotificationLogPolicy LogPolicy { get; private set; } = NotificationLogPolicy.MetadataOnly;

    /// <summary>TTL index'i bu alana bakar; dolduğunda döküman MongoDB tarafından silinir.</summary>
    public DateTime? ExpiresAt { get; private set; }

    public string? CorrelationId { get; private set; }

    private NotificationMessage() { }

    public static NotificationMessage Create(
        NotificationChannel channel,
        NotificationCategory category,
        IReadOnlyList<NotificationRecipient> to)
    {
        if (to.Count == 0)
        {
            throw new ArgumentException("En az bir alıcı gerekir.", nameof(to));
        }

        return new NotificationMessage
        {
            Id = Guid.NewGuid(),
            Channel = channel,
            Category = category,
            To = to,
            Status = NotificationStatus.Pending,
            NextAttemptAt = DateTime.UtcNow
        };
    }

    public void SetEmailEnvelope(
        string? fromAddress,
        string? fromDisplayName,
        string? replyTo,
        IReadOnlyList<NotificationRecipient> cc,
        IReadOnlyList<NotificationRecipient> bcc)
    {
        FromAddress = fromAddress;
        FromDisplayName = fromDisplayName;
        ReplyTo = replyTo;
        Cc = cc;
        Bcc = bcc;
    }

    public void SetContent(string? subject, string? htmlBody, string? textBody, string? link = null)
    {
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        Link = link;
    }

    public void SetAttachments(IReadOnlyList<NotificationAttachmentRef> attachments) =>
        Attachments = attachments;

    public void SetHeaders(IReadOnlyDictionary<string, string> headers) => Headers = headers;

    public void SetDelivery(NotificationPriority priority, DateTime? sendAt)
    {
        Priority = priority;

        if (sendAt.HasValue)
        {
            NextAttemptAt = sendAt.Value;
        }
    }

    public void SetSource(
        Guid? campaignId,
        string? templateCode,
        NotificationLogPolicy logPolicy,
        DateTime? expiresAt,
        string? correlationId)
    {
        CampaignId = campaignId;
        TemplateCode = templateCode;
        LogPolicy = logPolicy;
        ExpiresAt = expiresAt;
        CorrelationId = correlationId;
    }

    // ─── Durum geçişleri ─────────────────────────────────────────────────────

    public void MarkProcessing(string workerId, DateTime lockedUntil)
    {
        Status = NotificationStatus.Processing;
        LockedBy = workerId;
        LockedUntil = lockedUntil;
        Attempts++;
    }

    public void MarkSent(DateTime sentAt)
    {
        Status = NotificationStatus.Sent;
        SentAt = sentAt;
        LockedUntil = null;
        LockedBy = null;
        LastError = null;
    }

    /// <summary>Yeniden denenecekse <paramref name="nextAttemptAt"/> verilir; null ise kalıcı hata sayılır.</summary>
    public void MarkFailed(string error, DateTime? nextAttemptAt)
    {
        LastError = Truncate(error);
        LockedUntil = null;
        LockedBy = null;

        if (nextAttemptAt.HasValue)
        {
            Status = NotificationStatus.Pending;
            NextAttemptAt = nextAttemptAt.Value;
        }
        else
        {
            Status = NotificationStatus.Failed;
        }
    }

    public void MarkSkipped(string reason)
    {
        Status = NotificationStatus.Skipped;
        LastError = Truncate(reason);
        LockedUntil = null;
        LockedBy = null;
    }

    public void Cancel()
    {
        Status = NotificationStatus.Cancelled;
        LockedUntil = null;
        LockedBy = null;
    }

    /// <summary>Başarısız bir mesajı yönetici isteğiyle kuyruğa geri koyar.</summary>
    public void Requeue()
    {
        Status = NotificationStatus.Pending;
        NextAttemptAt = DateTime.UtcNow;
        Attempts = 0;
        LastError = null;
        LockedUntil = null;
        LockedBy = null;
    }

    /// <summary>
    /// Saklama politikası MetadataOnly ise gövdeyi ve ekleri siler.
    /// Kayıt tamamen silinecekse (None) bu çağrı yapılmaz; repository dökümanı kaldırır.
    /// </summary>
    public void StripContent()
    {
        HtmlBody = null;
        TextBody = null;
        Attachments = [];
    }

    /// <summary>Hata metni bir dökümanı şişirecek kadar uzun olabilir (yığın izi, sunucu yanıtı).</summary>
    private static string Truncate(string value) =>
        value.Length <= 2000 ? value : value[..2000];
}
