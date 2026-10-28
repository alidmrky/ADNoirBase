namespace Notification.Application.Delivery;

/// <summary>
/// Gönderime hazır ek. İçerik GridFS'ten gönderim anında okunur; kuyrukta yalnızca
/// referansı durur.
/// </summary>
public sealed record EmailAttachment(
    string FileName,
    string ContentType,
    byte[] Content,
    bool IsInline = false,
    string? ContentId = null);
