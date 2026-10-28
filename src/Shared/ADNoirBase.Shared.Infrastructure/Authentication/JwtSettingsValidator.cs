namespace ADNoirBase.Shared.Infrastructure.Authentication;

/// <summary>
/// JWT ayarlarını uygulama ayağa kalkarken doğrular. Eksik veya zayıf bir
/// imzalama anahtarıyla başlamaktansa açık bir hatayla durmak tercih edilir.
/// </summary>
public static class JwtSettingsValidator
{
    /// <summary>HMAC-SHA256 için anahtarın en az 256 bit olması gerekir.</summary>
    public const int MinimumSecretKeyLength = 32;

    public static void Validate(JwtSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        if (string.IsNullOrWhiteSpace(settings.SecretKey))
        {
            throw new InvalidOperationException(
                "Jwt:SecretKey tanımlı değil. Jwt__SecretKey ortam değişkenini ayarlayın " +
                "veya geliştirme için 'dotnet user-secrets set \"Jwt:SecretKey\" \"<deger>\"' " +
                "komutunu çalıştırın. Örnek üretim: openssl rand -base64 48");
        }

        if (settings.SecretKey.Length < MinimumSecretKeyLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey en az {MinimumSecretKeyLength} karakter olmalıdır; " +
                $"şu an {settings.SecretKey.Length} karakter.");
        }

        if (string.IsNullOrWhiteSpace(settings.Issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer tanımlı değil.");
        }

        if (string.IsNullOrWhiteSpace(settings.Audience))
        {
            throw new InvalidOperationException("Jwt:Audience tanımlı değil.");
        }
    }
}
