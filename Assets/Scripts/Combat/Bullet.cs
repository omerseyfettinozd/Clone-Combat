using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Authoritative bullet: moves on server, deals damage on hit, despawns after lifetime.
/// Yetkili mermi: sunucuda hareket eder, çarptığında hasar verir, süre bitince yok olur.
/// Ateş eden oyuncunun ekranında gizlenir (istemci tahtmin mermisi zaten gösterilir).
/// </summary>
public class Bullet : NetworkBehaviour
{
    private float _speed;
    private float _damage;
    private float _lifetime;
    private Vector3 _direction;
    private ulong _shooterClientId;
    private int _shooterTeamId;
    private bool _initialized;
    private float _spawnGraceTimer; // Client'ın spawn mesajını alması için minimum bekleme
    private bool _isGhostBullet;

    /// <summary>
    /// Called by WeaponController on the server after spawn.
    /// WeaponController tarafından sunucuda spawn sonrası çağrılır.
    /// </summary>
    public void Initialize(float speed, float damage, float lifetime, Vector3 direction, ulong shooterClientId, int shooterTeamId, Color bulletColor, bool isGhostBullet = false)
    {
        _speed = speed;
        _damage = damage;
        _lifetime = lifetime;
        _direction = direction.normalized;
        _shooterClientId = shooterClientId;
        _shooterTeamId = shooterTeamId;
        _initialized = true;
        _spawnGraceTimer = 0.1f; // 100ms - client'ın spawn mesajını alması için yeterli
        _isGhostBullet = isGhostBullet;

        // Merminin yönüne göre rotasyonunu ayarla
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Mermi görsellerini client'larda ayarla:
        // - Ateş eden oyuncu: GİZLE (yerel tahmin mermisi zaten var)
        // - Diğer oyuncular: Rengi ayarla
        // Ghost mermileri için: Hiçbir client'ta gizleme (yerel tahmin mermisi yok)
        ulong visualOwnerId = isGhostBullet ? ulong.MaxValue : shooterClientId;
        SetBulletVisualsClientRpc(visualOwnerId, bulletColor.r, bulletColor.g, bulletColor.b);

        // Merminin fiziksel olarak çarpmaması, sadece içinden geçip (trigger) "OnTriggerEnter2D" çağırması için
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }
    }

    /// <summary>
    /// Sets bullet visuals on all clients. Hides the bullet for the shooter (they have local prediction).
    /// Tüm client'larda mermi görsellerini ayarlar. Ateş eden için gizler (yerel tahmini var).
    /// </summary>
    [ClientRpc]
    private void SetBulletVisualsClientRpc(ulong shooterClientId, float r, float g, float b)
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        if (NetworkManager.Singleton.LocalClientId == shooterClientId)
        {
            // Ateş eden oyuncuda zaten yerel tahmin mermisi gösteriliyor,
            // ağ mermisini gizle (çift mermi görünmesin)
            sr.enabled = false;
        }
        else
        {
            // Diğer oyunculara rengi göster
            sr.color = new Color(r, g, b);
        }
    }

    private void Update()
    {
        // Sadece sunucuda hareket et, pozisyon NetworkTransform ile client'a gider
        if (!IsServer || !_initialized) return;

        // Grace timer: client'ın spawn alması için bekle
        if (_spawnGraceTimer > 0f)
        {
            _spawnGraceTimer -= Time.deltaTime;
        }

        transform.position += _direction * _speed * Time.deltaTime;

        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            DespawnBullet();
        }
    }

    private bool _hasHit = false;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsServer || !_initialized || _hasHit) return;
        if (_spawnGraceTimer > 0f) return; // Client henüz spawn almadı, bekle

        // Base'e çarptıysa — sadece düşman base'e hasar ver
        BaseHealth baseHealth = collision.GetComponentInParent<BaseHealth>();
        if (baseHealth != null)
        {
            // Kendi takımının base'ine hasar verme
            if (baseHealth.TeamId == _shooterTeamId) return;

            // Eğer mermi ghost tarafından atıldıysa base'e hasar verme, sadece yok ol
            if (_isGhostBullet)
            {
                Debug.Log($"[Bullet] Ghost bullet hit base team {_shooterTeamId} enemy! Dealing NO damage.");
                DespawnBullet();
                return;
            }

            Debug.Log($"[Bullet] Damaging base team {_shooterTeamId} enemy!");
            baseHealth.TakeDamage(_damage);
            DespawnBullet();
            return;
        }

        // Kendi sahibine çarpmasın (sadece oyuncular ve ghost'lar için, collider alt objede olabilir)
        NetworkObject netObj = collision.GetComponentInParent<NetworkObject>();
        if (netObj != null && netObj.OwnerClientId == _shooterClientId) return;

        // Kendi hayaletine çarpmasın (ghost'un orijinal sahibi == merminin sahibi ise geç)
        GhostPlayback ghost = collision.GetComponentInParent<GhostPlayback>();
        if (ghost != null && ghost.OriginalOwnerId == _shooterClientId) return;

        // Oyuncuya çarptıysa (Collider alt objede olabilir)
        HealthSystem health = collision.GetComponentInParent<HealthSystem>();
        if (health != null)
        {
            Debug.Log($"[Bullet] Damaging player {health.OwnerClientId}! Damage: {_damage}");
            health.TakeDamage(_damage, _shooterClientId);
            DespawnBullet();
            return;
        }

        // Başka bir mermiye çarpmasın (kendi içinde veya başka ağ mermisiyle)
        if (collision.GetComponentInParent<Bullet>() != null || collision.GetComponentInParent<LocalBulletVisual>() != null) return;

        // BİR BÖLGE TETİKLEYİCİSİNE ÇARPTIYSA (örneğin kameranın sınırları, alan belirteçleri)
        // Yukarıda oyuncu veya base olup olmadığını kontrol ettik, değilmiş. 
        // O zaman bu bir hayalet alan, içinden geçsin. Mermi sadece KATI cisimlere çarptığında yok olmalı.
        if (collision.isTrigger) return;

        // Buraya kadar geldiyse duvar, zemin veya alakasız bir KATI objeye çarpmıştır.
        Debug.Log($"[Bullet] Hit environment/solid obstacle! Despawning. Hit object: {collision.gameObject.name}, Layer: {collision.gameObject.layer}");
        DespawnBullet();
    }

    private void DespawnBullet()
    {
        if (_hasHit) return;
        _hasHit = true; // Sadece bir kez yok olmasını sağla
        
        // Mermiyi görünmez yap ve hareketini durdur (çarpışma anında hemen kaybolsun)
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
        _speed = 0f;

        // Görselin diğer client'larda da silinmesi için RPC çağır
        HideBulletClientRpc();

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            // Unity Netcode'da objeyi Spawn edildiği aynı saniyede (veya frame'de) Despawn etmek
            // "Deferred message / stale trigger" uyarılarına neden olur.
            // Bu yüzden gerçek ağdan silinmesini çok kısa bir süre (örneğin 0.1 saniye) geciktiriyoruz.
            Invoke(nameof(NetworkDespawnDelayed), 0.1f);
        }
    }

    private void NetworkDespawnDelayed()
    {
        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }

    [ClientRpc]
    private void HideBulletClientRpc()
    {
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null) sr.enabled = false;
    }
}
