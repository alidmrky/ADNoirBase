using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Notification.Application.Dtos.Templates;

namespace Notification.Application.Validators.Templates;

public sealed class CreateTemplateValidator : AbstractValidator<CreateTemplateRequest>
{
    public CreateTemplateValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(TemplateRules.CodeMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength)
            .Matches(TemplateRules.CodePattern).WithErrorCode(MessageCodes.Validation.InvalidFormat);

        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(TemplateRules.NameMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(TemplateRules.DescriptionMaxLength).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.Channel).IsInEnum().WithErrorCode(MessageCodes.Validation.OutOfRange);
        RuleFor(x => x.Category).IsInEnum().WithErrorCode(MessageCodes.Validation.OutOfRange);
        RuleFor(x => x.LogPolicy).IsInEnum().WithErrorCode(MessageCodes.Validation.OutOfRange);

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

        RuleFor(x => x.RetentionDays)
            .InclusiveBetween(0, TemplateRules.MaxRetentionDays)
            .WithErrorCode(MessageCodes.Validation.OutOfRange);

        RuleFor(x => x.Variables!)
            .Must(v => v.Count <= TemplateRules.MaxVariables)
            .WithErrorCode(MessageCodes.Validation.OutOfRange)
            .When(x => x.Variables is not null);

        RuleForEach(x => x.Variables).SetValidator(new TemplateVariableValidator());

        // Layout kendisi bir layout'a sarılamaz; sonsuz sarmalama oluşurdu.
        RuleFor(x => x.LayoutCode)
            .Empty().WithErrorCode(MessageCodes.Validation.NotAllowed)
            .When(x => x.IsLayout);

        // Layout'un tek işi içeriği sarmak; HTML gövdesi yoksa anlamı yok.
        RuleFor(x => x.HtmlBody)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .When(x => x.IsLayout);
    }
}
