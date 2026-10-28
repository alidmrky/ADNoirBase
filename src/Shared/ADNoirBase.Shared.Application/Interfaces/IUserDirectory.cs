using ADNoirBase.Shared.Contracts.Directory;

namespace ADNoirBase.Shared.Application.Interfaces;

/// <summary>
/// Kullanıcı bilgisine modül sınırını aşmadan erişim. Identity modülü implement eder,
/// bildirim modülü alıcı listesini bunun üzerinden çözer.
/// </summary>
public interface IUserDirectory
{
    Task<int> CountAsync(UserAudienceFilter filter, CancellationToken ct = default);

    /// <summary>
    /// Hedef kitleyi parça parça döner. Kampanya alıcıları on binlerce olabileceği için
    /// liste tek seferde belleğe alınmaz.
    /// </summary>
    IAsyncEnumerable<UserDirectoryEntry> StreamAsync(
        UserAudienceFilter filter,
        CancellationToken ct = default);

    Task<UserDirectoryEntry?> FindByIdAsync(Guid userId, CancellationToken ct = default);
}
