namespace Notification.Infrastructure.Persistence;

public static class NotificationCollections
{
    public const string Templates = "notification_templates";
    public const string Messages = "notification_messages";
    public const string Campaigns = "notification_campaigns";
    public const string InApp = "notification_inapp";
    public const string Preferences = "notification_preferences";

    /// <summary>GridFS bucket adı; ekler <c>{Attachments}.files</c> ve <c>.chunks</c> altında durur.</summary>
    public const string Attachments = "notification_attachments";
}
