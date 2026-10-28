using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Dtos;

public sealed record SendNotificationRequest
{
    public NotificationChannel Channel { get; init; } = NotificationChannel.Email;
    public NotificationCategory Category { get; init; } = NotificationCategory.Transactional;
    public IReadOnlyList<NotificationRecipient> To { get; init; } = [];
    public IReadOnlyList<NotificationRecipient> Cc { get; init; } = [];
    public IReadOnlyList<NotificationRecipient> Bcc { get; init; } = [];
    public string? TemplateCode { get; init; }
    public string? Subject { get; init; }
    public string? HtmlBody { get; init; }
    public string? TextBody { get; init; }
    public string? SmsBody { get; init; }
    public string? Link { get; init; }
    public IReadOnlyList<string> AttachmentIds { get; init; } = [];
    public IReadOnlyDictionary<string, string> Model { get; init; } = new Dictionary<string, string>();
}
