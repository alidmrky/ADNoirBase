using ADNoirBase.Shared.Domain.Entities;
using Identity.Domain.Enums;

namespace Identity.Domain.Entities;

public sealed class User : SoftDeletableEntity<Guid>
{
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;

    /// <summary>SMS bildirimleri için E.164 biçiminde numara; zorunlu değildir.</summary>
    public string? PhoneNumber { get; private set; }

    public UserStatus Status { get; private set; } = UserStatus.Active;
    public int FailedLoginCount { get; private set; }
    public DateTime? LastLoginAt { get; private set; }

    public ICollection<UserRole> UserRoles { get; private set; } = [];
    public ICollection<RefreshToken> RefreshTokens { get; private set; } = [];

    // EF Core için
    private User() { }

    public static User Create(
        string email,
        string passwordHash,
        string firstName,
        string lastName,
        string? phoneNumber = null)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
            FirstName = firstName.Trim(),
            LastName = lastName.Trim(),
            PhoneNumber = NormalizePhoneNumber(phoneNumber),
            Status = UserStatus.Active
        };
    }

    public void UpdateProfile(string firstName, string lastName, string? phoneNumber = null)
    {
        FirstName = firstName.Trim();
        LastName = lastName.Trim();
        PhoneNumber = NormalizePhoneNumber(phoneNumber);
    }

    /// <summary>Boş string ile null arasındaki farkı silerek "numara yok" durumunu tek biçimde saklar.</summary>
    private static string? NormalizePhoneNumber(string? phoneNumber) =>
        string.IsNullOrWhiteSpace(phoneNumber) ? null : phoneNumber.Trim();

    public void ChangePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        FailedLoginCount = 0;
    }

    public void RecordSuccessfulLogin()
    {
        FailedLoginCount = 0;
        LastLoginAt = DateTime.UtcNow;
        Status = UserStatus.Active;
    }

    public void RecordFailedLogin(int lockThreshold = 5)
    {
        FailedLoginCount++;
        if (FailedLoginCount >= lockThreshold)
        {
            Status = UserStatus.Locked;
        }
    }

    public void Deactivate() => Status = UserStatus.Inactive;

    public void Activate()
    {
        Status = UserStatus.Active;
        FailedLoginCount = 0;
    }

    public string FullName => $"{FirstName} {LastName}";
}
