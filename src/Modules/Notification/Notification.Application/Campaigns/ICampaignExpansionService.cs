using Notification.Domain.Entities;

namespace Notification.Application.Campaigns;

public interface ICampaignExpansionService
{
    /// <summary>
    /// Kampanyanın hedef kitlesini alıcı alıcı kuyruğa yazar. Uzun sürebilir;
    /// yalnızca arka plan worker'ı tarafından çağrılmalıdır.
    /// </summary>
    Task ExpandAsync(NotificationCampaign campaign, CancellationToken ct = default);

    /// <summary>
    /// Gönderimi süren kampanyalardan mesajları tükenmiş olanları tamamlanmış işaretler.
    /// </summary>
    Task CompleteFinishedCampaignsAsync(CancellationToken ct = default);
}
