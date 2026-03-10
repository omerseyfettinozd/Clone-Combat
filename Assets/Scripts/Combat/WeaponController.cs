using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Handles weapon firing logic with client-side prediction.
/// İstemci tarafı tahminli silah ateşleme mantığını yönetir.
/// Ateş eden oyuncu anında yerel mermi görür, sunucu hasarı işler.
/// </summary>
public class WeaponController : NetworkBehaviour
{
    [Header("References / Referanslar")]
    [SerializeField] private Transform _firePoint;       // Merminin çıkış noktası
    [SerializeField] private GameObject _bulletPrefab;   // Mermi prefab'ı (ağ üzerinden)

    [Header("Weapon / Silah")]
    [SerializeField] private WeaponData _currentWeapon;  // Aktif silah verisi

    private float _fireTimer;
    private bool _isShooting;

    public WeaponData CurrentWeapon => _currentWeapon;

    private void Update()
    {
        if (!IsOwner) return;

        _fireTimer -= Time.deltaTime;

        // Sol tıklama ile ateş etme (yeni Input System)
        Mouse mouse = Mouse.current;
        if (mouse != null && mouse.leftButton.isPressed && _fireTimer <= 0f && _currentWeapon != null && _firePoint != null)
        {
            _isShooting = true;
            _fireTimer = _currentWeapon.fireRate;

            // CLIENT PREDICTION: Yerel mermi hemen görünsün (0 gecikme)
            SpawnLocalPredictionBullet();

            // SERVER AUTHORITATIVE: Gerçek mermi sunucuda oluşsun (hasar + diğer oyuncuya görsel)
            FireServerRpc(
                _firePoint.position, 
                _firePoint.right,
                _currentWeapon.bulletSpeed,
                _currentWeapon.damage,
                _currentWeapon.bulletLifetime,
                _currentWeapon.bulletColor.r,
                _currentWeapon.bulletColor.g,
                _currentWeapon.bulletColor.b
            );
        }
        else
        {
            _isShooting = false;
        }
    }

    /// <summary>
    /// Spawns a local-only visual bullet for instant feedback (no network latency).
    /// Anında geri bildirim için yerel görsel mermi oluşturur (ağ gecikmesi yok).
    /// </summary>
    private void SpawnLocalPredictionBullet()
    {
        if (_bulletPrefab == null || _currentWeapon == null) return;

        // Bullet prefab'ından görsel bilgileri kopyalayarak temiz bir yerel obje oluştur
        GameObject localBullet = new GameObject("LocalBulletPrediction");
        localBullet.transform.position = _firePoint.position;
        localBullet.transform.localScale = _bulletPrefab.transform.localScale; // Prefab boyutunu kopyala

        // Sprite'ı kopyala
        SpriteRenderer prefabSR = _bulletPrefab.GetComponentInChildren<SpriteRenderer>();
        if (prefabSR != null)
        {
            SpriteRenderer sr = localBullet.AddComponent<SpriteRenderer>();
            sr.sprite = prefabSR.sprite;
            sr.color = _currentWeapon.bulletColor;
            sr.sortingLayerID = prefabSR.sortingLayerID;
            sr.sortingOrder = prefabSR.sortingOrder;
        }

        // Fizik bileşenleri ekle (Triggere girmesi için Rigidbody2D ve Collider2D gerekir)
        Rigidbody2D rb = localBullet.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;

        BoxCollider2D col = localBullet.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        // Mermi görselinin boyutuna göre collider ayarla (yaklaşık)
        if (prefabSR != null && prefabSR.sprite != null)
        {
            col.size = prefabSR.sprite.bounds.size;
        }
        else
        {
            col.size = new Vector2(0.2f, 0.2f);
        }

        // Yerel mermi davranışını ekle
        LocalBulletVisual visual = localBullet.AddComponent<LocalBulletVisual>();
        visual.Initialize(
            _currentWeapon.bulletSpeed,
            _currentWeapon.bulletLifetime,
            _firePoint.right,
            _currentWeapon.bulletColor
        );
    }

    /// <summary>
    /// Requests the server to spawn an authoritative bullet for damage and sync.
    /// Sunucudan hasar ve senkronizasyon için yetkili mermi oluşturmasını ister.
    /// </summary>
    [ServerRpc]
    private void FireServerRpc(Vector3 position, Vector3 direction, float speed, float damage, float lifetime, float bulletColorR, float bulletColorG, float bulletColorB)
    {
        if (_bulletPrefab == null) return;

        // Mermiyi sunucu tarafında oluştur
        GameObject bulletObj = Instantiate(_bulletPrefab, position, Quaternion.identity);

        // Ağ üzerinden spawn et
        NetworkObject netObj = bulletObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Bullet prefab'ında NetworkObject bileşeni bulunamadı!");
            Destroy(bulletObj);
            return;
        }
        netObj.Spawn();

        // Başlat
        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            Color bulletColor = new Color(bulletColorR, bulletColorG, bulletColorB);
            // Host (ServerClientId) = Team 0, Client = Team 1
            int shooterTeamId = (OwnerClientId == NetworkManager.ServerClientId) ? 0 : 1;
            bullet.Initialize(speed, damage, lifetime, direction, OwnerClientId, shooterTeamId, bulletColor);
        }
    }

    /// <summary>
    /// Sets the weapon data (called when purchasing a new weapon from shop).
    /// Silah verisini ayarlar (mağazadan yeni silah alındığında çağrılır).
    /// </summary>
    public void SetWeapon(WeaponData newWeapon)
    {
        _currentWeapon = newWeapon;
        _fireTimer = 0f;
    }

    /// <summary>
    /// Returns whether the player is currently shooting (used by Ghost recorder).
    /// Oyuncunun şu an ateş edip etmediğini döndürür (Hayalet kaydedici için).
    /// </summary>
    public bool IsShooting()
    {
        return _isShooting;
    }
}
