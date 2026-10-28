namespace Notification.Application.Templating;

public interface ITemplateRenderer
{
    /// <summary>
    /// Şablonu verilen modelle render eder.
    /// <paramref name="escapeHtml"/> true ise model değerleri HTML'e kaçışlanır; şablon metninin
    /// kendisi kaçışlanmaz, çünkü şablonu yalnızca yönetici yazar.
    /// </summary>
    TemplateRenderResult Render(
        string? template,
        IReadOnlyDictionary<string, object?> model,
        bool escapeHtml);

    /// <summary>Şablonu çalıştırmadan yalnızca söz dizimini doğrular.</summary>
    IReadOnlyList<string> Validate(string? template);
}
