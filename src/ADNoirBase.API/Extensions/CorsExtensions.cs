namespace ADNoirBase.API.Extensions;

public static class CorsExtensions
{
    public static IServiceCollection AddAppCors(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var allowedOrigins = configuration
            .GetSection("Cors:AllowedOrigins")
            .Get<string[]>() ?? [];

        var isWildcard = allowedOrigins.Contains("*");

        if (isWildcard && !environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "Cors:AllowedOrigins üretim ortamında '*' olamaz. " +
                "İzin verilen origin'leri Cors__AllowedOrigins__0, __1 ... olarak tanımlayın.");
        }

        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                if (isWildcard)
                {
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
                else
                {
                    // Origin listesi boşsa hiçbir çapraz kaynak isteği kabul edilmez.
                    policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
                }
            });
        });

        return services;
    }
}
