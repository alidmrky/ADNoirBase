using ADNoirBase.Shared.Contracts.Responses;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Services.Attachments;

public interface INotificationAttachmentService
{
    Task<BaseResponse<NotificationAttachmentRef>> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default);
    Task<BaseResponse<IReadOnlyList<NotificationAttachmentRef>>> ResolveAsync(IReadOnlyList<string> fileIds, CancellationToken ct = default);
}
