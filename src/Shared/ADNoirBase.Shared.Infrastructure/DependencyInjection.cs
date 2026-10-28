using System.Text;
using ADNoirBase.Shared.Application.Events;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Authentication;
using ADNoirBase.Shared.Infrastructure.Events;
using ADNoirBase.Shared.Infrastructure.Identity;
using ADNoirBase.Shared.Infrastructure.Persistence.Interceptors;
using ADNoirBase.Shared.Infrastructure.Persistence.Mongo;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using MongoDB.Driver;

namespace ADNoirBase.Shared.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddSharedInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddHttpContextAccessor();

        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<AuditInterceptor>();
        services.AddSingleton<IIntegrationEventPublisher, InProcessIntegrationEventPublisher>();
        services.AddSingleton<JwtTokenService>();
        services.AddSingleton<ITokenService, JwtTokenServiceAdapter>();

        services.Configure<JwtSettings>(
            configuration.GetSection(JwtSettings.SectionName));

        services.AddPasswordHashing(configuration);
        services.AddJwtAuthentication(configuration);
        services.AddMongoDb(configuration);

        return services;
    }

    /// <summary>API ve DbMigrator aynı maliyetle hash üretsin diye ayrı tutulur.</summary>
    public static IServiceCollection AddPasswordHashing(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(BcryptSettings.SectionName);
        var settings = section.Get<BcryptSettings>() ?? new BcryptSettings();

        BcryptSettingsValidator.Validate(settings);

        services.Configure<BcryptSettings>(section);
        services.AddSingleton<IPasswordHasher, BcryptPasswordHasher>();

        return services;
    }

    /// <summary>
    /// İstemci tembel üretilir: MongoDB kullanmayan bir kurulum bağlantı dizesi olmadan da açılır,
    /// MongoRepository kullanan bir modül ise eksik ayarla ilk istekte açık bir hata alır.
    /// </summary>
    public static IServiceCollection AddMongoDb(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<MongoDbSettings>(configuration.GetSection(MongoDbSettings.SectionName));

        // Class map'ler oluşmadan önce yapılmalı; modüller kendi map'lerini kayıt sırasında ekler.
        MongoSerialization.Configure();

        services.AddSingleton<IMongoClient>(_ =>
        {
            var connectionString = configuration.GetConnectionString("MongoDB");
            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    "ConnectionStrings:MongoDB tanımlı değil. MongoRepository kullanan bir modül için " +
                    "ConnectionStrings__MongoDB ortam değişkenini ayarlayın.");
            }

            return new MongoClient(connectionString);
        });

        services.AddSingleton(sp =>
        {
            var databaseName = sp.GetRequiredService<IOptions<MongoDbSettings>>().Value.DatabaseName;
            if (string.IsNullOrWhiteSpace(databaseName))
            {
                throw new InvalidOperationException("MongoDb:DatabaseName tanımlı değil.");
            }

            return sp.GetRequiredService<IMongoClient>().GetDatabase(databaseName);
        });

        return services;
    }

    private static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSettings = configuration
            .GetSection(JwtSettings.SectionName)
            .Get<JwtSettings>() ?? new JwtSettings();

        JwtSettingsValidator.Validate(jwtSettings);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings.Issuer,
                ValidAudience = jwtSettings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
                ClockSkew = TimeSpan.Zero
            };
        });

        services.AddAuthorization();

        return services;
    }
}
