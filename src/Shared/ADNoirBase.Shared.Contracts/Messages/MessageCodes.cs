namespace ADNoirBase.Shared.Contracts.Messages;

/// <summary>
/// Merkezi mesaj kodu kataloğu. Literal string kullanımı yasaktır — her zaman bu sabitler kullanılır.
/// Format: {module}.{entity}.{durum}
/// </summary>
public static class MessageCodes
{
    public static class Common
    {
        public const string Success               = "common.success";
        public const string Created               = "common.created";
        public const string Accepted              = "common.accepted";
        public const string NoContent             = "common.no_content";
        public const string NotFound              = "common.not_found";
        public const string ValidationFailed      = "common.validation_failed";
        public const string Conflict              = "common.conflict";
        public const string Unauthorized          = "common.unauthorized";
        public const string Forbidden             = "common.forbidden";
        public const string BusinessRuleViolation = "common.business_rule_violation";
        public const string ConcurrencyConflict   = "common.concurrency_conflict";
        public const string RateLimited           = "common.rate_limited";
        public const string ExternalServiceError  = "common.external_service_error";
        public const string UnexpectedError       = "common.unexpected_error";
    }

    public static class Validation
    {
        public const string Required       = "validation.required";
        public const string MaxLength      = "validation.max_length";
        public const string MinLength      = "validation.min_length";
        public const string InvalidFormat  = "validation.invalid_format";
        public const string InvalidEmail   = "validation.invalid_email";
        public const string OutOfRange     = "validation.out_of_range";
        public const string NotAllowed     = "validation.not_allowed";
        public const string MustMatch      = "validation.must_match";
    }

    public static class Auth
    {
        public const string PermissionDenied              = "auth.permission_denied";
        public const string TokenExpired                  = "auth.token_expired";
        public const string TokenInvalid                  = "auth.token_invalid";
        public const string PermissionMatrixUnavailable   = "auth.permission_matrix_unavailable";
    }

    public static class Gateway
    {
        public const string MethodNotAllowed     = "gateway.request.method_not_allowed";
        public const string UnsupportedMediaType = "gateway.request.unsupported_media_type";
    }
}
