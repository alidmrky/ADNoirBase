using ADNoirBase.Shared.Contracts.Messages;
using ADNoirBase.Shared.Contracts.Responses;
using FluentValidation.Results;

namespace ADNoirBase.Shared.Application.Validation;

public static class ValidationHelper
{
    /// <summary>FluentValidation hatalarını BaseResponse.ValidationFailed'e dönüştürür.</summary>
    public static BaseResponse ToValidationFailedResponse(ValidationResult result)
    {
        var errors = result.Errors
            .Select(e => new ResponseMessage(
                e.ErrorCode ?? MessageCodes.Validation.InvalidFormat,
                MessageSeverity.Error,
                e.ErrorMessage))
            .ToList();

        return BaseResponse.ValidationFailed(errors);
    }

    public static BaseResponse<T> ToValidationFailedResponse<T>(ValidationResult result)
    {
        var errors = result.Errors
            .Select(e => new ResponseMessage(
                e.ErrorCode ?? MessageCodes.Validation.InvalidFormat,
                MessageSeverity.Error,
                e.ErrorMessage))
            .ToList();

        return BaseResponse<T>.ValidationFailed(errors);
    }
}
