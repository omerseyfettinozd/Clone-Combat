# 📝 Clone Combat - Yapılacaklar Listesi (To-Do List)

Bu dosya, projedeki ilerlemeyi takip edebilmek üzere düzenlenmiş, onay kutulu (checkbox) bir kontrol listesidir.

## ✅ Tamamlanmış Görevler (Completed)
- [x] Unity Netcode, Relay ve Lobby ile temel çok oyunculu altyapı kuruldu.
- [x] 128Hz Tick Rate entegrasyonu ve ağ optimizasyonları (NetworkTransform vb.) tamamlandı.
- [x] "Input-Based" Ghost (Hayalet) kayıt ve oynatma mekaniği başarıyla uygulandı.
- [x] Silah satın alınabilen oyun içi ekonomi (Ölüm Mağazası - Coin) sistemi eklendi.
- [x] Silah türleri (Pistol, Assault, Sniper) ve bunların mermi, ateş senkronizasyonları ayarlandı.
- [x] Takım renk sistemi (Host: Mavi, Client: Kırmızı) oluşturuldu.
- [x] Ölüm anında sabit kalan, doğuştan itibaren SmoothDamp ile oyuncuyu izleyen Kamera Sistemi eklendi.
- [x] Vuruş hissini artırmak için "Hit Marker" entegrasyonu yapıldı.
- [x] Oyun kazanma amacı olan "Üs (Base) Yıkma" mantığı yerleştirildi.

## 🔥 Öncelikli (Yüksek)
- [ ] Mavi ve Kırmızı takımlar için ayrı ayrı çalışır durumda player prefab'ları hazırlanacak.
- [ ] Oyun içi UI elementleri (Lobi, Mağaza, HUD) Responsive (tüm ekran çözünürlüklerine duyarlı) hale getirilecek.
- [ ] Oyun Mobile Platformlarda oynamaya hazır (UI ve kontroller) hale getirilecek.
- [ ] Savaş arenasına geçmeden önce Lobide oylanıp belirlenecek bir "Harita Seçim Ekranı" eklenecek.

## 🛠️ Orta Öncelikli
- [ ] Ana üslerin (Base) canı zamanla kendiliğinden yenilenecek bir sistem eklenecek.
- [ ] Üslerin can yenilenme hızı oyuncuların kazandığı coinler (altınlar) ile yükseltilebilecek (Hızlandırılabilecek).
- [ ] Sadece bir Battle Arena değil; hareketli platformlar, yıkılabilir duvarlar ve zıplama yastıkları (jump pad) içeren yeni harita tasarımları yapılacak.
- [ ] Oyun içi puanlar ve altınlar ile doğrudan etkileşime girilebilen, dinamik harita tasarımları geliştirilecek.
- [ ] Harita ve oyuncu sınırları (Confiner) belirlenecek, dışına çıkışlar engellenecek.

## 🏃‍♀️ Karakter Sınıfları ve Yetenekler
- [ ] Zıplama gücü, karakter hızı veya maksimum can miktarı farklılık gösteren yeni karakter sınıfları oluşturulacak.
- [ ] Sadece silahlar değil, seçilen karaktere ve oyuna has "Aktif/Pasif Yetenekler" sisteme dahil edilecek.
- [ ] Farklı karakterler ve farklı durumlar için yeni "Animasyon State'leri" (yürüme, zıplama, mermi izi, çevre tepkileri) uygulanacak.

## ⚙️ Oyun Akıcılığı ve Optimizasyon
- [ ] Network gecikmelerini oyuncuya hissettirmemek için **Client-Side Prediction & Server Reconciliation** entegre edilecek.
- [ ] Gecikmesi yüksek oyuncular ile adil bir oynanış sağlamak adına **Lag Compensation** eklenecek.
- [ ] FPS ve Ping verilerini anlık takip edeceğimiz bir **Debug HUD** (Ekran Görüntüsü) kurulacak.

## 🎮 Yeni Oyun Modları ve Uzun Vadeli Hedefler
- [ ] İnternetsiz veya düşük gecikmeli bağlantı için Offline/LAN (Local Area Network) kurulum yeteneği eklenecek.
- [ ] Aynı bilgisayar ekranını ikiye bölerek oynamayı sağlayan "Splitscreen (Yerel Çok Oyunculu)" modu eklenecek.
- [ ] Beceri seviyesine göre oyuncuların karşılaşabileceği "Dereceli Eşleşme (Ranked)" sistemi geliştirilecek.
- [ ] Oyuncuların biten maçları tekrar izleyebilecekleri "Maç Tekrarı (Replay)" özellikleri tasarlanacak.
