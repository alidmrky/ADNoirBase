using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ADNoirBase.Shared.Infrastructure.Persistence.Interceptors;

/// <summary>
/// SaveChanges sırasında audit alanlarını otomatik doldurur.
/// Added → CreatedAt, CreatedBy
/// Modified → UpdatedAt, UpdatedBy
/// </summary>
public sealed class AuditInterceptor : SaveChangesInterceptor
{
    private readonly ICurrentUserService _currentUserService;

    public AuditInterceptor(ICurrentUserService currentUserService)
    {
        _currentUserService = currentUserService;
    }

    public override InterceptionResult<int> SavingChanges(
        DbContextEventData eventData,
        InterceptionResult<int> result)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        UpdateAuditFields(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private void UpdateAuditFields(DbContext? context)
    {
        if (context is null) return;

        var now = DateTime.UtcNow;
        var userId = _currentUserService.IsAuthenticated
            ? _currentUserService.UserId ?? "system"
            : "system";

        foreach (var entry in context.ChangeTracker.Entries())
        {
            if (entry.State == EntityState.Added && entry.Entity is AuditableEntity<Guid> addedGuid)
            {
                entry.Property(nameof(AuditableEntity<Guid>.CreatedAt)).CurrentValue = now;
                entry.Property(nameof(AuditableEntity<Guid>.CreatedBy)).CurrentValue = userId;
            }
            else if (entry.State == EntityState.Modified && entry.Entity is FullAuditableEntity<Guid> modifiedGuid)
            {
                entry.Property(nameof(FullAuditableEntity<Guid>.UpdatedAt)).CurrentValue = now;
                entry.Property(nameof(FullAuditableEntity<Guid>.UpdatedBy)).CurrentValue = userId;
                // CreatedAt ve CreatedBy değiştirilemez
                entry.Property(nameof(AuditableEntity<Guid>.CreatedAt)).IsModified = false;
                entry.Property(nameof(AuditableEntity<Guid>.CreatedBy)).IsModified = false;
            }
        }
    }
}
