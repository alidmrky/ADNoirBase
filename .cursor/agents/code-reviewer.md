# Code Reviewer Agent

Katman kurallarını ve mimari standartları denetler.

## Kullanım
"Kodu incele", "Review yap", "Mimari kurallara uygun mu?"

## Kontrol listesi

### Dosya organizasyonu
- [ ] Her dosyada yalnızca 1 tip (class/interface/record/enum)
- [ ] Dosya adı = tip adı
- [ ] Namespace = klasör yapısı

### Katman bağımlılıkları
- [ ] Domain → dış NuGet paketi import etmiyor mu?
- [ ] Application → Infrastructure referansı var mı? (YASAK)
- [ ] Modüller birbirine referans veriyor mu? (YASAK)

### BaseResponse kuralı
- [ ] Tüm servis metodları `Task<BaseResponse>` veya `Task<BaseResponse<T>>` dönüyor mu?
- [ ] Exception fırlatılıyor mu? (YASAK)
- [ ] `DomainException` kullanımı varsa doğru mu?

### MessageCode kuralı
- [ ] Literal string kullanılıyor mu? (YASAK)
- [ ] Yalnızca `MessageCodes.*` veya `[Mod]MessageCodes.*` sabitleri kullanılıyor mu?

### Repository kuralı
- [ ] `SoftDelete()` entity üzerinden mi çağrılıyor?
- [ ] `IsDeleted = true` doğrudan set ediliyor mu? (YASAK)
- [ ] `CancellationToken` her async metoda geçiyor mu?

### EF Core
- [ ] Global query filter var mı? (`HasQueryFilter(x => !x.IsDeleted)`)
- [ ] `AuditInterceptor` DbContext'e eklenmiş mi?
- [ ] Schema tanımlı mı?
