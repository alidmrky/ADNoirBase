using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Infrastructure.Persistence.Mongo;
using MongoDB.Driver;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationPreferenceRepository
    : MongoRepository<NotificationPreference, Guid>, INotificationPreferenceRepository
{
    public NotificationPreferenceRepository(IMongoDatabase database, ICurrentUserService currentUserService)
        : base(database, currentUserService, NotificationCollections.Preferences) { }

    public async Task<NotificationPreference?> FindByUserIdAsync(
        Guid userId, CancellationToken ct = default) =>
        await Collection.Find(Scope(p => p.UserId == userId)).FirstOrDefaultAsync(ct);

    public async Task<NotificationPreference?> FindByUnsubscribeTokenHashAsync(
        string tokenHash, CancellationToken ct = default) =>
        await Collection.Find(Scope(p => p.UnsubscribeTokenHash == tokenHash)).FirstOrDefaultAsync(ct);
}
