using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Application.Templating;

public sealed class NotificationComposer : INotificationComposer
{
    private readonly ITemplateRenderer _renderer;
    private readonly INotificationTemplateRepository _templateRepository;

    public NotificationComposer(
        ITemplateRenderer renderer,
        INotificationTemplateRepository templateRepository)
    {
        _renderer = renderer;
        _templateRepository = templateRepository;
    }

    public async Task<ComposedNotification> ComposeAsync(
        NotificationTemplate template,
        IReadOnlyDictionary<string, object?> model,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(template);

        var errors = new List<string>();

        // Konu ve düz metin HTML değildir; kaçışlamak "Ali & Veli" gibi değerleri bozar.
        var subject = Render(template.Subject, model, escapeHtml: false, "Subject", errors);
        var text = Render(template.TextBody, model, escapeHtml: false, "TextBody", errors);
        var sms = Render(template.SmsBody, model, escapeHtml: false, "SmsBody", errors);
        var html = Render(template.HtmlBody, model, escapeHtml: true, "HtmlBody", errors);

        if (html is not null && template.LayoutCode is not null)
        {
            html = await ApplyLayoutAsync(template.LayoutCode, html, subject, model, errors, ct);
        }

        return errors.Count > 0
            ? ComposedNotification.Failure(errors)
            : new ComposedNotification(subject, html, text, sms, []);
    }

    private async Task<string?> ApplyLayoutAsync(
        string layoutCode,
        string content,
        string? subject,
        IReadOnlyDictionary<string, object?> model,
        List<string> errors,
        CancellationToken ct)
    {
        var layout = await _templateRepository.FindByCodeAsync(layoutCode, ct);

        if (layout is null)
        {
            errors.Add($"Layout '{layoutCode}' bulunamadı.");
            return null;
        }

        if (!layout.IsLayout)
        {
            errors.Add($"'{layoutCode}' bir layout değil.");
            return null;
        }

        // İçerik zaten render edilip kaçışlandığı için layout'a ham HTML olarak geçer;
        // ikinci kez kaçışlanırsa etiketler ekranda metin olarak görünür.
        var layoutModel = new Dictionary<string, object?>(model)
        {
            [TemplateModelKeys.Content] = content,
            [TemplateModelKeys.Subject] = subject
        };

        var result = _renderer.Render(layout.HtmlBody, layoutModel, escapeHtml: false);

        if (!result.IsSuccess)
        {
            errors.AddRange(result.Errors.Select(e => $"Layout '{layoutCode}': {e}"));
            return null;
        }

        return result.Output;
    }

    private string? Render(
        string? template,
        IReadOnlyDictionary<string, object?> model,
        bool escapeHtml,
        string fieldName,
        List<string> errors)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return null;
        }

        var result = _renderer.Render(template, model, escapeHtml);

        if (!result.IsSuccess)
        {
            errors.AddRange(result.Errors.Select(e => $"{fieldName}: {e}"));
            return null;
        }

        return result.Output;
    }

    /// <summary>Kanalın zorunlu alanları doldurulmuş mu.</summary>
    public static bool HasRequiredContent(NotificationChannel channel, ComposedNotification composed) =>
        channel switch
        {
            NotificationChannel.Email =>
                !string.IsNullOrWhiteSpace(composed.Subject) &&
                (!string.IsNullOrWhiteSpace(composed.HtmlBody) || !string.IsNullOrWhiteSpace(composed.TextBody)),
            NotificationChannel.Sms => !string.IsNullOrWhiteSpace(composed.SmsBody),
            NotificationChannel.InApp =>
                !string.IsNullOrWhiteSpace(composed.Subject) && !string.IsNullOrWhiteSpace(composed.TextBody),
            _ => false
        };
}
