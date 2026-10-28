using Notification.Application.Delivery;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Tests;

internal sealed class AttachmentStoreFake : IAttachmentStore
{
    public int Uploads { get; private set; }
    public NotificationAttachmentRef? Stored { get; set; }
    public Task<NotificationAttachmentRef?> FindAsync(string fileId, CancellationToken ct = default) => Task.FromResult(Stored);
    public Task<byte[]?> ReadAllBytesAsync(string fileId, CancellationToken ct = default) => Task.FromResult<byte[]?>(null);
    public Task DeleteAsync(string fileId, CancellationToken ct = default) => Task.CompletedTask;
    public Task<NotificationAttachmentRef> UploadAsync(string fileName, string contentType, Stream content, bool isInline = false, string? contentId = null, CancellationToken ct = default)
    {
        Uploads++;
        Stored = new NotificationAttachmentRef("stored", fileName, contentType, content.Length);
        return Task.FromResult(Stored);
    }
}
