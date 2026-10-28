using MimeKit;
using MimeKit.Text;
using Notification.Application.Delivery;
using Notification.Domain.ValueObjects;

namespace Notification.Infrastructure.Email;

/// <summary>
/// Gönderilecek maili MIME yapısına çevirir. Tüm sağlayıcılar aynı yapıyı üretsin diye
/// tek bir yerde tutulur; aksi halde SMTP ile diske yazılan mail birbirinden farklı olurdu.
/// </summary>
public static class MimeMessageFactory
{
    public static MimeMessage Create(OutgoingEmail email)
    {
        ArgumentNullException.ThrowIfNull(email);

        var message = new MimeMessage();

        message.From.Add(new MailboxAddress(email.FromDisplayName ?? email.FromAddress, email.FromAddress));
        AddAll(message.To, email.To);
        AddAll(message.Cc, email.Cc);
        AddAll(message.Bcc, email.Bcc);

        if (!string.IsNullOrWhiteSpace(email.ReplyTo))
        {
            message.ReplyTo.Add(MailboxAddress.Parse(email.ReplyTo));
        }

        message.Subject = email.Subject;

        foreach (var (key, value) in email.Headers)
        {
            message.Headers.Add(key, value);
        }

        message.Body = BuildBody(email);

        return message;
    }

    private static MimeEntity BuildBody(OutgoingEmail email)
    {
        var builder = new BodyBuilder
        {
            HtmlBody = email.HtmlBody,
            TextBody = email.TextBody
        };

        foreach (var attachment in email.Attachments)
        {
            var contentType = ContentType.Parse(attachment.ContentType);

            if (attachment.IsInline)
            {
                var entity = builder.LinkedResources.Add(
                    attachment.FileName, attachment.Content, contentType);

                // HTML gövdedeki src="cid:..." bu kimliğe karşılık gelir.
                entity.ContentId = attachment.ContentId ?? MimeKit.Utils.MimeUtils.GenerateMessageId();
            }
            else
            {
                builder.Attachments.Add(attachment.FileName, attachment.Content, contentType);
            }
        }

        return builder.ToMessageBody();
    }

    private static void AddAll(
        InternetAddressList list, IReadOnlyList<NotificationRecipient> recipients)
    {
        foreach (var recipient in recipients)
        {
            list.Add(new MailboxAddress(recipient.DisplayName ?? recipient.Address, recipient.Address));
        }
    }
}
