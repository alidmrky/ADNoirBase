namespace Notification.Application.Delivery;

/// <summary>
/// Gönderim sonucu. Geçici ve kalıcı hata ayrımı kritiktir: geçici hatada mesaj yeniden
/// denenir, kalıcı hatada (örneğin adres geçersiz) denemek yalnızca gönderen itibarını yakar.
/// </summary>
public sealed record SendResult(bool IsSuccess, bool IsPermanent, string? Error)
{
    public static SendResult Success() => new(true, false, null);

    public static SendResult Transient(string error) => new(false, false, error);

    public static SendResult Permanent(string error) => new(false, true, error);
}
