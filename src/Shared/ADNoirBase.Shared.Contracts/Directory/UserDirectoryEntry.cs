namespace ADNoirBase.Shared.Contracts.Directory;

/// <summary>Bildirim gönderimi için gereken en az kullanıcı bilgisi.</summary>
public sealed record UserDirectoryEntry(
    Guid UserId,
    string Email,
    string? PhoneNumber,
    string FirstName,
    string LastName,
    UserDirectoryStatus Status)
{
    public string FullName => $"{FirstName} {LastName}";
}
