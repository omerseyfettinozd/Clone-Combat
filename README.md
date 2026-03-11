# ⚔️ Clone Combat

2D multiplayer platformer shooter built with Unity and Netcode for GameObjects.

Her ölümünde geride bir **ghost (hayalet)** bırakırsın — ghost'un senin hareketlerini ve ateşini tekrar eder. Öldükçe ordu büyür!

## 🎮 Özellikler

- **Gerçek Zamanlı Çok Oyunculu** — Unity Relay + Lobby ile online eşleşme
- **Ghost Sistemi** — Her ölümde kayıtlı hareketleri tekrar eden hayaletler oluşur
- **Takım Renkleri** — Host = 🔵 Mavi, Client = 🔴 Kırmızı
- **3 Silah Türü** — Pistol (ücretsiz), Assault, Sniper
- **Ekonomi Sistemi** — Öldürme ile coin kazan, ölünce yeni silah satın al
- **Üs Savaşı** — Rakibin üssünü yık!
- **Hit Marker** — İsabet anında görsel geri bildirim
- **128Hz Tick Rate** — Düşük gecikmeli ağ güncellemeleri

## 🛠️ Teknolojiler

| Teknoloji | Kullanım |
|-----------|----------|
| Unity 6 | Oyun motoru |
| Netcode for GameObjects | Multiplayer altyapı |
| Unity Relay | NAT traversal, sunucusuz bağlantı |
| Unity Lobby | Oda oluşturma ve katılma |
| C# | Oyun mantığı |

## 🚀 Başlangıç

### Gereksinimler
- Unity 6 (6000.x)
- Unity Gaming Services hesabı (Relay + Lobby için)

### Kurulum
1. Repo'yu klonla:
   ```bash
   git clone https://github.com/omerseyfettinozd/Clone-Combat.git
   ```
2. Unity Hub'da projeyi aç
3. **Edit → Project Settings → Services** → Unity Gaming Services'e bağlan
4. **Play** tuşuna bas → Ana menüden lobi oluştur veya katıl

### Nasıl Oynanır
1. Bir oyuncu **Create Lobby** ile oda oluşturur
2. Diğeri **Join Lobby** ile katılır
3. İki oyuncu da **Ready** olunca savaş başlar
4. Rakibi öldür → coin kazan → daha güçlü silah al
5. Rakibin üssünü yık ve kazan! 🏆

## 📁 Proje Yapısı

```
Assets/
├── Prefabs/
│   ├── Player/          # Oyuncu prefab'ı
│   └── Ghosts/          # Ghost prefab'ı
├── Scenes/
│   ├── MainMenu.unity   # Ana menü + lobi
│   └── BattleArena.unity # Savaş arenası
└── Scripts/
    ├── Camera/           # Kamera takip sistemi
    ├── Combat/           # Silah, mermi, hasar, sağlık
    ├── Economy/          # Coin sistemi
    ├── Ghost/            # Ghost kayıt ve oynatma
    ├── Networking/       # GameManager, Lobby, Netcode
    ├── Player/           # Oyuncu kontrolcüsü
    └── UI/               # Menü, HUD, mağaza
```

## 📋 Sürümler

Detaylı değişiklik günlüğü için [CHANGELOG.md](CHANGELOG.md) dosyasına bak.

| Sürüm | Tarih | Öne Çıkanlar |
|-------|-------|-------------|
| v0.2.0 | 2026-03-11 | Hit marker, ölüm görünürlüğü, ghost silah sync, 128Hz |
| v0.1.0 | 2026-03-10 | Takım renkleri, kamera sistemi, ghost düzeltmeleri |

## 🗺️ Yol Haritası

Detaylı yol haritası için [ROADMAP.md](ROADMAP.md) dosyasına bak.

Gelecek sürümlerde: harita çeşitliliği, seçilebilir karakterler, yerel çok oyunculu (splitscreen), LAN desteği ve daha fazlası.

## 📄 Lisans

Bu proje kişisel/eğitim amaçlı geliştirilmektedir.
