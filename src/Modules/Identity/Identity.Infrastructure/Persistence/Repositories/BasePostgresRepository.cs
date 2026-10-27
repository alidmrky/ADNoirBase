using System.Linq.Expressions;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Domain.Entities;
using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Persistence.Repositories;

/// <summary>
/// PostgreSQL için generic IRepository implementasyonu.
/// Soft delete, AuditInterceptor ve global query filter bu sınıfla çalışır.
/// </summary>
public abstract class BasePostgresRepository<TEntity, TId> : IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
    where TId : IEquatable<TId>
{
    protected readonly IdentityDbContext Context;
    protected readonly DbSet<TEntity> DbSet;
    private readonly ICurrentUserService _currentUserService;

    protected BasePostgresRepository(IdentityDbContext context, ICurrentUserService currentUserService)
    {
        Context = context;
        DbSet = context.Set<TEntity>();
        _currentUserService = currentUserService;
    }

    public virtual async Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default) =>
        await DbSet.FindAsync([id], ct);

    public virtual async Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.FirstOrDefaultAsync(predicate, ct);

    public virtual async Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default)
    {
        var query = predicate is null ? DbSet : DbSet.Where(predicate);
        return await query.ToListAsync(ct);
    }

    public virtual async Task<IReadOnlyList<TEntity>> ListLimitedAsync(
        int limit,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var query = predicate is null ? DbSet : DbSet.Where(predicate);
        return await query.Take(limit).ToListAsync(ct);
    }

    public virtual async Task<PagedList<TEntity>> ListPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default)
    {
        var query = predicate is null ? DbSet.AsQueryable() : DbSet.Where(predicate);
        var totalCount = await query.CountAsync(ct);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(ct);

        return new PagedList<TEntity>(items, totalCount, page, pageSize);
    }

    public virtual async Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null, CancellationToken ct = default) =>
        predicate is null
            ? await DbSet.CountAsync(ct)
            : await DbSet.CountAsync(predicate, ct);

    public virtual async Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default) =>
        await DbSet.AnyAsync(predicate, ct);

    public virtual async Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default)
    {
        await DbSet.AddAsync(entity, ct);
        await Context.SaveChangesAsync(ct);
        return entity;
    }

    public virtual async Task InsertManyAsync(
        IEnumerable<TEntity> entities, CancellationToken ct = default)
    {
        await DbSet.AddRangeAsync(entities, ct);
        await Context.SaveChangesAsync(ct);
    }

    public virtual async Task UpdateAsync(TEntity entity, CancellationToken ct = default)
    {
        DbSet.Update(entity);
        await Context.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteAsync(TEntity entity, CancellationToken ct = default)
    {
        if (entity is SoftDeletableEntity<TId> softDeletable)
        {
            var deletedBy = _currentUserService.UserId ?? "system";
            softDeletable.SoftDelete(deletedBy);
            DbSet.Update(entity);
        }
        else
        {
            DbSet.Remove(entity);
        }
        await Context.SaveChangesAsync(ct);
    }

    public virtual async Task DeleteByIdAsync(TId id, CancellationToken ct = default)
    {
        var entity = await FindByIdAsync(id, ct);
        if (entity is not null)
            await DeleteAsync(entity, ct);
    }
}
