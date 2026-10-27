# Güvenlik Sorunları — Kontrol Listesi

> Denetim tarihi: 27 Ekim 2026  
> Kaynak: Kanıta dayalı mimari denetim raporu  
> Tamamlananları `[x]` ile işaretle, commit mesajında `fix(security): S<numara>` formatını kullan.

---

## 🔴 Kritik — Hemen

### S2 — appsettings.json içinde cleartext kimlik bilgisi
- [ ] Tamamlandı
- **Konum:** `src/ADNoirBase.API/appsettings.json:3` (`Password=postgres`) ve `:7` (`CHANGE_THIS_SECRET_KEY_MIN_32_CHARS_LONG!!`)
- **Risk:** Kaynak kod paylaşımında (GitHub, CI log, ekip üyesi) DB parolası ve JWT imzalama anahtarı sızdırılır. Anahtar ele geçirilirse tüm token'lar sahte üretilebilir.
- **Çözüm:**
  1. `appsettings.json`'daki gerçek değerleri boş string veya placeholder yap (`""` veya `"CHANGE_ME"`)
  2. `.env.example` oluştur, gerçek değerleri `appsettings.Development.json` veya user-secrets'a taşı
  3. `dotnet user-secrets set "ConnectionStrings:PostgreSQL" "..."` (geliştirme)
  4. Production'da environment variable kullan

---

### S5 — AdminUserSeeder hardcoded fallback parolası
- [ ] Tamamlandı
- **Konum:** `src/Tools/ADNoirBase.DbMigrator/Seeders/AdminUserSeeder.cs:11`
  ```csharp
  var adminPassword = Environment.GetEnvironmentVariable("ADMIN_PASSWORD") ?? "Admin@123456";
  ```
- **Risk:** `ADMIN_PASSWORD` env var set edilmezse üretim ortamına bilinen parola (`Admin@123456`) ile admin hesabı açılır. Bu durumun fark edilmesi zordur.
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
- [ ] Tamamlandı
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
- [ ] Tamamlandı
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
- [ ] Tamamlandı
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
- [ ] Tamamlandı
- **Konum:** `src/Shared/ADNoirBase.Shared.Domain/Interfaces/Repositories/IRepository.cs` arayüzü var; MongoDB implementasyonu hiçbir modülde yok
- **Risk:** Boilerplate'in "çift DB desteği" vaadi gerçeklenmiş değil; ileride eklenmesi halinde test edilmemiş bir yol izlenecek.
- **Çözüm:** `src/Shared/ADNoirBase.Shared.Infrastructure/` altında `MongoRepository<TEntity, TId>` oluştur, `IRepository<TEntity, TId>` implement et.

---

### S8 — AuditInterceptor yalnızca Guid entity için çalışıyor
- [ ] Tamamlandı
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
- [ ] Tamamlandı
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

- [ ] Tamamlandı
- **Konum:** `src/ADNoirBase.API/appsettings.json:14` — `"AllowedOrigins": [ "*" ]`
- **Risk:** Her origin'den istek kabul edilir — CSRF koruması zayıflar.
- **Çözüm:** `appsettings.json`'da production URL'leri belirt; `"*"` yalnızca `appsettings.Development.json`'da kalsın.

---

## Referanslar

- OWASP ASVS Level 2: https://owasp.org/www-project-application-security-verification-standard/
- ISO/IEC 27001:2022 Annex A
- Denetim raporu tarihi: 27 Ekim 2026
