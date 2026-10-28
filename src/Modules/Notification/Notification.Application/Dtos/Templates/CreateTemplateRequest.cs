using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Templates;

public sealed record CreateTemplateRequest(
    string Code,
    string Name,
    string? Description,
    NotificationChannel Channel,
    NotificationCategory Category,
    bool IsLayout,
    string? LayoutCode,
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    string? SmsBody,
    IReadOnlyList<TemplateVariableDto>? Variables = null,
    NotificationLogPolicy LogPolicy = NotificationLogPolicy.MetadataOnly,
    int RetentionDays = 90);
