using System.Globalization;

namespace Notification.Application.Configuration;

/// <summary>
/// Ayarları uygulama ayağa kalkarken doğrular. Yanlış yapılandırılmış bir bildirim katmanının
/// hatası ancak ilk gönderimde ortaya çıkar; o noktada mail sessizce kaybolmuş olur.
/// </summary>
public static class NotificationSettingsValidator
{
    public static void Validate(NotificationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (!Uri.TryCreate(settings.AppUrl, UriKind.Absolute, out _))
        {
            throw new InvalidOperationException(
                $"Notification:AppUrl geçerli bir mutlak adres olmalıdır; şu an '{settings.AppUrl}'.");
        }

        if (string.IsNullOrWhiteSpace(settings.Email.FromAddress))
        {
            throw new InvalidOperationException("Notification:Email:FromAddress tanımlı değil.");
        }

        if (settings.Email.Provider == EmailProvider.Smtp &&
            string.IsNullOrWhiteSpace(settings.Email.Smtp.Host))
        {
            throw new InvalidOperationException(
                "Notification:Email:Provider 'Smtp' iken Notification:Email:Smtp:Host zorunludur.");
        }

        RequirePositive(settings.Delivery.MaxAttempts, "Notification:Delivery:MaxAttempts");
        RequirePositive(settings.Delivery.ThrottlePerSecond, "Notification:Delivery:ThrottlePerSecond");
        RequirePositive(settings.Delivery.PollSeconds, "Notification:Delivery:PollSeconds");
        RequirePositive(settings.Delivery.LockMinutes, "Notification:Delivery:LockMinutes");
        RequirePositive(settings.Campaigns.BatchSize, "Notification:Campaigns:BatchSize");
        RequirePositive(settings.Campaigns.PollSeconds, "Notification:Campaigns:PollSeconds");
        RequirePositive(settings.Campaigns.MaxRecipients, "Notification:Campaigns:MaxRecipients");
        RequirePositive(settings.Attachments.MaxCount, "Notification:Attachments:MaxCount");

        if (settings.Attachments.MaxBytes <= 0)
        {
            throw new InvalidOperationException("Notification:Attachments:MaxBytes sıfırdan büyük olmalıdır.");
        }
    }

    private static void RequirePositive(int value, string key)
    {
        if (value <= 0)
        {
            throw new InvalidOperationException(string.Create(
                CultureInfo.InvariantCulture,
                $"{key} sıfırdan büyük olmalıdır; şu an {value}."));
        }
    }
}
