namespace Notification.Application.Configuration;

public sealed class SmsSettings
{
    public SmsProvider Provider { get; init; } = SmsProvider.Log;

    public string? FromNumber { get; init; }

    /// <summary>Tek bir SMS'in karakter sınırı; aşan mesaj parçalara bölünür ve maliyeti artar.</summary>
    public int MaxLength { get; init; } = 918;
}
