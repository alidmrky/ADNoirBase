namespace Notification.Application.Templating;

/// <summary>
/// Render sonucu. Söz dizimi hatası istisna olarak fırlatılmaz; şablonlar UI'dan düzenlendiği için
/// hata mesajı satır numarasıyla birlikte kullanıcıya gösterilebilir olmalıdır.
/// </summary>
public sealed record TemplateRenderResult(string Output, IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Errors.Count == 0;

    public static TemplateRenderResult Success(string output) => new(output, []);

    public static TemplateRenderResult Failure(IReadOnlyList<string> errors) => new(string.Empty, errors);
}
