namespace Notification.Domain.Enums;

public enum CampaignStatus
{
    /// <summary>Hazırlanıyor; henüz alıcı üretilmedi.</summary>
    Draft = 1,

    /// <summary>Başlatıldı, worker'ın almasını bekliyor.</summary>
    Queued = 2,

    /// <summary>Alıcılar parça parça kuyruğa ekleniyor.</summary>
    Expanding = 3,

    /// <summary>Tüm alıcılar kuyruğa eklendi, gönderim sürüyor.</summary>
    Sending = 4,

    Completed = 5,

    Cancelled = 6,

    Failed = 7
}
