using ADNoirBase.Shared.Contracts.Messages;
using ADNoirBase.Shared.Contracts.Pagination;

namespace ADNoirBase.Shared.Contracts.Responses;

public record PagedResponse<T> : BaseResponse<PagedList<T>>
{
    protected PagedResponse() { }

    public static PagedResponse<T> Success(
        PagedList<T> data,
        string messageCode = MessageCodes.Common.Success) =>
        new() { Status = ResponseStatus.Success, Data = data, MessageCode = messageCode };

    public override PagedResponse<T> WithContext(string? correlationId, string? traceId) =>
        this with { CorrelationId = correlationId, TraceId = traceId };
}
