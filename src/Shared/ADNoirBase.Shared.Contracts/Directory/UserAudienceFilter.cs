namespace ADNoirBase.Shared.Contracts.Directory;

/// <summary>
/// Toplu gönderimin hedef kitlesi. Boş bırakılan her alan "bu kritere göre daraltma" anlamına gelir;
/// hepsi boşsa tüm kullanıcılar seçilmiş olur.
/// </summary>
public sealed record UserAudienceFilter
{
    public IReadOnlyList<string> RoleCodes { get; init; } = [];

    public IReadOnlyList<UserDirectoryStatus> Statuses { get; init; } = [];

    public IReadOnlyList<Guid> UserIds { get; init; } = [];
}
