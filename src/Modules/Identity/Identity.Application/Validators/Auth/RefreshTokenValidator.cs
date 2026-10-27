using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Identity.Application.Dtos.Auth;

namespace Identity.Application.Validators.Auth;

public sealed class RefreshTokenValidator : AbstractValidator<RefreshTokenRequest>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required);
    }
}
