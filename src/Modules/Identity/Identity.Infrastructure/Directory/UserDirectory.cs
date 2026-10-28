using System.Runtime.CompilerServices;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Directory;
using Identity.Domain.Entities;
using Identity.Domain.Enums;
using Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Identity.Infrastructure.Directory;

/// <summary>
/// IUserDirectory'nin Identity tarafındaki implementasyonu. Bildirim modülü alıcı listesini
/// bu servis üzerinden çözer ve Identity'ye referans vermek zorunda kalmaz.
/// </summary>
public sealed class UserDirectory : IUserDirectory
{
    private readonly IdentityDbContext _context;

    public UserDirectory(IdentityDbContext context)
    {
        _context = context;
    }

    public async Task<int> CountAsync(UserAudienceFilter filter, CancellationToken ct = default) =>
        await BuildQuery(filter).CountAsync(ct);

    public async IAsyncEnumerable<UserDirectoryEntry> StreamAsync(
        UserAudienceFilter filter,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        // Yalnızca gereken sütunlar çekilir; parola hash'i binlerce alıcı için belleğe alınmamalı.
        // Sıralama olmadan akış tekrar eden kayıtlar üretebilir; Id kararlı bir anahtardır.
        var query = BuildQuery(filter)
            .OrderBy(u => u.Id)
            .Select(u => new
            {
                u.Id,
                u.Email,
                u.PhoneNumber,
                u.FirstName,
                u.LastName,
                u.Status
            })
            .AsAsyncEnumerable();

        await foreach (var row in query.WithCancellation(ct))
        {
            yield return new UserDirectoryEntry(
                row.Id,
                row.Email,
                row.PhoneNumber,
                row.FirstName,
                row.LastName,
                ToDirectoryStatus(row.Status));
        }
    }

    public async Task<UserDirectoryEntry?> FindByIdAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, ct);

        return user is null ? null : ToEntry(user);
    }

    private IQueryable<User> BuildQuery(UserAudienceFilter filter)
    {
        ArgumentNullException.ThrowIfNull(filter);

        // Global query filter silinmiş kullanıcıları zaten eler.
        var query = _context.Users.AsNoTracking();

        if (filter.UserIds.Count > 0)
        {
            var ids = filter.UserIds.ToList();
            query = query.Where(u => ids.Contains(u.Id));
        }

        if (filter.Statuses.Count > 0)
        {
            var statuses = filter.Statuses.Select(ToUserStatus).ToList();
            query = query.Where(u => statuses.Contains(u.Status));
        }

        if (filter.RoleCodes.Count > 0)
        {
            var codes = filter.RoleCodes.Select(c => c.Trim().ToUpperInvariant()).ToList();
            query = query.Where(u => u.UserRoles.Any(ur => codes.Contains(ur.Role.Code)));
        }

        return query;
    }

    private static UserStatus ToUserStatus(UserDirectoryStatus status) => status switch
    {
        UserDirectoryStatus.Active => UserStatus.Active,
        UserDirectoryStatus.Inactive => UserStatus.Inactive,
        UserDirectoryStatus.Locked => UserStatus.Locked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown directory status.")
    };

    private static UserDirectoryStatus ToDirectoryStatus(UserStatus status) => status switch
    {
        UserStatus.Active => UserDirectoryStatus.Active,
        UserStatus.Inactive => UserDirectoryStatus.Inactive,
        UserStatus.Locked => UserDirectoryStatus.Locked,
        _ => throw new ArgumentOutOfRangeException(nameof(status), status, "Unknown user status.")
    };

    private static UserDirectoryEntry ToEntry(User user) =>
        new(user.Id, user.Email, user.PhoneNumber, user.FirstName, user.LastName,
            ToDirectoryStatus(user.Status));
}
