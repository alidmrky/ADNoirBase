using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Campaigns;

public sealed record CampaignListDto(
    Guid Id,
    string Name,
    NotificationChannel Channel,
    string TemplateCode,
    CampaignStatus Status,
    int TotalRecipients,
    int QueuedCount,
    DateTime? ScheduledAt,
    DateTime CreatedAt);
