namespace ADNoirBase.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddAppCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? ["*"];

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (allowedOrigins.Contains("*"))
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                else
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
            });
        });

        return services;
    }
}
