using System.Linq.Expressions;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Domain.Entities;

namespace ADNoirBase.Shared.Domain.Interfaces.Repositories;

/// <summary>
/// Tek repository arayüzü — hem PostgreSQL hem MongoDB implementasyonu bu kontratı kullanır.
/// DeletedBy ICurrentUserService üzerinden Infrastructure katmanı tarafından otomatik doldurulur.
/// </summary>
public interface IRepository<TEntity, TId>
    where TEntity : BaseEntity<TId>
    where TId : IEquatable<TId>
{
    // ─── Okuma ───────────────────────────────────────────────────────────────

    Task<TEntity?> FindByIdAsync(TId id, CancellationToken ct = default);

    Task<TEntity?> FindAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<IReadOnlyList<TEntity>> ListLimitedAsync(
        int limit,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<PagedList<TEntity>> ListPagedAsync(
        int page,
        int pageSize,
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<int> CountAsync(
        Expression<Func<TEntity, bool>>? predicate = null,
        CancellationToken ct = default);

    Task<bool> AnyAsync(
        Expression<Func<TEntity, bool>> predicate,
        CancellationToken ct = default);

    // ─── Yazma ───────────────────────────────────────────────────────────────

    Task<TEntity> InsertAsync(TEntity entity, CancellationToken ct = default);

    Task InsertManyAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

    Task UpdateAsync(TEntity entity, CancellationToken ct = default);

    // ─── Silme (soft delete) ──────────────────────────────────────────────────

    Task DeleteAsync(TEntity entity, CancellationToken ct = default);

    Task DeleteByIdAsync(TId id, CancellationToken ct = default);
}
