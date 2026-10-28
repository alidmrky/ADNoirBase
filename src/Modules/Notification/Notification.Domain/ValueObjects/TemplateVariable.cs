namespace Notification.Domain.ValueObjects;

/// <summary>Şablonun beklediği değişken. Yalnızca önizleme ve UI yardımı içindir, zorunluluk doğurmaz.</summary>
public sealed record TemplateVariable(
    string Name,
    string? Description = null,
    string? SampleValue = null);
