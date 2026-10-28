namespace Notification.Application.Preferences;

public interface IUnsubscribeLinkFactory
{
    /// <summary>
    /// Kullanıcının abonelikten çıkma adresini üretir ve doğrulama için gereken özeti kaydeder.
    /// Gizli anahtar tanımlı değilse null döner; çağıran linki atlamalıdır.
    /// </summary>
    Task<string?> CreateLinkAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Gelen token'ı aramada kullanılacak özete çevirir.</summary>
    string HashToken(string token);
}
