using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Messages;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Application.Services.InApp;

public sealed class InAppNotificationService(IInAppNotificationRepository repository) : IInAppNotificationService
{
    public async Task<BaseResponse<PagedList<InAppNotification>>> ListAsync(Guid userId, int page, int pageSize, bool unreadOnly, CancellationToken ct = default) =>
        BaseResponse<PagedList<InAppNotification>>.Success(await repository.ListForUserAsync(userId, Math.Max(1, page), Math.Clamp(pageSize, 1, 100), unreadOnly, ct));

    public async Task<BaseResponse<int>> CountUnreadAsync(Guid userId, CancellationToken ct = default) =>
        BaseResponse<int>.Success(await repository.CountUnreadAsync(userId, ct));

    public async Task<BaseResponse> MarkReadAsync(Guid userId, Guid id, CancellationToken ct = default)
    {
        var item = await repository.FindByIdAsync(id, ct);
        if (item is null || item.UserId != userId)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.InApp.NotFound);
        }

        item.MarkRead(DateTime.UtcNow);
        await repository.UpdateAsync(item, ct);
        return BaseResponse.Success(NotificationMessageCodes.InApp.MarkedRead);
    }

    public async Task<BaseResponse> MarkAllReadAsync(Guid userId, CancellationToken ct = default)
    {
        await repository.MarkAllReadAsync(userId, DateTime.UtcNow, ct);
        return BaseResponse.Success(NotificationMessageCodes.InApp.MarkedRead);
    }
}
