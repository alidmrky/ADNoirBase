using Notification.Domain.ValueObjects;

namespace Notification.Application.Delivery;

/// <summary>
/// Ek dosyalarının deposu. Dosyalar mesaj dökümanına gömülmez: MongoDB döküman sınırı
/// 16 MB'dır ve kuyrukta bekleyen mesajların içeriği belleğe yüklenmemelidir.
/// </summary>
public interface IAttachmentStore
{
    Task<NotificationAttachmentRef?> FindAsync(string fileId, CancellationToken ct = default);

    Task<NotificationAttachmentRef> UploadAsync(
        string fileName,
        string contentType,
        Stream content,
        bool isInline = false,
        string? contentId = null,
        CancellationToken ct = default);

    /// <summary>Dosya bulunamazsa null döner; silinmiş bir ek gönderimi tümden düşürmemelidir.</summary>
    Task<byte[]?> ReadAllBytesAsync(string fileId, CancellationToken ct = default);

    Task DeleteAsync(string fileId, CancellationToken ct = default);
}
