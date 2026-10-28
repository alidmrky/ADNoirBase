using ADNoirBase.Shared.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Entities;

/// <summary>
/// UI'dan düzenlenebilen bildirim şablonu. İçerik Scriban söz dizimi taşır
/// ve gönderim anında alıcıya göre render edilir.
/// </summary>
public sealed class NotificationTemplate : SoftDeletableEntity<Guid>
{
    /// <summary>Koddan referans verilen benzersiz anahtar, örneğin <c>identity.welcome</c>.</summary>
    public string Code { get; private set; } = string.Empty;

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public NotificationChannel Channel { get; private set; }

    public NotificationCategory Category { get; private set; }

    /// <summary>Sistem şablonları silinemez; kod bunlara koddan referans verir.</summary>
    public bool IsSystem { get; private set; }

    public bool IsActive { get; private set; } = true;

    /// <summary>
    /// Bu şablon bir layout ise true. Layout'lar doğrudan gönderilmez, başka şablonları sarar.
    /// </summary>
    public bool IsLayout { get; private set; }

    /// <summary>Ortak başlık/altlık sağlayan layout şablonunun kodu. Null ise sarmalama yapılmaz.</summary>
    public string? LayoutCode { get; private set; }

    public string? Subject { get; private set; }

    public string? HtmlBody { get; private set; }

    public string? TextBody { get; private set; }

    public string? SmsBody { get; private set; }

    public IReadOnlyList<TemplateVariable> Variables { get; private set; } = [];

    public NotificationLogPolicy LogPolicy { get; private set; } = NotificationLogPolicy.MetadataOnly;

    /// <summary>Gönderim kaydının kaç gün saklanacağı. 0 ise süresiz.</summary>
    public int RetentionDays { get; private set; }

    private NotificationTemplate() { }

    public static NotificationTemplate Create(
        string code,
        string name,
        NotificationChannel channel,
        NotificationCategory category,
        bool isSystem = false)
    {
        return new NotificationTemplate
        {
            Id = Guid.NewGuid(),
            Code = NormalizeCode(code),
            Name = name.Trim(),
            Channel = channel,
            Category = category,
            IsSystem = isSystem,
            IsActive = true
        };
    }

    public void UpdateDefinition(
        string name,
        string? description,
        NotificationCategory category,
        string? layoutCode,
        bool isActive)
    {
        Name = name.Trim();
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();
        Category = category;
        LayoutCode = string.IsNullOrWhiteSpace(layoutCode) ? null : NormalizeCode(layoutCode);
        IsActive = isActive;
    }

    public void UpdateContent(string? subject, string? htmlBody, string? textBody, string? smsBody)
    {
        Subject = subject;
        HtmlBody = htmlBody;
        TextBody = textBody;
        SmsBody = smsBody;
    }

    public void UpdateVariables(IEnumerable<TemplateVariable> variables) =>
        Variables = variables.ToList();

    public void UpdateRetention(NotificationLogPolicy logPolicy, int retentionDays)
    {
        if (retentionDays < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(retentionDays), retentionDays, "Saklama süresi negatif olamaz.");
        }

        LogPolicy = logPolicy;
        RetentionDays = retentionDays;
    }

    public void MarkAsLayout() => IsLayout = true;

    /// <summary>Bu şablonla gönderilen bir mesajın ne zaman otomatik silineceği.</summary>
    public DateTime? CalculateExpiry(DateTime from) =>
        RetentionDays > 0 ? from.AddDays(RetentionDays) : null;

    /// <summary>Kodlar büyük/küçük harf farkıyla ikiye bölünmemeli; tek biçimde saklanır.</summary>
    public static string NormalizeCode(string code) => code.Trim().ToLowerInvariant();
}
