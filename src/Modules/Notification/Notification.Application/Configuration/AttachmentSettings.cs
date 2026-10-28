namespace Notification.Application.Configuration;

public sealed class AttachmentSettings
{
    public long MaxBytes { get; init; } = 10 * 1024 * 1024;

    /// <summary>
    /// İzin verilen içerik tipleri. Beyaz liste tutulur: yönetici paneline yüklenen bir dosya
    /// alıcıya olduğu gibi gider, bu yüzden çalıştırılabilir türler kabul edilmemelidir.
    /// </summary>
    public IReadOnlyList<string> AllowedContentTypes { get; init; } =
    [
        "application/pdf",
        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        "text/csv",
        "text/plain",
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp"
    ];

    /// <summary>Tek bir mesaja eklenebilecek azami dosya sayısı.</summary>
    public int MaxCount { get; init; } = 10;
}
