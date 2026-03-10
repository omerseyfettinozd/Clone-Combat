using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Health system for players. Server-authoritative.
/// Oyuncu sağlık sistemi. Sunucu tarafında yönetilir.
/// </summary>
public class HealthSystem : NetworkBehaviour
{
    [Header("Health / Sağlık")]
    [SerializeField] private float _maxHealth = 100f;

    // Başlangıç değeri 0; gerçek değer OnNetworkSpawn'da _maxHealth ile atanır
    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth => _currentHealth.Value;
    public float MaxHealth => _maxHealth;

    // Ölüm olayı - GameManager ve GhostRecorder dinleyecek
    public System.Action<ulong> OnDeath; // ulong = killerClientId

    private bool _isDead;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
            _isDead = false;
        }
    }

    public override void OnNetworkDespawn()
    {
        // Delegate temizliği - memory leak'i önle
        OnDeath = null;
    }

    /// <summary>
    /// Server-only: Apply damage to this player.
    /// Sadece sunucu: Bu oyuncuya hasar uygula.
    /// </summary>
    public void TakeDamage(float damage, ulong attackerClientId)
    {
        if (!IsServer || _isDead) return;

        _currentHealth.Value = Mathf.Max(_currentHealth.Value - damage, 0f);

        Debug.Log($"Player {OwnerClientId} took {damage} damage. HP: {_currentHealth.Value}");

        if (_currentHealth.Value <= 0f)
        {
            HandleDeath(attackerClientId);
        }
    }

    private void HandleDeath(ulong killerClientId)
    {
        if (_isDead) return; // Çift ölüm çağrısını önle
        _isDead = true;

        Debug.Log($"Player {OwnerClientId} killed by {killerClientId}!");
        OnDeath?.Invoke(killerClientId);

        // GameManager'a ölümü bildir
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPlayerDied(OwnerClientId, killerClientId);
        }
    }

    /// <summary>
    /// Resets health to max (called on respawn).
    /// Sağlığı maksimuma sıfırlar (yeniden doğuşta çağrılır).
    /// </summary>
    public void ResetHealth()
    {
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
            _isDead = false;
        }
    }
}
