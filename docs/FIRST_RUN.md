# İlk Çalıştırma (First Run)

Bu doküman, projeyi **yeni bir bilgisayarda sıfırdan** ayağa kaldırmak için
izlenecek adımları ve makineye göre değiştirilmesi gereken yerleri anlatır.

Depoda hiçbir gizli değer (parola, anahtar) bulunmaz. Uygulama eksik bir
yapılandırmayla sessizce zayıf bir varsayılana düşmez; açık bir hatayla durur.
Bu yüzden ilk çalıştırmada aşağıdaki adımların tamamı gereklidir.

## Windows için hızlı başlangıç

Depo kökünde PowerShell ile:

```powershell
./scripts/Initialize-Local.ps1
```

Bu komut `.env` ve `ui/.env.local` dosyalarını rastgele gizli değerlerle
oluşturur; mevcut dosyaları değiştirmez. Admin e-postası ve parolası kökteki
`.env` dosyasında bulunur. Bu dosyaları paylaşmayın veya commit etmeyin.

.NET 9 SDK, PostgreSQL ve MongoDB hazır olduğunda, `.env` içindeki bağlantı
bilgilerini yerel veritabanınıza göre ayarlayın. Docker kullanıyorsanız önce
`docker compose up -d postgres mongodb` çalıştırın. Ardından:

```powershell
./scripts/Start-Local.ps1 Migrate
npm.cmd ci --prefix ui
./scripts/Start-Local.ps1 Api
```

İkinci bir PowerShell terminalinde:

```powershell
./scripts/Start-Local.ps1 Ui
```

API: <http://localhost:5197/swagger>, arayüz: <http://localhost:3000>.
Başlatma betiği `.env` değerlerini PowerShell oturumuna yükler; ayrıca user-secrets
tanımlamak gerekmez. `Migrate` mevcut veritabanına migration ve seed uygular.

---

## 1. Ön koşullar

| Araç | Sürüm | Not |
| --- | --- | --- |
| .NET SDK | `global.json` içindeki sürüm (9.0.100+) | `dotnet --list-sdks` ile kontrol edin |
| Node.js | 22 veya üzeri | Yalnızca `ui/` için |
| Docker Desktop | Güncel | PostgreSQL ve MongoDB için |
| Git | Güncel | — |

`global.json` dosyası `rollForward: latestFeature` kullanır: 9.0.1xx bandındaki
ve sonraki 9.0 feature band'lerindeki SDK'lar yeterlidir, 10.x çalışmaz.

Docker kullanmak istemiyorsanız PostgreSQL 16 ve MongoDB 7'yi makineye kurup
bağlantı dizelerini kendi değerlerinizle güncellemeniz gerekir.

---

## 2. Depoyu klonlayın ve `.env` dosyasını oluşturun

```bash
git clone https://github.com/alidmrky/ADNoirBase.git
cd ADNoirBase
```

```powershell
# PowerShell
Copy-Item .env.example .env
```

```bash
# bash
cp .env.example .env
```

`.env` dosyası `.gitignore` içindedir, asla commit edilmez.

### Gizli değerleri üretin

Doldurulması zorunlu olanlar: `POSTGRES_PASSWORD`, `Jwt__SecretKey`,
`ADMIN_PASSWORD`. `Jwt__SecretKey` en az 32 karakter, `ADMIN_PASSWORD` en az
12 karakter olmalıdır.

```powershell
# PowerShell — openssl gerektirmez
function New-Secret([int]$Bytes) {
    $buffer = [byte[]]::new($Bytes)
    [System.Security.Cryptography.RandomNumberGenerator]::Fill($buffer)
    [Convert]::ToBase64String($buffer)
}

New-Secret 24   # -> POSTGRES_PASSWORD
New-Secret 48   # -> Jwt__SecretKey
New-Secret 18   # -> ADMIN_PASSWORD
New-Secret 32   # -> ui/.env.local içindeki AUTH_SECRET
```

```bash
# bash / macOS / Linux
openssl rand -base64 24   # -> POSTGRES_PASSWORD
openssl rand -base64 48   # -> Jwt__SecretKey
openssl rand -base64 18   # -> ADMIN_PASSWORD
openssl rand -base64 32   # -> AUTH_SECRET
```

> `POSTGRES_PASSWORD` değerini hem `POSTGRES_PASSWORD` satırına hem de
> `ConnectionStrings__PostgreSQL` içindeki `Password=` kısmına yazmayı unutmayın.
> Parola `;` veya `=` içeriyorsa bağlantı dizesini bozar; böyle bir değer
> üretilirse yenisini alın.

---

## 3. Veritabanlarını başlatın

```bash
docker compose up -d postgres mongodb
```

Hazır olduklarını doğrulayın:

```bash
docker compose ps
```

İki servis de `healthy` görünmelidir.

---

## 4. Migration ve seed

DbMigrator, EF Core migration'larını uygular; `ADMIN` ve `USER` rollerini,
`.env` dosyasındaki bilgilerle admin kullanıcısını ve bildirim modülünün
MongoDB index'leri ile sistem şablonlarını oluşturur.

DbMigrator `.env` dosyasını **kendisi okumaz**; ortam değişkenlerini kabuğa
yüklemeniz gerekir. En kolay yol Compose üzerinden çalıştırmaktır:

```bash
docker compose run --rm migrator
```

Yerelden `dotnet run` ile çalıştırmak isterseniz önce `.env` dosyasını kabuğa
yükleyin:

```powershell
# PowerShell
Get-Content .env | Where-Object { $_ -match '^\s*[^#\s].*=' } | ForEach-Object {
    $name, $value = $_ -split '=', 2
    [Environment]::SetEnvironmentVariable($name.Trim(), $value.Trim(), 'Process')
}

dotnet run --project src/Tools/ADNoirBase.DbMigrator
```

```bash
# bash
set -a && source .env && set +a
dotnet run --project src/Tools/ADNoirBase.DbMigrator
```

İlk girişten sonra admin parolasını değiştirin.

---

## 5. API'yi çalıştırın

Geliştirme makinesinde gizli değerleri `.env` yerine **user-secrets** ile
tutmak daha güvenlidir; dosya depo dizininin dışında saklanır.

```bash
cd src/ADNoirBase.API

dotnet user-secrets set "ConnectionStrings:PostgreSQL" "Host=localhost;Port=5432;Database=adnoirbase;Username=adnoirbase;Password=<parola>"
dotnet user-secrets set "ConnectionStrings:MongoDB" "mongodb://localhost:27017"
dotnet user-secrets set "Jwt:SecretKey" "<48 baytlık anahtar>"
dotnet user-secrets set "Cors:AllowedOrigins:0" "http://localhost:3000"

cd ../..
dotnet run --project src/ADNoirBase.API
```

API `http://localhost:5197` adresinde açılır. Swagger yalnızca Development
ortamında `/swagger` altında yayınlanır.

Hızlı kontrol:

```bash
curl http://localhost:5197/health/live
```

---

## 6. Arayüzü çalıştırın

```bash
cd ui
```

```powershell
Copy-Item .env.local.example .env.local
```

`.env.local` içine `AUTH_SECRET` değerini yazın ve API adresinin doğru
olduğundan emin olun (varsayılan `http://localhost:5197`).

```bash
npm install
npm run dev
```

Arayüz `http://localhost:3000` adresinde açılır.

---

## 7. Veya: her şey Docker ile

```bash
docker compose up --build
```

Bu modda API `http://localhost:8080` adresinde çalışır. Arayüzü yerelden
çalıştırıyorsanız `ui/.env.local` içindeki portları `8080` yapın ve `.env`
dosyasındaki `Cors__AllowedOrigins__0` değerinin arayüzün adresiyle
eşleştiğinden emin olun.

---

## 8. Makineye göre değiştirilmesi gereken yerler

Aşağıdaki tablo, "bende çalışıyordu" sorunlarının büyük kısmını kapsar.

| Konu | Nerede | Ne zaman değiştirilir |
| --- | --- | --- |
| API portu | `src/ADNoirBase.API/Properties/launchSettings.json` | 5197 doluysa |
| API adresi (arayüz) | `ui/.env.local` → `NEXT_PUBLIC_API_URL`, `API_URL` | API portu değişirse veya Docker'da çalışıyorsa |
| CORS | `.env` → `Cors__AllowedOrigins__0` | Arayüz 3000 dışında bir portta çalışıyorsa. `*` yalnızca Development'ta kabul edilir |
| PostgreSQL / MongoDB | `.env` → `ConnectionStrings__*` | Veritabanı başka bir makinede veya farklı portta ise |
| Admin hesabı | `.env` → `ADMIN_EMAIL`, `ADMIN_PASSWORD` | Her kurulumda |
| Reverse proxy | `.env` → `ForwardedHeaders__Enabled`, `__KnownProxies__0` | Nginx/Traefik arkasındaysanız. `Enabled: true` iken liste boşsa uygulama başlamaz |
| Rate limit | `.env` → `RateLimiting__*` | Yük testinde veya limit testlerinde |
| BCrypt maliyeti | `.env` → `Bcrypt__WorkFactor` | Yavaş makinede 10, üretimde 12+ |
| Mail gönderimi | `.env` → `Notification__Email__*` | Gerçek SMTP'ye geçerken. Varsayılan `Log`, hiçbir yere mail göndermez |
| SMS gönderimi | `.env` → `Notification__Sms__Provider` | Gerçek sağlayıcı eklenince. Varsayılan `Log` |

### Bildirim sağlayıcıları

Geliştirme sırasında mail gönderimi varsayılan olarak **kapalıdır**.

| Sağlayıcı | Davranış | Ne zaman |
| --- | --- | --- |
| `Log` (varsayılan) | Maili yalnızca log'a ve MongoDB kaydına yazar | Günlük geliştirme |
| `Pickup` | `.mail-drop/` klasörüne `.eml` dosyası yazar; çift tıklayıp açabilirsiniz | Şablon çıktısını gözle kontrol ederken |
| `Smtp` | Gerçek SMTP sunucusuna bağlanır | Test veya üretim |

Tarayıcıdan gelen kutusu görmek isterseniz Compose'daki isteğe bağlı Mailpit
servisini başlatın:

```bash
docker compose --profile tools up -d mailpit
```

Ardından `.env` dosyasına ekleyin:

```
Notification__Email__Provider=Smtp
Notification__Email__Smtp__Host=localhost
Notification__Email__Smtp__Port=1025
Notification__Email__Smtp__Security=None
```

Gelen kutusu: <http://localhost:8025>

API de Docker'da çalışıyorsa SMTP host'u konteyner adı olmalıdır:
Compose varsayılanı `mailpit`; başka bir SMTP için `.env` içinde
`DOCKER_SMTP_HOST` tanımlayın. Yerel API ise `Notification__Email__Smtp__Host=localhost`
kullanır. Sağlayıcı değiştikten sonra API'yi yeniden başlatın.
Mailpit imajı [resmi Docker yönergelerine](https://mailpit.axllent.org/docs/install/docker/)
göre yapılandırılmıştır; portlar yalnızca localhost'a açılır.

### Bildirim ekranları ve ayarlar

- Yönetici: `/notification-admin` — şablon CRUD, sandbox önizleme, kendine test,
  manuel gönderim ve ekler, kampanyalar ve gönderim kayıtları.
- Kullanıcı: `/notifications` ve `/notification-preferences`.
- Anonim: `/unsubscribe?token=...`; işlem yalnızca düğmeye basıldığında POST edilir.
- Canlı bildirim: JWT ile `/hubs/notifications`; bağlantı kullanıcıya özel gruba katılır.

`scripts/Initialize-Local.ps1` yeni kurulumlarda `Notification__UnsubscribeSecret`
değerini de üretir. Eski `.env` dosyaları korunur; mevcut kuruluma eklemek için
en az 32 karakterlik rastgele bir değer üretip bu anahtarı ekleyin.
`Notification__AppUrl`, kullanıcının erişebildiği arayüz adresi olmalıdır.
SMTP kimlik bilgileri `Notification__Email__Smtp__UserName` ve
`Notification__Email__Smtp__Password` ile verilir. Üretim için SMTP güvenlik
ayarını sağlayıcınıza göre `StartTls` veya `SslOnConnect` olarak belirleyin.

Varsayılan ek sınırları dosya başına 10 MB ve gönderim başına 10 dosyadır:
`Notification__Attachments__MaxBytes` ve `Notification__Attachments__MaxCount`.
Gönderim/test/ek yükleme/kampanya başlatma uçları kullanıcı başına ortak
10 istek/dakika limiti kullanır (`RateLimiting__NotificationSend__*`).

### Yerel bildirim doğrulaması

API ve veritabanları açıkken, `Log` veya `Pickup` sağlayıcısıyla:

```powershell
node scripts/test-notifications.cjs
dotnet test tests/Notification.Application.Tests
```

Smoke testi benzersiz `@example.invalid` hesabı ve gönderim kayıtları oluşturur;
başarılı bitişte test hesabını pasifleştirir, test şablonu/kampanyasını siler.
Gönderim kayıtları ve ek dosyası inceleme için kalır. Aynı testi peş peşe
çalıştırırken hız sınırı penceresinin dolmasını bekleyin.

---

## 9. Kurulum doğrulama listesi

Veritabanı gerektiren davranışlar bu listeyle kontrol edilir.

1. **Sağlık:** `GET /health/live` → 200. `GET /health/ready` → token olmadan 401.
2. **Giriş:** `POST /api/auth/login` admin bilgileriyle → token döner.
3. **Audit alanları:** Profilinizi güncelleyin; veritabanında `UpdatedAt` ve
   `UpdatedBy` dolmalı, `CreatedAt` değişmemelidir.
4. **Refresh token tekrar kullanımı:** Token'ı bir kez yenileyin, sonra **eski**
   token'ı tekrar gönderin → 401 ve `identity.refresh_token.reuse_detected`.
   Yeni token da artık çalışmamalıdır (tüm oturumlar iptal edilir).
5. **BCrypt yükseltmesi:** `Bcrypt__WorkFactor=13` ile API'yi yeniden başlatıp
   giriş yapın; veritabanındaki hash `$2a$13$` ile başlamalıdır.
6. **Rate limit:** `/api/auth/login` adresine arka arkaya 6 istek → 6. istekte
   429 ve `Retry-After` başlığı.
7. **Bildirim:** Yönetim panelinden kendinize test maili gönderin; `Log`
   sağlayıcısında konsolda görünmeli, gönderim kaydı listede `Sent` olmalıdır.

---

## 10. Sık karşılaşılan hatalar

**`Jwt:SecretKey tanımlı değil`**
`.env` doldurulmamış ya da kabuğa yüklenmemiş. `dotnet run` `.env` dosyasını
okumaz; user-secrets kullanın veya değişkenleri kabuğa yükleyin (bkz. adım 4).

**`Cors:AllowedOrigins üretim ortamında '*' olamaz`**
`ASPNETCORE_ENVIRONMENT` Development değil ve origin listesi `*`. Arayüzün
gerçek adresini yazın.

**`ForwardedHeaders:Enabled true ise ... dolu olmalıdır`**
Proxy desteği açılmış ama güvenilen proxy listesi boş. Ya `Enabled: false`
yapın ya da proxy IP'sini ekleyin. Bu kontrol bilinçlidir: liste boşken her
istemci `X-Forwarded-For` ile kendi IP'sini taklit edip rate limit'i aşabilir.

**`ADMIN_PASSWORD ortam değişkeni tanımlı değil`**
Migrator `.env` dosyasını okumadı. `docker compose run --rm migrator` kullanın.

**Arayüzde "Failed to fetch" / CORS hatası**
`ui/.env.local` içindeki port ile API'nin çalıştığı port farklı, ya da
`Cors__AllowedOrigins__0` arayüzün adresiyle eşleşmiyor.

**`password authentication failed for user`**
`.env` içindeki `POSTGRES_PASSWORD` ile `ConnectionStrings__PostgreSQL`
içindeki `Password=` farklı. Ayrıca parolayı değiştirdiyseniz eski veritabanı
hacmi eski parolayı tutmaya devam eder: `docker compose down -v` ile sıfırlayın
(tüm veriler silinir).

**`The SDK 'Microsoft.NET.Sdk' specified could not be resolved`**
`global.json` ile uyumsuz bir .NET SDK sürümü. `dotnet --list-sdks` çıktısını
kontrol edin.

**MongoDB bağlantı hatası, ama MongoDB kullanmıyorum**
`IMongoClient` tembel üretilir; yalnızca bildirim modülü gibi Mongo kullanan bir
kod yolu tetiklendiğinde bağlanmaya çalışır. `/health/ready` ise her zaman
kontrol eder.
