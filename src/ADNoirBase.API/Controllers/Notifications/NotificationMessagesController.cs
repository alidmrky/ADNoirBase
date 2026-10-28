using ADNoirBase.API.Configuration;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Notification.Application.Dtos;
using Notification.Application.Services;
using Notification.Application.Services.Attachments;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;

namespace ADNoirBase.API.Controllers.Notifications;

[Authorize(Roles = "ADMIN")]
[Route("api/admin/notifications")]
public sealed class NotificationMessagesController(INotificationMessageRepository repository, INotificationSendService sender, INotificationAttachmentService attachments) : BaseApiController
{
    [HttpGet("messages")]
    public async Task<IActionResult> List(CancellationToken ct, [FromQuery] int page = 1, [FromQuery] int pageSize = 20,
        [FromQuery] NotificationChannel? channel = null, [FromQuery] NotificationStatus? status = null,
        [FromQuery] Guid? campaignId = null, [FromQuery] string? recipient = null) =>
        ToHttpResponse(BaseResponse<PagedList<NotificationMessage>>.Success(await repository.SearchAsync(
            Math.Max(1, page), Math.Clamp(pageSize, 1, 100), channel, status, campaignId, recipient, ct)));

    [HttpPost("send")]
    [EnableRateLimiting(RateLimitPolicies.NotificationSend)]
    public async Task<IActionResult> Send(SendNotificationRequest request, CancellationToken ct) => ToHttpResponse(await sender.SendAsync(request, ct));

    [HttpPost("attachments")]
    [EnableRateLimiting(RateLimitPolicies.NotificationSend)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken ct)
    {
        await using var stream = file.OpenReadStream();
        return ToHttpResponse(await attachments.UploadAsync(file.FileName, file.ContentType, stream, ct));
    }
}
