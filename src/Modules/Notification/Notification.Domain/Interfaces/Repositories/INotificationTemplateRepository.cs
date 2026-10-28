using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Domain.Interfaces.Repositories;
using Notification.Domain.Entities;
using Notification.Domain.Enums;

namespace Notification.Domain.Interfaces.Repositories;

public interface INotificationTemplateRepository : IRepository<NotificationTemplate, Guid>
{
    Task<NotificationTemplate?> FindByCodeAsync(string code, CancellationToken ct = default);

    /// <summary>
    /// Liste ekranının filtreli sorgusu. Serbest metin araması kültüre bağlı
    /// <c>ToLower</c> çağrısı yerine veritabanının büyük/küçük harf duyarsız eşlemesini kullanır.
    /// </summary>
    Task<PagedList<NotificationTemplate>> SearchAsync(
        int page,
        int pageSize,
        NotificationChannel? channel = null,
        bool? isActive = null,
        string? search = null,
        CancellationToken ct = default);

    /// <summary>Düzenleme sırasında şablonun kendisi çakışma sayılmamalıdır.</summary>
    Task<bool> CodeExistsAsync(string code, Guid? excludeId = null, CancellationToken ct = default);
}
