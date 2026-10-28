namespace ADNoirBase.Shared.Application.Interfaces;

public interface IPasswordHasher
{
    string Hash(string password);
    bool Verify(string password, string hash);

    /// <summary>Hash, yapılandırılmış maliyetten düşük bir maliyetle üretilmişse true döner.</summary>
    bool NeedsRehash(string hash);
}
