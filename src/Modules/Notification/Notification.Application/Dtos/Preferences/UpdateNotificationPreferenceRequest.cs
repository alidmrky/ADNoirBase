namespace Notification.Application.Dtos.Preferences;

public sealed record UpdateNotificationPreferenceRequest(
    bool EmailMarketing,
    bool SmsMarketing,
    bool InAppMarketing);
