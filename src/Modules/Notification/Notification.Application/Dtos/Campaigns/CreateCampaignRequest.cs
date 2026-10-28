using Notification.Domain.Enums;

namespace Notification.Application.Dtos.Campaigns;

public sealed record CreateCampaignRequest(
    string Name,
    NotificationChannel Channel,
    string TemplateCode,
    CampaignAudienceDto Audience,
    CampaignSendMode SendMode = CampaignSendMode.Individual,
    DateTime? ScheduledAt = null);
