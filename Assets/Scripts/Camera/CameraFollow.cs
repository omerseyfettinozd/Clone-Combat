using UnityEngine;

/// <summary>
/// Smooth camera follow for 2D multiplayer using SmoothDamp.
/// Fizik simülasyonuyla uyumlu, pürüzsüz 2D kamera takibi.
/// SmoothDamp, Lerp'ten farklı olarak hız tabanlıdır — ani duruşlar ve başlangıçlar olmaz.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Follow Settings / Takip Ayarları")]
    [SerializeField] private float _smoothTime = 0.12f;     // Hedefe ulaşma süresi (düşük = hızlı, yüksek = yumuşak)
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, -10f); // Kamera offset

    [Header("Lookahead / Öngörü")]
    [SerializeField] private float _lookAheadX = 1.5f;      // Hareket yönünde ileriye bakma mesafesi
    [SerializeField] private float _lookAheadSmooth = 0.3f;  // Öngörü geçiş yumuşaklığı

    private Transform _target;
    private Vector3 _velocity = Vector3.zero; // SmoothDamp iç hız referansı
    private float _currentLookAheadX;
    private float _lookAheadVelocity;
    private Rigidbody2D _targetRb;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Instance = this;
            return;
        }
        Instance = this;
    }

    /// <summary>
    /// Sets the camera target.
    /// Kamera hedefini ayarlar.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
        _targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;

        // İlk atamada hemen pozisyonu ayarla
        if (_target != null)
        {
            Vector3 desiredPos = _target.position + _offset;
            desiredPos.z = _offset.z;
            transform.position = desiredPos;
            _velocity = Vector3.zero;
        }
    }

    private void FixedUpdate()
    {
        // FixedUpdate'te takip et → fizik hareketleriyle senkron, jitter yok
        if (_target == null) return;

        // Hareket yönüne göre ileriye bakma (lookahead)
        float targetLookAhead = 0f;
        if (_targetRb != null && Mathf.Abs(_targetRb.linearVelocity.x) > 0.5f)
        {
            targetLookAhead = Mathf.Sign(_targetRb.linearVelocity.x) * _lookAheadX;
        }
        _currentLookAheadX = Mathf.SmoothDamp(_currentLookAheadX, targetLookAhead, ref _lookAheadVelocity, _lookAheadSmooth);

        // Hedef pozisyon = oyuncu + offset + lookahead
        Vector3 targetPos = _target.position + _offset;
        targetPos.x += _currentLookAheadX;
        targetPos.z = _offset.z;

        // SmoothDamp: Hız tabanlı yumuşak geçiş (Lerp'ten çok daha pürüzsüz)
        transform.position = Vector3.SmoothDamp(transform.position, targetPos, ref _velocity, _smoothTime);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
