using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Configuration;
using Notification.Application.Delivery;
using Notification.Application.Templating;
using Notification.Domain.Interfaces.Repositories;
using Notification.Infrastructure.Campaigns;
using Notification.Infrastructure.Delivery;
using Notification.Infrastructure.Email;
using Notification.Infrastructure.Persistence;
using Notification.Infrastructure.Persistence.Repositories;
using Notification.Infrastructure.Sms;
using Notification.Infrastructure.Storage;
using Notification.Infrastructure.Templating;

namespace Notification.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddNotificationInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        NotificationClassMaps.Register();

        services.AddScoped<INotificationTemplateRepository, NotificationTemplateRepository>();
        services.AddScoped<INotificationMessageRepository, NotificationMessageRepository>();
        services.AddScoped<INotificationCampaignRepository, NotificationCampaignRepository>();
        services.AddScoped<IInAppNotificationRepository, InAppNotificationRepository>();
        services.AddScoped<INotificationPreferenceRepository, NotificationPreferenceRepository>();

        services.AddScoped<NotificationIndexInitializer>();

        services.AddSingleton<ITemplateRenderer, ScribanTemplateRenderer>();
        services.AddScoped<IAttachmentStore, GridFsAttachmentStore>();

        var settings = configuration
            .GetSection(NotificationSettings.SectionName)
            .Get<NotificationSettings>() ?? new NotificationSettings();

        AddEmailSender(services, settings.Email.Provider);
        AddSmsSender(services, settings.Sms.Provider);

        return services;
    }

    /// <summary>
    /// Gönderim worker'ı yalnızca API tarafında çalışır. Migration aracı aynı servisleri
    /// kullanır ama kuyruğu işlememelidir.
    /// </summary>
    public static IServiceCollection AddNotificationDeliveryWorker(this IServiceCollection services)
    {
        services.AddHostedService<NotificationDeliveryWorker>();
        services.AddHostedService<CampaignWorker>();

        return services;
    }

    private static void AddEmailSender(IServiceCollection services, EmailProvider provider)
    {
        switch (provider)
        {
            case EmailProvider.Smtp:
                services.AddScoped<IEmailSender, SmtpEmailSender>();
                break;

            case EmailProvider.Pickup:
                services.AddScoped<IEmailSender, PickupDirectoryEmailSender>();
                break;

            case EmailProvider.Log:
            default:
                services.AddScoped<IEmailSender, LogEmailSender>();
                break;
        }
    }

    private static void AddSmsSender(IServiceCollection services, SmsProvider provider)
    {
        switch (provider)
        {
            // Gerçek bir SMS sağlayıcısı eklendiğinde yalnızca bu switch'e bir dal eklenir.
            case SmsProvider.Log:
            default:
                services.AddScoped<ISmsSender, LogSmsSender>();
                break;
        }
    }
}
