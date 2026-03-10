using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Manages coin economy for each player. Server-authoritative.
/// Her oyuncu için coin ekonomisini yönetir. Sunucu tarafında yetkilendirilmiştir.
/// </summary>
public class CoinManager : NetworkBehaviour
{
    public static CoinManager LocalInstance { get; private set; }

    private NetworkVariable<int> _coins = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public int Coins => _coins.Value;

    // Coin değiştiğinde UI'ı güncellemek için olay
    public System.Action<int> OnCoinsChanged;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            LocalInstance = this;
        }

        _coins.OnValueChanged += HandleCoinsChanged;
    }

    public override void OnNetworkDespawn()
    {
        // Delegate temizliği – memory leak'i önle
        _coins.OnValueChanged -= HandleCoinsChanged;

        if (IsOwner && LocalInstance == this)
        {
            LocalInstance = null;
        }

        OnCoinsChanged = null;
    }

    private void HandleCoinsChanged(int oldVal, int newVal)
    {
        if (IsOwner)
        {
            OnCoinsChanged?.Invoke(newVal);
        }
    }

    /// <summary>
    /// Server-only: Add coins to this player.
    /// Sadece sunucu: Bu oyuncuya coin ekle.
    /// </summary>
    public void AddCoins(int amount)
    {
        if (!IsServer || amount <= 0) return;
        _coins.Value += amount;
        Debug.Log($"Player {OwnerClientId} earned {amount} coins. Total: {_coins.Value}");
    }

    /// <summary>
    /// Server-only: Spend coins if the player has enough.
    /// Sadece sunucu: Yeterli coin varsa harca.
    /// </summary>
    public bool SpendCoins(int amount)
    {
        if (!IsServer || amount < 0) return false;

        if (_coins.Value >= amount)
        {
            _coins.Value -= amount;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Requests the server to purchase a weapon.
    /// Sunucudan silah satın almayı ister.
    /// </summary>
    [ServerRpc]
    public void RequestPurchaseServerRpc(int weaponIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.HandleWeaponPurchase(OwnerClientId, weaponIndex);
        }
    }
}
