# Clone Combat - Agent (AI) Yönergeleri

Bu dosya, kodlama asistanları (AI) ve projeye yeni katılan geliştiriciler için **Clone Combat** projesinin mimarisini, dizin yapısını ve kodlama standartlarını özetler. Kod yazarken ve sistemleri düzenlerken buradaki kurallara uyulması zorunludur.

## 🎮 Proje Özeti
**Clone Combat**, Unity 6 ve Netcode for GameObjects (NGO) kullanılarak geliştirilmiş, 2D online multiplayer (Relay + Lobby) bir platformer-shooter oyunudur. 

**Temel Mekanik (Ghost Sistemi):** Her oyuncu öldüğünde, haritada belirlediği spawn noktasında yeniden canlanır ve öldüğü yerin arkasında bir **ghost (hayalet)** bırakır. Bu ghost, oyuncunun önceki hayatındaki tüm hareketlerini ve ateş etme eylemlerini birebir tekrar eder.

**Genel Bilgiler:**
- **Takımlar:** Host oyuncu her zaman **Takım 1 (Mavi)**, Client oyuncu **Takım 2 (Kırmızı)**.
- **Ekonomi & Silahlar:** Düşman öldürerek kazanılan altın (coin), öldükten sonra menüde (Pistol, Assault, Sniper) yeni silah almak için kullanılır.
- **Amaç:** Rakibin üssünü yok etmek.

---

## 📁 Dizin Yapısı ve Dosya Konumları (Assets/Scripts)

Projenin temel kod mimarisi `Assets/Scripts` altındaki klasörlerde modüler bir şekilde ayrılmıştır:

- `Networking/`: Ağ bağlantısı, Relay & Lobby yönetimi ve oyun durumu senkronizasyonunu (`GameManager`, `ClientNetworkTransform`) içerir.
- `Player/`: Oyuncunun fiziksel hareketi, zıplama kontrolleri ve yerel girdilerin (Input) alındığı kontrolcüler (örn: `PlayerController`).
- `Ghost/`: Hayaletlerin kaydedilmesi (Record) ve oynatılması (Playback) ile ilgili veri (`GhostFrameData.cs`) ve kontrol scriptleri yer alır.
- `Combat/`: Silah çalışma mantıkları (`WeaponController.cs`), kurşun fiziği, hasar verme işlemleri (sağlık sistemleri) ve vuruş hissi (Hit marker vb.).
- `Economy/`: Altın kazanma, oyuncu cüzdanı ve silahlara harcama mantığını içerir.
- **`UI/`**: Sağlık barı, oyun içi mağaza (Shop), lobi kurma/katılma ve ana menü arayüzlerini kontrol eden scriptler.
- **`Camera/`**: Kameranın oyuncuyu takibi ve oyuncu öldüğünde kameranın sabit kalması (hareketsiz beklemesi) mantığını içerir.

---

## 🛠️ Kodlama Standartları ve İlkeler

### 1. Netcode for GameObjects (NGO) Standartları
- **Yetkilendirme:** Network güvenliği (Health yönetimi, puan/coin ekleme) `Server / Host` yetkisinde (`ServerRpc` vb.) olmalıdır. 
- **Değişken Senkronizasyonu:** Ağ üzerinden değişmesi ve takip edilmesi gereken değerler (Can durumu, aktif silah, takım vb.) `NetworkVariable<T>` mantığıyla tanımlanmalı ve olay (event) tetikleyicisi (`OnValueChanged`) kullanılarak arayüzler güncellenmelidir.
- **İsimlendirme:** Client'tan giden rpc'ler `[ServerRpc]` etiketi ve `...ServerRpc` sonekiyle (örn: `FireServerRpc`), Server'dan tüm client'lara dağıtılan metodlar `[ClientRpc]` etiketi ve `...ClientRpc` sonekiyle bitmelidir.
- **Prefablar:** Ağ üzerinde oluşturulan tüm dinamik objeler `NetworkObject` bileşeni taşımalı ve Spawner aracılığıyla oluşturulmalıdır.

### 2. Ghost Sistemi (Kritik Kurallar)
- **Girdi (Input) Tabanlı Sistem:** Hayaletler, uzaydaki mutlak pozisyonları (x,y transform) kopyalayarak ("Teleport" şeklinde) **HAREKET ETMEZLER.** 
- Ghost sistemi `GhostFrameData` üzerinden oyuncunun o anki girdilerini (sağa/sola basma, zıplama, fare ile nişan alma açısı ve ateş tuşu) kaydeder.
- Hayalet oynatıldığında (Playback), bu kaydedilmiş "sanal girdiler" (virtual inputs) bir rigibody'ye / kontrolcüye verilerek hayaletin *fizik motoru içinde* doğal olarak hareket etmesi sağlanır. 
- AI, ghost sistemi üzerinde herhangi bir değişiklik yaparken bu "Input-Based" doğayı baz almalıdır. Asla ghost pozisyonlarını transform.position üzerinden override etmeyin. Çevresel çarpmalar ve fizik motoru baz alınmalıdır.

### 3. C# ve Unity Pratikleri
- **Kapsülleme & Modülerlik:** Diğer scriptlerin dışarıdaki verilere izinsiz erişimini engelleyin. Özellikleri (Property) veya Inspector arayüzünde görünmesini istediğiniz özel değişkenleri (`[SerializeField] private`) ile tanımlayın. Her Script sadece kendi işinden (Single Responsibility) sorumlu olsun.
- **Performans Optimizasyonu:** `Update()` fonksiyonları içinde karmaşık string operasyonları veya `GetComponent<T>()` ya da `FindObjectOfType()` gibi maliyetli aramalar yapmayın.
- **Null Reference ve Hata Ayıklama:** Obje referanslarını Inspector üzerinden atamaya özen gösterin, bulunamaması durumunda uygun ve anlaşılır `Debug.LogWarning` atın.

Bu kurallar oyundaki temiz mimariyi korumak, geliştirme hızını artırmak ve multiplayer bug'larını engellemek amacıyla tasarlanmıştır. Yaptığınız değişikliklerin bu yönergelere uygun olduğundan mutlaka emin olun.
