# Rol atama hatası ve rol yönetimi

## Hata tanımı

Geçerli bir kullanıcıya mevcut bir rol atanırken
`POST /api/admin/users/{userId}/roles/{roleId}` HTTP 500 dönebiliyor.
Önceki yerel kontrolde bu hata gerçek PostgreSQL üzerinde gözlendi.
Kullanıcı detay ekranı rolleri yalnızca gösteriyor; rol atama/kaldırma kontrolleri
ve yönetici için rol kataloğu ekranı bulunmuyor.

## Kod incelemesindeki neden

`AssignRoleAsync`, takip edilen kullanıcının `UserRoles` koleksiyonuna yeni bir
`UserRole` ekliyor. Yeni ilişkinin GUID anahtarı `UserRole.Create` içinde üretiliyor.
Ardından genel `PostgresRepository.UpdateAsync`, tüm entity grafiği üzerinde
`DbSet.Update(user)` çağırıyor. Anahtarı önceden verilmiş yeni ilişki EF tarafından
`Modified` kabul edilebiliyor; INSERT yerine var olmayan satıra UPDATE denenmesi
concurrency hatasına ve HTTP 500'e yol açıyor.

Bu neden, EF change tracker regresyon testi ve gerçek PostgreSQL üzerinden
aynı endpoint tekrar çağrılarak doğrulandı. Yeni repository ilişkiyi açıkça
`Added` olarak kaydediyor; kaldırma işlemi doğrudan ilişki satırını siliyor.

## Çözüm adımları

1. Yeni ilişkiyi `Added`, kaldırılan ilişkiyi `Deleted` olarak açıkça yöneten
   modüle özel repository işlemleri ekle. Genel repository davranışını değiştirme.
2. Aynı rolün tekrar/eşzamanlı atanmasını mevcut unique index ile güvenceye al;
   kullanıcıya anlamlı 409 yanıtı ver. Olmayan kullanıcı/rol için 404 döndür.
3. Rol kataloğu için ADMIN yetkili liste, detay, oluşturma, güncelleme ve silme
   servisleri/API uçları ekle. Kodlar normalize edilir ve sonradan değiştirilemez.
   Sistem rolleri silinemez/düzenlenemez; atanmış roller silinemez.
4. Yöneticinin kendi ADMIN rolünü kaldırarak erişimini kaybetmesini engelle.
5. Admin navigasyonuna `/roles` sayfası ekle. Rol oluşturma/düzenleme/silme,
   arama, sayfalama ve hata/boş/yükleniyor durumlarını göster.
6. Kullanıcı listesinden detay sayfasına bağlantı ekle; detayda rol seçimi,
   atama ve kaldırma sağla. Başarılı işlemden sonra güncel rolleri yeniden oku.
7. Backend regresyon ve servis testlerini; gerçek API üzerinde atama, tekrar
   atama, kaldırma, yetki kontrolü ve rol CRUD testlerini çalıştır. UI için
   TypeScript, lint, build ve tarayıcı doğrulaması yap.

## Kabul ölçütleri

- Yeni rol ilişkisi INSERT edilir; atama/kaldırma HTTP 500 üretmez.
- Tekrar atama 409; olmayan ilişkiyi kaldırma 409; olmayan kullanıcı/rol 404.
- ADMIN olmayan kullanıcı rol yönetimine erişemez.
- Sistem/atanmış rol korumaları backend'de uygulanır, UI'da açıklanır.
- Rol sayfası ve kullanıcı detayındaki rol kontrolleri API ile çalışır.
- Rol değişikliği mevcut JWT'nin içeriğini değiştirmez: yeni rol bilgisi yeni
  giriş veya token yenilemesinde alınır. Otomatik UI token yenileme bu işin dışında.

## Durum

Backend ve UI geliştirmeleri tamamlandı. Veritabanı migration değişikliği yok.

- Admin menüsünde `/roles`: arama, sayfalama, oluşturma, düzenleme ve silme.
- Kullanıcı detayında rol atama/kaldırma; kendi ADMIN rolünü kaldırma koruması.
- `dotnet test ADNoirBase.sln --no-restore`: 21 test başarılı (10 yeni rol testi dahil).
- UI `npm run lint` ve `npm run build` (TypeScript dahil) başarılı.
- `node scripts/test-roles.cjs`: gerçek yerel PostgreSQL üzerinde 6 kontrol grubu
  başarılı; eşzamanlı atama tek kayıt ve 200/409 üretiyor.
- Tarayıcı üzerinden son etkileşim kontrolü henüz tamamlanmadı.

API testi için yerel API'nin 5197 portunda çalışması ve `.env` içinde yönetici
bilgilerinin bulunması gerekir. Test kendi rollerini temizler, test hesabını
devre dışı bırakır. `--keep-ui-user` seçeneği tarayıcı kontrolü için geçici hesabı
saklar; ardından `--cleanup-ui-user` ile erişimi kaldırılmalıdır.
