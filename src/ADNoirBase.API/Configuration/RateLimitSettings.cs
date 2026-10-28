namespace ADNoirBase.API.Configuration;

public sealed class RateLimitSettings
{
    public const string SectionName = "RateLimiting";

    /// <summary>
    /// Kapatıldığında politikalar kayıtlı kalır ama hiçbir istek sınırlanmaz.
    /// Entegrasyon testlerinin limitlere takılmaması için gereklidir.
    /// </summary>
    public bool Enabled { get; init; } = true;
    public RateLimitWindow NotificationSend { get; init; } = new() { PermitLimit = 10, WindowSeconds = 60 };

    /// <summary>Tüm uçlar için istemci başına tavan.</summary>
    public RateLimitWindow Global { get; init; } = new() { PermitLimit = 100, WindowSeconds = 60 };

    /// <summary>AuthController uçları için sıkı limit.</summary>
    public RateLimitWindow Authentication { get; init; } = new() { PermitLimit = 5, WindowSeconds = 60 };
}
