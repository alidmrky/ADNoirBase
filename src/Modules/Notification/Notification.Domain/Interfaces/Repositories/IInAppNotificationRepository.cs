using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Notification.Domain.Entities;

namespace Notification.Domain.Interfaces.Repositories;

public interface IInAppNotificationRepository : IRepository<InAppNotification, Guid>
{
    Task<PagedList<InAppNotification>> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken ct = default);

    Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default);

    Task<long> MarkAllReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default);
}
