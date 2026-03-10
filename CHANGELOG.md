# 📋 Changelog — Clone Combat

Tüm önemli değişiklikler bu dosyada belgelenir.

---

## [v0.1.0] — 2026-03-10

### 🆕 Eklenen
- **Takım Renkleri**: Host = Mavi, Client = Kırmızı. Oyuncular ve ghost'lar takım renklerinde görünür
- **Kamera Takip Sistemi**: SmoothDamp tabanlı fizik-uyumlu kamera takibi + hareket yönüne lookahead
- **Ağ Optimizasyonu**: Tick rate 30Hz → 60Hz, fizik rate eşitlendi
- **Silah Senkronizasyonu**: Client silah satın aldığında ghost'a doğru silah aktarılıyor

### 🐛 Düzeltilen
- **Ghost Zıplama**: Ghost'lar artık oyuncu gibi zıplayabiliyor
- **Ghost Ateş**: Update/FixedUpdate zamanlama farkından atış kaydı atlanıyordu → ConsumeShootFlag ile çözüldü
- **Ghost Mermi Görünürlüğü**: Ghost mermileri orijinal oyuncunun ekranında yanlışlıkla gizleniyordu
- **Client Silah Sorunu**: Client farklı silah alınca ghost'a yansımıyordu → SetWeaponClientRpc eklendi
- **Duplicate Ghost**: Her ghost listeye 2 kez ekleniyordu → kaldırıldı
- **Ölüm Mağazası**: Başarısız satın almada shop kapanıyordu → sunucu onayında kapanıyor
- **Nişan Açısı**: GetAimAngle() 0-360 döndürüyordu, ghost -180/180 bekliyordu → Mathf.DeltaAngle ile düzeltildi
- **BattleHUD Performans**: Her frame CoinManager event yeniden bağlanıyordu → tek seferlik bağlama
- **Hareket Jitter**: Non-owner Rigidbody Dynamic kalıyordu → Kinematic yapıldı

### ⚡ İyileştirilen
- **IgnoreCollisions Performansı**: Her fizik frame yerine ~1 saniyede bir çalışıyor
- **GhostPlayback**: _groundCheck null olduğunda LogWarning veriyor
- **GhostRecorder**: Silah eşleştirme 3 aşamalı (referans → isim → fallback)
- **Yerçekimi**: Jump force 12 → 7, daha gerçekçi zıplama

### 📁 Yeni Dosyalar
- `Assets/Scripts/Camera/CameraFollow.cs`
- `Assets/Scripts/Networking/NetworkOptimizer.cs`
- `ROADMAP.md`
- `CHANGELOG.md`
