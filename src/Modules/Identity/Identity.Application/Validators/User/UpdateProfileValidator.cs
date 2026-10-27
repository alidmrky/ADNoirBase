using ADNoirBase.Shared.Contracts.Messages;
using FluentValidation;
using Identity.Application.Dtos.User;

namespace Identity.Application.Validators.User;

public sealed class UpdateProfileValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileValidator()
    {
        RuleFor(x => x.FirstName)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(100).WithErrorCode(MessageCodes.Validation.MaxLength);

        RuleFor(x => x.LastName)
            .NotEmpty().WithErrorCode(MessageCodes.Validation.Required)
            .MaximumLength(100).WithErrorCode(MessageCodes.Validation.MaxLength);
    }
}
