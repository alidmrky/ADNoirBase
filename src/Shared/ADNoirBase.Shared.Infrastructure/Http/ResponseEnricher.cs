using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Http;

namespace ADNoirBase.Shared.Infrastructure.Http;

/// <summary>
/// CorrelationId ve TraceId'yi BaseResponse'a ekler.
/// Middleware boundary'sinde çağrılır, servis kodu çağırmaz.
/// </summary>
public static class ResponseEnricher
{
    public static BaseResponse Enrich(BaseResponse response, HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? context.TraceIdentifier;
        var traceId = context.TraceIdentifier;

        return response.WithContext(correlationId, traceId);
    }

    public static BaseResponse<T> Enrich<T>(BaseResponse<T> response, HttpContext context)
    {
        var correlationId = context.Request.Headers["X-Correlation-Id"].FirstOrDefault()
                            ?? context.TraceIdentifier;
        var traceId = context.TraceIdentifier;

        return (BaseResponse<T>)response.WithContext(correlationId, traceId);
    }
}
