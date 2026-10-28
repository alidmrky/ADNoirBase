using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Templates;

public sealed record TemplateDto(
    Guid Id,
    string Code,
    string Name,
    string? Description,
    NotificationChannel Channel,
    NotificationCategory Category,
    bool IsSystem,
    bool IsLayout,
    bool IsActive,
    string? LayoutCode,
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    string? SmsBody,
    IReadOnlyList<TemplateVariableDto> Variables,
    NotificationLogPolicy LogPolicy,
    int RetentionDays,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
