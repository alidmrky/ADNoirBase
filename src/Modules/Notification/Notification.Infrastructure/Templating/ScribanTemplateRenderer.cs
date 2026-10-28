using System.Globalization;
using System.Net;
using Notification.Application.Templating;
using Scriban;
using Scriban.Runtime;
using Scriban.Syntax;

namespace Notification.Infrastructure.Templating;

/// <summary>
/// Şablonları Scriban ile render eder. Şablon metnini yönetici yazar, ama model değerleri
/// kullanıcıdan gelebilir; bu yüzden HTML çıktıda model değerleri kaçışlanır.
/// TemplateLoader atanmadığı için <c>include</c> çalışmaz: şablondan dosya sistemine erişilemez.
/// </summary>
public sealed class ScribanTemplateRenderer : ITemplateRenderer
{
    /// <summary>Sonsuz döngüyü bir isteği kilitlemeden önce durdurur.</summary>
    private const int LoopLimit = 1000;

    private const int RecursiveLimit = 20;

    /// <summary>Render sonucu bu boyutu aşarsa şablon bir bellek tüketme aracına dönüşmüş demektir.</summary>
    private const int MaxOutputLength = 512 * 1024;

    public TemplateRenderResult Render(
        string? template,
        IReadOnlyDictionary<string, object?> model,
        bool escapeHtml)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (string.IsNullOrEmpty(template))
        {
            return TemplateRenderResult.Success(string.Empty);
        }

        var parsed = Template.Parse(template);

        if (parsed.HasErrors)
        {
            return TemplateRenderResult.Failure(DescribeErrors(parsed));
        }

        var context = new TemplateContext
        {
            LoopLimit = LoopLimit,
            RecursiveLimit = RecursiveLimit,

            // Eksik bir değişken şablonu patlatmasın; boş yazılması daha iyi bir davranıştır.
            EnableRelaxedMemberAccess = true,
            StrictVariables = false
        };

        context.PushGlobal(BuildScriptObject(model, escapeHtml));

        try
        {
            var output = parsed.Render(context);

            return output.Length > MaxOutputLength
                ? TemplateRenderResult.Failure([$"Render sonucu {MaxOutputLength} baytı aşıyor."])
                : TemplateRenderResult.Success(output);
        }
        catch (ScriptRuntimeException ex)
        {
            return TemplateRenderResult.Failure([Describe(ex)]);
        }
    }

    public IReadOnlyList<string> Validate(string? template)
    {
        if (string.IsNullOrEmpty(template))
        {
            return [];
        }

        var parsed = Template.Parse(template);
        return parsed.HasErrors ? DescribeErrors(parsed) : [];
    }

    private static ScriptObject BuildScriptObject(
        IReadOnlyDictionary<string, object?> model,
        bool escapeHtml)
    {
        var script = new ScriptObject();

        foreach (var (key, value) in model)
        {
            script[key] = escapeHtml ? EscapeValue(value) : value;
        }

        return script;
    }

    /// <summary>
    /// Model değerleri çıktıya gömülmeden kaçışlanır. Scriban'ın yazma davranışını değiştirmek
    /// yerine değerin kendisi kaçışlanır; böylece kaçışlama kuralı tek yerde ve görünür kalır.
    /// </summary>
    private static object? EscapeValue(object? value) => value switch
    {
        null => null,
        string text => WebUtility.HtmlEncode(text),
        IReadOnlyDictionary<string, object?> nested => nested.ToDictionary(
            pair => pair.Key,
            pair => EscapeValue(pair.Value)),
        IEnumerable<object?> items => items.Select(EscapeValue).ToList(),
        _ => value
    };

    private static List<string> DescribeErrors(Template parsed) =>
        parsed.Messages
            .Where(m => m.Type == Scriban.Parsing.ParserMessageType.Error)
            .Select(m => string.Create(
                CultureInfo.InvariantCulture,
                $"Satır {m.Span.Start.Line + 1}, sütun {m.Span.Start.Column + 1}: {m.Message}"))
            .ToList();

    private static string Describe(ScriptRuntimeException exception) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"Satır {exception.Span.Start.Line + 1}, sütun {exception.Span.Start.Column + 1}: {exception.OriginalMessage}");
}
