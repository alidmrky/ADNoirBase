namespace ADNoirBase.Shared.Infrastructure.Authentication;

public sealed class BcryptSettings
{
    public const string SectionName = "Bcrypt";

    /// <summary>Log2 maliyet; her artış hash süresini ikiye katlar.</summary>
    public int WorkFactor { get; init; } = 12;
}
