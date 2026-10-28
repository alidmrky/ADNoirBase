using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Dispatching;
using Notification.Application.Dtos;

namespace Notification.Application.Services;

public interface INotificationSendService
{
    Task<BaseResponse<EnqueueResult>> SendAsync(SendNotificationRequest request, CancellationToken ct = default);
}
