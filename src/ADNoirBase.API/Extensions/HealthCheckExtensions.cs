using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using MongoDB.Driver;

namespace ADNoirBase.API.Extensions;

public static class HealthCheckExtensions
{
    /// <summary>Veritabanlarına dokunan, yalnızca hazırlık probu için kullanılan etiket.</summary>
    private const string ReadyTag = "ready";

    public static IServiceCollection AddAppHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoConnectionString = configuration.GetConnectionString("MongoDB")!;

        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("PostgreSQL")!,
                name: "postgresql",
                tags: [ReadyTag])
            .AddMongoDb(
                sp => new MongoClient(mongoConnectionString),
                name: "mongodb",
                tags: [ReadyTag]);

        return services;
    }

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        // Liveness: yalnızca sürecin ayakta olduğunu söyler, hiçbir bağımlılığı
        // yoklamaz ve hiçbir altyapı bilgisi sızdırmaz. Yük dengeleyiciler ve
        // konteyner orkestratörleri kimlik doğrulayamadığı için açık bırakılır.
        endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            Predicate = _ => false,
            ResponseWriter = static (context, _) =>
            {
                context.Response.ContentType = "text/plain";
                return context.Response.WriteAsync("Healthy");
            }
        }).AllowAnonymous();

        // Readiness: veritabanı adlarını ve hata ayrıntılarını içerir, bu yüzden
        // kimlik doğrulaması ister.
        endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains(ReadyTag)
        }).RequireAuthorization();

        return endpoints;
    }
}
