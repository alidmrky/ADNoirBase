using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Templates;

public sealed record TemplateListDto(
    Guid Id,
    string Code,
    string Name,
    NotificationChannel Channel,
    NotificationCategory Category,
    bool IsSystem,
    bool IsLayout,
    bool IsActive,
    DateTime CreatedAt,
    DateTime? UpdatedAt);
