namespace Notification.Application.Configuration;

public enum SmtpSecurity
{
    /// <summary>Şifreleme yok. Yalnızca Mailpit gibi yerel test sunucuları için.</summary>
    None = 1,

    /// <summary>Düz bağlantı açılır, sonra TLS'e yükseltilir. Genellikle 587.</summary>
    StartTls = 2,

    /// <summary>Bağlantı baştan TLS. Genellikle 465.</summary>
    SslOnConnect = 3,

    /// <summary>Sunucunun desteklediğine göre otomatik seçilir.</summary>
    Auto = 4
}
