using ADNoirBase.Shared.Application.Common;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Dtos.Campaigns;

namespace Notification.Application.Services.Campaigns;

public interface INotificationCampaignService
{
    Task<BaseResponse<PagedList<CampaignListDto>>> ListAsync(
        PagedRequest request, CancellationToken ct = default);

    Task<BaseResponse<CampaignDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<BaseResponse<CampaignDto>> CreateAsync(
        CreateCampaignRequest request, CancellationToken ct = default);

    /// <summary>Gönderimden önce hedef kitlenin büyüklüğünü hesaplar.</summary>
    Task<BaseResponse<CampaignAudiencePreviewDto>> PreviewAudienceAsync(
        CampaignAudienceDto audience, CancellationToken ct = default);

    /// <summary>Kampanyayı kuyruğa alır; alıcı üretimi arka planda yapılır.</summary>
    Task<BaseResponse> StartAsync(Guid id, CancellationToken ct = default);

    /// <summary>Kampanyayı ve henüz gönderilmemiş mesajlarını iptal eder.</summary>
    Task<BaseResponse> CancelAsync(Guid id, CancellationToken ct = default);

    Task<BaseResponse> DeleteAsync(Guid id, CancellationToken ct = default);
}
