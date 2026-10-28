using System.Security.Claims;
using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Notification.Application.Dtos.Preferences;
using Notification.Application.Services.InApp;
using Notification.Application.Services.Preferences;

namespace ADNoirBase.API.Controllers.Notifications;

[Authorize]
[Route("api/notifications")]
public sealed class NotificationsController(IInAppNotificationService inApp, INotificationPreferenceService preferences) : BaseApiController
{
    private Guid? CurrentUserId => Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : null;

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] bool unreadOnly = false) =>
        CurrentUserId is { } id ? ToHttpResponse(await inApp.ListAsync(id, page, pageSize, unreadOnly, ct)) : ToHttpResponse(BaseResponse.Unauthorized());
    [HttpGet("unread-count")]
    public async Task<IActionResult> Count(CancellationToken ct) =>
        CurrentUserId is { } id ? ToHttpResponse(await inApp.CountUnreadAsync(id, ct)) : ToHttpResponse(BaseResponse.Unauthorized());
    [HttpPost("{notificationId:guid}/read")]
    public async Task<IActionResult> Read(Guid notificationId, CancellationToken ct) =>
        CurrentUserId is { } id ? ToHttpResponse(await inApp.MarkReadAsync(id, notificationId, ct)) : ToHttpResponse(BaseResponse.Unauthorized());
    [HttpPost("read-all")]
    public async Task<IActionResult> ReadAll(CancellationToken ct) =>
        CurrentUserId is { } id ? ToHttpResponse(await inApp.MarkAllReadAsync(id, ct)) : ToHttpResponse(BaseResponse.Unauthorized());
    [HttpGet("preferences")]
    public async Task<IActionResult> Preferences(CancellationToken ct) =>
        CurrentUserId is { } id ? ToHttpResponse(await preferences.GetAsync(id, ct)) : ToHttpResponse(BaseResponse.Unauthorized());
    [HttpPut("preferences")]
    public async Task<IActionResult> UpdatePreferences(UpdateNotificationPreferenceRequest request, CancellationToken ct) =>
        CurrentUserId is { } id ? ToHttpResponse(await preferences.UpdateAsync(id, request, ct)) : ToHttpResponse(BaseResponse.Unauthorized());
    [AllowAnonymous]
    [HttpPost("unsubscribe")]
    public async Task<IActionResult> Unsubscribe(UnsubscribeRequest request, CancellationToken ct) =>
        ToHttpResponse(await preferences.UnsubscribeAsync(request.Token, ct));
}
