using ADNoirBase.Shared.Application.Interfaces;
using Microsoft.Extensions.Options;

namespace ADNoirBase.Shared.Infrastructure.Authentication;

public sealed class BcryptPasswordHasher : IPasswordHasher
{
    private readonly int _workFactor;

    public BcryptPasswordHasher(IOptions<BcryptSettings> settings)
    {
        _workFactor = settings.Value.WorkFactor;
    }

    public string Hash(string password) =>
        BCrypt.Net.BCrypt.HashPassword(password, _workFactor);

    // Maliyet hash'in içinde saklı olduğu için eski maliyetle üretilmiş hash'ler doğrulanmaya devam eder.
    public bool Verify(string password, string hash) =>
        BCrypt.Net.BCrypt.Verify(password, hash);

    public bool NeedsRehash(string hash) =>
        BCrypt.Net.BCrypt.PasswordNeedsRehash(hash, _workFactor);
}
