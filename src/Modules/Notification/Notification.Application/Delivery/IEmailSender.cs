namespace Notification.Application.Delivery;

/// <summary>
/// E-posta taşıyıcısı. Sağlayıcı seçimi yapılandırmadan gelir; geliştirme ortamında
/// SMTP kapalıyken mailler günlüğe veya diske yazılır.
/// </summary>
public interface IEmailSender
{
    Task<SendResult> SendAsync(OutgoingEmail email, CancellationToken ct = default);
}
