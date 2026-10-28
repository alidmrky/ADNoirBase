using Notification.Domain.Entities;

namespace Notification.Application.Templating;

public interface INotificationComposer
{
    /// <summary>Şablonu, varsa layout'uyla birlikte render eder.</summary>
    Task<ComposedNotification> ComposeAsync(
        NotificationTemplate template,
        IReadOnlyDictionary<string, object?> model,
        CancellationToken ct = default);
}
