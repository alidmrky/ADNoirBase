namespace ADNoirBase.API.Configuration;

/// <summary>Tek bir sabit pencere limitinin ayarları.</summary>
public sealed class RateLimitWindow
{
    /// <summary>Pencere başına izin verilen istek sayısı.</summary>
    public int PermitLimit { get; init; }

    /// <summary>Pencere uzunluğu, saniye cinsinden.</summary>
    public int WindowSeconds { get; init; }
}
