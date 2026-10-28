using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Serializers;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Infrastructure.Persistence;

public static class NotificationClassMaps
{
    private static int _registered;

    /// <summary>
    /// Enum'lar global olarak string saklanır, ama kuyruk sorgusu önceliğe göre sıralar.
    /// String sıralamada "Low" ile "Normal" arasında alfabetik bir sıra oluşur ve düşük öncelik
    /// normalin önüne geçer; bu yüzden yalnızca bu alan sayı olarak saklanır.
    /// </summary>
    public static void Register()
    {
        if (Interlocked.Exchange(ref _registered, 1) == 1)
        {
            return;
        }

        BsonClassMap.TryRegisterClassMap<NotificationMessage>(map =>
        {
            map.AutoMap();
            map.GetMemberMap(m => m.Priority)
                .SetSerializer(new EnumSerializer<NotificationPriority>(BsonType.Int32));
        });
    }
}
