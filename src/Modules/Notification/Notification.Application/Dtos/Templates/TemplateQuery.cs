using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Templates;

public sealed record TemplateQuery(
    int Page = 1,
    int PageSize = 20,
    NotificationChannel? Channel = null,
    bool? IsActive = null,
    string? Search = null);
