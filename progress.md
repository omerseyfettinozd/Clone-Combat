# 📈 Clone Combat Progress & Roadmap

Bu dosya **Clone Combat** projesindeki güncel ilerlememizi ve gelecekteki hedeflerimizi özetler.

## ✅ Şu Ana Kadar Yaptıklarımız (Tamamlananlar)

### 🟢 Temel Sistemler ve Çok Oyunculu Altyapı
- **Ağ Altyapısı**: Unity Netcode for GameObjects (NGO) kullanılarak Dedicated Server / Relay tabanlı yapı kuruldu. Tick rate **128Hz** (rekabetçi oyunlardaki gibi) seviyesine çıkartıldı.
- **Relay & Lobby**: Oyuncuların port açmadan birbirleriyle oynayabilmesi için Unity Relay ve Lobby sistemleri entegre edildi. Lobi oluşturma ve katılma ekranı yapıldı.
- **Takım Sistemi**: Host oyuncu (Mavi Takım) ve Client oyuncunun (Kırmızı Takım) renkleriyle birbirinden ayrılması sağlandı.

### 👻 Ghost (Hayalet) Mekaniği (Çekirdek Oyun Döngüsü)
- **Input-Based (Girdi Tabanlı) Kayıt Sistemi**: Hayaletlerin sadece x,y koordinatlarına ışınlanması yerine; oyuncunun geçmiş hayattaki girdilerini (hareket, zıplama, nişan alma, ateş etme) simüle ederek fizik motoruyla uyumlu (yerçekimi ve engelleri hesaba katarak) şekilde çalışması sağlandı.
- **Bağımsız Ghost Yönetimi**: Ölen oyuncu nerede isterse orada doğarken, arkasında aynı işlemleri yapan hayaletler ordusu kopyalanıyor ve bu hayaletler diğer oyuncularla gerçekçi fizik etkileşimine giriyor.
- **Silah ve Animasyon Senkronizasyonu**: Hangi hayaletin hangi silaha sahip olduğu ve nereye nişan aldığı tüm istemcilere (client) sorunsuzca senkronize edildi.

### ⚔️ Savaş, Ekonomi ve Karakter Kontrolü
- **Silahlar**: Pistol (ücretsiz), Assault ve Sniper olmak üzere 3 farklı silah sisteme entegre edildi.
- **Ekonomi Sistemi**: Öldürme başına altın (coin) kazanma ve ölüm sonrası mağazasından (Shop) bu altınlarla yeni silah alarak güçlenme döngüsü tamamlandı.
- **Görsel Geri Bildirim**: Mermi isabetinde (Hit Marker) beyaz parlama efekti ve ölen oyuncunun tüm ekranlardan gizlenmesi sağlandı.
- **Kamera Geliştirmeleri**: Ölüm anında kameranın durması/sabit kalması, oyuncu canlanınca tekrar SmoothDamp metodolojisiyle yumuşak bir şekilde takibe başlaması eklendi.
- **Üs Yıkma Amacı**: Rakibin üssünü yok ederek oyunu kazanma mantığı eklendi.

---

## 🚀 Gelecekte Yapılacaklar (Roadmap)

### 🎯 Oyun Akıcılığı ve Ağ İyileştirmeleri (v0.2.x - v0.3.x)
- **Client-Side Prediction & Server Reconciliation**: Client tarafında buton basıldığında lag hissetmemek için anında hareket edip sonrasında sunucuyla doğrulanması.
- **Lag Compensation**: Gecikmesi yüksek oyunculara da adil bir vuruş hissi sağlayan geriye dönük hesaplama (hit registration) sistemleri.
- **FPS & Ping Ekranı**: Oyun içinde gecikmeyi ve FPS'i takip edebileceğimiz Debug HUD eklentisi.

### 🌍 Harita Tasarımı ve Dinamik Yapılar (v0.3.x - v0.4.x)
- **Harita Tasarlama ve Etkileşim**: Oyun içi kazanılan altınlar (coinler) ile etkileşime girilebilen harita yapıları ve dinamik tasarımlar.
- **Harita Seçim Ekranı**: Savaş arenası (Battle Arena) sahnesine geçmeden önce lobi içinde oylama yapılabilen harita seçme ekranı.
- **Üs (Base) Geliştirmeleri**: Zamanla üslerin kendi canını yenilemesi ve bu can yenilenme hızının oyuncunun harcayacağı coinlerle artırılabilmesi.
- **Üs Savunma Sistemleri (Turret)**: Kazanılan altınlarla üssü koruması için otomatik nişan alıp ateş eden savunma kuleleri (turret) inşa etme özelliği.
- **Yeni Haritalar ve Dinamik Platformlar**: Sadece bir Battle Arena yerine hareketli asansörler, yıkılabilir duvarlar ve zıplama yastıkları (jump pad) içeren genişletilmiş bölümler.
- **Tuzaklı Bölgeler**: Haritanın çeşitli yerlerinde aktifleşebilen ölümcül dikenler, lav havuzları veya düşen tavanlar gibi çevresel etkileşimli tuzaklar.
- **Ghost Geçirmez Kalkanlar**: Sadece gerçek oyuncuların içerisinden geçebildiği, hayaletlerin çarpıp kaldığı taktiksel enerji duvarları.
- **Harita Sınırları (Confiner)**: Kameranın ve oyuncunun belirli bir sınırın dışına çıkmasını önleme.

### 👻 Ghost Etkileşimleri ve Savaş Mekanikleri (v0.4.x)
- **Satın Alınabilir Hayaletler**: Oyuncuların mağazadan kendi ordularına katmak üzere fazladan yapay zeka (hayalet) satın alabilmesi.
- **Rakip Hayaletlerini Yok Etme**: Düşman hayaletlerine hasar verip onları haritadan silmeye veya etkisiz hale getirmeye olanak sağlayan yeni savaş mekanikleri.

### 🧑‍🚀 Karakterler ve Kozmetikler (v0.5.x)
- **Takımlara Özel Karakterler**: Her iki takım (Mavi ve Kırmızı) için görsel ve yapısal olarak ayrı ayrı player prefab'larının hazırlanması.
- **Farklı Karakter Sınıfları**: Zıplama gücü, hızı veya canı farklılık gösteren seçilebilir yeni karakter türleri.
- **Karakter Yetenekleri**: Sadece silah değil, karaktere özgü pasif ya da aktif yeteneklerin eklenmesi.
- **Animasyon ve Görsel Revizyon**: Yürüme, sekmeler ve vurulma anları için özel animasyon state'lerinin entegrasyonu; mermi izi ve çevre etkileşimi (VFX) iyileştirmeleri.

### 🎮 Yeni Modlar: Kralı Koru, Splitscreen ve LAN (v0.6.x - v0.7.x)
- **Farklı Oyun Modları (Kralı Koru)**: Klasik üs yıkmanın yanı sıra, haritadaki VIP'yi veya objeyi kendi üssüne ilk götürenin kazandığı "Kralı Koru" (VIP Modu) gibi alternatif eğlenceli oyun modları.
- **Yerel Çok Oyunculu (Splitscreen)**: Aynı ekranda, aynı bilgisayar üzerinde birden fazla kişiyle oynayabilme.
- **Offline & LAN**: İnternet bağlantısı olmadan LAN (Yerel Ağ) üzerinden arkadaşlar arası sıfır gecikmeli sunucu kurma ve katılma özellikleri.

### 📱 UI Geliştirmeleri ve Mobil Uyum (v0.6.x)
- **Responsive UI**: Oyundaki arayüz tasarımlarının (Lobi, Mağaza, Oyun İçi HUD) farklı ekran çözünürlüklerine tam uyumlu ve esnek (responsive) hale getirilmesi.
- **Mobil Ekran Desteği**: Oyunun mobil platformlarda da sorunsuz görüntülenebilecek seviyeye ulaştırılması.

### 🏆 Uzun Vadeli Büyük Hedefler
- **Dereceli Eşleşme (Ranked)**: Beceri seviyesine (MMR) göre rakip bulma sistemi.
- **Maç Tekrarı (Replay)**: Geçmiş maçları izleme özellikleri.
- **Mobil ve Konsol Desteği**: Dokunmatik kontrollerin ya da native Gamepad desteğinin oyuna tam entegre edilmesi.
