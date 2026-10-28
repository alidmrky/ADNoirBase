namespace Notification.Application.Delivery;

public interface ISmsSender
{
    Task<SendResult> SendAsync(OutgoingSms sms, CancellationToken ct = default);
}
