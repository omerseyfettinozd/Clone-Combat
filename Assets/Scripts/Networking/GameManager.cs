using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central game manager. Handles spawning, respawning, ghost creation, and game-over.
/// Merkezi oyun yöneticisi. Doğma, yeniden doğma, hayalet oluşturma ve oyun sonu yönetir.
/// </summary>
public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Prefabs / Prefab'lar")]
    [SerializeField] private GameObject _ghostPrefab;

    [Header("Spawn Points / Doğuş Noktaları")]
    [SerializeField] private Transform _spawnPointLeft;
    [SerializeField] private Transform _spawnPointRight;

    [Header("Bases / Üsler")]
    [SerializeField] private BaseHealth _baseLeft;
    [SerializeField] private BaseHealth _baseRight;

    [Header("Weapons / Silahlar")]
    [SerializeField] private WeaponData[] _availableWeapons; // 0=Pistol, 1=Assault, 2=Sniper
    
    public WeaponData[] AvailableWeapons => _availableWeapons;

    [Header("Economy / Ekonomi")]
    [SerializeField] private int _killReward = 25; // Öldürme başına coin ödülü

    [Header("Team Colors / Takım Renkleri")]
    [SerializeField] private Color _teamColorLeft = new Color(0.2f, 0.5f, 1f);  // Mavi (Team 0 - Host)
    [SerializeField] private Color _teamColorRight = new Color(1f, 0.3f, 0.3f); // Kırmızı (Team 1 - Client)

    // Tüm aktif hayaletlerin listesi
    private List<GameObject> _activeGhosts = new List<GameObject>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsServer)
        {
            // Arena sahnesine geçildiğinde tüm oyuncuları spawn noktalarına taşı
            PositionAllPlayersAtSpawnPoints();
        }
    }

    /// <summary>
    /// Moves all connected players to their team's spawn points.
    /// Tüm bağlı oyuncuları takımlarının spawn noktalarına taşır.
    /// </summary>
    private void PositionAllPlayersAtSpawnPoints()
    {
        foreach (var kvp in NetworkManager.Singleton.ConnectedClients)
        {
            ulong clientId = kvp.Key;
            NetworkObject playerObj = kvp.Value.PlayerObject;
            if (playerObj == null) continue;

            Transform spawnPoint = (clientId == NetworkManager.ServerClientId) ? _spawnPointLeft : _spawnPointRight;
            if (spawnPoint == null)
            {
                Debug.LogWarning($"Spawn point for client {clientId} not assigned!");
                continue;
            }

            // Sunucu tarafında pozisyonu ayarla
            playerObj.transform.position = spawnPoint.position;

            // ClientNetworkTransform owner-authoritative olduğu için,
            // client'a da pozisyonunu güncelle demek lazım
            TeleportPlayerClientRpc(spawnPoint.position, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });

            // Takım rengini tüm client'larda ayarla
            Color teamColor = (clientId == NetworkManager.ServerClientId) ? _teamColorLeft : _teamColorRight;
            SetTeamColorClientRpc(playerObj, teamColor.r, teamColor.g, teamColor.b);
        }
    }

    /// <summary>
    /// Sets the team color on all clients for a player.
    /// Bir oyuncunun takım rengini tüm client'larda ayarlar.
    /// </summary>
    [ClientRpc]
    private void SetTeamColorClientRpc(NetworkObjectReference playerRef, float r, float g, float b)
    {
        if (playerRef.TryGet(out NetworkObject playerObj))
        {
            Color teamColor = new Color(r, g, b);
            SpriteRenderer[] srs = playerObj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                sr.color = teamColor;
            }
        }
    }

    /// <summary>
    /// Returns the team color for the given client.
    /// Verilen client için takım rengini döndürür.
    /// </summary>
    public Color GetTeamColor(ulong clientId)
    {
        return (clientId == NetworkManager.ServerClientId) ? _teamColorLeft : _teamColorRight;
    }

    [ClientRpc]
    private void TeleportPlayerClientRpc(Vector3 position, ClientRpcParams rpcParams = default)
    {
        // Kendi oyuncumuzu spawn noktasına taşı
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
        {
            localPlayer.transform.position = position;

            // Rigidbody hızını sıfırla (lobideki momentum taşınmasın)
            Rigidbody2D rb = localPlayer.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    public override void OnDestroy()
    {
        // Singleton temizliği
        if (Instance == this)
        {
            Instance = null;
        }
    }

    /// <summary>
    /// Called when a player dies. Creates ghost and respawns player.
    /// Oyuncu öldüğünde çağrılır. Hayalet oluşturur ve oyuncuyu yeniden doğurur.
    /// </summary>
    public void OnPlayerDied(ulong deadPlayerId, ulong killerClientId)
    {
        if (!IsServer) return;

        // Öldüren oyuncuya coin ver
        RewardKiller(killerClientId);

        // Ölen oyuncunun kayıtlı verilerini al ve hayalet oluştur
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(deadPlayerId))
        {
            Debug.LogWarning($"Player {deadPlayerId} disconnected before death could be processed.");
            return;
        }

        NetworkObject deadPlayerNetObj = NetworkManager.Singleton.ConnectedClients[deadPlayerId].PlayerObject;
        if (deadPlayerNetObj == null) return;

        // --- DEĞİŞİKLİK: Kayıt verileri SADECE İlgili Client'ta (Owner) bulunur. ---
        // Bu yüzden ClientRpc ile verileri istemeli ve ardından ServerRpc ile hayaleti oluşturmalıyız.
        GhostRecorder recorder = deadPlayerNetObj.GetComponent<GhostRecorder>();
        if (recorder != null)
        {
            // Client'tan verileri toplayıp ServerRpc ile dönmesini isteyeceğiz.
            recorder.RequestFramesAndSpawnGhostClientRpc();
        }

        // Oyuncuyu TÜM client'larda gizle (öldüren taraf da görsün ki öldü)
        HidePlayerClientRpc(deadPlayerNetObj);

        // Ölüm mağazasını göster (sadece ölen oyuncuya)
        ShowDeathShopClientRpc(new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { deadPlayerId }
            }
        });
    }

    /// <summary>
    /// ServerRpc called by GhostRecorder on the client to send the recorded frames to the server.
    /// İstemcideki GhostRecorder tarafından kaydedilen veri sunucuya gönderilir, sunucu burada Ghost oluşturur.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void SpawnGhostFromServerRpc(GhostFrameData[] frames, ulong ownerClientId, int weaponIndex)
    {
        if (!IsServer) return;
        
        WeaponData weaponData = null;
        if (weaponIndex >= 0 && weaponIndex < _availableWeapons.Length)
        {
            weaponData = _availableWeapons[weaponIndex];
        }

        Debug.Log($"[GameManager] Player {ownerClientId} died. Received recorded frames: {frames.Length}");

        if (frames.Length > 0)
        {
            SpawnGhost(frames, ownerClientId, weaponData);
        }
    }

    /// <summary>
    /// Spawns a ghost with the recorded frame data.
    /// Kaydedilmiş kare verileriyle bir hayalet oluşturur.
    /// </summary>
    private void SpawnGhost(GhostFrameData[] frames, ulong ownerClientId, WeaponData weaponData)
    {
        if (_ghostPrefab == null || frames == null || frames.Length == 0) return;

        // Ghost'un başlatılacağı (ve döneceği) spawn noktasını belirle
        Transform teamSpawnPoint = (ownerClientId == NetworkManager.ServerClientId) ? _spawnPointLeft : _spawnPointRight;
        if (teamSpawnPoint == null) return;

        Vector3 spawnPos = teamSpawnPoint.position;
        GameObject ghostObj = Instantiate(_ghostPrefab, spawnPos, Quaternion.identity);

        NetworkObject netObj = ghostObj.GetComponent<NetworkObject>();
        if (netObj == null)
        {
            Debug.LogError("Ghost prefab'ında NetworkObject bulunamadı!");
            Destroy(ghostObj);
            return;
        }
        netObj.Spawn();

        _activeGhosts.Add(ghostObj);
        Debug.Log($"[GameManager] SpawnGhost executed! Total active ghosts: {_activeGhosts.Count}");

        GhostPlayback playback = ghostObj.GetComponent<GhostPlayback>();
        if (playback != null)
        {
            playback.Initialize(frames, weaponData, ownerClientId, spawnPos);
        }
    }

    /// <summary>
    /// Respawns the player at their team's spawn point.
    /// Oyuncuyu takımının doğuş noktasında yeniden doğurur.
    /// </summary>
    private void RespawnPlayer(ulong clientId, NetworkObject playerNetObj)
    {
        if (playerNetObj == null) return;

        // Host (clientId 0) sol tarafta, Client sağ tarafta doğar
        Transform spawnPoint = (clientId == NetworkManager.ServerClientId) ? _spawnPointLeft : _spawnPointRight;
        if (spawnPoint == null)
        {
            Debug.LogWarning($"Spawn point for client {clientId} not assigned!");
            return;
        }

        playerNetObj.transform.position = spawnPoint.position;

        // ClientNetworkTransform owner-authoritative olduğu için client'a da bildir
        TeleportPlayerClientRpc(spawnPoint.position, new ClientRpcParams
        {
            Send = new ClientRpcSendParams
            {
                TargetClientIds = new[] { clientId }
            }
        });

        // Takım rengini yeniden uygula (respawn sonrası renk kaybolmamasını sağla)
        Color teamColor = GetTeamColor(clientId);
        SetTeamColorClientRpc(playerNetObj, teamColor.r, teamColor.g, teamColor.b);

        // Sağlığı sıfırla
        HealthSystem health = playerNetObj.GetComponent<HealthSystem>();
        if (health != null)
        {
            health.ResetHealth();
        }

        // Yeni hayalet kaydını başlat (Sahibi olan Client üzerinde)
        GhostRecorder recorder = playerNetObj.GetComponent<GhostRecorder>();
        if (recorder != null)
        {
            recorder.StartNewRecordingClientRpc();
        }
    }

    /// <summary>
    /// Rewards the killer with coins.
    /// Öldüren oyuncuya coin ödülü verir.
    /// </summary>
    private void RewardKiller(ulong killerClientId)
    {
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(killerClientId)) return;

        NetworkObject killerObj = NetworkManager.Singleton.ConnectedClients[killerClientId].PlayerObject;
        if (killerObj == null) return;

        CoinManager coinManager = killerObj.GetComponent<CoinManager>();
        if (coinManager != null)
        {
            coinManager.AddCoins(_killReward);
        }
    }

    /// <summary>
    /// Handles weapon purchase request from a player.
    /// Oyuncudan gelen silah satın alma isteğini işler.
    /// </summary>
    public void HandleWeaponPurchase(ulong clientId, int weaponIndex)
    {
        if (!IsServer) return;
        if (_availableWeapons == null || weaponIndex < 0 || weaponIndex >= _availableWeapons.Length) return;

        WeaponData weapon = _availableWeapons[weaponIndex];
        if (weapon == null) return;

        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        NetworkObject playerObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerObj == null) return;

        CoinManager coinManager = playerObj.GetComponent<CoinManager>();
        WeaponController weaponCtrl = playerObj.GetComponent<WeaponController>();

        if (coinManager == null || weaponCtrl == null) return;

        if (weapon.cost == 0 || coinManager.SpendCoins(weapon.cost))
        {
            weaponCtrl.SetWeapon(weapon);
            
            // Client tarafında da silahı güncelle (host için zaten aynı makinede)
            weaponCtrl.SetWeaponClientRpc(weaponIndex, new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });
            
            Debug.Log($"Player {clientId} purchased {weapon.weaponName}!");

            // Satın alma başarılı, şimdi respawn et
            RespawnPlayer(clientId, playerObj);

            // Oyuncuyu tüm client'larda tekrar görünür yap
            ShowPlayerClientRpc(playerObj);

            // Ölüm mağazasını kapat (satın alma başarılı oldu)
            HideDeathShopClientRpc(new ClientRpcParams
            {
                Send = new ClientRpcSendParams
                {
                    TargetClientIds = new[] { clientId }
                }
            });
        }
        else
        {
            Debug.Log($"Player {clientId} can't afford {weapon.weaponName}!");
        }
    }

    [ClientRpc]
    private void ShowDeathShopClientRpc(ClientRpcParams rpcParams = default)
    {
        DeathShopUI shopUI = FindFirstObjectByType<DeathShopUI>();
        if (shopUI != null)
        {
            shopUI.ShowShop();
        }
    }

    [ClientRpc]
    private void HidePlayerClientRpc(NetworkObjectReference playerRef)
    {
        // Ölen oyuncunun karakterini TÜM client'larda gizle
        if (playerRef.TryGet(out NetworkObject playerObj))
        {
            // Sprite'ları gizle
            SpriteRenderer[] srs = playerObj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                sr.enabled = false;
            }

            // Collider'ları kapat
            Collider2D[] cols = playerObj.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in cols)
            {
                col.enabled = false;
            }

            // Rigidbody'yi dondur (yerçekimiyle düşmesin, kamera takip etmesin)
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.linearVelocity = Vector2.zero;
                rb.bodyType = RigidbodyType2D.Kinematic;
            }
        }
    }

    /// <summary>
    /// Called by DeathShopUI after weapon selection to respawn.
    /// DeathShopUI silah seçimi sonrası oyuncuyu yeniden doğurmak için çağırır.
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void RequestRespawnServerRpc(ServerRpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;
        if (!NetworkManager.Singleton.ConnectedClients.ContainsKey(clientId)) return;

        NetworkObject playerNetObj = NetworkManager.Singleton.ConnectedClients[clientId].PlayerObject;
        if (playerNetObj == null) return;

        RespawnPlayer(clientId, playerNetObj);

        // Oyuncuyu tüm client'larda tekrar görünür yap
        ShowPlayerClientRpc(playerNetObj);
    }

    [ClientRpc]
    private void ShowPlayerClientRpc(NetworkObjectReference playerRef)
    {
        // Respawn olan oyuncunun görsellerini TÜM client'larda aç
        if (playerRef.TryGet(out NetworkObject playerObj))
        {
            SpriteRenderer[] srs = playerObj.GetComponentsInChildren<SpriteRenderer>(true);
            foreach (var sr in srs)
            {
                sr.enabled = true;
            }

            Collider2D[] cols = playerObj.GetComponentsInChildren<Collider2D>(true);
            foreach (var col in cols)
            {
                col.enabled = true;
            }

            // Rigidbody'yi geri aç (owner için Dynamic, diğerleri için Kinematic)
            Rigidbody2D rb = playerObj.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                bool isLocalPlayer = playerObj.IsOwner;
                rb.bodyType = isLocalPlayer ? RigidbodyType2D.Dynamic : RigidbodyType2D.Kinematic;
                rb.linearVelocity = Vector2.zero;
            }
        }
    }

    [ClientRpc]
    private void HideDeathShopClientRpc(ClientRpcParams rpcParams = default)
    {
        DeathShopUI shopUI = FindFirstObjectByType<DeathShopUI>();
        if (shopUI != null)
        {
            shopUI.HideShop();
        }
    }

    /// <summary>
    /// Called when a base is destroyed. Ends the game.
    /// Base yıkıldığında çağrılır. Oyunu bitirir.
    /// </summary>
    public void OnBaseDestroyed(int winnerTeam)
    {
        if (!IsServer) return;

        Debug.Log($"GAME OVER! Winner: Team {winnerTeam}");
        AnnounceWinnerClientRpc(winnerTeam);
    }

    [ClientRpc]
    private void AnnounceWinnerClientRpc(int winnerTeam)
    {
        Debug.Log($"Team {winnerTeam} WINS!");
        // TODO: Oyun sonu ekranı göster
    }
}
