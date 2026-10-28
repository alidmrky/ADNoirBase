using ADNoirBase.Shared.Domain.Entities;

namespace Notification.Domain.Entities;

/// <summary>Uygulama içi bildirim. Zil ikonundaki listeyi besler.</summary>
public sealed class InAppNotification : FullAuditableEntity<Guid>
{
    public Guid UserId { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    /// <summary>Tıklanınca gidilecek uygulama içi adres.</summary>
    public string? Link { get; private set; }

    public DateTime? ReadAt { get; private set; }

    /// <summary>TTL index'i bu alana bakar.</summary>
    public DateTime? ExpiresAt { get; private set; }

    public bool IsRead => ReadAt.HasValue;

    private InAppNotification() { }

    public static InAppNotification Create(
        Guid userId,
        string title,
        string body,
        string? link = null,
        DateTime? expiresAt = null)
    {
        return new InAppNotification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Body = body,
            Link = string.IsNullOrWhiteSpace(link) ? null : link.Trim(),
            ExpiresAt = expiresAt
        };
    }

    /// <summary>Okundu işaretini ilk okuma zamanında sabitler; tekrar çağrılması zamanı kaydırmaz.</summary>
    public void MarkRead(DateTime readAt) => ReadAt ??= readAt;
}
