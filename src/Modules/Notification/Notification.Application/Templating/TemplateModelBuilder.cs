using ADNoirBase.Shared.Contracts.Directory;
using Notification.Application.Configuration;

namespace Notification.Application.Templating;

/// <summary>
/// Her render'da bulunması gereken ortak değişkenleri üretir. Şablon yazarı bu anahtarlara
/// her zaman güvenebilsin diye eksik bilgiler boş string olarak doldurulur.
/// </summary>
public static class TemplateModelBuilder
{
    public static Dictionary<string, object?> BuildBase(NotificationSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            [TemplateModelKeys.AppName] = settings.AppName,
            [TemplateModelKeys.AppUrl] = settings.AppUrl.TrimEnd('/'),
            [TemplateModelKeys.Year] = DateTime.UtcNow.Year.ToString(System.Globalization.CultureInfo.InvariantCulture),
            [TemplateModelKeys.FirstName] = string.Empty,
            [TemplateModelKeys.LastName] = string.Empty,
            [TemplateModelKeys.FullName] = string.Empty,
            [TemplateModelKeys.Email] = string.Empty,
            [TemplateModelKeys.PhoneNumber] = string.Empty,
            [TemplateModelKeys.UnsubscribeUrl] = string.Empty
        };
    }

    public static void ApplyUser(Dictionary<string, object?> model, UserDirectoryEntry user)
    {
        ArgumentNullException.ThrowIfNull(model);
        ArgumentNullException.ThrowIfNull(user);

        model[TemplateModelKeys.FirstName] = user.FirstName;
        model[TemplateModelKeys.LastName] = user.LastName;
        model[TemplateModelKeys.FullName] = user.FullName;
        model[TemplateModelKeys.Email] = user.Email;
        model[TemplateModelKeys.PhoneNumber] = user.PhoneNumber ?? string.Empty;
    }

    /// <summary>
    /// Çağıranın verdiği değişkenleri ekler. Ortak anahtarların üzerine yazılabilir;
    /// örneğin bir sipariş maili {{ full_name }} yerine alıcı adını kendisi belirleyebilir.
    /// </summary>
    public static void ApplyCustom(
        Dictionary<string, object?> model,
        IReadOnlyDictionary<string, object?>? values)
    {
        ArgumentNullException.ThrowIfNull(model);

        if (values is null)
        {
            return;
        }

        foreach (var (key, value) in values)
        {
            model[key] = value;
        }
    }
}
