using ADNoirBase.Shared.Contracts.Directory;
using Notification.Domain.Enums;

namespace Notification.Domain.ValueObjects;

/// <summary>Kampanyanın hedef kitlesi. Hangi alanın dolu olacağını <see cref="Type"/> belirler.</summary>
public sealed record CampaignAudience(
    CampaignAudienceType Type,
    IReadOnlyList<string> RoleCodes,
    IReadOnlyList<UserDirectoryStatus> Statuses,
    IReadOnlyList<Guid> UserIds,
    IReadOnlyList<string> ManualAddresses)
{
    public static CampaignAudience AllUsers() =>
        new(CampaignAudienceType.AllUsers, [], [], [], []);

    /// <summary>Elle girilen adresler kullanıcı rehberinde aranmaz, doğrudan kuyruğa yazılır.</summary>
    public bool ResolvesFromDirectory => Type != CampaignAudienceType.ManualAddresses;

    /// <summary>IUserDirectory'nin anladığı filtreye çevirir.</summary>
    public UserAudienceFilter ToFilter() => Type switch
    {
        CampaignAudienceType.AllUsers => new UserAudienceFilter(),
        CampaignAudienceType.ByRole => new UserAudienceFilter { RoleCodes = RoleCodes },
        CampaignAudienceType.ByStatus => new UserAudienceFilter { Statuses = Statuses },
        CampaignAudienceType.SelectedUsers => new UserAudienceFilter { UserIds = UserIds },
        CampaignAudienceType.ManualAddresses => throw new InvalidOperationException(
            "Elle girilen adresler kullanıcı rehberinden çözülmez; önce ResolvesFromDirectory kontrol edilmelidir."),
        _ => throw new ArgumentOutOfRangeException(nameof(Type), Type, "Unknown audience type.")
    };
}
