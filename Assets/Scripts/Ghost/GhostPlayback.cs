using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Plays back recorded ghost data. Moves and shoots like the original player.
/// Kaydedilmiş hayalet verilerini oynatır. Orijinal oyuncu gibi hareket eder ve ateş eder.
/// </summary>
public class GhostPlayback : NetworkBehaviour
{
    [Header("References / Referanslar")]
    [SerializeField] private Transform _weaponPivot;
    [SerializeField] private Transform _firePoint;
    [SerializeField] private GameObject _bulletPrefab;
    [SerializeField] private WeaponData _weaponData;

    [Header("Physics & Movement / Fizik & Hareket")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _jumpForce = 7f;
    [SerializeField] private Transform _groundCheck;
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;

    private GhostFrameData[] _frames;
    private int _currentFrameIndex;
    private bool _isPlaying;
    private Rigidbody2D _rb;
    private bool _isGrounded;
    private Vector3 _spawnPosition;
    private int _collisionCheckCounter;

    /// <summary>
    /// The original player's client ID who created this ghost.
    /// Bu hayaleti oluşturan orijinal oyuncunun client ID'si.
    /// </summary>
    public ulong OriginalOwnerId { get; private set; }

    /// <summary>
    /// Initializes the ghost with recorded frame data and a starting spawn position.
    /// Hayaleti kaydedilmiş kare verileriyle ve başlangıç spawn pozisyonuyla başlatır.
    /// </summary>
    public void Initialize(GhostFrameData[] frames, WeaponData weaponData, ulong originalOwnerId, Vector3 spawnPosition)
    {
        if (frames == null || frames.Length == 0)
        {
            Debug.LogWarning("Ghost initialized with empty frame data!");
            _isPlaying = false;
            return;
        }

        _rb = GetComponent<Rigidbody2D>();
        
        _frames = frames;
        _weaponData = weaponData;
        _currentFrameIndex = 0;
        _isPlaying = true;
        OriginalOwnerId = originalOwnerId;
        _spawnPosition = spawnPosition;

        transform.position = spawnPosition;

        // Collider ve Rigidbody ayarlarını tüm clientlarda aynı olması için RPC ile gönder
        // OriginalOwnerId sadece server'da set ediliyor, client'a da gönder
        SetupPhysicsClientRpc(originalOwnerId);

        // Eğer NetworkTransform varsa ve ilk pozisyon atamasıysa Teleport yap
        Unity.Netcode.Components.NetworkTransform nt = GetComponent<Unity.Netcode.Components.NetworkTransform>();
        if (nt != null)
        {
            nt.Teleport(spawnPosition, Quaternion.identity, transform.localScale);
        }
    }

    [ClientRpc]
    private void SetupPhysicsClientRpc(ulong originalOwnerId)
    {
        // Client tarafında da OriginalOwnerId'yi ayarla (takım rengi belirleme için gerekli)
        OriginalOwnerId = originalOwnerId;
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();

        // Player'lar ile fiziksel itişmeyi/çarpışmayı engellemek için IgnoreCollision kullanıyoruz (Zeminle çarpışması lazım!)
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            Collider2D[] pCols = p.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var pCol in pCols)
                {
                    Physics2D.IgnoreCollision(myCol, pCol, true);
                }
            }
        }

        // Diğer Ghost'lar ile fiziksel çarpışmayı engelliyoruz
        GhostPlayback[] ghosts = Object.FindObjectsByType<GhostPlayback>(FindObjectsSortMode.None);
        foreach (var g in ghosts)
        {
            if (g == this) continue;
            Collider2D[] gCols = g.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var gCol in gCols)
                {
                    Physics2D.IgnoreCollision(myCol, gCol, true);
                }
            }
        }

        // Rigidbody2D ayarları
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Ghost'un fizik simülasyonu çalışmalı (Dynamic), ama oyuncuları itmesini istemediğimiz için
            // Player layernı yoksayacak Trigger collider kullanıyoruz.
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.simulated = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        }

        // --- Ghost'lar arka planda kalsın + Takım rengi ---
        Color teamColor = Color.white;
        if (GameManager.Instance != null)
        {
            teamColor = GameManager.Instance.GetTeamColor(OriginalOwnerId);
        }

        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.sortingOrder = 5; // Oyuncudan (10) daha düşük bir değere çekiyoruz.
            // Takım rengi + yarı şeffaf (ghost olduğu belli olsun)
            teamColor.a = 0.5f;
            sr.color = teamColor;
        }
    }

    private void IgnoreCollisionsContinuously()
    {
        // Hayaletlerin ve oyuncuların doğma zamanları farklı olabildiği için,
        // çarpışma yoksayma işlemini düzenli olarak (veya gerektiğinde) yapmak daha garantidir.
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        
        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            Collider2D[] pCols = p.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var pCol in pCols)
                {
                    Physics2D.IgnoreCollision(myCol, pCol, true);
                }
            }
        }

        GhostPlayback[] ghosts = Object.FindObjectsByType<GhostPlayback>(FindObjectsSortMode.None);
        foreach (var g in ghosts)
        {
            if (g == this) continue;
            Collider2D[] gCols = g.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var gCol in gCols)
                {
                    Physics2D.IgnoreCollision(myCol, gCol, true);
                }
            }
        }
    }

    private void CheckGround()
    {
        if (_groundCheck != null)
        {
            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
        }
        else
        {
            Debug.LogWarning("[GhostPlayback] _groundCheck atanmamış! Ghost zıplayamaz. Prefab'ı kontrol edin.", this);
        }
    }

    private void FixedUpdate()
    {
        if (!IsServer || !_isPlaying || _frames == null) return;

        CheckGround();

        // Performans: Her frame FindObjectsByType yerine, ~1 saniyede bir çalıştır
        _collisionCheckCounter++;
        if (_collisionCheckCounter >= 60)
        {
            _collisionCheckCounter = 0;
            IgnoreCollisionsContinuously();
        }

        // Oynatma bittiğinde başa sar (sürekli tekrar etsin)
        if (_currentFrameIndex >= _frames.Length)
        {
            _currentFrameIndex = 0;
            
            // Ghost'u başlangıç noktasına (Spawn Point) ışınla
            Unity.Netcode.Components.NetworkTransform nt = GetComponent<Unity.Netcode.Components.NetworkTransform>();
            if (nt != null)
            {
                nt.Teleport(_spawnPosition, transform.rotation, transform.localScale);
            }
            else
            {
                transform.position = _spawnPosition;
            }
            
            // Hızını sıfırla ki havada kalıp düşmesin
            if (_rb != null)
            {
                _rb.linearVelocity = Vector2.zero;
            }
        }

        GhostFrameData frame = _frames[_currentFrameIndex];

        // 1. Yatay Hareket (MoveInputX)
        if (_rb != null)
        {
            _rb.linearVelocity = new Vector2(frame.MoveInputX * _moveSpeed, _rb.linearVelocity.y);
            
            // 2. Zıplama (JumpPressed)
            if (frame.JumpPressed && _isGrounded)
            {
                _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            }
        }

        // Karakter Yüzünü Döndürme
        if (frame.AimAngle > 90f || frame.AimAngle < -90f)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else
            transform.localScale = new Vector3(1f, 1f, 1f);

        // 3. Nişan Alma
        if (_weaponPivot != null)
        {
            _weaponPivot.rotation = Quaternion.Euler(0f, 0f, frame.AimAngle);
        }

        if (frame.IsShooting && _firePoint != null && _bulletPrefab != null && _weaponData != null)
        {
            GhostFire();
        }

        _currentFrameIndex++;
    }

    private void GhostFire()
    {
        GameObject bulletObj = Instantiate(_bulletPrefab, _firePoint.position, Quaternion.identity);

        NetworkObject netObj = bulletObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Ghost bullet prefab'ında NetworkObject bulunamadı!");
            Destroy(bulletObj);
            return;
        }
        netObj.Spawn();

        Bullet bullet = bulletObj.GetComponent<Bullet>();
        if (bullet != null)
        {
            // Ghost mermileri orijinal sahibinin ID'si ve takımı ile işaretlenir
            int ghostTeamId = (OriginalOwnerId == NetworkManager.ServerClientId) ? 0 : 1;
            bullet.Initialize(
                _weaponData.bulletSpeed,
                _weaponData.damage,
                _weaponData.bulletLifetime,
                _firePoint.right,
                OriginalOwnerId,
                ghostTeamId,
                _weaponData.bulletColor,
                isGhostBullet: true
            );
        }
    }
}
