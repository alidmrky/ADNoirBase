using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Notification.Domain.Entities;

namespace Notification.Domain.Interfaces.Repositories;

public interface INotificationCampaignRepository : IRepository<NotificationCampaign, Guid>
{
    /// <summary>
    /// Zamanı gelmiş, sıradaki kampanyayı atomik olarak sahiplenir.
    /// Alıcı üretimi uzun sürdüğü için iki worker'ın aynı kampanyayı açması alıcıları ikiye katlardı.
    /// </summary>
    Task<NotificationCampaign?> ClaimNextDueAsync(DateTime now, CancellationToken ct = default);
}
