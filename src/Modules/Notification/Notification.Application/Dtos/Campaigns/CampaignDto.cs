using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Campaigns;

public sealed record CampaignDto(
    Guid Id,
    string Name,
    NotificationChannel Channel,
    string TemplateCode,
    CampaignAudienceDto Audience,
    CampaignSendMode SendMode,
    CampaignStatus Status,
    int TotalRecipients,
    int QueuedCount,
    DateTime? ScheduledAt,
    DateTime? StartedAt,
    DateTime? CompletedAt,
    string? LastError,
    DateTime CreatedAt);
