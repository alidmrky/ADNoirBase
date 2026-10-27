using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Identity.Application.Dtos.Auth;

namespace Identity.Application.Validators.Auth;

public sealed class LoginValidator : AbstractValidator<LoginRequest>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .EmailAddress().WithErrorCode(MessageCodes.Validation.InvalidEmail);

        RuleFor(x => x.Password)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required);
    }
}
