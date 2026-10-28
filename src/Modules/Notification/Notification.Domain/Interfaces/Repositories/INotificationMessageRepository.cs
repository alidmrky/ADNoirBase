using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Domain.Interfaces.Repositories;

public interface INotificationMessageRepository : IRepository<NotificationMessage, Guid>
{
    /// <summary>
    /// Gönderilmeyi bekleyen bir mesajı atomik olarak sahiplenir ve döner. Birden fazla sunucu
    /// aynı anda çalıştığında aynı mesajın iki kez gönderilmemesi buna bağlıdır.
    /// Sırayla: önce yüksek öncelik, sonra en eski.
    /// </summary>
    Task<NotificationMessage?> ClaimNextPendingAsync(
        string workerId,
        TimeSpan lockDuration,
        CancellationToken ct = default);

    /// <summary>Kampanya iptal edildiğinde henüz gönderilmemiş mesajlarını iptal eder.</summary>
    Task<long> CancelPendingByCampaignAsync(Guid campaignId, CancellationToken ct = default);

    Task<int> CountByCampaignAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>
    /// Henüz sonuçlanmamış (Pending veya Processing) mesaj sayısı.
    /// Kampanyanın tamamlandığına bu sayı sıfırlandığında karar verilir.
    /// </summary>
    Task<int> CountUnfinishedByCampaignAsync(Guid campaignId, CancellationToken ct = default);

    /// <summary>Gönderim kaydı ekranının filtreli sorgusu.</summary>
    Task<PagedList<NotificationMessage>> SearchAsync(
        int page,
        int pageSize,
        NotificationChannel? channel = null,
        NotificationStatus? status = null,
        Guid? campaignId = null,
        string? recipient = null,
        CancellationToken ct = default);
}
