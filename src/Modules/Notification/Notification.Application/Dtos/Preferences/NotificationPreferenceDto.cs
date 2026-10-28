namespace Notification.Application.Dtos.Preferences;

public sealed record NotificationPreferenceDto(
    bool EmailMarketing,
    bool SmsMarketing,
    bool InAppMarketing,
    DateTime? UnsubscribedAt);
