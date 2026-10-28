using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationCampaignRepository
    : MongoRepository<NotificationCampaign, Guid>, INotificationCampaignRepository
{
    public NotificationCampaignRepository(IMongoDatabase database, ICurrentUserService currentUserService)
        : base(database, currentUserService, NotificationCollections.Campaigns) { }

    public async Task<NotificationCampaign?> ClaimNextDueAsync(
        DateTime now, CancellationToken ct = default)
    {
        var builder = Builders<NotificationCampaign>.Filter;
        var due = builder.And(
            builder.Eq(c => c.IsDeleted, false),
            builder.Eq(c => c.Status, CampaignStatus.Queued),
            builder.Or(
                builder.Eq(c => c.ScheduledAt, null),
                builder.Lte(c => c.ScheduledAt, now)));

        // Durumu tek adımda Expanding'e çekmek, iki worker'ın aynı kampanyanın alıcılarını
        // iki kez üretmesini engeller.
        var update = Builders<NotificationCampaign>.Update
            .Set(c => c.Status, CampaignStatus.Expanding)
            .Set(c => c.StartedAt, now)
            .Set(c => c.QueuedCount, 0);

        var options = new FindOneAndUpdateOptions<NotificationCampaign>
        {
            Sort = Builders<NotificationCampaign>.Sort.Ascending(c => c.CreatedAt),
            ReturnDocument = ReturnDocument.After
        };

        return await Collection.FindOneAndUpdateAsync(due, update, options, ct);
    }
}
