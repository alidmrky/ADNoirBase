using ADNoirBase.Shared.Contracts.Directory;
using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Campaigns;

/// <summary>Hangi alanın dolacağını <see cref="Type"/> belirler; diğerleri boş bırakılır.</summary>
public sealed record CampaignAudienceDto(
    CampaignAudienceType Type,
    IReadOnlyList<string>? RoleCodes = null,
    IReadOnlyList<UserDirectoryStatus>? Statuses = null,
    IReadOnlyList<Guid>? UserIds = null,
    IReadOnlyList<string>? ManualAddresses = null);
