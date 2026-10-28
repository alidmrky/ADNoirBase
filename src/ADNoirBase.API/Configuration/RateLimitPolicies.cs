namespace ADNoirBase.API.Configuration;

/// <summary>Rate limit politika adları. Literal string yasak — her zaman bu sabitler kullanılır.</summary>
public static class RateLimitPolicies
{
    /// <summary>Kimlik doğrulama uçları için sıkı limit. Brute-force ve credential stuffing'e karşı.</summary>
    public const string Authentication = "authentication";
    public const string NotificationSend = "notification-send";
}
