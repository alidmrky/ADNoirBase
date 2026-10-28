namespace Notification.Application.Dtos.Templates;

/// <summary>
/// Test gönderimi yalnızca isteği yapan yöneticinin kendi adresine yapılır; hedef adres
/// istekten alınsaydı panel bir spam aracına dönüşürdü.
/// </summary>
public sealed record TemplateTestSendRequest(
    IReadOnlyDictionary<string, string>? SampleValues = null);
