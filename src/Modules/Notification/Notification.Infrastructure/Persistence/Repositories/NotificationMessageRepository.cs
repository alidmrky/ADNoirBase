using System.Text.RegularExpressions;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Infrastructure.Persistence.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;
using Notification.Domain.ValueObjects;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationMessageRepository
    : MongoRepository<NotificationMessage, Guid>, INotificationMessageRepository
{
    public NotificationMessageRepository(IMongoDatabase database, ICurrentUserService currentUserService)
        : base(database, currentUserService, NotificationCollections.Messages) { }

    public async Task<NotificationMessage?> ClaimNextPendingAsync(
        string workerId,
        TimeSpan lockDuration,
        CancellationToken ct = default)
    {
        var now = DateTime.UtcNow;
        var builder = Builders<NotificationMessage>.Filter;

        // Kilidi süresi dolmuş Processing kayıtları da geri alınır: worker gönderim sırasında
        // çökerse mesaj sonsuza kadar Processing'de kalmamalıdır.
        var claimable = builder.Or(
            builder.And(
                builder.Eq(m => m.Status, NotificationStatus.Pending),
                builder.Lte(m => m.NextAttemptAt, now)),
            builder.And(
                builder.Eq(m => m.Status, NotificationStatus.Processing),
                builder.Lt(m => m.LockedUntil, now)));

        var update = Builders<NotificationMessage>.Update
            .Set(m => m.Status, NotificationStatus.Processing)
            .Set(m => m.LockedBy, workerId)
            .Set(m => m.LockedUntil, now.Add(lockDuration))
            .Inc(m => m.Attempts, 1);

        var options = new FindOneAndUpdateOptions<NotificationMessage>
        {
            // Önce yüksek öncelik, eşitlikte en eski. Priority sayı olarak saklanır
            // (bkz. NotificationClassMaps), aksi halde bu sıralama alfabetik olurdu.
            Sort = Builders<NotificationMessage>.Sort
                .Ascending(m => m.Priority)
                .Ascending(m => m.NextAttemptAt),
            ReturnDocument = ReturnDocument.After
        };

        return await Collection.FindOneAndUpdateAsync(claimable, update, options, ct);
    }

    public async Task<long> CancelPendingByCampaignAsync(Guid campaignId, CancellationToken ct = default)
    {
        var builder = Builders<NotificationMessage>.Filter;
        var filter = builder.And(
            builder.Eq(m => m.CampaignId, campaignId),
            builder.In(m => m.Status, new[] { NotificationStatus.Pending, NotificationStatus.Processing }));

        var update = Builders<NotificationMessage>.Update
            .Set(m => m.Status, NotificationStatus.Cancelled)
            .Set(m => m.LockedUntil, (DateTime?)null)
            .Set(m => m.LockedBy, (string?)null);

        var result = await Collection.UpdateManyAsync(filter, update, cancellationToken: ct);
        return result.ModifiedCount;
    }

    public async Task<int> CountByCampaignAsync(Guid campaignId, CancellationToken ct = default) =>
        checked((int)await Collection.CountDocumentsAsync(
            Builders<NotificationMessage>.Filter.Eq(m => m.CampaignId, campaignId),
            cancellationToken: ct));

    public async Task<int> CountUnfinishedByCampaignAsync(
        Guid campaignId, CancellationToken ct = default)
    {
        var builder = Builders<NotificationMessage>.Filter;
        var filter = builder.And(
            builder.Eq(m => m.CampaignId, campaignId),
            builder.In(m => m.Status, new[] { NotificationStatus.Pending, NotificationStatus.Processing }));

        return checked((int)await Collection.CountDocumentsAsync(filter, cancellationToken: ct));
    }

    public async Task<PagedList<NotificationMessage>> SearchAsync(
        int page,
        int pageSize,
        NotificationChannel? channel = null,
        NotificationStatus? status = null,
        Guid? campaignId = null,
        string? recipient = null,
        CancellationToken ct = default)
    {
        var builder = Builders<NotificationMessage>.Filter;
        var filter = Scope(null);

        if (channel.HasValue)
        {
            filter &= builder.Eq(m => m.Channel, channel.Value);
        }

        if (status.HasValue)
        {
            filter &= builder.Eq(m => m.Status, status.Value);
        }

        if (campaignId.HasValue)
        {
            filter &= builder.Eq(m => m.CampaignId, campaignId.Value);
        }

        if (!string.IsNullOrWhiteSpace(recipient))
        {
            var pattern = new BsonRegularExpression(Regex.Escape(recipient.Trim()), "i");
            filter &= builder.ElemMatch(
                m => m.To,
                Builders<NotificationRecipient>.Filter.Regex(r => r.Address, pattern));
        }

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await Collection
            .Find(filter)
            .SortByDescending(m => m.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedList<NotificationMessage>(items, checked((int)total), page, pageSize);
    }
}
