namespace ADNoirBase.Shared.Infrastructure.Authentication;

/// <summary>
/// BCrypt maliyetini uygulama ayağa kalkarken doğrular. Çok düşük değer brute-force'u
/// kolaylaştırır; çok yüksek değer her login'i saniyeler süren bir DoS kapısına çevirir.
/// </summary>
public static class BcryptSettingsValidator
{
    /// <summary>OWASP Password Storage Cheat Sheet alt sınırı.</summary>
    public const int MinimumWorkFactor = 10;

    public const int MaximumWorkFactor = 16;

    public static void Validate(BcryptSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (settings.WorkFactor is < MinimumWorkFactor or > MaximumWorkFactor)
        {
            throw new InvalidOperationException(
                $"Bcrypt:WorkFactor {MinimumWorkFactor} ile {MaximumWorkFactor} arasında olmalıdır; " +
                $"şu an {settings.WorkFactor}.");
        }
    }
}
