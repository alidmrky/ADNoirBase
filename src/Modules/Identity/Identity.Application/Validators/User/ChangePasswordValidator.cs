using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Identity.Application.Dtos.User;

namespace Identity.Application.Validators.User;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required);

        RuleFor(x => x.NewPassword)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MinimumLength(8).WithErrorCode(MessageCodes.Validation.MinLength)
            .MaximumLength(128).WithErrorCode(MessageCodes.Validation.MaxLength)
            .NotEqual(x => x.CurrentPassword).WithErrorCode(MessageCodes.Validation.NotAllowed);

        RuleFor(x => x.ConfirmNewPassword)
            .Equal(x => x.NewPassword).WithErrorCode(MessageCodes.Validation.MustMatch);
    }
}
