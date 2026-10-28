using ADNoirBase.Shared.Application.Events;
using ADNoirBase.Shared.Contracts.IntegrationEvents;
using Notification.Application.Dispatching;
using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Events;

public sealed class UserRegisteredNotificationHandler(INotificationDispatcher dispatcher) : IIntegrationEventHandler<UserRegisteredIntegrationEvent>
{
    public async Task HandleAsync(UserRegisteredIntegrationEvent integrationEvent, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(integrationEvent);
        var result = await dispatcher.EnqueueAsync(new NotificationRequest
        {
            Channel = NotificationChannel.Email,
            Category = NotificationCategory.Transactional,
            TemplateCode = "identity.welcome",
            To = [new NotificationRecipient(integrationEvent.Email, integrationEvent.FirstName + " " + integrationEvent.LastName, integrationEvent.UserId)]
        }, ct);
        if (result.Errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("; ", result.Errors));
        }
    }
}

