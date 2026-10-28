using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Campaigns;
using Notification.Application.Configuration;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Infrastructure.Campaigns;

/// <summary>
/// Zamanı gelmiş kampanyaları sahiplenir, alıcılarını kuyruğa yazar ve gönderimi biten
/// kampanyaları tamamlanmış işaretler. Gönderimin kendisi teslimat worker'ının işidir.
/// </summary>
public sealed class CampaignWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<CampaignWorker> _logger;
    private readonly CampaignSettings _settings;

    public CampaignWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationSettings> settings,
        ILogger<CampaignWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value.Campaigns;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Kampanya worker'ı yapılandırmayla kapatıldı.");
            return;
        }

        _logger.LogInformation("Kampanya worker'ı başladı.");

        var pollDelay = TimeSpan.FromSeconds(_settings.PollSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            await RunCycleAsync(stoppingToken);
            await Task.Delay(pollDelay, stoppingToken);
        }
    }

    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Tek bir kampanyanın hatası worker'ı durdurmamalı; hata günlüğe yazılır " +
                        "ve bir sonraki turda devam edilir.")]
    private async Task RunCycleAsync(CancellationToken ct)
    {
        try
        {
            while (!ct.IsCancellationRequested)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var repository = scope.ServiceProvider.GetRequiredService<INotificationCampaignRepository>();
                var campaign = await repository.ClaimNextDueAsync(DateTime.UtcNow, ct);

                if (campaign is null)
                {
                    break;
                }

                var expansion = scope.ServiceProvider.GetRequiredService<ICampaignExpansionService>();
                await expansion.ExpandAsync(campaign, ct);
            }

            await using var completionScope = _scopeFactory.CreateAsyncScope();

            await completionScope.ServiceProvider
                .GetRequiredService<ICampaignExpansionService>()
                .CompleteFinishedCampaignsAsync(ct);
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Kampanya turu işlenirken hata oluştu.");
        }
    }
}
