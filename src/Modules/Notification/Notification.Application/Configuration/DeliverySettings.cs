namespace Notification.Application.Configuration;

public sealed class DeliverySettings
{
    /// <summary>Arka plan worker'ı çalışsın mı. Testlerde ve migration çalıştırmalarında kapatılır.</summary>
    public bool Enabled { get; init; } = true;

    public int MaxAttempts { get; init; } = 5;

    /// <summary>Saniyede gönderilecek azami mesaj. Sağlayıcıların hız sınırına takılmamak için.</summary>
    public int ThrottlePerSecond { get; init; } = 10;

    /// <summary>Kuyruk boşken iki yoklama arasındaki bekleme.</summary>
    public int PollSeconds { get; init; } = 5;

    /// <summary>
    /// Bir mesajın worker tarafından kilitli kalacağı süre. Worker çökerse mesaj bu süre
    /// sonunda başka bir worker tarafından alınabilir hale gelir.
    /// </summary>
    public int LockMinutes { get; init; } = 5;

    /// <summary>Yeniden deneme bekleme süresinin tabanı; her denemede ikiye katlanır.</summary>
    public int RetryBaseSeconds { get; init; } = 30;

    public int RetryMaxSeconds { get; init; } = 3600;
}
