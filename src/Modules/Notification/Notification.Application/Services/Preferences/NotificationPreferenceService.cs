using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Dtos.Preferences;
using Notification.Application.Messages;
using Notification.Application.Preferences;
using Notification.Domain.Entities;
using Notification.Domain.Interfaces.Repositories;

namespace Notification.Application.Services.Preferences;

public sealed class NotificationPreferenceService : INotificationPreferenceService
{
    private readonly INotificationPreferenceRepository _repository;
    private readonly IUnsubscribeLinkFactory _linkFactory;

    public NotificationPreferenceService(
        INotificationPreferenceRepository repository,
        IUnsubscribeLinkFactory linkFactory)
    {
        _repository = repository;
        _linkFactory = linkFactory;
    }

    public async Task<BaseResponse<NotificationPreferenceDto>> GetAsync(
        Guid userId, CancellationToken ct = default)
    {
        var preference = await _repository.FindByUserIdAsync(userId, ct);

        // Kayıt yokluğu "varsayılan" demektir; mevcut kullanıcılar için toplu kayıt üretmeyiz.
        var dto = preference is null
            ? new NotificationPreferenceDto(true, true, true, null)
            : ToDto(preference);

        return BaseResponse<NotificationPreferenceDto>.Success(dto);
    }

    public async Task<BaseResponse> UpdateAsync(
        Guid userId, UpdateNotificationPreferenceRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var preference = await _repository.FindByUserIdAsync(userId, ct);

        if (preference is null)
        {
            preference = NotificationPreference.CreateDefault(userId);
            preference.Update(request.EmailMarketing, request.SmsMarketing, request.InAppMarketing);
            await _repository.InsertAsync(preference, ct);
        }
        else
        {
            preference.Update(request.EmailMarketing, request.SmsMarketing, request.InAppMarketing);
            await _repository.UpdateAsync(preference, ct);
        }

        return BaseResponse.Success(NotificationMessageCodes.Preference.Updated);
    }

    public async Task<BaseResponse> UnsubscribeAsync(string token, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Preference.TokenInvalid);
        }

        var hash = _linkFactory.HashToken(token);
        var preference = await _repository.FindByUnsubscribeTokenHashAsync(hash, ct);

        if (preference is null)
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Preference.TokenInvalid);
        }

        // Zaten çıkmış bir kullanıcı için de başarı döneriz: linke ikinci kez tıklamak
        // kullanıcıya hata göstermemelidir.
        if (preference.UnsubscribedAt is null)
        {
            preference.UnsubscribeFromMarketing(DateTime.UtcNow);
            await _repository.UpdateAsync(preference, ct);
        }

        return BaseResponse.Success(NotificationMessageCodes.Preference.Unsubscribed);
    }

    private static NotificationPreferenceDto ToDto(NotificationPreference preference) => new(
        preference.EmailMarketing,
        preference.SmsMarketing,
        preference.InAppMarketing,
        preference.UnsubscribedAt);
}
