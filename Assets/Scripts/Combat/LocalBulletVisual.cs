using UnityEngine;

/// <summary>
/// Local-only bullet for client-side prediction. Gives instant visual feedback.
/// İstemci tarafı tahmin için yerel mermi. Anında görsel geri bildirim sağlar.
/// Bu mermi ağ üzerinden senkronize OLMAZ, sadece ateş eden oyuncunun ekranında görünür.
/// </summary>
public class LocalBulletVisual : MonoBehaviour
{
    private float _speed;
    private float _lifetime;
    private Vector3 _direction;

    /// <summary>
    /// Initializes the local prediction bullet.
    /// Yerel tahmin mermisini başlatır.
    /// </summary>
    public void Initialize(float speed, float lifetime, Vector3 direction, Color color)
    {
        _speed = speed;
        _lifetime = lifetime;
        _direction = direction.normalized;

        // Rotasyonu ayarla
        float angle = Mathf.Atan2(_direction.y, _direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Rengi ayarla
        SpriteRenderer sr = GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = color;
        }
    }

    private void Update()
    {
        // Yerel hareket – sunucu beklenmez
        transform.position += _direction * _speed * Time.deltaTime;

        _lifetime -= Time.deltaTime;
        if (_lifetime <= 0f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Başka bir mermiye çarpmasın
        if (collision.GetComponent<Bullet>() != null || collision.GetComponent<LocalBulletVisual>() != null) return;

        // Kendi oyuncumuza çarpmasın (yerel tahmin mermisi olduğu için yerel oyuncuyu yok say)
        Unity.Netcode.NetworkObject netObj = collision.GetComponentInParent<Unity.Netcode.NetworkObject>();
        if (netObj != null && netObj.IsOwner) return;

        // Kendi hayaletine çarpmasın
        GhostPlayback ghost = collision.GetComponentInParent<GhostPlayback>();
        if (ghost != null && ghost.OriginalOwnerId == Unity.Netcode.NetworkManager.Singleton.LocalClientId) return;

        // Düşman oyuncuya veya düşman base'e çarptığında (trigger olsa da olmasa da) yok olmak istiyoruz
        if (collision.GetComponentInParent<HealthSystem>() != null || collision.GetComponentInParent<BaseHealth>() != null)
        {
            Destroy(gameObject);
            return;
        }

        // Bölge tetikleyicisi ise görünmez alanlardan (kameralar, spawner'lar) geç, yok olma
        if (collision.isTrigger) return;

        // Diğer katı şeylere çarptığında (duvar, zemin vs.) görsel olarak yok ol
        Destroy(gameObject);
    }
}
