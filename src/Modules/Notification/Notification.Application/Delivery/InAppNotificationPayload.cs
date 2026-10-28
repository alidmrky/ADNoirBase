namespace Notification.Application.Delivery;

/// <summary>Canlı bağlantı üzerinden tarayıcıya gönderilen uygulama içi bildirim.</summary>
public sealed record InAppNotificationPayload(
    Guid Id,
    string Title,
    string Body,
    string? Link,
    DateTime CreatedAt,
    int UnreadCount);
