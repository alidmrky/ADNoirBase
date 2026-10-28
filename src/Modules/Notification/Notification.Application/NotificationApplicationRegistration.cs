using ADNoirBase.Shared.Application;
using ADNoirBase.Shared.Application.Events;
using ADNoirBase.Shared.Contracts.IntegrationEvents;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Notification.Application.Campaigns;
using Notification.Application.Configuration;
using Notification.Application.Delivery;
using Notification.Application.Dispatching;
using Notification.Application.Events;
using Notification.Application.Preferences;
using Notification.Application.Services;
using Notification.Application.Services.Attachments;
using Notification.Application.Services.Campaigns;
using Notification.Application.Services.InApp;
using Notification.Application.Services.Preferences;
using Notification.Application.Services.Templates;
using Notification.Application.Templating;

namespace Notification.Application;

public sealed class NotificationApplicationRegistration : IModuleRegistration
{
    public IServiceCollection Register(IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        var section = configuration.GetSection(NotificationSettings.SectionName);
        NotificationSettingsValidator.Validate(section.Get<NotificationSettings>() ?? new NotificationSettings());

        services.Configure<NotificationSettings>(section);

        services.AddScoped<INotificationComposer, NotificationComposer>();
        services.AddScoped<INotificationDispatcher, NotificationDispatcher>();
        services.AddScoped<INotificationDeliveryService, NotificationDeliveryService>();
        services.AddScoped<IUnsubscribeLinkFactory, UnsubscribeLinkFactory>();
        services.AddScoped<ICampaignExpansionService, CampaignExpansionService>();
        services.AddScoped<INotificationTemplateService, NotificationTemplateService>();
        services.AddScoped<INotificationCampaignService, NotificationCampaignService>();
        services.AddScoped<INotificationPreferenceService, NotificationPreferenceService>();
        services.AddScoped<INotificationAttachmentService, NotificationAttachmentService>();
        services.AddScoped<IInAppNotificationService, InAppNotificationService>();
        services.AddScoped<INotificationSendService, NotificationSendService>();
        services.AddScoped<IIntegrationEventHandler<UserRegisteredIntegrationEvent>, UserRegisteredNotificationHandler>();
        services.AddScoped<IIntegrationEventHandler<UserPasswordChangedIntegrationEvent>, UserPasswordChangedNotificationHandler>();

        // Canlı bağlantı katmanı API'de kurulur; burada kaydedilen varsayılan sessizce hiçbir şey yapmaz.
        services.AddSingleton<IInAppNotificationPublisher, NullInAppNotificationPublisher>();

        services.AddValidatorsFromAssemblyContaining<NotificationApplicationRegistration>();

        return services;
    }
}
