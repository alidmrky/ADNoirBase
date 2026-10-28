using Microsoft.Extensions.Logging;
using Notification.Application.Delivery;

namespace Notification.Infrastructure.Sms;

/// <summary>
/// Geliştirme sağlayıcısı: SMS'i günlüğe yazar. Gerçek bir sağlayıcı eklendiğinde
/// yalnızca <see cref="ISmsSender"/> implementasyonu değişir, çağıran kod aynı kalır.
/// </summary>
public sealed class LogSmsSender : ISmsSender
{
    private readonly ILogger<LogSmsSender> _logger;

    public LogSmsSender(ILogger<LogSmsSender> logger) => _logger = logger;

    public Task<SendResult> SendAsync(OutgoingSms sms, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(sms);

        _logger.LogInformation(
            "[SMS] {MessageId} | Kime: {PhoneNumber} | Gövde: {Body}",
            sms.MessageId, sms.PhoneNumber, sms.Body);

        return Task.FromResult(SendResult.Success());
    }
}
