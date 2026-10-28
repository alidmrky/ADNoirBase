using System.Text.Json;
using System.Text.Json.Serialization;

namespace ADNoirBase.API.Configuration;

/// <summary>
/// MVC pipeline'ının dışında yanıt yazan bileşenler için ortak serileştirme ayarı.
/// Program.cs'deki AddJsonOptions çağrısıyla aynı kalmalıdır; aksi halde hata
/// yanıtları başarı yanıtlarından farklı bir şekilde serileşir.
/// </summary>
public static class ApiJsonOptions
{
    public static readonly JsonSerializerOptions Default = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
}
