using ADNoirBase.Shared.Contracts.Messages;

namespace ADNoirBase.Shared.Contracts.Responses;

/// <summary>Veri taşıyan türetilmiş response tipi.</summary>
public record BaseResponse<T> : BaseResponse
{
    protected BaseResponse() { }

    public T? Data { get; init; }

    // ─── Factory'ler ──────────────────────────────────────────────────────────

    public static BaseResponse<T> Success(
        T data,
        string messageCode = MessageCodes.Common.Success,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Success, Data = data, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args) };

    public static BaseResponse<T> Created(
        T data,
        string messageCode = MessageCodes.Common.Created,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Created, Data = data, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args) };

    public new static BaseResponse<T> NotFound(
        string messageCode = MessageCodes.Common.NotFound,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.NotFound, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args), Errors = [new ResponseMessage(messageCode, MessageSeverity.Error)] };

    public new static BaseResponse<T> ValidationFailed(
        IReadOnlyList<ResponseMessage> errors,
        string messageCode = MessageCodes.Common.ValidationFailed) =>
        new() { Status = ResponseStatus.ValidationFailed, MessageCode = messageCode, Errors = errors };

    public new static BaseResponse<T> Conflict(
        string messageCode = MessageCodes.Common.Conflict,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Conflict, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args), Errors = [new ResponseMessage(messageCode, MessageSeverity.Error)] };

    public new static BaseResponse<T> Unauthorized(
        string messageCode = MessageCodes.Common.Unauthorized,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Unauthorized, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args), Errors = [new ResponseMessage(messageCode, MessageSeverity.Error)] };

    public new static BaseResponse<T> Forbidden(
        string messageCode = MessageCodes.Common.Forbidden,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.Forbidden, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args), Errors = [new ResponseMessage(messageCode, MessageSeverity.Error)] };

    public new static BaseResponse<T> BusinessRuleViolation(
        string messageCode = MessageCodes.Common.BusinessRuleViolation,
        params (string Key, object? Value)[] args) =>
        new() { Status = ResponseStatus.BusinessRuleViolation, MessageCode = messageCode, MessageArgs = ResponseArgs.From(args), Errors = [new ResponseMessage(messageCode, MessageSeverity.Error)] };

    // ─── Boundary helper'ları ─────────────────────────────────────────────────

    public override BaseResponse<T> WithContext(string? correlationId, string? traceId) =>
        this with { CorrelationId = correlationId, TraceId = traceId };

    public override BaseResponse<T> WithResolvedMessages(
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
}
