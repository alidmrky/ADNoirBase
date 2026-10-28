namespace ADNoirBase.API.Configuration;

/// <summary>
/// Reverse proxy arkasında çalışırken gerçek istemci IP'sinin okunabilmesi için
/// kullanılır. Rate limiting IP'ye göre bölümlendiği için bu ayar yanlışsa ya
/// tüm kullanıcılar tek kovayı paylaşır ya da istemciler IP'lerini taklit edebilir.
/// </summary>
public sealed class ForwardedHeadersSettings
{
    public const string SectionName = "ForwardedHeaders";

    /// <summary>Varsayılan olarak kapalıdır; yalnızca proxy arkasındaysanız açın.</summary>
    public bool Enabled { get; init; }

    /// <summary>Güvenilen proxy adresleri, örneğin "10.0.0.5".</summary>
    public IReadOnlyList<string> KnownProxies { get; init; } = [];

    /// <summary>Güvenilen ağlar CIDR biçiminde, örneğin "10.0.0.0/8".</summary>
    public IReadOnlyList<string> KnownNetworks { get; init; } = [];
}
