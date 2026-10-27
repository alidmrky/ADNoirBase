using System.Text.Json;
using System.Text.Json.Serialization;
using ADNoirBase.Shared.Contracts.Messages;
using ADNoirBase.Shared.Contracts.Responses;
using ADNoirBase.Shared.Domain.Exceptions;

namespace ADNoirBase.API.Middleware;

public sealed class ExceptionHandlingMiddleware
{
    // Controller'ların AddJsonOptions ayarıyla aynı olmalı; bu middleware
    // pipeline'ın dışında kendi serileştirmesini yapar.
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (DomainException ex)
        {
            _logger.LogWarning(ex, "Domain exception: {MessageCode}", ex.MessageCode);
            await WriteResponseAsync(context, StatusCodes.Status422UnprocessableEntity,
                BaseResponse.BusinessRuleViolation(ex.MessageCode));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception");
            await WriteResponseAsync(context, StatusCodes.Status500InternalServerError,
                BaseResponse.Failure(ResponseStatus.UnexpectedError, MessageCodes.Common.UnexpectedError));
        }
    }

    private static async Task WriteResponseAsync(HttpContext context, int statusCode, BaseResponse response)
    {
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/json";

        var json = JsonSerializer.Serialize(response, SerializerOptions);

        await context.Response.WriteAsync(json);
    }
}
