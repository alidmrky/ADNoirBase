namespace ADNoirBase.Shared.Domain.Entities;

// ─── Guid Id alias'ları — çoğu entity bu kullanır ────────────────────────────

public abstract class BaseEntity : BaseEntity<Guid> { }

public abstract class AuditableEntity : AuditableEntity<Guid> { }

public abstract class FullAuditableEntity : FullAuditableEntity<Guid> { }

public abstract class SoftDeletableEntity : SoftDeletableEntity<Guid> { }

// ─── int Id alias'ları — legacy veya performans odaklı tablolar ───────────────

public abstract class IntEntity : BaseEntity<int> { }

public abstract class IntAuditableEntity : AuditableEntity<int> { }

public abstract class IntSoftDeletableEntity : SoftDeletableEntity<int> { }

// ─── string Id alias'ları — MongoDB dökümanları veya harici sistem ID'leri ───

public abstract class StringEntity : BaseEntity<string> { }

public abstract class StringAuditableEntity : AuditableEntity<string> { }
