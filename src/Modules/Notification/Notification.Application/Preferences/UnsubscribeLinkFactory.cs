using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Application.Preferences;

/// <summary>
/// Token kullanıcı kimliğinden HMAC ile türetilir, veritabanında yalnızca SHA-256 özeti durur.
/// Türetilmiş olması linkin her mailde aynı kalmasını sağlar; özet olarak saklanması ise
/// veritabanı sızsa bile linklerin kullanılamaz olmasını sağlar.
/// </summary>
public sealed class UnsubscribeLinkFactory : IUnsubscribeLinkFactory
{
    private readonly INotificationPreferenceRepository _preferenceRepository;
    private readonly ILogger<UnsubscribeLinkFactory> _logger;
    private readonly NotificationSettings _settings;

    public UnsubscribeLinkFactory(
        INotificationPreferenceRepository preferenceRepository,
        IOptions<NotificationSettings> settings,
        ILogger<UnsubscribeLinkFactory> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _preferenceRepository = preferenceRepository;
        _logger = logger;
        _settings = settings.Value;
    }

    public async Task<string?> CreateLinkAsync(Guid userId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.UnsubscribeSecret))
        {
            _logger.LogWarning(
                "Notification:UnsubscribeSecret tanımlı olmadığı için abonelikten çıkma linki üretilemedi.");
            return null;
        }

        var token = DeriveToken(userId);
        var hash = HashToken(token);

        var preference = await _preferenceRepository.FindByUserIdAsync(userId, ct);

        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(userId);
            preference.SetUnsubscribeTokenHash(hash);
            await _preferenceRepository.InsertAsync(preference, ct);
        }
        else if (preference.UnsubscribeTokenHash != hash)
        {
            preference.SetUnsubscribeTokenHash(hash);
            await _preferenceRepository.UpdateAsync(preference, ct);
        }

        return $"{_settings.AppUrl.TrimEnd('/')}/unsubscribe?token={Uri.EscapeDataString(token)}";
    }

    public string HashToken(string token) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

    private string DeriveToken(Guid userId)
    {
        var key = Encoding.UTF8.GetBytes(_settings.UnsubscribeSecret);
        var payload = Encoding.UTF8.GetBytes(userId.ToString("N"));

        return Convert.ToBase64String(HMACSHA256.HashData(key, payload))
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');
    }
}
