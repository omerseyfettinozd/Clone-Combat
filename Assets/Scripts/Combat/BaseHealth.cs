using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Health system for Base objects. When HP reaches 0, game ends.
/// Base (Ana Üs) sağlık sistemi. Can 0'a düşünce oyun biter.
/// </summary>
public class BaseHealth : NetworkBehaviour
{
    [Header("Base Settings / Üs Ayarları")]
    [SerializeField] private float _maxHealth = 500f;
    [SerializeField] private int _teamId = 0; // 0 = Sol takım, 1 = Sağ takım

    // Başlangıç değeri 0; gerçek değer OnNetworkSpawn'da _maxHealth ile atanır
    private NetworkVariable<float> _currentHealth = new NetworkVariable<float>(
        0f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public float CurrentHealth => _currentHealth.Value;
    public float MaxHealth => _maxHealth;
    public int TeamId => _teamId;

    // Sağlık değiştiğinde UI güncellemesi için olay
    public System.Action<float, float> OnHealthChanged; // (currentHP, maxHP)

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            _currentHealth.Value = _maxHealth;
        }

        // Sağlık değişikliklerini dinle
        _currentHealth.OnValueChanged += HandleHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        _currentHealth.OnValueChanged -= HandleHealthChanged;
    }

    private void HandleHealthChanged(float oldValue, float newValue)
    {
        OnHealthChanged?.Invoke(newValue, _maxHealth);
    }

    /// <summary>
    /// Server-only: Apply damage to this base.
    /// Sadece sunucu: Bu üsse hasar uygula.
    /// </summary>
    public void TakeDamage(float damage)
    {
        if (!IsServer) return;

        _currentHealth.Value = Mathf.Max(_currentHealth.Value - damage, 0f);

        Debug.Log($"Base (Team {_teamId}) took {damage} damage. HP: {_currentHealth.Value}");

        if (_currentHealth.Value <= 0f)
        {
            HandleBaseDestroyed();
        }
    }

    private void HandleBaseDestroyed()
    {
        Debug.Log($"Base (Team {_teamId}) DESTROYED! Game Over!");

        if (GameManager.Instance != null)
        {
            // Kaybeden takımın base'i yıkıldı, kazanan diğer takımdır
            int winnerTeam = (_teamId == 0) ? 1 : 0;
            GameManager.Instance.OnBaseDestroyed(winnerTeam);
        }
    }
}
