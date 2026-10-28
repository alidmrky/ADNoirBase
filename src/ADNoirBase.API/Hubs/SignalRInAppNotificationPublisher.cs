using Microsoft.AspNetCore.SignalR;
using Notification.Application.Delivery;

namespace ADNoirBase.API.Hubs;

public sealed class SignalRInAppNotificationPublisher(IHubContext<NotificationHub> hub) : IInAppNotificationPublisher
{
    public Task PublishAsync(Guid userId, InAppNotificationPayload payload, CancellationToken ct = default) =>
        hub.Clients.Group($"user:{userId}").SendAsync("NotificationReceived", payload, ct);
}
