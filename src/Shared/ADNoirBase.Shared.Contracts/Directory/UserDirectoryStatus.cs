namespace ADNoirBase.Shared.Contracts.Directory;

/// <summary>
/// Identity'nin UserStatus enum'unun modül sınırını aşan karşılığı.
/// Bildirim modülü Identity'ye referans veremediği için hedef kitle bu tiple ifade edilir.
/// </summary>
public enum UserDirectoryStatus
{
    Active = 1,
    Inactive = 2,
    Locked = 3
}
