using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Notification.Application.Dtos.Templates;

namespace Notification.Application.Validators.Templates;

public sealed class TemplatePreviewValidator : AbstractValidator<TemplatePreviewRequest>
{
    public TemplatePreviewValidator()
    {
        RuleFor(x => x.LayoutCode)
            .Matches(TemplateRules.CodePattern).WithErrorCode(MessageCodes.Validation.InvalidFormat)
            .When(x => !string.IsNullOrWhiteSpace(x.LayoutCode));

        RuleFor(x => x.Subject)
            .MaximumLength(TemplateRules.SubjectMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.HtmlBody)
            .MaximumLength(TemplateRules.BodyMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.TextBody)
            .MaximumLength(TemplateRules.BodyMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.SmsBody)
            .MaximumLength(TemplateRules.SmsBodyMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength);
    }
}
