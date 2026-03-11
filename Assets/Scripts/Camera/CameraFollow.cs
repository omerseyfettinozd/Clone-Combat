using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Smooth camera follow for 2D multiplayer using SmoothDamp.
/// Fizik simülasyonuyla uyumlu, pürüzsüz 2D kamera takibi.
/// Sahne değişimlerinde otomatik olarak local player'ı bulur.
/// </summary>
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Instance { get; private set; }

    [Header("Follow Settings / Takip Ayarları")]
    [SerializeField] private float _smoothTime = 0.12f;     // Hedefe ulaşma süresi
    [SerializeField] private Vector3 _offset = new Vector3(0f, 1f, -10f);

    [Header("Lookahead / Öngörü")]
    [SerializeField] private float _lookAheadX = 1.5f;
    [SerializeField] private float _lookAheadSmooth = 0.3f;

    private Transform _target;
    private Vector3 _velocity = Vector3.zero;
    private float _currentLookAheadX;
    private float _lookAheadVelocity;
    private Rigidbody2D _targetRb;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        // Sahne yüklendiğinde local player'ı otomatik bul
        FindLocalPlayer();
    }

    /// <summary>
    /// Finds the local player and sets it as the camera target.
    /// Local player'ı bulur ve kamera hedefi olarak atar.
    /// </summary>
    private void FindLocalPlayer()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.LocalClient == null) return;
        
        NetworkObject localPlayer = NetworkManager.Singleton.LocalClient.PlayerObject;
        if (localPlayer != null)
        {
            SetTarget(localPlayer.transform);
            Debug.Log("[CameraFollow] Local player found and assigned as target.");
        }
        else
        {
            // Player henüz spawn olmadıysa tekrar dene
            Invoke(nameof(FindLocalPlayer), 0.2f);
        }
    }

    /// <summary>
    /// Sets the camera target.
    /// Kamera hedefini ayarlar.
    /// </summary>
    public void SetTarget(Transform target)
    {
        _target = target;
        _targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;

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
        if (_target == null)
        {
            // Hedef kaybolmuşsa (ölüm, respawn vs.) tekrar bul
            FindLocalPlayer();
            return;
        }

        // Hareket yönüne göre lookahead
        float targetLookAhead = 0f;
        if (_targetRb != null && Mathf.Abs(_targetRb.linearVelocity.x) > 0.5f)
        {
            targetLookAhead = Mathf.Sign(_targetRb.linearVelocity.x) * _lookAheadX;
        }
        _currentLookAheadX = Mathf.SmoothDamp(_currentLookAheadX, targetLookAhead, ref _lookAheadVelocity, _lookAheadSmooth);

        // Hedef pozisyon
        Vector3 targetPos = _target.position + _offset;
        targetPos.x += _currentLookAheadX;
        targetPos.z = _offset.z;

        // SmoothDamp
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
