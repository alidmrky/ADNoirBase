using ADNoirBase.Shared.Application.Interfaces;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Preferences;
using Notification.Application.Templating;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Dispatching;

/// <summary>
/// Bir istek tam olarak bir e-posta zarfına karşılık gelir: To/Cc/Bcc aynı mesajda taşınır.
/// Alıcıya göre kişiselleştirilmiş toplu gönderim, kampanya worker'ının alıcı başına
/// ayrı istek üretmesiyle yapılır.
/// </summary>
public sealed class NotificationDispatcher : INotificationDispatcher
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationMessageRepository _messageRepository;
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly INotificationComposer _composer;
    private readonly IUserDirectory _userDirectory;
    private readonly IUnsubscribeLinkFactory _unsubscribeLinkFactory;
    private readonly NotificationSettings _settings;

    public NotificationDispatcher(
        INotificationTemplateRepository templateRepository,
        INotificationMessageRepository messageRepository,
        INotificationPreferenceRepository preferenceRepository,
        INotificationComposer composer,
        IUserDirectory userDirectory,
        IUnsubscribeLinkFactory unsubscribeLinkFactory,
        IOptions<NotificationSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _templateRepository = templateRepository;
        _messageRepository = messageRepository;
        _preferenceRepository = preferenceRepository;
        _composer = composer;
        _userDirectory = userDirectory;
        _unsubscribeLinkFactory = unsubscribeLinkFactory;
        _settings = settings.Value;
    }

    public async Task<EnqueueResult> EnqueueAsync(
        NotificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.To.Count == 0)
        {
            return EnqueueResult.Failure("En az bir alıcı gerekir.");
        }

        var template = await ResolveTemplateAsync(request, ct);

        if (template is null && request.TemplateCode is not null)
        {
            return EnqueueResult.Failure($"Şablon '{request.TemplateCode}' bulunamadı veya kullanılamaz.");
        }

        var (to, cc, bcc, skipped) = await FilterByPreferencesAsync(request, ct);

        if (to.Count == 0)
        {
            return new EnqueueResult([], skipped, []);
        }

        var model = await BuildModelAsync(request, to, ct);
        var source = template ?? BuildInlineTemplate(request);
        var composed = await _composer.ComposeAsync(source, model, ct);

        if (!composed.IsSuccess)
        {
            return new EnqueueResult([], skipped, composed.Errors);
        }

        if (!NotificationComposer.HasRequiredContent(request.Channel, composed))
        {
            return new EnqueueResult([], skipped, ["Kanalın gerektirdiği içerik alanları boş."]);
        }

        var message = BuildMessage(request, to, cc, bcc, composed, template, model);
        await _messageRepository.InsertAsync(message, ct);

        return new EnqueueResult([message.Id], skipped, []);
    }

    private async Task<NotificationTemplate?> ResolveTemplateAsync(
        NotificationRequest request, CancellationToken ct)
    {
        if (request.TemplateCode is null)
        {
            return null;
        }

        var template = await _templateRepository.FindByCodeAsync(request.TemplateCode, ct);

        if (template is null || !template.IsActive || template.IsLayout ||
            template.Channel != request.Channel)
        {
            return null;
        }

        return template;
    }

    /// <summary>
    /// Şablonsuz gönderimde de Scriban çalışsın diye kalıcı olmayan bir şablon kurulur;
    /// böylece render ve kaçışlama kuralları tek yerde kalır.
    /// </summary>
    private static NotificationTemplate BuildInlineTemplate(NotificationRequest request)
    {
        var inline = NotificationTemplate.Create(
            "inline", "inline", request.Channel, request.Category);

        inline.UpdateContent(request.Subject, request.HtmlBody, request.TextBody, request.SmsBody);

        return inline;
    }

    private async Task<(
        List<NotificationRecipient> To,
        List<NotificationRecipient> Cc,
        List<NotificationRecipient> Bcc,
        List<string> Skipped)> FilterByPreferencesAsync(
        NotificationRequest request, CancellationToken ct)
    {
        var skipped = new List<string>();

        // İşlemsel bildirimler tercihlerden etkilenmez: parola değişikliği uyarısı kapatılamaz.
        if (request.BypassPreferences || request.Category != NotificationCategory.Marketing)
        {
            return ([.. request.To], [.. request.Cc], [.. request.Bcc], skipped);
        }

        var to = await FilterAsync(request.To, request.Channel, skipped, ct);
        var cc = await FilterAsync(request.Cc, request.Channel, skipped, ct);
        var bcc = await FilterAsync(request.Bcc, request.Channel, skipped, ct);

        return (to, cc, bcc, skipped);
    }

    private async Task<List<NotificationRecipient>> FilterAsync(
        IReadOnlyList<NotificationRecipient> recipients,
        NotificationChannel channel,
        List<string> skipped,
        CancellationToken ct)
    {
        var allowed = new List<NotificationRecipient>(recipients.Count);

        foreach (var recipient in recipients)
        {
            // Sistemde kaydı olmayan adreslerin tercih kaydı da olamaz; elle girilen
            // adresler yöneticinin açık tercihidir ve filtrelenmez.
            if (recipient.UserId is not { } userId)
            {
                allowed.Add(recipient);
                continue;
            }

            var preference = await _preferenceRepository.FindByUserIdAsync(userId, ct);

            if (preference is null || preference.Allows(channel, NotificationCategory.Marketing))
            {
                allowed.Add(recipient);
            }
            else
            {
                skipped.Add(recipient.Address);
            }
        }

        return allowed;
    }

    private async Task<Dictionary<string, object?>> BuildModelAsync(
        NotificationRequest request,
        List<NotificationRecipient> to,
        CancellationToken ct)
    {
        var model = TemplateModelBuilder.BuildBase(_settings);

        // Kişiselleştirme yalnızca tek alıcılı gönderimde anlamlıdır; çok alıcılı tek bir
        // zarfta "{{ full_name }}" kimin adı olacağı belirsizdir ve boş bırakılır.
        if (to.Count == 1 && to[0].UserId is { } userId)
        {
            var user = await _userDirectory.FindByIdAsync(userId, ct);

            if (user is not null)
            {
                TemplateModelBuilder.ApplyUser(model, user);
            }

            if (request.Category == NotificationCategory.Marketing)
            {
                var link = await _unsubscribeLinkFactory.CreateLinkAsync(userId, ct);

                if (link is not null)
                {
                    model[TemplateModelKeys.UnsubscribeUrl] = link;
                }
            }
        }

        TemplateModelBuilder.ApplyCustom(model, request.Model);

        return model;
    }

    private NotificationMessage BuildMessage(
        NotificationRequest request,
        IReadOnlyList<NotificationRecipient> to,
        IReadOnlyList<NotificationRecipient> cc,
        IReadOnlyList<NotificationRecipient> bcc,
        ComposedNotification composed,
        NotificationTemplate? template,
        IReadOnlyDictionary<string, object?> model)
    {
        var message = NotificationMessage.Create(request.Channel, request.Category, to);

        if (request.Channel == NotificationChannel.Email)
        {
            message.SetEmailEnvelope(
                _settings.Email.FromAddress,
                _settings.Email.FromDisplayName,
                request.ReplyTo ?? _settings.Email.ReplyTo,
                cc,
                bcc);
        }

        // SMS gövdesi kanalın tek içeriğidir; mesajda TextBody alanında taşınır.
        var textBody = request.Channel == NotificationChannel.Sms ? composed.SmsBody : composed.TextBody;

        message.SetContent(composed.Subject, composed.HtmlBody, textBody, request.Link);
        message.SetAttachments(request.Attachments);
        message.SetHeaders(BuildHeaders(request, model));
        message.SetDelivery(request.Priority, request.SendAt);

        var logPolicy = template?.LogPolicy ?? NotificationLogPolicy.MetadataOnly;

        message.SetSource(
            request.CampaignId,
            template?.Code,
            logPolicy,
            template?.CalculateExpiry(DateTime.UtcNow),
            correlationId: null);

        return message;
    }

    /// <summary>
    /// RFC 8058 List-Unsubscribe başlığı. Posta istemcileri bu başlığı gördüğünde
    /// kendi "abonelikten çık" düğmesini gösterir; başlık yoksa kullanıcı spam tuşuna basar.
    /// </summary>
    private static Dictionary<string, string> BuildHeaders(
        NotificationRequest request, IReadOnlyDictionary<string, object?> model)
    {
        var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (request.Category != NotificationCategory.Marketing ||
            request.Channel != NotificationChannel.Email)
        {
            return headers;
        }

        if (model.TryGetValue(TemplateModelKeys.UnsubscribeUrl, out var value) &&
            value is string url && !string.IsNullOrWhiteSpace(url))
        {
            headers["List-Unsubscribe"] = $"<{url}>";
            headers["List-Unsubscribe-Post"] = "List-Unsubscribe=One-Click";
        }

        return headers;
    }
}
