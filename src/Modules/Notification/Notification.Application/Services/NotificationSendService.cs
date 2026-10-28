using System.Net.Mail;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Responses;
using Notification.Application.Dispatching;
using Notification.Application.Dtos;
using Notification.Application.Messages;
using Notification.Application.Services.Attachments;
using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Services;

public sealed class NotificationSendService(INotificationDispatcher dispatcher, INotificationAttachmentService attachments, IUserDirectory users) : INotificationSendService
{
    public async Task<BaseResponse<EnqueueResult>> SendAsync(SendNotificationRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!Enum.IsDefined(request.Channel) || !Enum.IsDefined(request.Category) || request.To is null || request.Cc is null || request.Bcc is null || request.Model is null || request.AttachmentIds is null)
        {
            return BaseResponse<EnqueueResult>.BusinessRuleViolation(NotificationMessageCodes.Message.RecipientInvalid);
        }

        var recipients = request.To.Concat(request.Cc).Concat(request.Bcc).ToList();
        if (request.To.Count == 0 || recipients.Count > 100 ||
            (request.Channel != NotificationChannel.Email && (request.To.Count != 1 || request.Cc.Count > 0 || request.Bcc.Count > 0 || request.AttachmentIds.Count > 0)))
        {
            return BaseResponse<EnqueueResult>.BusinessRuleViolation(NotificationMessageCodes.Message.RecipientRequired);
        }

        foreach (var recipient in recipients)
        {
            if (recipient is null || !ValidAddress(recipient, request.Channel))
            {
                return BaseResponse<EnqueueResult>.BusinessRuleViolation(NotificationMessageCodes.Message.RecipientInvalid);
            }

            if (recipient.UserId is { } userId)
            {
                var user = await users.FindByIdAsync(userId, ct);
                if (user is null || (request.Channel == NotificationChannel.Email && !string.Equals(user.Email, recipient.Address, StringComparison.OrdinalIgnoreCase)) ||
                    (request.Channel == NotificationChannel.Sms && user.PhoneNumber != recipient.Address))
                {
                    return BaseResponse<EnqueueResult>.BusinessRuleViolation(NotificationMessageCodes.Message.RecipientInvalid);
                }
            }
        }
        if (request.Link is { Length: > 0 } link && (!link.StartsWith('/') || link.StartsWith("//", StringComparison.Ordinal) || link.Contains('\\')))
        {
            return BaseResponse<EnqueueResult>.BusinessRuleViolation(NotificationMessageCodes.Message.RecipientInvalid);
        }

        var resolved = await attachments.ResolveAsync(request.AttachmentIds, ct);
        if (!resolved.IsSuccess)
        {
            return BaseResponse<EnqueueResult>.BusinessRuleViolation(resolved.MessageCode);
        }

        var result = await dispatcher.EnqueueAsync(new NotificationRequest
        {
            Channel = request.Channel, Category = request.Category,
            To = request.To, Cc = request.Cc, Bcc = request.Bcc,
            TemplateCode = string.IsNullOrWhiteSpace(request.TemplateCode) ? null : request.TemplateCode,
            Subject = request.Subject, HtmlBody = request.HtmlBody, TextBody = request.TextBody,
            SmsBody = request.SmsBody, Link = request.Link, Attachments = resolved.Data!,
            Model = request.Model.ToDictionary(x => x.Key, x => (object?)x.Value)
        }, ct);
        return result.IsSuccess ? BaseResponse<EnqueueResult>.Success(result, NotificationMessageCodes.Message.Queued)
            : BaseResponse<EnqueueResult>.BusinessRuleViolation(NotificationMessageCodes.Template.RenderFailed);
    }

    private static bool ValidAddress(NotificationRecipient recipient, NotificationChannel channel) => channel switch
    {
        NotificationChannel.Email => MailAddress.TryCreate(recipient.Address, out var address) && address.Address == recipient.Address,
        NotificationChannel.Sms => recipient.Address is { Length: >= 8 and <= 16 } && recipient.Address[0] == '+' && recipient.Address[1..].All(char.IsAsciiDigit),
        NotificationChannel.InApp => recipient.UserId is { } id && id != Guid.Empty,
        _ => false
    };
}
