using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Identity.Application.Dtos.Auth;

namespace Identity.Application.Validators.Auth;

public sealed class RegisterValidator : AbstractValidator<RegisterRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .EmailAddress().WithErrorCode(MessageCodes.Validation.InvalidEmail)
            .MaximumLength(256).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MinimumLength(8).WithErrorCode(MessageCodes.Validation.MinLength)
            .MaximumLength(128).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.ConfirmPassword)
            .Equal(x => x.Password).WithErrorCode(MessageCodes.Validation.MustMatch);

        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(100).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(100).WithErrorCode(MessageCodes.Validation.MaxLength);
    }
}
