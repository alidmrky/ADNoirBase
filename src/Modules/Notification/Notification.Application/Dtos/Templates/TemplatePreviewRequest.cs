namespace Notification.Application.Dtos.Templates;

/// <summary>
/// İçerik istekle birlikte gönderilir; böylece henüz kaydedilmemiş düzenlemeler de önizlenebilir.
/// </summary>
public sealed record TemplatePreviewRequest(
    string? LayoutCode,
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    string? SmsBody,
    IReadOnlyDictionary<string, string>? SampleValues = null);
