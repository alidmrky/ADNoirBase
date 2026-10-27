# ADNoirBase — Project Overview

## Mimari

**Modular Monolith + Clean Architecture**

Her modül kendi Clean Architecture katmanlarına sahip bağımsız projelerden oluşur.
Tek API entry point tüm modülleri birleştirir.

### Bağımlılık Zinciri
```
Shared.Contracts
    ↑
Shared.Domain
    ↑
Shared.Application ←── [Mod].Domain
    ↑                        ↑
Shared.Infrastructure    [Mod].Application
    ↑                        ↑
    └─────── [Mod].Infrastructure
                    ↑
              ADNoirBase.API
```

---

## Katmanlar

| Katman | Sorumluluk | Dış Bağımlılık |
|--------|-----------|----------------|
| `Shared.Contracts` | BaseResponse, MessageCodes | Yok |
| `Shared.Domain` | Entity hiyerarşisi, IRepository | Yalnızca Contracts |
| `Shared.Application` | ICurrentUserService, PagedRequest | Domain |
| `Shared.Infrastructure` | JWT, AuditInterceptor, CurrentUserService | Application |
| `[Mod].Domain` | Modül entity'leri, repo arayüzleri | Shared.Domain |
| `[Mod].Application` | Servisler, DTO'lar, Validator'lar | Mod.Domain + Shared.App |
| `[Mod].Infrastructure` | EF Core, Repository impl. | Mod.App + Shared.Infra |
| `API` | Controller'lar, middleware | Tüm Infrastructure'lar |

---

## Entity Hiyerarşisi

```
BaseEntity<TId>
  └── AuditableEntity<TId>         + CreatedAt, CreatedBy
        └── FullAuditableEntity<TId>     + UpdatedAt, UpdatedBy
              └── SoftDeletableEntity<TId>   + IsDeleted, DeletedAt, DeletedBy
```

**Id Tipleri:**
- `BaseEntity` (Guid) — çoğu entity
- `IntEntity` (int) — performans/legacy tablolar
- `StringEntity` (string) — MongoDB dökümanları

---

## Response Kontratı

Tüm servis metodları `BaseResponse` veya `BaseResponse<T>` döner. ProblemDetails kullanılmaz.

```csharp
// Başarı
return BaseResponse<UserDto>.Success(dto, IdentityMessageCodes.Auth.LoginSuccess);

// Hata
return BaseResponse<UserDto>.NotFound(IdentityMessageCodes.User.NotFound);

// Validasyon
return ValidationHelper.ToValidationFailedResponse<UserDto>(validationResult);
```

---

## Mesaj Kodu Sistemi

| Proje | Sınıf | Örnek |
|-------|-------|-------|
| Shared.Contracts | `MessageCodes.Common` | `common.not_found` |
| Shared.Contracts | `MessageCodes.Validation` | `validation.required` |
| Shared.Contracts | `MessageCodes.Auth` | `auth.token_expired` |
| Identity.Application | `IdentityMessageCodes` | `identity.user.not_found` |
| [Mod].Application | `[Mod]MessageCodes` | `order.not_found` |

**Kural:** Literal string yasak. Her zaman sabit kullan.

---

## Yeni Modül Ekleme

1. `src/Modules/[ModuleName]/` altında 3 proje oluştur
2. Referansları ayarla (kurallara bak: `.cursor/rules/`)
3. `[Mod]MessageCodes.cs` oluştur
4. `IModuleRegistration` implement et (`DependencyInjection.cs`)
5. `[Mod]DbContext` oluştur, schema tanımla
6. `BasePostgresRepository` extend et
7. `Program.cs`'de `AddModules()` ve Infrastructure extension'ını çağır
8. `.cursor/agents/module-creator.md` agent'ını kullan

---

## Veritabanı Seçim Rehberi

| Durum | Tercih |
|-------|--------|
| İlişkisel veri, transaction gereksinimi | PostgreSQL |
| Kimlik yönetimi, kritik veri | PostgreSQL |
| Belge tabanlı, schema esnekliği | MongoDB |
| Log, event, aktivite kaydı | MongoDB |
| Her ikisi de olabilir | Modül bazında karar ver |

---

## DbMigrator

```bash
# Lokalde migration çalıştır
cd src/Tools/ADNoirBase.DbMigrator
dotnet run

# Docker ile
docker-compose up migrator
```

Migrator sırası:
1. EF Core migration'larını uygula
2. Varsayılan rolleri seed et (ADMIN, USER)
3. Admin kullanıcı oluştur (`ADMIN_EMAIL`, `ADMIN_PASSWORD` env var'larından)

---

## Geliştirme Ortamı

```bash
# Docker ile tüm altyapıyı başlat
docker-compose up -d postgres mongodb

# Migration + seed
cd src/Tools/ADNoirBase.DbMigrator && dotnet run

# API'yi başlat
cd src/ADNoirBase.API && dotnet run

# Swagger
http://localhost:{port}/swagger
```
