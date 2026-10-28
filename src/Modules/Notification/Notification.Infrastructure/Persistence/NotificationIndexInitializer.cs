using MongoDB.Driver;
using Notification.Domain.Entities;

namespace Notification.Infrastructure.Persistence;

/// <summary>
/// Bildirim koleksiyonlarının index'lerini oluşturur. DbMigrator tarafından çağrılır —
/// index oluşturmak her uygulama açılışında yapılacak bir iş değildir.
/// <c>CreateMany</c> aynı tanım için tekrar çalıştırılabilir.
/// </summary>
public sealed class NotificationIndexInitializer
{
    private readonly IMongoDatabase _database;

    public NotificationIndexInitializer(IMongoDatabase database)
    {
        _database = database;
    }

    public async Task CreateIndexesAsync(CancellationToken ct = default)
    {
        await CreateTemplateIndexesAsync(ct);
        await CreateMessageIndexesAsync(ct);
        await CreateCampaignIndexesAsync(ct);
        await CreateInAppIndexesAsync(ct);
        await CreatePreferenceIndexesAsync(ct);
    }

    private async Task CreateTemplateIndexesAsync(CancellationToken ct)
    {
        var collection = _database.GetCollection<NotificationTemplate>(NotificationCollections.Templates);
        var keys = Builders<NotificationTemplate>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
            [
                // Soft delete edilmiş bir şablonun kodu yeniden kullanılabilmeli.
                new CreateIndexModel<NotificationTemplate>(
                    keys.Ascending(t => t.Code),
                    new CreateIndexOptions<NotificationTemplate>
                    {
                        Name = "ux_code_active",
                        Unique = true,
                        PartialFilterExpression =
                            Builders<NotificationTemplate>.Filter.Eq(t => t.IsDeleted, false)
                    }),
                new CreateIndexModel<NotificationTemplate>(
                    keys.Ascending(t => t.Channel).Ascending(t => t.IsActive),
                    new CreateIndexOptions { Name = "ix_channel_active" })
            ],
            ct);
    }

    private async Task CreateMessageIndexesAsync(CancellationToken ct)
    {
        var collection = _database.GetCollection<NotificationMessage>(NotificationCollections.Messages);
        var keys = Builders<NotificationMessage>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
            [
                // Kuyruktan çekme sorgusunun tam karşılığı; worker sayısı arttıkça kritikleşir.
                new CreateIndexModel<NotificationMessage>(
                    keys.Ascending(m => m.Status)
                        .Ascending(m => m.Priority)
                        .Ascending(m => m.NextAttemptAt),
                    new CreateIndexOptions { Name = "ix_queue" }),
                new CreateIndexModel<NotificationMessage>(
                    keys.Ascending(m => m.CampaignId),
                    new CreateIndexOptions { Name = "ix_campaign" }),
                new CreateIndexModel<NotificationMessage>(
                    keys.Descending(m => m.CreatedAt),
                    new CreateIndexOptions { Name = "ix_created_desc" }),

                // ExpiresAt null olan kayıtlar süresiz saklanır; TTL yalnızca dolu olanları siler.
                new CreateIndexModel<NotificationMessage>(
                    keys.Ascending(m => m.ExpiresAt),
                    new CreateIndexOptions { Name = "ttl_expires", ExpireAfter = TimeSpan.Zero })
            ],
            ct);
    }

    private async Task CreateCampaignIndexesAsync(CancellationToken ct)
    {
        var collection = _database.GetCollection<NotificationCampaign>(NotificationCollections.Campaigns);
        var keys = Builders<NotificationCampaign>.IndexKeys;

        await collection.Indexes.CreateOneAsync(
            new CreateIndexModel<NotificationCampaign>(
                keys.Ascending(c => c.Status).Ascending(c => c.ScheduledAt),
                new CreateIndexOptions { Name = "ix_due" }),
            cancellationToken: ct);
    }

    private async Task CreateInAppIndexesAsync(CancellationToken ct)
    {
        var collection = _database.GetCollection<InAppNotification>(NotificationCollections.InApp);
        var keys = Builders<InAppNotification>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<InAppNotification>(
                    keys.Ascending(n => n.UserId)
                        .Ascending(n => n.ReadAt)
                        .Descending(n => n.CreatedAt),
                    new CreateIndexOptions { Name = "ix_user_unread" }),
                new CreateIndexModel<InAppNotification>(
                    keys.Ascending(n => n.ExpiresAt),
                    new CreateIndexOptions { Name = "ttl_expires", ExpireAfter = TimeSpan.Zero })
            ],
            ct);
    }

    private async Task CreatePreferenceIndexesAsync(CancellationToken ct)
    {
        var collection = _database.GetCollection<NotificationPreference>(NotificationCollections.Preferences);
        var keys = Builders<NotificationPreference>.IndexKeys;

        await collection.Indexes.CreateManyAsync(
            [
                new CreateIndexModel<NotificationPreference>(
                    keys.Ascending(p => p.UserId),
                    new CreateIndexOptions { Name = "ux_user", Unique = true }),

                // Token'ı olmayan kayıtlar çok olacağı için seyrek index; aksi halde
                // null değerler benzersizlik kuralını ihlal ederdi.
                new CreateIndexModel<NotificationPreference>(
                    keys.Ascending(p => p.UnsubscribeTokenHash),
                    new CreateIndexOptions { Name = "ux_unsubscribe_token", Unique = true, Sparse = true })
            ],
            ct);
    }
}
