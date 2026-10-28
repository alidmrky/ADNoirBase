using Microsoft.Extensions.Logging;
using Notification.Application.Delivery;

namespace Notification.Infrastructure.Email;

/// <summary>
/// Geliştirme sağlayıcısı: maili göndermek yerine günlüğe yazar. SMTP kapalıyken
/// uygulamanın tüm akışı çalışmaya devam eder.
/// </summary>
public sealed class LogEmailSender : IEmailSender
{
    private readonly ILogger<LogEmailSender> _logger;

    public LogEmailSender(ILogger<LogEmailSender> logger) => _logger = logger;

    public Task<SendResult> SendAsync(OutgoingEmail email, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        _logger.LogInformation(
            "[E-POSTA] {MessageId} | Kime: {To} | Cc: {Cc} | Konu: {Subject} | Ek: {AttachmentCount}",
            email.MessageId,
            string.Join(", ", email.To.Select(r => r.Address)),
            string.Join(", ", email.Cc.Select(r => r.Address)),
            email.Subject,
            email.Attachments.Count);

        return Task.FromResult(SendResult.Success());
    }
}
