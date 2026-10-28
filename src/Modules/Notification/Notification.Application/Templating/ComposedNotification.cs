namespace Notification.Application.Templating;

/// <summary>Şablonun render edilmiş hali. Kanala göre alanların bir kısmı null kalır.</summary>
public sealed record ComposedNotification(
    string? Subject,
    string? HtmlBody,
    string? TextBody,
    string? SmsBody,
    IReadOnlyList<string> Errors)
{
    public bool IsSuccess => Errors.Count == 0;

    public static ComposedNotification Failure(IReadOnlyList<string> errors) =>
        new(null, null, null, null, errors);
}
