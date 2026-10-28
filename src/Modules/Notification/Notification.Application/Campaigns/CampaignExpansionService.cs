using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Directory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Dispatching;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Campaigns;

public sealed class CampaignExpansionService : ICampaignExpansionService
{
    private readonly INotificationCampaignRepository _campaignRepository;
    private readonly INotificationMessageRepository _messageRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly INotificationDispatcher _dispatcher;
    private readonly IUserDirectory _userDirectory;
    private readonly ILogger<CampaignExpansionService> _logger;
    private readonly NotificationSettings _settings;

    public CampaignExpansionService(
        INotificationCampaignRepository campaignRepository,
        INotificationMessageRepository messageRepository,
        INotificationTemplateRepository templateRepository,
        INotificationDispatcher dispatcher,
        IUserDirectory userDirectory,
        IOptions<NotificationSettings> settings,
        ILogger<CampaignExpansionService> logger)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _campaignRepository = campaignRepository;
        _messageRepository = messageRepository;
        _templateRepository = templateRepository;
        _dispatcher = dispatcher;
        _userDirectory = userDirectory;
        _logger = logger;
        _settings = settings.Value;
    }

    private int BatchSize => _settings.Campaigns.BatchSize;

    public async Task ExpandAsync(NotificationCampaign campaign, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(campaign);

        var template = await _templateRepository.FindByCodeAsync(campaign.TemplateCode, ct);

        if (template is null || !template.IsActive || template.Channel != campaign.Channel)
        {
            await FailAsync(campaign, $"Şablon '{campaign.TemplateCode}' kullanılamıyor.", ct);
            return;
        }

        var recipients = await ResolveRecipientsAsync(campaign, ct);

        if (recipients is null)
        {
            return;
        }

        var queued = campaign.SendMode == CampaignSendMode.SingleBcc
            ? await QueueAsBccBatchesAsync(campaign, recipients, ct)
            : await QueueIndividuallyAsync(campaign, recipients, ct);

        if (queued < 0)
        {
            return;
        }

        campaign.FinishExpanding(queued);
        await _campaignRepository.UpdateAsync(campaign, ct);

        _logger.LogInformation(
            "Kampanya {CampaignId}: {Queued} mesaj kuyruğa eklendi.", campaign.Id, queued);
    }

    public async Task CompleteFinishedCampaignsAsync(CancellationToken ct = default)
    {
        var sending = await _campaignRepository.ListAsync(c => c.Status == CampaignStatus.Sending, ct);

        foreach (var campaign in sending)
        {
            var unfinished = await _messageRepository.CountUnfinishedByCampaignAsync(campaign.Id, ct);

            if (unfinished > 0)
            {
                continue;
            }

            campaign.Complete(DateTime.UtcNow);
            await _campaignRepository.UpdateAsync(campaign, ct);

            _logger.LogInformation("Kampanya {CampaignId} tamamlandı.", campaign.Id);
        }
    }

    /// <summary>
    /// Hedef kitleyi tek listeye çevirir. Üst sınır (varsayılan 100.000) aşılmadığı için
    /// liste belleğe sığar; sınırın asıl amacı yanlışlıkla devasa gönderimi engellemektir.
    /// Sınır aşılırsa kampanya başarısız işaretlenir ve null döner.
    /// </summary>
    private async Task<List<NotificationRecipient>?> ResolveRecipientsAsync(
        NotificationCampaign campaign, CancellationToken ct)
    {
        var maximum = _settings.Campaigns.MaxRecipients;

        if (!campaign.Audience.ResolvesFromDirectory)
        {
            var addresses = campaign.Audience.ManualAddresses;

            if (addresses.Count > maximum)
            {
                await FailAsync(campaign, $"Hedef kitle {addresses.Count} adres; üst sınır {maximum}.", ct);
                return null;
            }

            return [.. addresses.Select(a => new NotificationRecipient(a))];
        }

        var filter = campaign.Audience.ToFilter();
        var total = await _userDirectory.CountAsync(filter, ct);

        if (total > maximum)
        {
            await FailAsync(campaign, $"Hedef kitle {total} kişi; üst sınır {maximum}.", ct);
            return null;
        }

        var recipients = new List<NotificationRecipient>(total);

        await foreach (var user in _userDirectory.StreamAsync(filter, ct))
        {
            var address = ResolveAddress(campaign.Channel, user);

            // İletişim bilgisi olmayan kullanıcı kampanyayı durdurmaz; yalnızca atlanır.
            if (!string.IsNullOrWhiteSpace(address))
            {
                recipients.Add(new NotificationRecipient(address, user.FullName, user.UserId));
            }
        }

        return recipients;
    }

    /// <summary>Kuyruğa eklenen mesaj sayısı; iptal edilmişse -1.</summary>
    private async Task<int> QueueIndividuallyAsync(
        NotificationCampaign campaign, List<NotificationRecipient> recipients, CancellationToken ct)
    {
        var queued = 0;

        for (var i = 0; i < recipients.Count; i++)
        {
            if (i % BatchSize == 0 && await HandleCancellationAsync(campaign, ct))
            {
                return -1;
            }

            var result = await _dispatcher.EnqueueAsync(
                BuildRequest(campaign, [recipients[i]], []), ct);

            queued += result.QueuedMessageIds.Count;

            if ((i + 1) % BatchSize == 0)
            {
                campaign.ReportProgress(queued, recipients.Count);
                await _campaignRepository.UpdateAsync(campaign, ct);
            }
        }

        return queued;
    }

    /// <summary>
    /// Tek mail, alıcılar BCC'de. Kişiselleştirme ve abonelikten çıkma linki bu modda
    /// çalışmaz; buna karşılık binlerce alıcı birkaç mesaja sığar.
    /// </summary>
    private async Task<int> QueueAsBccBatchesAsync(
        NotificationCampaign campaign, List<NotificationRecipient> recipients, CancellationToken ct)
    {
        var envelope = new NotificationRecipient(
            _settings.Email.FromAddress, _settings.Email.FromDisplayName);

        var queued = 0;

        for (var offset = 0; offset < recipients.Count; offset += BatchSize)
        {
            if (await HandleCancellationAsync(campaign, ct))
            {
                return -1;
            }

            var batch = recipients.GetRange(offset, Math.Min(BatchSize, recipients.Count - offset));
            var result = await _dispatcher.EnqueueAsync(BuildRequest(campaign, [envelope], batch), ct);

            queued += result.QueuedMessageIds.Count;

            campaign.ReportProgress(queued, recipients.Count);
            await _campaignRepository.UpdateAsync(campaign, ct);
        }

        return queued;
    }

    /// <summary>
    /// İptal, alıcı üretimi sürerken de etkili olmalıdır: yüz bin kişilik bir kampanyada
    /// "iptal" düğmesinin üretim bitene kadar beklemesi anlamsız olurdu.
    /// </summary>
    private async Task<bool> HandleCancellationAsync(NotificationCampaign campaign, CancellationToken ct)
    {
        var current = await _campaignRepository.FindByIdAsync(campaign.Id, ct);

        if (current is null || current.Status != CampaignStatus.Cancelled)
        {
            return false;
        }

        await _messageRepository.CancelPendingByCampaignAsync(campaign.Id, ct);

        _logger.LogInformation("Kampanya {CampaignId} üretim sırasında iptal edildi.", campaign.Id);

        return true;
    }

    private static NotificationRequest BuildRequest(
        NotificationCampaign campaign,
        IReadOnlyList<NotificationRecipient> to,
        IReadOnlyList<NotificationRecipient> bcc) =>
        new()
        {
            Channel = campaign.Channel,
            Category = NotificationCategory.Marketing,
            To = to,
            Bcc = bcc,
            TemplateCode = campaign.TemplateCode,
            CampaignId = campaign.Id,
            // Kampanyalar işlemsel bildirimlerin gerisinde kalmalı; parola sıfırlama maili
            // yüz bin kişilik bir gönderimin arkasında beklememeli.
            Priority = NotificationPriority.Low
        };

    private static string? ResolveAddress(NotificationChannel channel, UserDirectoryEntry user) =>
        channel == NotificationChannel.Sms ? user.PhoneNumber : user.Email;

    private async Task FailAsync(NotificationCampaign campaign, string error, CancellationToken ct)
    {
        campaign.Fail(error);
        await _campaignRepository.UpdateAsync(campaign, ct);

        _logger.LogWarning("Kampanya {CampaignId} başarısız: {Error}", campaign.Id, error);
    }
}
