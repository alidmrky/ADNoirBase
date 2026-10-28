namespace Notification.Application.Templating;

/// <summary>
/// Her şablona otomatik geçilen değişkenler. UI bunları değişken panelinde gösterir.
/// Scriban söz diziminde <c>{{ first_name }}</c> şeklinde kullanılır.
/// </summary>
public static class TemplateModelKeys
{
    public const string FirstName = "first_name";
    public const string LastName = "last_name";
    public const string FullName = "full_name";
    public const string Email = "email";
    public const string PhoneNumber = "phone_number";

    /// <summary>Layout, içeriği bu değişkenle sarar.</summary>
    public const string Content = "content";

    public const string Subject = "subject";
    public const string AppName = "app_name";
    public const string AppUrl = "app_url";

    /// <summary>Yalnızca Marketing kategorisindeki bildirimlerde doludur.</summary>
    public const string UnsubscribeUrl = "unsubscribe_url";

    public const string Year = "year";
}
