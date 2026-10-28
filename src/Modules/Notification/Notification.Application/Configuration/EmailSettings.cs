namespace Notification.Application.Configuration;

public sealed class EmailSettings
{
    public EmailProvider Provider { get; init; } = EmailProvider.Log;

    public string FromAddress { get; init; } = "no-reply@localhost";

    public string FromDisplayName { get; init; } = "ADNoirBase";

    public string? ReplyTo { get; init; }

    /// <summary>Pickup sağlayıcısının .eml dosyalarını bıraktığı klasör.</summary>
    public string PickupDirectory { get; init; } = ".mail-drop";

    public SmtpSettings Smtp { get; init; } = new();
}
