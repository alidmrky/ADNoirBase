namespace Notification.Domain.ValueObjects;

/// <summary>
/// Ekin kendisi değil, GridFS'teki kaydına işaret. Kuyrukta bekleyen binlerce mesajın
/// dosya içeriğini taşıması belleği ve döküman boyut sınırını zorlar.
/// </summary>
/// <param name="ContentId">
/// Inline görseller için CID. HTML gövdede <c>src="cid:..."</c> ile kullanılır.
/// </param>
public sealed record NotificationAttachmentRef(
    string FileId,
    string FileName,
    string ContentType,
    long Length,
    bool IsInline = false,
    string? ContentId = null);
