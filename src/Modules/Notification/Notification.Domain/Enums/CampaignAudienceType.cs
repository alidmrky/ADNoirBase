namespace Notification.Domain.Enums;

public enum CampaignAudienceType
{
    /// <summary>Silinmemiş tüm kullanıcılar.</summary>
    AllUsers = 1,

    /// <summary>Belirtilen rollerden en az birine sahip kullanıcılar.</summary>
    ByRole = 2,

    /// <summary>Belirtilen durumdaki kullanıcılar.</summary>
    ByStatus = 3,

    /// <summary>Elle seçilen kullanıcılar.</summary>
    SelectedUsers = 4,

    /// <summary>Sisteme kayıtlı olmayabilecek, elle girilen adresler.</summary>
    ManualAddresses = 5
}
