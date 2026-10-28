namespace Notification.Domain.Enums;

/// <summary>Kuyruktan çekilme sırası. Toplu kampanyalar işlemsel maillerin önüne geçmemelidir.</summary>
public enum NotificationPriority
{
    High = 1,
    Normal = 2,
    Low = 3
}
