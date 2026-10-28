using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Dtos.Preferences;

namespace Notification.Application.Services.Preferences;

public interface INotificationPreferenceService
{
    /// <summary>Kaydı olmayan kullanıcı için varsayılan ("her şey açık") değerleri döner.</summary>
    Task<BaseResponse<NotificationPreferenceDto>> GetAsync(Guid userId, CancellationToken ct = default);

    Task<BaseResponse> UpdateAsync(
        Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default);

    /// <summary>Mail içindeki tek tıkla abonelikten çıkma linkini işler.</summary>
    Task<BaseResponse> UnsubscribeAsync(string token, CancellationToken ct = default);
}
