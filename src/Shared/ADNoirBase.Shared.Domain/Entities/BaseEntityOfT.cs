namespace ADNoirBase.Shared.Domain.Entities;

/// <summary>Katman 1: Yalnızca kimlik. Generic Id tipi — Guid, int veya string olabilir.</summary>
public abstract class BaseEntity<TId>
    where TId : IEquatable<TId>
{
    public TId Id { get; protected set; } = default!;
}
