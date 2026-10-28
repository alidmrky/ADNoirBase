using ADNoirBase.API.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Notification.Application.Dtos.Templates;
using Notification.Application.Services.Templates;

namespace ADNoirBase.API.Controllers.Notifications;

[Authorize(Roles = "ADMIN")]
[Route("api/admin/notifications/templates")]
public sealed class NotificationTemplatesController(INotificationTemplateService service) : BaseApiController
{
    [HttpGet]
    public async Task<IActionResult> List([FromQuery] TemplateQuery query, CancellationToken ct) => ToHttpResponse(await service.ListAsync(query, ct));
    [HttpGet("{id:guid}")]
    public async Task<IActionResult> Get(Guid id, CancellationToken ct) => ToHttpResponse(await service.GetByIdAsync(id, ct));
    [HttpPost]
    public async Task<IActionResult> Create(CreateTemplateRequest request, CancellationToken ct) => ToHttpResponse(await service.CreateAsync(request, ct));
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, UpdateTemplateRequest request, CancellationToken ct) => ToHttpResponse(await service.UpdateAsync(id, request, ct));
    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct) => ToHttpResponse(await service.DeleteAsync(id, ct));
    [HttpPost("preview")]
    public async Task<IActionResult> Preview(TemplatePreviewRequest request, CancellationToken ct) => ToHttpResponse(await service.PreviewAsync(request, ct));
    [HttpPost("{id:guid}/test")]
    [EnableRateLimiting(RateLimitPolicies.NotificationSend)]
    public async Task<IActionResult> Test(Guid id, TemplateTestSendRequest request, CancellationToken ct) => ToHttpResponse(await service.TestSendAsync(id, request, ct));
}
