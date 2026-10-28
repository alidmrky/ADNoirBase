namespace Notification.Domain.Enums;

public enum CampaignSendMode
{
    /// <summary>Her alıcıya ayrı mail. Kişiselleştirme ve abonelikten çıkma linki yalnızca bu modda çalışır.</summary>
    Individual = 1,

    /// <summary>Tek mail, alıcılar BCC'de. Hızlıdır ama kişiselleştirme yapılamaz.</summary>
    SingleBcc = 2
}
