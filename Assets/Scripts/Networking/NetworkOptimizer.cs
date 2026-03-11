using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Configures network settings for smooth gameplay at startup.
/// Oyun başlangıcında pürüzsüz oyun için ağ ayarlarını yapılandırır.
/// NetworkManager objesine eklenir.
/// </summary>
public class NetworkOptimizer : MonoBehaviour
{
    [Header("Tick Rate / Güncelleme Hızı")]
    [SerializeField] private int _tickRate = 128; // CS:GO competitive = 128Hz

    private void Awake()
    {
        // Tick rate'i artır: Saniyede kaç kez ağ güncellemesi yapılacağını belirler
        // Varsayılan 30Hz → 60Hz (2x daha sık güncelleme, 2x daha az gecikme)
        NetworkManager.Singleton.NetworkConfig.TickRate = (uint)_tickRate;

        // Physics rate'i tick rate ile eşitle (fizik ve ağ senkronizasyonu)
        Time.fixedDeltaTime = 1f / _tickRate;

        Debug.Log($"[NetworkOptimizer] Tick Rate: {_tickRate}Hz | FixedDeltaTime: {Time.fixedDeltaTime:F4}s");
    }
}
