# Güvenlik Sorunları — Kontrol Listesi

> Denetim tarihi: 27 Ekim 2026  
> Kaynak: Kanıta dayalı mimari denetim raporu  
> Tamamlananları `[x]` ile işaretle, commit mesajında `fix(security): S<numara>` formatını kullan.

---

## 🔴 Kritik — Hemen

### S2 — appsettings.json içinde cleartext kimlik bilgisi
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `appsettings.json` ve DbMigrator `appsettings.json` içindeki tüm gizli değerler boşaltıldı. `docker-compose.yml` artık `${VAR:?}` ile `.env`'den okur ve değişken eksikse başlamaz. Kök dizine `.env.example` eklendi. `JwtSettingsValidator` anahtar eksik veya 32 karakterden kısaysa uygulamayı başlatmıyor.
- **Konum:** `src/ADNoirBase.API/appsettings.json:3` (DB parolası) ve `:7` (JWT imzalama anahtarı) — değerler bilerek buraya yazılmadı
- **Risk:** Kaynak kod paylaşımında (GitHub, CI log, ekip üyesi) DB parolası ve JWT imzalama anahtarı sızdırılır. Anahtar ele geçirilirse tüm token'lar sahte üretilebilir.
- **Çözüm:**
  1. `appsettings.json`'daki gerçek değerleri boş string veya placeholder yap (`""` veya `"CHANGE_ME"`)
  2. `.env.example` oluştur, gerçek değerleri `appsettings.Development.json` veya user-secrets'a taşı
  3. `dotnet user-secrets set "ConnectionStrings:PostgreSQL" "..."` (geliştirme)
  4. Production'da environment variable kullan

---

### S5 — AdminUserSeeder hardcoded fallback parolası
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `ADMIN_EMAIL` ve `ADMIN_PASSWORD` artık `ReadRequired` ile zorunlu; eksikse `InvalidOperationException` fırlatılıyor. Ayrıca parola için 12 karakter alt sınırı eklendi.
- **Konum:** `src/Tools/ADNoirBase.DbMigrator/Seeders/AdminUserSeeder.cs:11`
  ```csharp
  var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "<sabit varsayılan parola>";
  ```
- **Risk:** `ADMIN_PASSWORD` env var set edilmezse üretim ortamına kaynak kodda açıkça duran, herkesin bilebileceği bir parolayla admin hesabı açılır. Bu durumun fark edilmesi zordur.
- **Çözüm:** Fallback'i kaldır — env var yoksa `InvalidOperationException` fırlat:
  ```csharp
  var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD")
      ?? throw new InvalidOperationException("ADMIN_PASSWORD environment variable is required.");
  ```

---

### S3 — Sıfır çalışan test
- [ ] Tamamlandı
- **Konum:** `tests/` — 5 proje, hepsi boş `UnitTest1.cs`
- **Risk:** Refactor sonrası regresyon tespiti imkânsız. `BaseResponse` davranışı, validator mantığı, AuthService akışı hiç doğrulanmamış.
- **Çözüm:** (Faz 3) Öncelik sırası:
  1. `BaseResponse` birim testleri (`IsSuccess` → `Status` mantığı)
  2. `AuthService` birim testleri (mock repository ile Login/Register/Refresh)
  3. `FluentValidation` validator testleri
  4. `Architecture.Tests` — `NetArchTest.Rules` ile katman bağımlılık kuralları

---

## 🟠 Yüksek

### S4 — Rate limiting yok
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `RateLimitExtensions` ile iki katmanlı sabit pencere limiti eklendi: tüm uçlar için istemci başına 100/dk, `AuthController` için `[EnableRateLimiting]` ile 5/dk. Limit aşıldığında `BaseResponse` zarfı içinde `RateLimited` durumu ve `Retry-After` başlığı dönüyor. Bölümleme kimliği doğrulanmış isteklerde kullanıcı, anonimlerde IP bazlı. Limitler `RateLimiting` bölümünden ayarlanır; `Enabled: false` ile testlerde kapatılabilir.
- **Ek:** Rate limiting IP'ye göre bölümlendiği için reverse proxy arkasında gerçek IP gerekir. `ProxyExtensions` bunu `ForwardedHeaders` bölümünden yönetiyor ve `Enabled: true` iken `KnownProxies`/`KnownNetworks` boşsa başlamayı reddediyor — aksi halde istemciler `X-Forwarded-For` ile limiti aşabilirdi.
- **Doğrulama:** Limit 3'e çekilip `/api/auth/login`'e 8 istek atıldı; 4. istekten itibaren `429` + `Retry-After: 60` alındı.
- **Konum:** `src/ADNoirBase.API/Program.cs` — `AddRateLimiter` çağrısı yok; `BaseApiController.cs:17` yalnızca 429 mapping var
- **Risk:** `/api/auth/login` sınırsız istek alabilir — credential stuffing ve brute-force saldırısına açık. `FailedLoginCount` uygulamada var ama IP bazlı engelleme yok.
- **Çözüm:**
  ```csharp
  // Program.cs
  builder.Services.AddRateLimiter(options =>
  {
      options.AddFixedWindowLimiter("login", o =>
      {
          o.PermitLimit = 5;
          o.Window = TimeSpan.FromMinutes(1);
          o.QueueProcessingOrder = QueueProcessingOrder.OldestFirst;
          o.QueueLimit = 0;
      });
  });
  // AuthController'da: [EnableRateLimiting("login")]
  ```

---

### S6 — Refresh token reuse detection (token aile revoke) yok
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `AuthService.RefreshTokenAsync` artık token'ı durumundan bağımsız arayan `FindByTokenAsync` ile buluyor. Revoke edilmiş bir token sunulursa `RevokeAllUserTokensAsync` ile kullanıcının tüm aktif token'ları iptal ediliyor ve `identity.refresh_token.reuse_detected` dönüyor. Süresi dolmuş token için artık ayrı `identity.refresh_token.expired` kodu dönüyor.
- **Bilinen davranış:** Aynı token'la eşzamanlı iki refresh (ör. iki sekme) ikinci istekte reuse sayılır ve kullanıcı yeniden giriş yapmak zorunda kalır. Bu, saldırganla meşru istemciyi ayırt edemeyen her rotation şemasının bilinçli bedelidir.
- **Doğrulama:** Build ve mevcut testler geçti. Veritabanıyla uçtan uca akış **doğrulanamadı** (yerelde PostgreSQL yok); başka ortamda denenecek.
- **Konum:** `src/Modules/Identity/Identity.Domain/Entities/RefreshToken.cs` — `ReplacedByToken` alanı var ama kontrol edilmiyor
- **Risk:** Çalınmış refresh token iki kez sunulursa aktif oturum açılabilir. OWASP ASVS 3.3.3 ihlali.
- **Çözüm:** `AuthService.RefreshTokenAsync`'te: revoke edilmiş bir token sunulursa, `ReplacedByToken` zinciri takip edilerek tüm aile revoke edilmeli:
  ```csharp
  // Revoke edilmiş token tekrar sunulduysa → tüm kullanıcı token'larını revoke et
  if (existingToken.IsRevoked)
  {
      await _refreshTokenRepository.RevokeAllUserTokensAsync(existingToken.UserId, ct);
      return BaseResponse<RefreshTokenResponse>.Unauthorized(IdentityMessageCodes.Auth.RefreshTokenReuseDetected);
  }
  ```

---

### S9 — Security headers yok
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `SecurityHeadersMiddleware` pipeline'ın en başına eklendi, böylece hata yanıtları da başlıkları taşıyor: `X-Content-Type-Options`, `X-Frame-Options`, `Referrer-Policy`, `X-Permitted-Cross-Domain-Policies`, `Permissions-Policy` ve `Content-Security-Policy`. CSP, JSON API için `default-src 'none'`; Swagger UI inline stil/script kullandığı için yalnızca `/swagger` altında gevşetiliyor. Development dışında `UseHsts()` devrede. Ayrıca Kestrel'in `Server` başlığı kapatıldı.
- **Doğrulama:** `/health/live` yanıtında altı başlığın tamamı mevcut, `Server` başlığı yok.
- **Konum:** `src/ADNoirBase.API/Program.cs` — `UseHsts`, `X-Frame-Options`, `Content-Security-Policy`, `X-Content-Type-Options` middleware çağrısı yok
- **Risk:** Tarayıcı tabanlı istemciler için clickjacking, MIME sniffing, protocol downgrade saldırılarına açık. OWASP ASVS 14.4 gereklilikleri karşılanmıyor.
- **Çözüm:** `Program.cs`'e middleware ekle:
  ```csharp
  app.UseHsts();
  app.Use(async (context, next) =>
  {
      context.Response.Headers.Append("X-Content-Type-Options", "nosniff");
      context.Response.Headers.Append("X-Frame-Options", "DENY");
      context.Response.Headers.Append("Referrer-Policy", "strict-origin-when-cross-origin");
      await next();
  });
  ```

---

## 🟡 Orta

### S1 — Git repository yoktu
- [x] Tamamlandı — `git init` + push yapıldı (27 Ekim 2026)

---

### S7 — MongoDB IRepository implementasyonu yok
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `Shared.Infrastructure/Persistence/Mongo/MongoRepository<TEntity, TId>` eklendi. EF Core'daki interceptor ve global query filter Mongo'da olmadığı için audit alanlarını `IAuditableEntity` üzerinden kendisi dolduruyor, soft delete filtresini her sorguya ekliyor. `IMongoClient` tembel kaydediliyor: MongoDB kullanmayan kurulum bağlantı dizesi olmadan açılır. Guid için `Standard` gösterim ve `IgnoreExtraElements` convention'ı kayıtlı.
- **Ek:** Simetri için `BasePostgresRepository` Identity modülünden `Shared.Infrastructure/Persistence/Postgres/PostgresRepository<TContext, TEntity, TId>` olarak taşındı; yeni modüller artık kopyalamak zorunda değil. "Kim yaptı" değeri (`GetAuditUserId`) üç yerde tekrarlanıyordu ve Postgres tarafı `IsAuthenticated` kontrolü yapmıyordu; tek yardımcıya toplandı.
- **Doğrulama:** Build geçti. Gerçek MongoDB'ye karşı **doğrulanamadı** (yerelde MongoDB yok).
- **Konum:** `src/Shared/ADNoirBase.Shared.Domain/Interfaces/Repositories/IRepository.cs` arayüzü var; MongoDB implementasyonu hiçbir modülde yok
- **Risk:** Boilerplate'in "çift DB desteği" vaadi gerçeklenmiş değil; ileride eklenmesi halinde test edilmemiş bir yol izlenecek.
- **Çözüm:** `src/Shared/ADNoirBase.Shared.Infrastructure/` altında `MongoRepository<TEntity, TId>` oluştur, `IRepository<TEntity, TId>` implement et.

---

### S8 — AuditInterceptor yalnızca Guid entity için çalışıyor
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `IAuditableEntity` ve `IFullAuditableEntity` arayüzleri eklendi; `AuditableEntity<TId>` ve `FullAuditableEntity<TId>` bunları uyguluyor. Interceptor artık `ChangeTracker.Entries<IAuditableEntity>()` ile Id tipinden bağımsız çalışıyor. Ayrıca yalnızca `AuditableEntity` olan (FullAuditable olmayan) entity'lerde de güncellemede `CreatedAt`/`CreatedBy` korunuyor. Audit setter'ları `private` yapıldı; `MarkCreated`/`MarkUpdated` açık arayüz implementasyonu olduğu için entity API'sinde görünmüyor.
- **Doğrulama:** Build geçti. Veritabanına yazma **doğrulanamadı** (yerelde PostgreSQL yok).
- **Konum:** `src/Shared/ADNoirBase.Shared.Infrastructure/Persistence/Interceptors/AuditInterceptor.cs:37`
  ```csharp
  entry.Entity is AuditableEntity<Guid>  // <int> ve <string> için çalışmaz
  ```
- **Risk:** `IntEntity` veya `StringEntity` kullanan modüllerde `CreatedBy` boş kalır — veri bütünlüğü ihlali sessizce oluşur.
- **Çözüm:** Generic pattern match veya interface tabanlı (`IAuditableEntity`) yaklaşım:
  ```csharp
  if (entry.Entity is IAuditableEntity auditableEntity)
  {
      auditableEntity.SetAuditFields(now, userId);
  }
  ```

---

### S10 — BCrypt work factor appsettings'den yönetilemiyor
- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `Bcrypt:WorkFactor` (varsayılan 12) eklendi; `BcryptSettingsValidator` 10–16 dışındaki değerde uygulamayı başlatmıyor. API ve DbMigrator aynı `AddPasswordHashing` kaydını kullanıyor; seeder artık BCrypt'i doğrudan çağırmıyor. Maliyet artırıldığında eski hash'ler doğrulanmaya devam ediyor ve kullanıcının bir sonraki başarılı girişinde yeni maliyetle yeniden hash'leniyor (`IPasswordHasher.NeedsRehash`).
- **Doğrulama:** Build geçti. Login sırasında yeniden hash'leme **doğrulanamadı** (yerelde PostgreSQL yok).
- **Konum:** `src/Shared/ADNoirBase.Shared.Infrastructure/Authentication/BcryptPasswordHasher.cs`
  ```csharp
  BCrypt.Net.BCrypt.HashPassword(password)  // cost parametresi yok, varsayılan = 10
  ```
- **Risk:** Kütüphane default değeri değişirse fark edilmeden farklı hash maliyeti uygulanır; güvenlik konfigürasyonu kod içinde saklı.
- **Çözüm:**
  ```csharp
  // appsettings.json'a ekle:
  "Bcrypt": { "WorkFactor": 12 }
  
  // BcryptPasswordHasher'da:
  BCrypt.Net.BCrypt.HashPassword(password, _settings.WorkFactor)
  ```

---

## Ek: CORS Konfigürasyonu

- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** Varsayılan `AllowedOrigins` boş listeye çekildi. `CorsExtensions` artık Development dışında `"*"` görürse `InvalidOperationException` fırlatıyor; liste boşsa hiçbir çapraz kaynak isteği kabul edilmiyor.
- **Konum:** `src/ADNoirBase.API/appsettings.json:14` — `"AllowedOrigins": [ "*" ]`

---

## Ek: /health endpoint'i kimlik doğrulaması istemiyordu

- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** Tek `/health` ucu ikiye ayrıldı. `/health/live` hiçbir bağımlılığı yoklamayan, düz `Healthy` dönen anonim uç. Veritabanı adlarını ve hata ayrıntılarını içeren `/health/ready` ise `RequireAuthorization()` ile korunuyor.
- **Konum:** `src/ADNoirBase.API/Extensions/HealthCheckExtensions.cs`

---

## Ek: Enum'lar sözleşmede sayı olarak serileştiriliyordu

- [x] Tamamlandı — 27 Ekim 2026
- **Yapılanlar:** `ResponseStatus` ve `MessageSeverity` JSON'a sayı olarak yazılıyordu, oysa `ui/lib/api-client.ts` bunları string olarak tipliyordu. API'ye `JsonStringEnumConverter` eklendi; `ExceptionHandlingMiddleware` kendi serileştiricisini kullandığı için orada da aynı ayar yapıldı.
- **Konum:** `src/ADNoirBase.API/Program.cs`, `src/ADNoirBase.API/Middleware/ExceptionHandlingMiddleware.cs`

---

## Referanslar

- OWASP ASVS Level 2: https://owasp.org/www-project-application-security-verification-standard/
- ISO/IEC 27001:2022 Annex A
- Denetim raporu tarihi: 27 Ekim 2026
