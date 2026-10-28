using System.Globalization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Delivery;

namespace Notification.Infrastructure.Email;

/// <summary>
/// Maili diske <c>.eml</c> olarak yazar. Günlük kaydının aksine gerçek MIME çıktısını üretir,
/// böylece şablonun bir posta istemcisinde nasıl göründüğü SMTP kurmadan görülebilir.
/// </summary>
public sealed class PickupDirectoryEmailSender : IEmailSender
{
    private readonly ILogger<PickupDirectoryEmailSender> _logger;
    private readonly string _directory;

    public PickupDirectoryEmailSender(
        IOptions<NotificationSettings> settings,
        ILogger<PickupDirectoryEmailSender> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _logger = logger;
        _directory = Path.GetFullPath(settings.Value.Email.PickupDirectory);
    }

    public async Task<SendResult> SendAsync(OutgoingEmail email, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(email);

        Directory.CreateDirectory(_directory);

        var fileName = string.Create(
            CultureInfo.InvariantCulture,
            $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{email.MessageId:N}.eml");

        var path = Path.Combine(_directory, fileName);

        using var message = MimeMessageFactory.Create(email);
        await using var stream = File.Create(path);
        await message.WriteToAsync(stream, ct);

        _logger.LogInformation("[E-POSTA] {MessageId} dosyaya yazıldı: {Path}", email.MessageId, path);

        return SendResult.Success();
    }
}
