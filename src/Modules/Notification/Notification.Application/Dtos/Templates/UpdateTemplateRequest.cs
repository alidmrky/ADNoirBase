using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Templates;

/// <summary>
/// Kod ve kanal güncellenemez: koddan referans verilen şablonun anahtarı değişirse
/// gönderim sessizce çalışmaz hale gelir.
/// </summary>
public sealed record UpdateTemplateRequest(
    string Name,
    string? Description,
    NotificationCategory Category,
    bool IsActive,
    string? LayoutCode,
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    string? SmsBody,
    IReadOnlyList<TemplateVariableDto>? Variables = null,
    NotificationLogPolicy LogPolicy = NotificationLogPolicy.MetadataOnly,
    int RetentionDays = 90);
