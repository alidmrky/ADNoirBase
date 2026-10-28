using System.Linq.Expressions;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Domain.Entities;
using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using MongoDB.Driver;

namespace ADNoirBase.Shared.Infrastructure.Persistence.Mongo;

/// <summary>
/// MongoDB için generic IRepository implementasyonu.
/// EF Core'daki AuditInterceptor ve global query filter burada yok; audit alanları ve
/// soft delete filtresi bu sınıf tarafından uygulanır.
/// Mongo Id üretmez — entity'ler Id'lerini factory metodunda atamalıdır (Guid veya string).
/// </summary>
public abstract class MongoRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
    where TId : IEquatable<TId>
{
    private static readonly FilterDefinition<TEntity> NotDeletedFilter = BuildNotDeletedFilter();

    private readonly ICurrentUserService _currentUserService;

    protected IMongoCollection<TEntity> Collection { get; }

    protected MongoRepository(
        IMongoDatabase database,
        ICurrentUserService currentUserService,
        string collectionName)
    {
        Collection = database.GetCollection<TEntity>(collectionName);
        _currentUserService = currentUserService;
    }

    // ─── Okuma ───────────────────────────────────────────────────────────────

    public virtual async Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default) =>
        await Collection.Find(NotDeletedFilter & ById(id)).FirstOrDefaultAsync(ct);

    public virtual async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        await Collection.Find(Scope(predicate)).FirstOrDefaultAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default) =>
        await Collection.Find(Scope(predicate)).ToListAsync(ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListLimitedAsync(
        int limit,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default) =>
        await Collection.Find(Scope(predicate)).Limit(limit).ToListAsync(ct);

    public virtual async Task<PagedList<TEntity>> ListPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var filter = Scope(predicate);
        var totalCount = await Collection.CountDocumentsAsync(filter, cancellationToken: ct);

        // Sıralama olmadan Mongo sayfalar arasında aynı dökümanı tekrar döndürebilir.
        var items = await Collection.Find(filter)
            .SortBy(e => e.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(ct);

        return new PagedList<TEntity>(items, checked((int)totalCount), page, pageSize);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default) =>
        checked((int)await Collection.CountDocumentsAsync(Scope(predicate), cancellationToken: ct));

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        await Collection.Find(Scope(predicate)).Limit(1).AnyAsync(ct);

    // ─── Yazma ───────────────────────────────────────────────────────────────

    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default)
    {
        MarkCreated(entity);
        await Collection.InsertOneAsync(entity, cancellationToken: ct);
        return entity;
    }

    public virtual async Task InsertManyAsync(
        IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        var list = entities.ToList();

        // Driver boş listeyi hata olarak reddeder; EF Core tarafındaki davranışla aynı kalsın.
        if (list.Count == 0)
        {
            return;
        }

        foreach (var entity in list)
        {
            MarkCreated(entity);
        }

        await Collection.InsertManyAsync(list, cancellationToken: ct);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity is IFullAuditableEntity auditable)
        {
            auditable.MarkUpdated(DateTime.UtcNow, CurrentUserId);
        }

        await Collection.ReplaceOneAsync(ById(entity.Id), entity, cancellationToken: ct);
    }

    // ─── Silme (soft delete) ──────────────────────────────────────────────────

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity is SoftDeletableEntity<TId> softDeletable)
        {
            softDeletable.SoftDelete(CurrentUserId);
            await UpdateAsync(entity, ct);
        }
        else
        {
            await Collection.DeleteOneAsync(ById(entity.Id), ct);
        }
    }

    public virtual async Task DeleteByIdAsync(TId id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(id, ct);
        if (entity is not null)
        {
            await DeleteAsync(entity, ct);
        }
    }

    // ─── Yardımcılar ─────────────────────────────────────────────────────────

    protected static FilterDefinition<TEntity> ById(TId id) =>
        Builders<TEntity>.Filter.Eq(e => e.Id, id);

    /// <summary>Kullanıcı filtresini soft delete filtresiyle birleştirir; türetilmiş sorgular da bunu kullanmalıdır.</summary>
    protected static FilterDefinition<TEntity> Scope(Expression<Func<TEntity, bool>>? predicate) =>
        predicate is null
            ? NotDeletedFilter
            : NotDeletedFilter & Builders<TEntity>.Filter.Where(predicate);

    private string CurrentUserId => _currentUserService.GetAuditUserId();

    private void MarkCreated(TEntity entity)
    {
        if (entity is IAuditableEntity auditable)
        {
            auditable.MarkCreated(DateTime.UtcNow, CurrentUserId);
        }
    }

    private static FilterDefinition<TEntity> BuildNotDeletedFilter()
    {
        if (!typeof(SoftDeletableEntity<TId>).IsAssignableFrom(typeof(TEntity)))
        {
            return Builders<TEntity>.Filter.Empty;
        }

        // e => !e.IsDeleted — TEntity üzerinden kurulur ki driver alan adını class map'ten çözsün.
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var isDeleted = Expression.Property(parameter, nameof(SoftDeletableEntity<TId>.IsDeleted));
        var lambda = Expression.Lambda<Func<TEntity, bool>>(Expression.Not(isDeleted), parameter);

        return Builders<TEntity>.Filter.Where(lambda);
    }
}
