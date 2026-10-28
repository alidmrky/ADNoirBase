using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Mvc;

namespace ADNoirBase.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public abstract class BaseApiController : ControllerBase
{
    /// <summary>BaseResponse.Status'u doğru HTTP status koduna çevirir.</summary>
    protected IActionResult ToHttpResponse(BaseResponse response) =>
        response.Status switch
        {
            ResponseStatus.Success              => Ok(response),
            ResponseStatus.Created              => StatusCode(201, response),
            ResponseStatus.Accepted             => Accepted(response),
            ResponseStatus.NoContent            => NoContent(),
            ResponseStatus.NotFound             => NotFound(response),
            ResponseStatus.ValidationFailed     => UnprocessableEntity(response),
            ResponseStatus.Conflict             => Conflict(response),
            ResponseStatus.Unauthorized         => Unauthorized(response),
            ResponseStatus.Forbidden            => StatusCode(403, response),
            ResponseStatus.BusinessRuleViolation => UnprocessableEntity(response),
            ResponseStatus.ConcurrencyConflict  => Conflict(response),
            ResponseStatus.RateLimited          => StatusCode(429, response),
            _                                   => StatusCode(500, response)
        };
}
