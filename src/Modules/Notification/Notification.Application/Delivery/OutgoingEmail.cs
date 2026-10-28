using Notification.Domain.ValueObjects;

namespace Notification.Application.Delivery;

public sealed record OutgoingEmail
{
    public required string FromAddress { get; init; }

    public string? FromDisplayName { get; init; }

    public string? ReplyTo { get; init; }

    public required IReadOnlyList<NotificationRecipient> To { get; init; }

    public IReadOnlyList<NotificationRecipient> Cc { get; init; } = [];

    public IReadOnlyList<NotificationRecipient> Bcc { get; init; } = [];

    public required string Subject { get; init; }

    public string? HtmlBody { get; init; }

    public string? TextBody { get; init; }

    public IReadOnlyList<EmailAttachment> Attachments { get; init; } = [];

    /// <summary>List-Unsubscribe gibi ek başlıklar.</summary>
    public IReadOnlyDictionary<string, string> Headers { get; init; } =
        new Dictionary<string, string>();

    /// <summary>Günlüklerde ve .eml dosya adında kullanılır.</summary>
    public required Guid MessageId { get; init; }
}
