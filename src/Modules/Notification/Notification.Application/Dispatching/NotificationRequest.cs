using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Dispatching;

/// <summary>
/// Kuyruğa alınacak bildirim. İçerik ya <see cref="TemplateCode"/> ile şablondan gelir
/// ya da doğrudan <see cref="Subject"/>/<see cref="HtmlBody"/> alanlarıyla verilir.
/// </summary>
public sealed record NotificationRequest
{
    public required NotificationChannel Channel { get; init; }

    public NotificationCategory Category { get; init; } = NotificationCategory.Transactional;

    public required IReadOnlyList<NotificationRecipient> To { get; init; }

    public IReadOnlyList<NotificationRecipient> Cc { get; init; } = [];

    public IReadOnlyList<NotificationRecipient> Bcc { get; init; } = [];

    public string? ReplyTo { get; init; }

    /// <summary>Doluysa içerik bu şablondan render edilir ve gövde alanları yok sayılır.</summary>
    public string? TemplateCode { get; init; }

    /// <summary>Şablona geçilecek değişkenler. Ortak değişkenler dispatcher tarafından eklenir.</summary>
    public IReadOnlyDictionary<string, object?> Model { get; init; } =
        new Dictionary<string, object?>();

    public string? Subject { get; init; }

    public string? HtmlBody { get; init; }

    public string? TextBody { get; init; }

    public string? SmsBody { get; init; }

    /// <summary>Uygulama içi bildirimin tıklandığında gideceği adres.</summary>
    public string? Link { get; init; }

    public IReadOnlyList<NotificationAttachmentRef> Attachments { get; init; } = [];

    public NotificationPriority Priority { get; init; } = NotificationPriority.Normal;

    /// <summary>İleri tarihli gönderim. Null ise ilk turda işlenir.</summary>
    public DateTime? SendAt { get; init; }

    public Guid? CampaignId { get; init; }

    /// <summary>
    /// Tercih kontrolünü atlar. Yalnızca yöneticinin kendisine attığı test gönderimlerinde
    /// kullanılır; toplu gönderimde kullanılırsa abonelikten çıkma anlamsızlaşır.
    /// </summary>
    public bool BypassPreferences { get; init; }
}
