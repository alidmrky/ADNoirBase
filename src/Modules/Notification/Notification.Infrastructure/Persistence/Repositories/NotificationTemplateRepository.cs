using System.Text.RegularExpressions;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Infrastructure.Persistence.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Infrastructure.Persistence.Repositories;

public sealed class NotificationTemplateRepository
    : MongoRepository<NotificationTemplate, Guid>, INotificationTemplateRepository
{
    public NotificationTemplateRepository(IMongoDatabase database, ICurrentUserService currentUserService)
        : base(database, currentUserService, NotificationCollections.Templates) { }

    public async Task<NotificationTemplate?> FindByCodeAsync(string code, CancellationToken ct = default)
    {
        var normalized = NotificationTemplate.NormalizeCode(code);
        return await Collection.Find(Scope(t => t.Code == normalized)).FirstOrDefaultAsync(ct);
    }

    public async Task<bool> CodeExistsAsync(
        string code, Guid? excludeId = null, CancellationToken ct = default)
    {
        var normalized = NotificationTemplate.NormalizeCode(code);
        var filter = Scope(t => t.Code == normalized);

        if (excludeId.HasValue)
        {
            filter &= Builders<NotificationTemplate>.Filter.Ne(t => t.Id, excludeId.Value);
        }

        return await Collection.Find(filter).Limit(1).AnyAsync(ct);
    }

    public async Task<PagedList<NotificationTemplate>> SearchAsync(
        int page,
        int pageSize,
        NotificationChannel? channel = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default)
    {
        var builder = Builders<NotificationTemplate>.Filter;
        var filter = Scope(null);

        if (channel.HasValue)
        {
            filter &= builder.Eq(t => t.Channel, channel.Value);
        }

        if (isActive.HasValue)
        {
            filter &= builder.Eq(t => t.IsActive, isActive.Value);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            var pattern = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filter &= builder.Or(
                builder.Regex(t => t.Code, pattern),
                builder.Regex(t => t.Name, pattern));
        }

        var total = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);

        var items = await Collection
            .Find(filter)
            .SortBy(t => t.Code)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedList<NotificationTemplate>(items, (int)total, page, pageSize);
    }
}
