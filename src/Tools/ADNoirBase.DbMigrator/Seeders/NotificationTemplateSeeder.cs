using Microsoft.Extensions.Logging;
using Notification.Domain.Interfaces.Repositories;

namespace ADNoirBase.DbMigrator.Seeders;

public sealed class NotificationTemplateSeeder
{
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly ILogger<NotificationTemplateSeeder> _logger;

    public NotificationTemplateSeeder(
        INotificationTemplateRepository templateRepository,
        ILogger<NotificationTemplateSeeder> logger)
    {
        _templateRepository = templateRepository;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken ct = default)
    {
        foreach (var template in SystemNotificationTemplates.Build())
        {
            if (await _templateRepository.CodeExistsAsync(template.Code, ct: ct))
            {
                continue;
            }

            await _templateRepository.InsertAsync(template, ct);
            _logger.LogInformation("Notification template seeded: {Code}", template.Code);
        }
    }
}
