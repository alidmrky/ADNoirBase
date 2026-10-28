namespace Notification.Application.Messages;

/// <summary>Notification modülüne ait tüm mesaj kodları. Literal string kullanımı yasaktır.</summary>
public static class NotificationMessageCodes
{
    public static class Template
    {
        public const string NotFound          = "notification.template.not_found";
        public const string CodeConflict      = "notification.template.code_conflict";
        public const string SystemImmutable   = "notification.template.system_immutable";
        public const string Created           = "notification.template.created";
        public const string Updated           = "notification.template.updated";
        public const string Deleted           = "notification.template.deleted";
        public const string RenderFailed      = "notification.template.render_failed";
        public const string ContentMissing    = "notification.template.content_missing";
        public const string Inactive          = "notification.template.inactive";
        public const string ChannelMismatch   = "notification.template.channel_mismatch";
    }

    public static class Message
    {
        public const string NotFound          = "notification.message.not_found";
        public const string Queued            = "notification.message.queued";
        public const string Requeued          = "notification.message.requeued";
        public const string NotRetryable      = "notification.message.not_retryable";
        public const string RecipientRequired = "notification.message.recipient_required";
        public const string RecipientInvalid  = "notification.message.recipient_invalid";
    }

    public static class Attachment
    {
        public const string NotFound          = "notification.attachment.not_found";
        public const string TooLarge          = "notification.attachment.too_large";
        public const string TypeNotAllowed    = "notification.attachment.type_not_allowed";
        public const string Uploaded          = "notification.attachment.uploaded";
        public const string TooMany           = "notification.attachment.too_many";
    }

    public static class Campaign
    {
        public const string Invalid = "notification.campaign.invalid";
        public const string NotFound          = "notification.campaign.not_found";
        public const string Created           = "notification.campaign.created";
        public const string Started           = "notification.campaign.started";
        public const string Cancelled         = "notification.campaign.cancelled";
        public const string NotStartable      = "notification.campaign.not_startable";
        public const string NotCancellable    = "notification.campaign.not_cancellable";
        public const string AudienceEmpty     = "notification.campaign.audience_empty";
        public const string AudienceTooLarge  = "notification.campaign.audience_too_large";
    }

    public static class Preference
    {
        public const string Updated           = "notification.preference.updated";
        public const string Unsubscribed      = "notification.preference.unsubscribed";
        public const string TokenInvalid      = "notification.preference.token_invalid";
    }

    public static class InApp
    {
        public const string NotFound          = "notification.inapp.not_found";
        public const string MarkedRead        = "notification.inapp.marked_read";
    }
}
