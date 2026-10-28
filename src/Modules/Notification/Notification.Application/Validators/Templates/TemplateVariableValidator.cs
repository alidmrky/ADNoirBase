using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Notification.Application.Dtos.Templates;

namespace Notification.Application.Validators.Templates;

public sealed class TemplateVariableValidator : AbstractValidator<TemplateVariableDto>
{
    public TemplateVariableValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(TemplateRules.VariableNameMaxLength)
            .WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.Description)
            .MaximumLength(TemplateRules.DescriptionMaxLength)
            .WithErrorCode(MessageCodes.Validation.MaxLength);
    }
}
