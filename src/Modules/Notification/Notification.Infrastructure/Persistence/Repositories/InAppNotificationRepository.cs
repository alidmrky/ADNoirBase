using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class InAppNotificationRepository
    : MongoRepository<InAppNotification, Guid>, IInAppNotificationRepository
{
    public InAppNotificationRepository(IMongoDatabase database, ICurrentUserService currentUserService)
        : base(database, currentUserService, NotificationCollections.InApp) { }

    public async Task<PagedList<InAppNotification>> ListForUserAsync(
        Guid userId,
        int page,
        int pageSize,
        bool unreadOnly,
        CancellationToken ct = default)
    {
        var filter = Builders<InAppNotification>.Filter.Eq(n => n.UserId, userId);

        if (unreadOnly)
        {
            filter &= Builders<InAppNotification>.Filter.Eq(n => n.ReadAt, null);
        }

        var totalCount = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await Collection.Find(filter)
            .SortByDescending(n => n.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedList<InAppNotification>(items, checked((int)totalCount), page, pageSize);
    }

    public async Task<int> CountUnreadAsync(Guid userId, CancellationToken ct = default)
    {
        var builder = Builders<InAppNotification>.Filter;
        var filter = builder.Eq(n => n.UserId, userId) & builder.Eq(n => n.ReadAt, null);

        return checked((int)await Collection.CountDocumentsAsync(filter, cancellationToken: ct));
    }

    public async Task<long> MarkAllReadAsync(Guid userId, DateTime readAt, CancellationToken ct = default)
    {
        var builder = Builders<InAppNotification>.Filter;
        var filter = builder.Eq(n => n.UserId, userId) & builder.Eq(n => n.ReadAt, null);

        var update = Builders<InAppNotification>.Update.Set(n => n.ReadAt, readAt);
        var result = await Collection.UpdateManyAsync(filter, update, cancellationToken: ct);

        return result.ModifiedCount;
    }
}
