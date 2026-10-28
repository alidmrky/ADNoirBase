using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Delivery;

public sealed class NotificationDeliveryService : INotificationDeliveryService
{
    private readonly IEmailSender _emailSender;
    private readonly ISmsSender _smsSender;
    private readonly IAttachmentStore _attachmentStore;
    private readonly IInAppNotificationPublisher _inAppPublisher;
    private readonly IInAppNotificationRepository _inAppRepository;
    private readonly INotificationMessageRepository _messageRepository;
    private readonly ILogger<NotificationDeliveryService> _logger;
    private readonly NotificationSettings _settings;

    public NotificationDeliveryService(
        IEmailSender emailSender,
        ISmsSender smsSender,
        IAttachmentStore attachmentStore,
        IInAppNotificationPublisher inAppPublisher,
        IInAppNotificationRepository inAppRepository,
        INotificationMessageRepository messageRepository,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationDeliveryService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _emailSender = emailSender;
        _smsSender = smsSender;
        _attachmentStore = attachmentStore;
        _inAppPublisher = inAppPublisher;
        _inAppRepository = inAppRepository;
        _messageRepository = messageRepository;
        _logger = logger;
        _settings = settings.Value;
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Tek bir mesajın beklenmeyen hatası worker döngüsünü durdurmamalı; " +
                        "hata mesaja yazılır ve yeniden deneme zamanlanır.")]
    public async Task ProcessAsync(NotificationMessage message, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        SendResult result;

        try
        {
            result = await SendAsync(message, ct);
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor. Mesaj Processing durumunda kalır ve kilit süresi
            // dolduğunda başka bir worker tarafından alınır.
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirim {MessageId} gönderilirken beklenmeyen hata oluştu.", message.Id);
            result = SendResult.Transient(ex.Message);
        }

        if (result.IsSuccess)
        {
            await CompleteAsync(message, ct);
        }
        else
        {
            await FailAsync(message, result, ct);
        }
    }

    private Task<SendResult> SendAsync(NotificationMessage message, CancellationToken ct) =>
        message.Channel switch
        {
            NotificationChannel.Email => SendEmailAsync(message, ct),
            NotificationChannel.Sms => SendSmsAsync(message, ct),
            NotificationChannel.InApp => SendInAppAsync(message, ct),
            _ => Task.FromResult(SendResult.Permanent($"Desteklenmeyen kanal: {message.Channel}."))
        };

    private async Task<SendResult> SendEmailAsync(NotificationMessage message, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(message.Subject))
        {
            return SendResult.Permanent("E-posta konusu boş.");
        }

        var email = new OutgoingEmail
        {
            MessageId = message.Id,
            FromAddress = message.FromAddress ?? _settings.Email.FromAddress,
            FromDisplayName = message.FromDisplayName ?? _settings.Email.FromDisplayName,
            ReplyTo = message.ReplyTo,
            To = message.To,
            Cc = message.Cc,
            Bcc = message.Bcc,
            Subject = message.Subject,
            HtmlBody = message.HtmlBody,
            TextBody = message.TextBody,
            Attachments = await LoadAttachmentsAsync(message.Attachments, ct),
            Headers = message.Headers
        };

        return await _emailSender.SendAsync(email, ct);
    }

    private async Task<SendResult> SendSmsAsync(NotificationMessage message, CancellationToken ct)
    {
        var recipient = message.To.Count > 0 ? message.To[0] : null;

        if (recipient is null || string.IsNullOrWhiteSpace(recipient.Address))
        {
            return SendResult.Permanent("SMS alıcısı yok.");
        }

        if (string.IsNullOrWhiteSpace(message.TextBody))
        {
            return SendResult.Permanent("SMS gövdesi boş.");
        }

        return await _smsSender.SendAsync(
            new OutgoingSms
            {
                MessageId = message.Id,
                PhoneNumber = recipient.Address,
                Body = message.TextBody
            },
            ct);
    }

    private async Task<SendResult> SendInAppAsync(NotificationMessage message, CancellationToken ct)
    {
        var recipient = message.To.Count > 0 ? message.To[0] : null;

        if (recipient?.UserId is not { } userId)
        {
            return SendResult.Permanent("Uygulama içi bildirim için kullanıcı kimliği gerekir.");
        }

        var notification = InAppNotification.Create(
            userId,
            message.Subject ?? string.Empty,
            message.TextBody ?? string.Empty,
            message.Link,
            message.ExpiresAt);

        await _inAppRepository.InsertAsync(notification, ct);

        var unreadCount = await _inAppRepository.CountUnreadAsync(userId, ct);

        await _inAppPublisher.PublishAsync(
            userId,
            new InAppNotificationPayload(
                notification.Id,
                notification.Title,
                notification.Body,
                notification.Link,
                notification.CreatedAt,
                unreadCount),
            ct);

        return SendResult.Success();
    }

    private async Task<IReadOnlyList<EmailAttachment>> LoadAttachmentsAsync(
        IReadOnlyList<NotificationAttachmentRef> references, CancellationToken ct)
    {
        if (references.Count == 0)
        {
            return [];
        }

        var attachments = new List<EmailAttachment>(references.Count);

        foreach (var reference in references)
        {
            var content = await _attachmentStore.ReadAllBytesAsync(reference.FileId, ct);

            if (content is null)
            {
                // Ek silinmişse mail eksik gider ama gider; tüm gönderimi düşürmek
                // kullanıcının asıl bildirimi almasını engellerdi.
                _logger.LogWarning(
                    "Ek {FileId} bulunamadığı için {FileName} mesaja eklenmedi.",
                    reference.FileId, reference.FileName);
                continue;
            }

            attachments.Add(new EmailAttachment(
                reference.FileName,
                reference.ContentType,
                content,
                reference.IsInline,
                reference.ContentId));
        }

        return attachments;
    }

    /// <summary>Başarılı gönderimden sonra şablonun saklama politikasını uygular.</summary>
    private async Task CompleteAsync(NotificationMessage message, CancellationToken ct)
    {
        message.MarkSent(DateTime.UtcNow);

        switch (message.LogPolicy)
        {
            case NotificationLogPolicy.None:
                await _messageRepository.DeleteAsync(message, ct);
                break;

            case NotificationLogPolicy.MetadataOnly:
                message.StripContent();
                await _messageRepository.UpdateAsync(message, ct);
                break;

            case NotificationLogPolicy.Full:
            default:
                await _messageRepository.UpdateAsync(message, ct);
                break;
        }
    }

    private async Task FailAsync(NotificationMessage message, SendResult result, CancellationToken ct)
    {
        var error = result.Error ?? "Bilinmeyen hata.";
        var exhausted = message.Attempts >= _settings.Delivery.MaxAttempts;

        if (result.IsPermanent || exhausted)
        {
            message.MarkFailed(error, nextAttemptAt: null);

            _logger.LogWarning(
                "Bildirim {MessageId} kalıcı olarak başarısız oldu ({Attempts} deneme): {Error}",
                message.Id, message.Attempts, error);
        }
        else
        {
            var delay = CalculateBackoff(message.Attempts);
            message.MarkFailed(error, DateTime.UtcNow.Add(delay));

            _logger.LogInformation(
                "Bildirim {MessageId} başarısız oldu, {Delay} sonra yeniden denenecek: {Error}",
                message.Id, delay, error);
        }

        await _messageRepository.UpdateAsync(message, ct);
    }

    /// <summary>
    /// Üstel geri çekilme. Sağlayıcı geçici olarak hız sınırı uyguladığında aynı anda
    /// tekrar denemek durumu kötüleştirir.
    /// </summary>
    private TimeSpan CalculateBackoff(int attempts)
    {
        var exponent = Math.Min(attempts - 1, 16);
        var seconds = _settings.Delivery.RetryBaseSeconds * Math.Pow(2, exponent);

        return TimeSpan.FromSeconds(Math.Min(seconds, _settings.Delivery.RetryMaxSeconds));
    }
}
