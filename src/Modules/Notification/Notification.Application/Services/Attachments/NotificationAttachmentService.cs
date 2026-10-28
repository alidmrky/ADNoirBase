using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Delivery;
using Notification.Application.Messages;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Services.Attachments;

public sealed class NotificationAttachmentService(IAttachmentStore store, IOptions<NotificationSettings> options) : INotificationAttachmentService
{
    private readonly AttachmentSettings _settings = options.Value.Attachments;

    public async Task<BaseResponse<NotificationAttachmentRef>> UploadAsync(string fileName, string contentType, Stream content, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);
        if (!_settings.AllowedContentTypes.Contains(contentType, StringComparer.OrdinalIgnoreCase))
        {
            return BaseResponse<NotificationAttachmentRef>.BusinessRuleViolation(NotificationMessageCodes.Attachment.TypeNotAllowed);
        }

        // Measure the stream itself; Content-Length and client metadata are not trusted.
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await content.ReadAsync(chunk, ct)) > 0)
        {
            if (buffer.Length + read > _settings.MaxBytes)
            {
                return BaseResponse<NotificationAttachmentRef>.BusinessRuleViolation(NotificationMessageCodes.Attachment.TooLarge);
            }

            await buffer.WriteAsync(chunk.AsMemory(0, read), ct);
        }
        buffer.Position = 0;
        var safeName = Path.GetFileName(fileName.Replace('\\', '/')).Replace("\r", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);
        var attachment = await store.UploadAsync(string.IsNullOrWhiteSpace(safeName) ? "attachment" : safeName, contentType, buffer, ct: ct);
        return BaseResponse<NotificationAttachmentRef>.Created(attachment, NotificationMessageCodes.Attachment.Uploaded);
    }

    public async Task<BaseResponse<IReadOnlyList<NotificationAttachmentRef>>> ResolveAsync(IReadOnlyList<string> fileIds, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(fileIds);
        if (fileIds.Count > _settings.MaxCount)
        {
            return BaseResponse<IReadOnlyList<NotificationAttachmentRef>>.BusinessRuleViolation(NotificationMessageCodes.Attachment.TooMany);
        }

        var result = new List<NotificationAttachmentRef>();
        foreach (var id in fileIds.Distinct(StringComparer.Ordinal))
        {
            var item = await store.FindAsync(id, ct);
            if (item is null)
            {
                return BaseResponse<IReadOnlyList<NotificationAttachmentRef>>.NotFound(NotificationMessageCodes.Attachment.NotFound);
            }

            if (item.Length > _settings.MaxBytes)
            {
                return BaseResponse<IReadOnlyList<NotificationAttachmentRef>>.BusinessRuleViolation(NotificationMessageCodes.Attachment.TooLarge);
            }

            if (!_settings.AllowedContentTypes.Contains(item.ContentType, StringComparer.OrdinalIgnoreCase))
            {
                return BaseResponse<IReadOnlyList<NotificationAttachmentRef>>.BusinessRuleViolation(NotificationMessageCodes.Attachment.TypeNotAllowed);
            }

            result.Add(item);
        }
        return BaseResponse<IReadOnlyList<NotificationAttachmentRef>>.Success(result);
    }
}
