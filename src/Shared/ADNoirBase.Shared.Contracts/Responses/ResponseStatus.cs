namespace ADNoirBase.Shared.Contracts.Responses;

public enum ResponseStatus
{
    Success,
    Created,
    Accepted,
    NoContent,
    NotFound,
    ValidationFailed,
    Conflict,
    Unauthorized,
    Forbidden,
    BusinessRuleViolation,
    ConcurrencyConflict,
    RateLimited,
    ExternalServiceError,
    UnexpectedError
}
