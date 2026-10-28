using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Delivery;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Infrastructure.Delivery;

/// <summary>
/// Kuyruğu yoklayıp mesajları sırayla gönderir. Mesajlar atomik olarak sahiplenildiği için
/// birden fazla sunucu aynı anda çalışabilir; aynı mesaj iki kez gönderilmez.
/// </summary>
public sealed class NotificationDeliveryWorker : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<NotificationDeliveryWorker> _logger;
    private readonly DeliverySettings _settings;
    private readonly string _workerId;

    public NotificationDeliveryWorker(
        IServiceScopeFactory scopeFactory,
        IOptions<NotificationSettings> settings,
        ILogger<NotificationDeliveryWorker> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value.Delivery;

        // Kilidi kimin aldığı, çöken bir sunucunun mesajlarını teşhis etmek için gerekir.
        _workerId = string.Create(
            CultureInfo.InvariantCulture,
            $"{Environment.MachineName}:{Environment.ProcessId}");
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation("Bildirim gönderim worker'ı yapılandırmayla kapatıldı.");
            return;
        }

        _logger.LogInformation("Bildirim gönderim worker'ı başladı: {WorkerId}", _workerId);

        var idleDelay = TimeSpan.FromSeconds(_settings.PollSeconds);
        var throttleDelay = TimeSpan.FromSeconds(1.0 / _settings.ThrottlePerSecond);

        while (!stoppingToken.IsCancellationRequested)
        {
            var processed = await DrainQueueAsync(throttleDelay, stoppingToken);

            if (processed == 0)
            {
                await Task.Delay(idleDelay, stoppingToken);
            }
        }
    }

    /// <summary>Kuyruk boşalana kadar mesaj işler ve işlenen sayısını döner.</summary>
    [SuppressMessage(
        "Design",
        "CA1031:Do not catch general exception types",
        Justification = "Veritabanı erişimindeki geçici bir hata worker'ı tümden durdurmamalı; " +
                        "hata günlüğe yazılır ve bir sonraki yoklamada tekrar denenir.")]
    private async Task<int> DrainQueueAsync(TimeSpan throttleDelay, CancellationToken ct)
    {
        var processed = 0;

        try
        {
            var lockDuration = TimeSpan.FromMinutes(_settings.LockMinutes);

            while (!ct.IsCancellationRequested)
            {
                await using var scope = _scopeFactory.CreateAsyncScope();

                var repository = scope.ServiceProvider.GetRequiredService<INotificationMessageRepository>();
                var message = await repository.ClaimNextPendingAsync(_workerId, lockDuration, ct);

                if (message is null)
                {
                    break;
                }

                var delivery = scope.ServiceProvider.GetRequiredService<INotificationDeliveryService>();
                await delivery.ProcessAsync(message, ct);

                processed++;

                await Task.Delay(throttleDelay, ct);
            }
        }
        catch (OperationCanceledException)
        {
            // Uygulama kapanıyor.
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Bildirim kuyruğu işlenirken hata oluştu.");
        }

        return processed;
    }
}
