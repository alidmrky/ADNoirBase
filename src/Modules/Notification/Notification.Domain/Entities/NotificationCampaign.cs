using ADNoirBase.Shared.Domain.Entities;
using Notification.Domain.Enums;
using Notification.Domain.ValueObjects;

namespace Notification.Domain.Entities;

/// <summary>
/// Toplu gönderim. Alıcılar istek içinde değil, arka planda parça parça kuyruğa eklenir;
/// on binlerce alıcıda HTTP isteği zaman aşımına düşerdi.
/// </summary>
public sealed class NotificationCampaign : SoftDeletableEntity<Guid>
{
    public string Name { get; private set; } = string.Empty;

    public NotificationChannel Channel { get; private set; }

    public string TemplateCode { get; private set; } = string.Empty;

    public CampaignAudience Audience { get; private set; } = CampaignAudience.AllUsers();

    public CampaignSendMode SendMode { get; private set; } = CampaignSendMode.Individual;

    public CampaignStatus Status { get; private set; } = CampaignStatus.Draft;

    /// <summary>İleri tarihli gönderim. Null ise başlatıldığı anda işlenir.</summary>
    public DateTime? ScheduledAt { get; private set; }

    public DateTime? StartedAt { get; private set; }

    public DateTime? CompletedAt { get; private set; }

    /// <summary>Alıcı sayısı; alıcı üretimi bitene kadar artmaya devam eder.</summary>
    public int TotalRecipients { get; private set; }

    public int QueuedCount { get; private set; }

    public string? LastError { get; private set; }

    private NotificationCampaign() { }

    public static NotificationCampaign Create(
        string name,
        NotificationChannel channel,
        string templateCode,
        CampaignAudience audience,
        CampaignSendMode sendMode,
        DateTime? scheduledAt)
    {
        return new NotificationCampaign
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Channel = channel,
            TemplateCode = NotificationTemplate.NormalizeCode(templateCode),
            Audience = audience,
            SendMode = sendMode,
            ScheduledAt = scheduledAt,
            Status = CampaignStatus.Draft
        };
    }

    public void Queue()
    {
        if (Status != CampaignStatus.Draft)
        {
            throw new InvalidOperationException(
                $"Yalnızca taslak kampanya başlatılabilir; şu anki durum {Status}.");
        }

        Status = CampaignStatus.Queued;
    }

    public void BeginExpanding(DateTime startedAt)
    {
        Status = CampaignStatus.Expanding;
        StartedAt = startedAt;
        QueuedCount = 0;
    }

    public void ReportProgress(int queuedCount, int totalRecipients)
    {
        QueuedCount = queuedCount;
        TotalRecipients = totalRecipients;
    }

    /// <summary>Tüm alıcılar kuyruğa eklendi; bundan sonrası gönderim worker'ının işi.</summary>
    public void FinishExpanding(int totalRecipients)
    {
        TotalRecipients = totalRecipients;
        QueuedCount = totalRecipients;
        Status = CampaignStatus.Sending;
    }

    public void Complete(DateTime completedAt)
    {
        Status = CampaignStatus.Completed;
        CompletedAt = completedAt;
    }

    public void Cancel()
    {
        if (Status is CampaignStatus.Completed or CampaignStatus.Cancelled)
        {
            throw new InvalidOperationException(
                $"Tamamlanmış veya iptal edilmiş kampanya iptal edilemez; şu anki durum {Status}.");
        }

        Status = CampaignStatus.Cancelled;
        CompletedAt = DateTime.UtcNow;
    }

    public void Fail(string error)
    {
        Status = CampaignStatus.Failed;
        LastError = error.Length <= 2000 ? error : error[..2000];
        CompletedAt = DateTime.UtcNow;
    }

    /// <summary>Zamanlanmış kampanya için gönderim vakti geldi mi.</summary>
    public bool IsDue(DateTime now) => ScheduledAt is null || ScheduledAt <= now;
}
