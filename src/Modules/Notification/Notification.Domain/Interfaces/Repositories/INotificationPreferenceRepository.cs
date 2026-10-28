using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Notification.Domain.Entities;

namespace Notification.Domain.Interfaces.Repositories;

public interface INotificationPreferenceRepository : IRepository<NotificationPreference, Guid>
{
    Task<NotificationPreference?> FindByUserIdAsync(Guid userId, CancellationToken ct = default);

    Task<NotificationPreference?> FindByUnsubscribeTokenHashAsync(
        string tokenHash,
        CancellationToken ct = default);
}
