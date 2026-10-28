using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Notification.Domain.Entities;

namespace Notification.Application.Services.InApp;

public interface IInAppNotificationService
{
    Task<BaseResponse<PagedList<InAppNotification>>> ListAsync(Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken ct = default);
    Task<BaseResponse<int>> CountUnreadAsync(Guid userId, CancellationToken ct = default);
    Task<BaseResponse> MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default);
    Task<BaseResponse> MarkAllReadAsync(Guid userId, CancellationToken ct = default);
}
