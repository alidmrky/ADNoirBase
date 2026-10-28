using ADNoirBase.DbMigrator.Seeders;
using ADNoirBase.DbMigrator.Services;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure;
using ADNoirBase.Shared.Infrastructure.Persistence.Interceptors;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Notification.Infrastructure;
using Notification.Infrastructure.Persistence;

var host = Host.CreateDefaultBuilder(args)
    .ConfigureAppConfiguration(config =>
    {
        config.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: false);
        config.AddEnvironmentVariables();
    })
    .ConfigureServices((context, services) =>
    {
        services.AddLogging(logging => logging.AddConsole());

        services.AddScoped<ICurrentUserService, MigratorCurrentUserService>();
        services.AddScoped<AuditInterceptor>();
        services.AddPasswordHashing(context.Configuration);

        services.AddDbContext<IdentityDbContext>((sp, options) =>
        {
            options.UseNpgsql(context.Configuration.GetConnectionString("PostgreSQL"));
            options.AddInterceptors(sp.GetRequiredService<AuditInterceptor>());
        });

        services.AddMongoDb(context.Configuration);
        services.AddNotificationInfrastructure(context.Configuration);

        services.AddScoped<DefaultRolesSeeder>();
        services.AddScoped<AdminUserSeeder>();
        services.AddScoped<NotificationTemplateSeeder>();
    })
    .Build();

using var scope = host.Services.CreateScope();
var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

try
{
    logger.LogInformation("DbMigrator başlıyor...");

    var identityContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();

    logger.LogInformation("Identity migration'ları uygulanıyor...");
    await identityContext.Database.MigrateAsync();

    logger.LogInformation("Seed verileri ekleniyor...");
    await scope.ServiceProvider.GetRequiredService<DefaultRolesSeeder>().SeedAsync();
    await scope.ServiceProvider.GetRequiredService<AdminUserSeeder>().SeedAsync();

    logger.LogInformation("MongoDB index'leri oluşturuluyor...");
    await scope.ServiceProvider.GetRequiredService<NotificationIndexInitializer>().CreateIndexesAsync();

    logger.LogInformation("Bildirim şablonları ekleniyor...");
    await scope.ServiceProvider.GetRequiredService<NotificationTemplateSeeder>().SeedAsync();

    logger.LogInformation("DbMigrator tamamlandı.");
}
catch (Exception ex)
{
    logger.LogError(ex, "DbMigrator hatası");
    throw;
}
