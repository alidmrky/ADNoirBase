using ADNoirBase.API.Configuration;
using ADNoirBase.Shared.Application.Common;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Notification.Application.Dtos.Campaigns;
using Notification.Application.Services.Campaigns;

namespace ADNoirBase.API.Controllers.Notifications;

[Authorize(Roles = "ADMIN")]
[Route("api/admin/notifications/campaigns")]
public sealed class NotificationCampaignsController(INotificationCampaignService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] PagedRequest request, CancellationToken ct) => ToHttpResponse(await service.ListAsync(request, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToHttpResponse(await service.GetByIdAsync(id, ct));
    [HttpPost]
    public async Task<IActionResult> Create(CreateCampaignRequest request, CancellationToken ct) => ToHttpResponse(await service.CreateAsync(request, ct));
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(CampaignAudienceDto request, CancellationToken ct) => ToHttpResponse(await service.PreviewAudienceAsync(request, ct));
    [HttpPost("{id:guid}/start")]
    [EnableRateLimiting(RateLimitPolicies.NotificationSend)]
    public async Task<IActionResult> Start(Guid id, CancellationToken ct) => ToHttpResponse(await service.StartAsync(id, ct));
    [HttpPost("{id:guid}/cancel")]
    public async Task<IActionResult> Cancel(Guid id, CancellationToken ct) => ToHttpResponse(await service.CancelAsync(id, ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToHttpResponse(await service.DeleteAsync(id, ct));
}
