namespace Notification.Application.Configuration;

public sealed class SmtpSettings
{
    public string Host { get; init; } = string.Empty;

    public int Port { get; init; } = 587;

    public SmtpSecurity Security { get; init; } = SmtpSecurity.StartTls;

    public string? UserName { get; init; }

    /// <summary>Depoda boş bırakılır; Notification__Email__Smtp__Password ortam değişkeninden verilir.</summary>
    public string? Password { get; init; }

    public int TimeoutSeconds { get; init; } = 30;
}
