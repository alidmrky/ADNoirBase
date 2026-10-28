namespace Notification.Application.Dtos.Templates;

public sealed record TemplatePreviewResponse(
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    string? SmsBody,
    IReadOnlyList<string> Errors);
