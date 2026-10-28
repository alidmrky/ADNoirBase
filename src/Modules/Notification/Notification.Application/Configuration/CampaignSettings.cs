namespace Notification.Application.Configuration;

public sealed class CampaignSettings
{
    public bool Enabled { get; init; } = true;

    /// <summary>Alıcılar bu büyüklükte parçalar halinde kuyruğa yazılır.</summary>
    public int BatchSize { get; init; } = 500;

    public int PollSeconds { get; init; } = 10;

    /// <summary>Tek bir kampanyanın azami alıcı sayısı; yanlışlıkla devasa gönderime karşı emniyet.</summary>
    public int MaxRecipients { get; init; } = 100_000;
}
