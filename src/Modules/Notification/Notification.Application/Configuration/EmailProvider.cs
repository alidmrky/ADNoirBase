namespace Notification.Application.Configuration;

public enum EmailProvider
{
    /// <summary>Hiçbir yere gönderim yapmaz, yalnızca log'a yazar. Development varsayılanı.</summary>
    Log = 1,

    /// <summary>Diske .eml dosyası bırakır; çift tıklayıp mail istemcisinde açılabilir.</summary>
    Pickup = 2,

    Smtp = 3
}
