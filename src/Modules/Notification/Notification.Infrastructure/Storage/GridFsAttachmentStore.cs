using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GridFS;
using Notification.Application.Delivery;
using Notification.Domain.ValueObjects;

namespace Notification.Infrastructure.Storage;

public sealed class GridFsAttachmentStore : IAttachmentStore
{
    private readonly GridFSBucket _bucket;

    public GridFsAttachmentStore(IMongoDatabase database) =>
        _bucket = new GridFSBucket(database, new GridFSBucketOptions { BucketName = "notification_attachments" });

    public async Task<NotificationAttachmentRef> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        bool isInline = false,
        string? contentId = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(content);

        var options = new GridFSUploadOptions
        {
            Metadata = new BsonDocument
            {
                { "contentType", contentType },
                { "isInline", isInline }
            }
        };

        var fileId = await _bucket.UploadFromStreamAsync(fileName, content, options, ct);
        var length = await GetLengthAsync(fileId, ct);

        return new NotificationAttachmentRef(
            fileId.ToString(), fileName, contentType, length, isInline, contentId);
    }

    public async Task<byte[]?> ReadAllBytesAsync(string fileId, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(fileId, out var objectId))
        {
            return null;
        }

        try
        {
            return await _bucket.DownloadAsBytesAsync(objectId, cancellationToken: ct);
        }
        catch (GridFSFileNotFoundException)
        {
            return null;
        }
    }

    public async Task<NotificationAttachmentRef?> FindAsync(string fileId, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(fileId, out var id))
        {
            return null;
        }

        using var cursor = await _bucket.FindAsync(Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, id), cancellationToken: ct);
        var file = await cursor.FirstOrDefaultAsync(ct);
        return file is null ? null : new NotificationAttachmentRef(fileId, file.Filename,
            file.Metadata.GetValue("contentType", "application/octet-stream").AsString, file.Length,
            file.Metadata.GetValue("isInline", false).AsBoolean);
    }

    public async Task DeleteAsync(string fileId, CancellationToken ct = default)
    {
        if (!ObjectId.TryParse(fileId, out var objectId))
        {
            return;
        }

        try
        {
            await _bucket.DeleteAsync(objectId, ct);
        }
        catch (GridFSFileNotFoundException)
        {
            // Silinmiş bir dosyayı tekrar silmek hata değildir.
        }
    }

    /// <summary>Yükleme sonrası gerçek boyut; istemcinin bildirdiği uzunluğa güvenilmez.</summary>
    private async Task<long> GetLengthAsync(ObjectId fileId, CancellationToken ct)
    {
        var filter = Builders<GridFSFileInfo>.Filter.Eq(f => f.Id, fileId);
        var info = await (await _bucket.FindAsync(filter, cancellationToken: ct)).FirstOrDefaultAsync(ct);

        return info?.Length ?? 0;
    }
}
