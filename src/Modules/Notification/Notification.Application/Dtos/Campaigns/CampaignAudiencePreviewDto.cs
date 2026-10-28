namespace Notification.Application.Dtos.Campaigns;

/// <summary>
/// Kampanya başlatılmadan önce kaç kişiye gideceğini gösterir. Yönetici "gönder" demeden
/// önce sayıyı görmelidir; geri alınamayan bir işlemdir.
/// </summary>
public sealed record CampaignAudiencePreviewDto(int RecipientCount, bool ExceedsLimit, int Limit);
