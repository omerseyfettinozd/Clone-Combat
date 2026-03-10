using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Platformer-style player controller with gravity, horizontal movement, and jumping.
/// Platformer tarzı oyuncu kontrolcüsü: yerçekimi, yatay hareket ve zıplama.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [Header("Movement / Hareket")]
    [SerializeField] private float _moveSpeed = 7f;
    [SerializeField] private float _jumpForce = 12f;

    [Header("Ground Check / Zemin Kontrolü")]
    [SerializeField] private Transform _groundCheck;      // Ayakların altındaki kontrol noktası
    [SerializeField] private float _groundCheckRadius = 0.2f;
    [SerializeField] private LayerMask _groundLayer;       // "Ground" layer'ı

    [Header("References / Referanslar")]
    [SerializeField] private Transform _weaponPivot;

    [Header("Network / Ağ Ayarları")]
    [SerializeField] private float _aimSyncThreshold = 0.5f; // Derece cinsinden eşik değeri (düşük = daha akıcı)

    private Rigidbody2D _rb;
    private Camera _mainCamera;
    private bool _isGrounded;
    private float _moveInputX;
    private float _lastSyncedAngle;
    private Vector3 _cachedMouseWorldPos; // Duplicate hesaplamayı önlemek için cache
    private bool _hasJumpedThisFrame; // Ghost Recorder için zıplama durumu

    private void Awake()
    {
        _rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        SetupPhysicsAndRendering();
    }

    private void SetupPhysicsAndRendering()
    {
        // 1. Oyuncu üstte (ön planda) görünsün
        SpriteRenderer[] srs = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            sr.sortingOrder = 10;
        }

        // 2. Çarpışmaları yoksay (Hayaletler ve diğer oyuncular)
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        
        GhostPlayback[] ghosts = Object.FindObjectsByType<GhostPlayback>(FindObjectsSortMode.None);
        foreach (var g in ghosts)
        {
            Collider2D[] gCols = g.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var gCol in gCols)
                {
                    Physics2D.IgnoreCollision(myCol, gCol, true);
                }
            }
        }

        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p == this) continue;
            Collider2D[] pCols = p.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var pCol in pCols)
                {
                    Physics2D.IgnoreCollision(myCol, pCol, true);
                }
            }
        }
    }

    private void Start()
    {
        _mainCamera = Camera.main;
    }

    private void Update()
    {
        if (!IsOwner) return;

        // Kamera sahneler arası geçişte kaybolabilir, her frame kontrol et
        if (_mainCamera == null) _mainCamera = Camera.main;
        if (_mainCamera == null) return;

        HandleMovementInput();
        HandleJump();
        UpdateMouseWorldPosition(); // Mouse pozisyonunu bir kez hesapla
        HandleAiming();
        HandleFlip();
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        CheckGround();
        ApplyMovement();
        IgnoreCollisionsContinuously();
    }

    private void IgnoreCollisionsContinuously()
    {
        Collider2D[] myCols = GetComponentsInChildren<Collider2D>();
        
        GhostPlayback[] ghosts = Object.FindObjectsByType<GhostPlayback>(FindObjectsSortMode.None);
        foreach (var g in ghosts)
        {
            Collider2D[] gCols = g.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var gCol in gCols)
                {
                    Physics2D.IgnoreCollision(myCol, gCol, true);
                }
            }
        }

        PlayerController[] players = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
        foreach (var p in players)
        {
            if (p == this) continue;
            Collider2D[] pCols = p.GetComponentsInChildren<Collider2D>();
            foreach (var myCol in myCols)
            {
                foreach (var pCol in pCols)
                {
                    Physics2D.IgnoreCollision(myCol, pCol, true);
                }
            }
        }
    }

    /// <summary>
    /// Reads A/D or Arrow keys for horizontal movement.
    /// A/D veya Yön tuşları ile yatay hareket girdisi okur.
    /// </summary>
    private void HandleMovementInput()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        _moveInputX = 0f;
        if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) _moveInputX = 1f;
        else if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) _moveInputX = -1f;
    }

    /// <summary>
    /// Handles jump input (Space or W).
    /// Zıplama girdisini işler (Space veya W).
    /// </summary>
    private void HandleJump()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        bool jumpTriggered = keyboard.spaceKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame;
        
        // Zıplama durumunu (başarılı zıplamayı) FrameData için kaydet
        if (jumpTriggered && _isGrounded)
        {
            _rb.linearVelocity = new Vector2(_rb.linearVelocity.x, _jumpForce);
            _hasJumpedThisFrame = true;
        }
    }

    /// <summary>
    /// Checks if the player is touching the ground.
    /// Oyuncunun zeminde olup olmadığını kontrol eder.
    /// </summary>
    private void CheckGround()
    {
        if (_groundCheck != null)
        {
            _isGrounded = Physics2D.OverlapCircle(_groundCheck.position, _groundCheckRadius, _groundLayer);
        }
    }

    /// <summary>
    /// Applies horizontal movement via Rigidbody2D (preserves vertical velocity for gravity).
    /// Rigidbody2D ile yatay hareketi uygular (yerçekimi için dikey hızı korur).
    /// </summary>
    private void ApplyMovement()
    {
        _rb.linearVelocity = new Vector2(_moveInputX * _moveSpeed, _rb.linearVelocity.y);
    }

    /// <summary>
    /// Calculates mouse world position once per frame (optimization).
    /// Fare dünya pozisyonunu frame başına bir kez hesaplar (optimizasyon).
    /// </summary>
    private void UpdateMouseWorldPosition()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null) return;

        Vector3 mouseScreenPos = mouse.position.ReadValue();
        _cachedMouseWorldPos = _mainCamera.ScreenToWorldPoint(new Vector3(mouseScreenPos.x, mouseScreenPos.y, 0f));
        _cachedMouseWorldPos.z = 0f;
    }

    /// <summary>
    /// Rotates the weapon pivot towards the mouse position.
    /// Silahı fare pozisyonuna doğru döndürür.
    /// </summary>
    private void HandleAiming()
    {
        if (_weaponPivot == null) return;

        Vector2 direction = (_cachedMouseWorldPos - _weaponPivot.position).normalized;
        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

        _weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        // Sadece açı belirli bir eşiği aştığında RPC gönder (ağ trafiğini azalt)
        if (Mathf.Abs(Mathf.DeltaAngle(_lastSyncedAngle, angle)) > _aimSyncThreshold)
        {
            _lastSyncedAngle = angle;
            SyncAimServerRpc(angle);
        }
    }

    /// <summary>
    /// Flips the character sprite based on mouse position.
    /// Fare pozisyonuna göre karakterin yönünü çevirir.
    /// </summary>
    private void HandleFlip()
    {
        // _cachedMouseWorldPos zaten hesaplandı, tekrar hesaplamaya gerek yok
        if (_cachedMouseWorldPos.x < transform.position.x)
            transform.localScale = new Vector3(-1f, 1f, 1f);
        else
            transform.localScale = new Vector3(1f, 1f, 1f);
    }

    [ServerRpc]
    private void SyncAimServerRpc(float angle)
    {
        SyncAimClientRpc(angle);
    }

    [ClientRpc]
    private void SyncAimClientRpc(float angle)
    {
        if (IsOwner) return;
        if (_weaponPivot != null)
        {
            _weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    public Vector2 GetMoveInput()
    {
        return new Vector2(_moveInputX, 0f);
    }

    public float GetAimAngle()
    {
        if (_weaponPivot == null) return 0f;
        return _weaponPivot.rotation.eulerAngles.z;
    }

    /// <summary>
    /// GhostRecorder'ın bu frame içinde zıplama yapılıp yapılmadığını okuyup sıfırlaması için kullanılır.
    /// </summary>
    public bool ConsumeJumpFlag()
    {
        if (_hasJumpedThisFrame)
        {
            _hasJumpedThisFrame = false;
            return true;
        }
        return false;
    }
}
