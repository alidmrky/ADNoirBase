namespace Notification.Application.Configuration;

public sealed class NotificationSettings
{
    public const string SectionName = "Notification";

    /// <summary>Şablonlarda {{ app_name }} olarak kullanılır.</summary>
    public string AppName { get; init; } = "ADNoirBase";

    /// <summary>
    /// Maillerdeki bağlantıların tabanı. Abonelikten çıkma linki de buradan üretilir,
    /// bu yüzden kullanıcının tarayıcıdan eriştiği adres olmalıdır — API'nin değil.
    /// </summary>
    public string AppUrl { get; init; } = "http://localhost:3000";

    /// <summary>
    /// Abonelikten çıkma token'ının türetildiği gizli anahtar. Boşsa pazarlama maillerine
    /// abonelikten çıkma linki eklenmez — bu da çoğu ülkede yasal bir sorundur, bu yüzden
    /// pazarlama gönderimi yapılacak ortamlarda mutlaka tanımlanmalıdır.
    /// </summary>
    public string UnsubscribeSecret { get; init; } = string.Empty;

    public EmailSettings Email { get; init; } = new();

    public SmsSettings Sms { get; init; } = new();

    public DeliverySettings Delivery { get; init; } = new();

    public AttachmentSettings Attachments { get; init; } = new();

    public CampaignSettings Campaigns { get; init; } = new();
}
