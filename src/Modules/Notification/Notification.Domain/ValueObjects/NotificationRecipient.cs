namespace Notification.Domain.ValueObjects;

/// <summary>
/// Bir alıcı. <paramref name="Address"/> kanala göre e-posta adresi veya telefon numarasıdır.
/// <paramref name="UserId"/> yalnızca alıcı sistemde kayıtlıysa doludur; elle girilen adreslerde null kalır.
/// </summary>
public sealed record NotificationRecipient(
    string Address,
    string? DisplayName = null,
    Guid? UserId = null);
