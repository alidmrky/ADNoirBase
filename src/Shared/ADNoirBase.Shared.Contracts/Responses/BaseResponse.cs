using ADNoirBase.Shared.Contracts.Messages;

namespace ADNoirBase.Shared.Contracts.Responses;

/// <summary>
/// Tüm servis operasyonlarının döndürdüğü zarf tipi. Bu kontratın dışına çıkılamaz.
/// ProblemDetails kullanılmaz — 404 ve 500 dahil her şey bu zarf içinde döner.
/// </summary>
public record BaseResponse
{
    protected BaseResponse() { }

    /// <summary>
    /// Status'tan türetilir — "IsSuccess=true, Status=NotFound" durumu temsil edilemez.
    /// </summary>
    public bool IsSuccess =>
        Status is ResponseStatus.Success
            or ResponseStatus.Created
            or ResponseStatus.Accepted
            or ResponseStatus.NoContent;

    public ResponseStatus Status { get; init; }

    /// <summary>Mesaj kataloğu anahtarı. Literal string yasak — her zaman MessageCodes sabiti kullanılır.</summary>
    public string MessageCode { get; init; } = MessageCodes.Common.Success;

    /// <summary>Caller'ın kültürü için çözümlenmiş metin. Boundary'de doldurulur, servis kodu set etmez.</summary>
    public string? Message { get; init; }

    public IReadOnlyDictionary<string, object?>? MessageArgs { get; init; }

    public IReadOnlyList<ResponseMessage> Errors { get; init; } = [];

    public IReadOnlyList<ResponseMessage> Warnings { get; init; } = [];

    /// <summary>Boundary'de doldurulur — servis kodu set etmez.</summary>
    public string? CorrelationId { get; init; }

    /// <summary>Boundary'de doldurulur — servis kodu set etmez.</summary>
    public string? TraceId { get; init; }

    public DateTime TimestampUtc { get; init; } = DateTime.UtcNow;

    // ─── Factory'ler — servis kodu yalnızca bunları kullanır ─────────────────

    public static BaseResponse Success(
        string messageCode = MessageCodes.Common.Success,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Success, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args) };

    public static BaseResponse Created(
        string messageCode = MessageCodes.Common.Created,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Created, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args) };

    public static BaseResponse Accepted(
        string messageCode = MessageCodes.Common.Accepted,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Accepted, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args) };

    public static BaseResponse NoContent(
        string messageCode = MessageCodes.Common.NoContent,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.NoContent, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args) };

    public static BaseResponse NotFound(
        string messageCode = MessageCodes.Common.NotFound,
        params (string Key, object? Value)[] args) =>
        Failure(ResponseStatus.NotFound, messageCode, args);

    public static BaseResponse ValidationFailed(
        IReadOnlyList<ResponseMessage> errors,
        string messageCode = MessageCodes.Common.ValidationFailed) =>
        new() { Status = ResponseStatus.ValidationFailed, MessageCode = messageCode, Errors = errors };

    public static BaseResponse Conflict(
        string messageCode = MessageCodes.Common.Conflict,
        params (string Key, object? Value)[] args) =>
        Failure(ResponseStatus.Conflict, messageCode, args);

    public static BaseResponse Unauthorized(
        string messageCode = MessageCodes.Common.Unauthorized,
        params (string Key, object? Value)[] args) =>
        Failure(ResponseStatus.Unauthorized, messageCode, args);

    public static BaseResponse Forbidden(
        string messageCode = MessageCodes.Common.Forbidden,
        params (string Key, object? Value)[] args) =>
        Failure(ResponseStatus.Forbidden, messageCode, args);

    public static BaseResponse BusinessRuleViolation(
        string messageCode = MessageCodes.Common.BusinessRuleViolation,
        params (string Key, object? Value)[] args) =>
        Failure(ResponseStatus.BusinessRuleViolation, messageCode, args);

    public static BaseResponse ConcurrencyConflict(
        string messageCode = MessageCodes.Common.ConcurrencyConflict,
        params (string Key, object? Value)[] args) =>
        Failure(ResponseStatus.ConcurrencyConflict, messageCode, args);

    public static BaseResponse Failure(
        ResponseStatus status,
        string messageCode,
        params (string Key, object? Value)[] args)
    {
        var arguments = ResponseArgs.From(args);
        return new BaseResponse
        {
            Status = status,
            MessageCode = messageCode,
            MessageArgs = arguments,
            Errors = [new ResponseMessage(messageCode, MessageSeverity.Error, Args: arguments)]
        };
    }

    // ─── Boundary helper'ları — servis kodu çağırmaz ─────────────────────────

    public virtual BaseResponse WithContext(string? correlationId, string? traceId) =>
        this with { CorrelationId = correlationId, TraceId = traceId };

    public virtual BaseResponse WithResolvedMessages(
        string? summaryText,
        Func<ResponseMessage, string?> resolver)
    {
        ArgumentNullException.ThrowIfNull(resolver);
        return this with
        {
            Message = summaryText,
            Errors = [.. Errors.Select(e => e.WithResolvedText(resolver(e)))],
            Warnings = [.. Warnings.Select(w => w.WithResolvedText(resolver(w)))]
        };
    }

    public virtual BaseResponse WithWarnings(params ResponseMessage[] warnings) =>
        this with { Warnings = [.. Warnings, .. warnings] };
}
