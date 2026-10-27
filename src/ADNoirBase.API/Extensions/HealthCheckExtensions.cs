using MongoDB.Driver;

namespace ADNoirBase.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddAppHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var mongoConnectionString = configuration.GetConnectionString("MongoDB")!;

        services.AddHealthChecks()
            .AddNpgSql(configuration.GetConnectionString("PostgreSQL")!)
            .AddMongoDb(
                sp => new MongoClient(mongoConnectionString),
                name: "mongodb");

        return services;
    }

    public static IEndpointRouteBuilder MapAppHealthChecks(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapHealthChecks("/health");
        return endpoints;
    }
}
