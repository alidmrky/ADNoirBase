# Bildirim Modülü — Durum ve Kalan İşler

Bu dosya bildirim katmanının nerede kaldığını takip eder. Bir aşama bitince ilgili
kutuyu işaretle ve "Kalan işler" bölümünden sil.

> **Doğrulama (28 Ekim 2026):** Yerel PostgreSQL ve MongoDB üzerinde API ve
> arayüz çalıştırıldı. Backend/arayüz derlemeleri, lint ve TypeScript kontrolü
> başarılı. Bildirim modülünün 6 birim testi ve 9 uçtan uca kontrol grubu geçti.
> Ayrıca gönderim hız sınırında 11. isteğin 429 ve Retry-After döndürdüğü doğrulandı.
> Docker/Mailpit ve gerçek SMTP/SMS sağlayıcıları bu makinede çalıştırılmadı.

---

## Tamamlananlar

### 1. First-run dokümanı
- `docs/FIRST_RUN.md` — başka bilgisayarda kurulum, makineye göre değişmesi gereken
  ayarlar tablosu, doğrulama kontrol listesi, sık karşılaşılan hatalar.
- `ui/.env.local.example` portu 5197'ye çekildi, API'ye `UserSecretsId` eklendi.

### 2. Paylaşılan sözleşmeler
- `IIntegrationEvent`, `IIntegrationEventPublisher`, `IIntegrationEventHandler<T>` ve
  süreç içi publisher (handler hataları yutulur, loglanır).
- `IUserDirectory` — Identity modülü implement eder, bildirim modülü alıcıları bunun
  üzerinden çözer. Modüller birbirine doğrudan referans vermez.
- `User.PhoneNumber` + migration (`20261027160026_AddUserPhoneNumber`) + DTO'lar + UI.

### 3. Domain ve kalıcılık
- `Notification.Domain` / `.Application` / `.Infrastructure` projeleri.
- 8 enum, 4 value object, 5 entity (`NotificationTemplate`, `NotificationMessage`,
  `NotificationCampaign`, `InAppNotification`, `NotificationPreference`).
- 5 Mongo repository, index'ler (TTL dahil), `NotificationClassMaps`
  (`Priority` Int32 olarak saklanır — string olsaydı öncelik sıralaması alfabetik olurdu).
- DbMigrator: Mongo index oluşturma + sistem şablonu seed'i.

### 4. Şablonlar
- `ScribanTemplateRenderer` — sandbox (döngü/özyineleme sınırı, `include` kapalı,
  çıktı boyutu sınırı), model değerleri HTML'e kaçışlanır.
- `NotificationComposer` — layout sarmalama, kanal bazlı zorunlu alan kontrolü.
- `INotificationTemplateService` — CRUD, önizleme (kaydedilmemiş içerik üzerinde),
  test gönderimi (yalnızca isteği yapanın kendi adresine).
- FluentValidation validator'ları, `NotificationMessageCodes`.
- 5 sistem şablonu: `layout.default`, `identity.welcome`, `identity.password_changed`,
  `sms.verification_code`, `marketing.announcement`.

### 5. Gönderim
- `NotificationDispatcher` — şablon çözme, tercih filtresi, render, kuyruğa yazma.
  Bir istek = bir e-posta zarfı (To/Cc/Bcc aynı mesajda).
- `NotificationDeliveryService` — kanal yönlendirme, GridFS'ten ek yükleme,
  üstel geri çekilme, kalıcı/geçici hata ayrımı, `LogPolicy` uygulaması.
- `NotificationDeliveryWorker` — atomik claim, throttle, kilit süresi.
- `IEmailSender`: `Log`, `Pickup` (.eml dosyası), `Smtp` (MailKit).
- `ISmsSender`: `Log`.
- `GridFsAttachmentStore`, `UnsubscribeLinkFactory` (HMAC token, SHA-256 özet saklanır).

### 6. Kampanya ve tercihler
- `CampaignExpansionService` — hedef kitle çözme, `Individual` ve `SingleBcc` modları,
  parça parça ilerleme, üretim sırasında iptal.
- `CampaignWorker` — zamanı gelen kampanyayı sahiplenir, biten kampanyayı tamamlar.
- `INotificationCampaignService` — liste, detay, oluştur, hedef kitle önizleme,
  başlat, iptal, sil.
- `INotificationPreferenceService` — oku, güncelle, abonelikten çık.

---

## Tamamlanan ek geliştirmeler (A–F)

- [x] **A — Ek yükleme:** `INotificationAttachmentService`; akışın gerçek boyutu,
  MIME beyaz listesi, dosya adı temizleme, gönderim başına `MaxCount` ve
  GridFS'teki gerçek metadata ile referans doğrulaması.
- [x] **B — Uygulama içi:** kullanıcıya özel liste/sayaç/okundu işlemleri,
  `/hubs/notifications`, yalnızca hub yolunda JWT query token desteği,
  `user:{id}` grupları ve gerçek SignalR publisher.
- [x] **C — Sistem olayları:** başarılı kullanıcı kaydı ve parola değişikliğinden
  sonra integration event; Notification handler'larıyla ilgili mailin kuyruğa alınması.
- [x] **D — API:** admin şablon/kampanya/gönderim kaydı/ek/manuel gönderim uçları,
  kullanıcı tercihleri ve bildirimleri, anonim unsubscribe POST, ADMIN rol kontrolü
  ve ayrı gönderim hız sınırı.
- [x] **E — UI:** `/notification-admin` içinde şablon editörü ve sandbox iframe
  önizleme, kendine test, manuel gönderim/Cc/Bcc/ekler, kampanya oluşturma ve takip,
  filtreli gönderim kayıtları. Zil + SignalR, `/notifications`,
  `/notification-preferences`, public `/unsubscribe` ve navigasyon eklendi.
  Altı eksik shadcn bileşeni de eklendi.
- [x] **F — Yapılandırma/dokümantasyon:** isteğe bağlı Mailpit tools profili,
  `Notification__*` ortam şablonları, FIRST_RUN ve README bölümleri,
  otomatik yerel smoke testi ve ek yükleme birim testleri.

## Doğrulama kapsamı

`node scripts/test-notifications.cjs` aşağıdaki akışları gerçek yerel
PostgreSQL/MongoDB ve `Log` sağlayıcılarıyla doğrular:

1. Anonim erişim ve ADMIN olmayan kullanıcının yönetim uçlarına erişimi engellenir.
2. Kayıt olayı hoş geldin maili üretir ve worker gönderimi tamamlar.
3. Parola değişikliği olayı bildirim üretir ve worker gönderimi tamamlar.
4. Yasak dosya türü, boyut, adet ve geçersiz GridFS referansları reddedilir.
5. Şablon oluşturma ve önizlemede HTML kaçışlama çalışır.
6. SignalR bildirimi kullanıcıya ulaşır; başka kullanıcının okundu işlemi
   reddedilir; gerçek sahibin okunmamış sayacı güncellenir.
7. Abonelikten çıkma tekrarlanabilir; pazarlama gönderimi tercihe göre atlanır.
8. Kampanya genişletilir, gönderilir ve Completed durumuna gelir.
9. Test şablonu/kampanyası silinir; test hesabı pasifleştirilir.

Tarayıcıda yönetici girişi, şablon listesi/editörü, sandbox önizleme,
manuel gönderim ve kampanya ekranları kontrol edildi; zilin canlı bağlantısı görüldü.

## Kalan işler

Planlanan A–F geliştirme maddelerinde açık kod işi yok. Ortama bağlı doğrulamalar:

- [ ] Docker olan bir makinede `--profile tools` ile Mailpit ve Compose akışını dene.
- [ ] Gerçek SMTP/SMS sağlayıcısı kullanılacaksa sağlayıcıya özel teslimat testi yap.
  Mevcut SMS implementasyonu yalnızca `Log`; gerçek sağlayıcı bu planın dışında.

---
## Bilinen sınırlamalar

- Arayüzün mevcut Auth.js akışında otomatik access-token yenileme yoktur;
  token süresi dolunca API 401 döner ve yeniden giriş gerekir. SignalR bağlantısı
  da token süresi dolunca kapanır; bu mevcut kimlik doğrulama altyapısının sınırıdır.
- Kuyruk, kampanya ilerleme sayaçları ve olay publisher'ı tam bir transactional
  outbox sağlamaz; süreç çökmesi/yeniden deneme için exactly-once garantisi yoktur.
- Ek doğrulaması MIME beyaz listesi ve boyut/adet sınırlarına dayanır;
  dosya içerik analizi veya antivirüs taraması içermez.

- **`Notification:UnsubscribeSecret` boşken** pazarlama maillerine abonelikten çıkma
  linki ve `List-Unsubscribe` başlığı eklenmez; yalnızca uyarı loglanır. Pazarlama
  gönderimi yapılacak her ortamda bu değer tanımlanmalıdır.
- **`SingleBcc` modunda** kişiselleştirme ve abonelikten çıkma linki çalışmaz;
  tek mail, alıcılar BCC'de.
- **Kişiselleştirme** yalnızca tek alıcılı gönderimde uygulanır. Çok alıcılı tek bir
  zarfta `{{ full_name }}` boş kalır.
- **SMS sağlayıcısı** yalnızca `Log`. Gerçek sağlayıcı eklemek için
  `Notification.Infrastructure/DependencyInjection.AddSmsSender` içindeki switch'e
  bir dal eklemek yeterli.
