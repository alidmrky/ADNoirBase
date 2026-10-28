# Module Creator Agent

Yeni bir modül oluşturmak için bu agent'ı kullan.

## Kullanım
"X adında yeni modül oluştur" veya "Add Y module"

## Agent'ın yapacakları

### 1. Proje dosyaları oluştur
```
src/Modules/[ModuleName]/
├── [ModuleName].Domain/
├── [ModuleName].Application/
└── [ModuleName].Infrastructure/
```

### 2. Solution'a ekle ve referansları ayarla
- `[Mod].Domain` → `Shared.Domain`
- `[Mod].Application` → `[Mod].Domain` + `Shared.Application`
- `[Mod].Infrastructure` → `[Mod].Application` + `Shared.Infrastructure`
- `API` → `[Mod].Infrastructure` ekle

### 3. Klasör yapısını oluştur
Domain: `Entities/`, `Enums/`, `Interfaces/Repositories/`
Application: `Messages/`, `Dtos/`, `Services/`, `Validators/`
Infrastructure: `Persistence/Configurations/`, `Persistence/Migrations/`, `Persistence/Repositories/`

### 4. Zorunlu dosyaları yaz
- `[Mod]MessageCodes.cs` — modüle ait mesaj kodları
- `DependencyInjection.cs` — `IModuleRegistration` implement eder
- `[Mod]DbContext.cs` — `HasDefaultSchema("[modname]")` ile
- `PostgresRepository<[Mod]DbContext, TEntity, TId>` (veya `MongoRepository<TEntity, TId>`) extend eden repository'ler

### 5. Program.cs güncelle
- `AddModules()` çağrısına marker type ekle
- Infrastructure extension metodunu çağır

## Kurallar
- Her dosyada 1 class
- MessageCodes literal string içermez
- Tüm servis metodları BaseResponse döner
- EF global query filter (soft delete) eklenir
