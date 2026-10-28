using ADNoirBase.Shared.Application.Common;
using System.Net.Mail;
using ADNoirBase.Shared.Application.Interfaces;
using ADNoirBase.Shared.Contracts.Pagination;
using ADNoirBase.Shared.Contracts.Responses;
using Microsoft.Extensions.Options;
using Notification.Application.Configuration;
using Notification.Application.Dtos.Campaigns;
using Notification.Application.Messages;
using Notification.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.Interfaces.Repositories;
using Notification.Domain.ValueObjects;

namespace Notification.Application.Services.Campaigns;

public sealed class NotificationCampaignService : INotificationCampaignService
{
    private readonly INotificationCampaignRepository _campaignRepository;
    private readonly INotificationMessageRepository _messageRepository;
    private readonly INotificationTemplateRepository _templateRepository;
    private readonly IUserDirectory _userDirectory;
    private readonly CampaignSettings _settings;

    public NotificationCampaignService(
        INotificationCampaignRepository campaignRepository,
        INotificationMessageRepository messageRepository,
        INotificationTemplateRepository templateRepository,
        IUserDirectory userDirectory,
        IOptions<NotificationSettings> settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        _campaignRepository = campaignRepository;
        _messageRepository = messageRepository;
        _templateRepository = templateRepository;
        _userDirectory = userDirectory;
        _settings = settings.Value.Campaigns;
    }

    public async Task<BaseResponse<PagedList<CampaignListDto>>> ListAsync(
        PagedRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var paged = await _campaignRepository.ListPagedAsync(request.Page, request.PageSize, null, ct);

        var items = paged.Items
            .Select(c => new CampaignListDto(
                c.Id, c.Name, c.Channel, c.TemplateCode, c.Status,
                c.TotalRecipients, c.QueuedCount, c.ScheduledAt, c.CreatedAt))
            .ToList();

        return BaseResponse<PagedList<CampaignListDto>>.Success(
            new PagedList<CampaignListDto>(items, paged.TotalCount, paged.Page, paged.PageSize));
    }

    public async Task<BaseResponse<CampaignDto>> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _campaignRepository.FindByIdAsync(id, ct);

        return campaign is null
            ? BaseResponse<CampaignDto>.NotFound(NotificationMessageCodes.Campaign.NotFound)
            : BaseResponse<CampaignDto>.Success(ToDto(campaign));
    }

    public async Task<BaseResponse<CampaignDto>> CreateAsync(
        CreateCampaignRequest request, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Length > 200 ||
            string.IsNullOrWhiteSpace(request.TemplateCode) || !Enum.IsDefined(request.Channel) ||
            !Enum.IsDefined(request.SendMode) || !ValidAudience(request.Audience) ||
            (request.SendMode == CampaignSendMode.SingleBcc && request.Channel != NotificationChannel.Email) ||
            (request.Audience.Type == CampaignAudienceType.ManualAddresses &&
                (request.Channel == NotificationChannel.InApp || request.Audience.ManualAddresses!.Any(address =>
                    request.Channel == NotificationChannel.Email
                        ? !MailAddress.TryCreate(address, out var mail) || mail.Address != address
                        : string.IsNullOrEmpty(address) || address.Length is < 8 or > 16 || address[0] != '+' || !address[1..].All(char.IsAsciiDigit)))))
        {
            return BaseResponse<CampaignDto>.BusinessRuleViolation(NotificationMessageCodes.Campaign.Invalid);
        }

        var template = await _templateRepository.FindByCodeAsync(request.TemplateCode, ct);

        if (template is null)
        {
            return BaseResponse<CampaignDto>.NotFound(NotificationMessageCodes.Template.NotFound);
        }

        if (!template.IsActive || template.IsLayout)
        {
            return BaseResponse<CampaignDto>.BusinessRuleViolation(
                NotificationMessageCodes.Template.Inactive);
        }

        if (template.Channel != request.Channel)
        {
            return BaseResponse<CampaignDto>.BusinessRuleViolation(
                NotificationMessageCodes.Template.ChannelMismatch);
        }

        var audience = ToAudience(request.Audience);

        if (IsAudienceEmpty(audience))
        {
            return BaseResponse<CampaignDto>.BusinessRuleViolation(
                NotificationMessageCodes.Campaign.AudienceEmpty);
        }

        var campaign = NotificationCampaign.Create(
            request.Name, request.Channel, template.Code, audience, request.SendMode, request.ScheduledAt);

        await _campaignRepository.InsertAsync(campaign, ct);

        return BaseResponse<CampaignDto>.Created(
            ToDto(campaign), NotificationMessageCodes.Campaign.Created);
    }

    public async Task<BaseResponse<CampaignAudiencePreviewDto>> PreviewAudienceAsync(
        CampaignAudienceDto audience, CancellationToken ct = default)
    {
        if (!ValidAudience(audience))
        {
            return BaseResponse<CampaignAudiencePreviewDto>.BusinessRuleViolation(NotificationMessageCodes.Campaign.Invalid);
        }
        var resolved = ToAudience(audience);

        var count = resolved.ResolvesFromDirectory
            ? await _userDirectory.CountAsync(resolved.ToFilter(), ct)
            : resolved.ManualAddresses.Count;

        return BaseResponse<CampaignAudiencePreviewDto>.Success(
            new CampaignAudiencePreviewDto(
                count, count > _settings.MaxRecipients, _settings.MaxRecipients));
    }

    public async Task<BaseResponse> StartAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _campaignRepository.FindByIdAsync(id, ct);

        if (campaign is null)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.Campaign.NotFound);
        }

        if (campaign.Status != CampaignStatus.Draft)
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Campaign.NotStartable);
        }

        campaign.Queue();
        await _campaignRepository.UpdateAsync(campaign, ct);

        return BaseResponse.Success(NotificationMessageCodes.Campaign.Started);
    }

    public async Task<BaseResponse> CancelAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _campaignRepository.FindByIdAsync(id, ct);

        if (campaign is null)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.Campaign.NotFound);
        }

        if (campaign.Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Campaign.NotCancellable);
        }

        campaign.Cancel();
        await _campaignRepository.UpdateAsync(campaign, ct);

        // Alıcı üretimi hâlâ sürüyor olabilir; worker da durumu görüp kalanları iptal eder.
        await _messageRepository.CancelPendingByCampaignAsync(campaign.Id, ct);

        return BaseResponse.Success(NotificationMessageCodes.Campaign.Cancelled);
    }

    public async Task<BaseResponse> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var campaign = await _campaignRepository.FindByIdAsync(id, ct);

        if (campaign is null)
        {
            return BaseResponse.NotFound(NotificationMessageCodes.Campaign.NotFound);
        }

        // Gönderimi süren bir kampanyayı silmek, kuyruğa yazılmış mesajları sahipsiz bırakır.
        if (campaign.Status is CampaignStatus.Queued or CampaignStatus.Expanding or CampaignStatus.Sending)
        {
            return BaseResponse.BusinessRuleViolation(NotificationMessageCodes.Campaign.NotCancellable);
        }

        await _campaignRepository.DeleteAsync(campaign, ct);

        return BaseResponse.Success();
    }

    private static bool IsAudienceEmpty(CampaignAudience audience) => audience.Type switch
    {
        CampaignAudienceType.AllUsers => false,
        CampaignAudienceType.ByRole => audience.RoleCodes.Count == 0,
        CampaignAudienceType.ByStatus => audience.Statuses.Count == 0,
        CampaignAudienceType.SelectedUsers => audience.UserIds.Count == 0,
        CampaignAudienceType.ManualAddresses => audience.ManualAddresses.Count == 0,
        _ => true
    };

    private static bool ValidAudience(CampaignAudienceDto? audience) => audience is not null && audience.Type switch
    {
        CampaignAudienceType.AllUsers => true,
        CampaignAudienceType.ByRole => audience.RoleCodes is { Count: > 0 } && audience.RoleCodes.All(r => !string.IsNullOrWhiteSpace(r)),
        CampaignAudienceType.ByStatus => audience.Statuses is { Count: > 0 } && audience.Statuses.All(s => Enum.IsDefined(s)),
        CampaignAudienceType.SelectedUsers => audience.UserIds is { Count: > 0 } && audience.UserIds.All(id => id != Guid.Empty),
        CampaignAudienceType.ManualAddresses => audience.ManualAddresses is { Count: > 0 } && audience.ManualAddresses.All(a => !string.IsNullOrWhiteSpace(a)),
        _ => false
    };

    private static CampaignAudience ToAudience(CampaignAudienceDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new CampaignAudience(
            dto.Type,
            dto.RoleCodes ?? [],
            dto.Statuses ?? [],
            dto.UserIds ?? [],
            dto.ManualAddresses ?? []);
    }

    private static CampaignDto ToDto(NotificationCampaign c) => new(
        c.Id,
        c.Name,
        c.Channel,
        c.TemplateCode,
        new CampaignAudienceDto(
            c.Audience.Type,
            c.Audience.RoleCodes,
            c.Audience.Statuses,
            c.Audience.UserIds,
            c.Audience.ManualAddresses),
        c.SendMode,
        c.Status,
        c.TotalRecipients,
        c.QueuedCount,
        c.ScheduledAt,
        c.StartedAt,
        c.CompletedAt,
        c.LastError,
        c.CreatedAt);
}
