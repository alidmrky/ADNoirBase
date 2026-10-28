using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Dtos.Templates;

namespace Notification.Application.Services.Templates;

public interface INotificationTemplateService
{
    Task<BaseResponse<PagedList<TemplateListDto>>> ListAsync(
        TemplateQuery query, CancellationToken ct = default);

    Task<BaseResponse<TemplateDto>> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<BaseResponse<TemplateDto>> CreateAsync(
        CreateTemplateRequest request, CancellationToken ct = default);

    Task<BaseResponse> UpdateAsync(
        Guid id, UpdateTemplateRequest request, CancellationToken ct = default);

    Task<BaseResponse> DeleteAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// İçeriği örnek verilerle render eder. Kaydedilmemiş düzenlemeler de önizlenebilsin diye
    /// içerik istekle birlikte gelir, veritabanından okunmaz.
    /// </summary>
    Task<BaseResponse<TemplatePreviewResponse>> PreviewAsync(
        TemplatePreviewRequest request, CancellationToken ct = default);

    /// <summary>Şablonu yalnızca isteği yapan kullanıcının kendi adresine gönderir.</summary>
    Task<BaseResponse> TestSendAsync(
        Guid id, TemplateTestSendRequest request, CancellationToken ct = default);
}
