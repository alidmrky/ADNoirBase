using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Delivery;

namespace Notification.Infrastructure.Email;

public sealed class SmtpEmailSender : IEmailSender
{
    private readonly ILogger<SmtpEmailSender> _logger;
    private readonly EmailSettings _email;

    public SmtpEmailSender(IOptions<NotificationSettings> settings, ILogger<SmtpEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _logger = logger;
        _email = settings.Value.Email;
    }

    public async Task<SendResult> SendAsync(OutgoingEmail email, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        var smtp = _email.Smtp;

        using var message = MimeMessageFactory.Create(email);
        using var client = new SmtpClient { Timeout = smtp.TimeoutSeconds * 1000 };

        try
        {
            await client.ConnectAsync(smtp.Host, smtp.Port, ToSecureSocketOptions(smtp.Security), ct);

            if (!string.IsNullOrWhiteSpace(smtp.UserName))
            {
                await client.AuthenticateAsync(smtp.UserName, smtp.Password ?? string.Empty, ct);
            }

            await client.SendAsync(message, ct);
            await client.DisconnectAsync(quit: true, ct);

            return SendResult.Success();
        }
        catch (SmtpCommandException ex)
        {
            // 5xx sunucunun kesin reddidir — adres yok, alan adı yanlış. Tekrar denemek
            // yalnızca gönderen itibarını yıpratır.
            var permanent = ex.StatusCode >= SmtpStatusCode.CommandUnrecognized;

            _logger.LogWarning(
                ex, "SMTP {StatusCode} yanıtı: {MessageId}", ex.StatusCode, email.MessageId);

            return permanent
                ? SendResult.Permanent($"SMTP {ex.StatusCode}: {ex.Message}")
                : SendResult.Transient($"SMTP {ex.StatusCode}: {ex.Message}");
        }
        catch (SmtpProtocolException ex)
        {
            return SendResult.Transient($"SMTP protokol hatası: {ex.Message}");
        }
        catch (AuthenticationException ex)
        {
            // Kimlik bilgisi yanlışsa her denemede yanlış olacaktır.
            return SendResult.Permanent($"SMTP kimlik doğrulama hatası: {ex.Message}");
        }
        catch (IOException ex)
        {
            return SendResult.Transient($"SMTP bağlantı hatası: {ex.Message}");
        }
    }

    private static SecureSocketOptions ToSecureSocketOptions(SmtpSecurity security) =>
        security switch
        {
            SmtpSecurity.None => SecureSocketOptions.None,
            SmtpSecurity.StartTls => SecureSocketOptions.StartTls,
            SmtpSecurity.SslOnConnect => SecureSocketOptions.SslOnConnect,
            _ => SecureSocketOptions.Auto
        };
}
