using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Manages Lobby, Relay, and Ready state. DontDestroyOnLoad singleton.
/// Lobi, Relay ve Hazır durumunu yönetir.
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    private const string KEY_RELAY_JOIN_CODE = "RelayJoinCode";
    private const string KEY_PLAYER_READY = "PlayerReady";

    private Lobby _currentLobby;
    private float _heartbeatTimer = 15f;  // İlk frame'de hemen göndermemesi için başlangıç değeri
    private float _pollTimer = 3f;        // İlk frame'de hemen poll yapmaması için başlangıç değeri
    private bool _isReady;
    private bool _gameStarting;
    private bool _isAuthenticated;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private async void Start()
    {
        // Input lag'ı azaltmak için hedef FPS ayarla
        Application.targetFrameRate = 120;

        await Authenticate();
    }

    private void Update()
    {
        if (!_isAuthenticated || _currentLobby == null) return;

        HandleLobbyHeartbeat();
        HandleLobbyPolling();
        CheckGameStart();
    }

    // --- AUTHENTICATION ---
    private async Task Authenticate()
    {
        try
        {
            // Aynı makinede birden fazla instance için farklı profil oluştur
            // Multiplayer Play Mode'da her instance farklı dataPath'e sahiptir
            string profile = "Player" + Mathf.Abs(Application.dataPath.GetHashCode()) % 100000;

            var initOptions = new InitializationOptions();
            initOptions.SetProfile(profile);
            await UnityServices.InitializeAsync(initOptions);
            
            AuthenticationService.Instance.SignedIn += () =>
            {
                Debug.Log("Signed in as " + AuthenticationService.Instance.PlayerId + " (Profile: " + profile + ")");
            };

            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            _isAuthenticated = true;
        }
        catch (System.Exception e)
        {
            Debug.LogError("Authentication failed: " + e.Message);
        }
    }

    // --- LOBBY OPERATIONS ---
    public async void CreateLobby(string lobbyName, int maxPlayers)
    {
        if (!_isAuthenticated)
        {
            Debug.LogWarning("Cannot create lobby: not authenticated yet.");
            return;
        }

        try
        {
            // Önce Relay'i oluştur ve kodu al
            string relayJoinCode = await CreateRelay(maxPlayers);
            if (string.IsNullOrEmpty(relayJoinCode))
            {
                Debug.LogError("Failed to create Relay allocation.");
                return;
            }

            CreateLobbyOptions options = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = CreatePlayerData(false),
                Data = new Dictionary<string, DataObject>
                {
                    { KEY_RELAY_JOIN_CODE, new DataObject(DataObject.VisibilityOptions.Public, relayJoinCode) }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyName, maxPlayers, options);
            _currentLobby = lobby;
            Debug.Log("Created Lobby! Code: " + lobby.LobbyCode + " | Relay: " + relayJoinCode);
            
            // Host olarak başla
            NetworkManager.Singleton.StartHost();
        }
        catch (System.Exception e)
        {
            Debug.LogError("CreateLobby failed: " + e.Message);
        }
    }

    public async void JoinLobbyByCode(string lobbyCode)
    {
        if (!_isAuthenticated)
        {
            Debug.LogWarning("Cannot join lobby: not authenticated yet.");
            return;
        }

        if (string.IsNullOrEmpty(lobbyCode))
        {
            Debug.LogWarning("Lobby code is empty!");
            return;
        }

        try
        {
            JoinLobbyByCodeOptions options = new JoinLobbyByCodeOptions
            {
                Player = CreatePlayerData(false)
            };

            Lobby lobby = await LobbyService.Instance.JoinLobbyByCodeAsync(lobbyCode, options);
            _currentLobby = lobby;
            Debug.Log("Joined Lobby with code " + lobbyCode);

            if (!_currentLobby.Data.ContainsKey(KEY_RELAY_JOIN_CODE))
            {
                Debug.LogError("Lobby data doesn't contain Relay join code!");
                return;
            }

            string relayJoinCode = _currentLobby.Data[KEY_RELAY_JOIN_CODE].Value;
            Debug.Log("Relay Join Code from lobby: " + relayJoinCode);

            if (!string.IsNullOrEmpty(relayJoinCode) && relayJoinCode != "0")
            {
                await JoinRelay(relayJoinCode);
            }
            else
            {
                Debug.LogWarning("Relay Join Code is invalid! Host hasn't set it yet.");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("JoinLobby failed: " + e.Message);
        }
    }

    public async void LeaveLobby()
    {
        try
        {
            if (_currentLobby != null)
            {
                string playerId = AuthenticationService.Instance.PlayerId;
                
                if (_currentLobby.HostId == playerId)
                {
                    await LobbyService.Instance.DeleteLobbyAsync(_currentLobby.Id);
                }
                else
                {
                    await LobbyService.Instance.RemovePlayerAsync(_currentLobby.Id, playerId);
                }

                _currentLobby = null;
                _isReady = false;
                _gameStarting = false;

                if (NetworkManager.Singleton != null)
                {
                    NetworkManager.Singleton.Shutdown();
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError("LeaveLobby failed: " + e.Message);
            // Hata olsa bile yerel durumu temizle
            _currentLobby = null;
            _isReady = false;
            _gameStarting = false;
        }
    }

    // --- READY SYSTEM ---
    public async void SetReady(bool ready)
    {
        if (_currentLobby == null) return;

        try
        {
            _isReady = ready;

            UpdatePlayerOptions updateOptions = new UpdatePlayerOptions
            {
                Data = new Dictionary<string, PlayerDataObject>
                {
                    { KEY_PLAYER_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, ready.ToString()) }
                }
            };

            string playerId = AuthenticationService.Instance.PlayerId;
            _currentLobby = await LobbyService.Instance.UpdatePlayerAsync(_currentLobby.Id, playerId, updateOptions);
        }
        catch (System.Exception e)
        {
            Debug.LogError("SetReady failed: " + e.Message);
        }
    }

    public bool IsInLobby()
    {
        return _currentLobby != null;
    }

    public bool IsHostReady()
    {
        if (_currentLobby == null) return false;
        foreach (var player in _currentLobby.Players)
        {
            if (player.Id == _currentLobby.HostId)
            {
                return player.Data != null && 
                       player.Data.ContainsKey(KEY_PLAYER_READY) && 
                       player.Data[KEY_PLAYER_READY].Value == "True";
            }
        }
        return false;
    }

    public bool IsClientReady()
    {
        if (_currentLobby == null) return false;
        foreach (var player in _currentLobby.Players)
        {
            if (player.Id != _currentLobby.HostId)
            {
                return player.Data != null && 
                       player.Data.ContainsKey(KEY_PLAYER_READY) && 
                       player.Data[KEY_PLAYER_READY].Value == "True";
            }
        }
        return false;
    }

    private void CheckGameStart()
    {
        if (_currentLobby == null || _gameStarting) return;
        if (!_isAuthenticated) return;
        if (_currentLobby.HostId != AuthenticationService.Instance.PlayerId) return;

        // Host tarafı: 2 kişi hazırsa oyunu başlat
        if (_currentLobby.Players.Count == 2 && IsHostReady() && IsClientReady())
        {
            _gameStarting = true;
            Debug.Log("Both players ready! Initiating game start sequence...");
            StartCoroutine(DelayedGameStart());
        }
    }

    private System.Collections.IEnumerator DelayedGameStart()
    {
        // Client'ın Relay bağlantısını tam olarak kurması için kısa bir süre bekle
        yield return new WaitForSeconds(1.5f);
        
        if (NetworkManager.Singleton != null)
        {
            Debug.Log("Loading BattleArena...");
            NetworkManager.Singleton.SceneManager.LoadScene("BattleArena", UnityEngine.SceneManagement.LoadSceneMode.Single);
        }
        else
        {
            Debug.LogError("NetworkManager not found! Cannot load scene.");
            _gameStarting = false;
        }
    }

    // --- RELAY ---
    private async Task<string> CreateRelay(int maxConnections)
    {
        try
        {
            Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections - 1);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log("Relay Join Code: " + joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                allocation.RelayServer.IpV4,
                (ushort)allocation.RelayServer.Port,
                allocation.AllocationIdBytes,
                allocation.Key,
                allocation.ConnectionData
            );

            return joinCode;
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("CreateRelay failed: " + e.Message);
            return null;
        }
    }

    private async Task JoinRelay(string joinCode)
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            Debug.Log("Joined Relay with Join Code: " + joinCode);

            NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(
                joinAllocation.RelayServer.IpV4,
                (ushort)joinAllocation.RelayServer.Port,
                joinAllocation.AllocationIdBytes,
                joinAllocation.Key,
                joinAllocation.ConnectionData,
                joinAllocation.HostConnectionData
            );

            NetworkManager.Singleton.StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError("JoinRelay failed: " + e.Message);
        }
    }

    // --- KEEP ALIVE ---
    private async void HandleLobbyHeartbeat()
    {
        if (_currentLobby == null) return;
        if (_currentLobby.HostId != AuthenticationService.Instance.PlayerId) return;

        _heartbeatTimer -= Time.deltaTime;
        if (_heartbeatTimer < 0f)
        {
            _heartbeatTimer = 15f;
            try
            {
                await LobbyService.Instance.SendHeartbeatPingAsync(_currentLobby.Id);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Heartbeat failed: " + e.Message);
            }
        }
    }

    private async void HandleLobbyPolling()
    {
        if (_currentLobby == null) return;

        _pollTimer -= Time.deltaTime;
        if (_pollTimer < 0f)
        {
            _pollTimer = 3f;
            try
            {
                _currentLobby = await LobbyService.Instance.GetLobbyAsync(_currentLobby.Id);
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("Lobby poll failed: " + e.Message);
                // Lobi artık yoksa temizle
                _currentLobby = null;
            }
        }
    }

    // --- HELPERS ---
    private Player CreatePlayerData(bool ready)
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
            {
                { KEY_PLAYER_READY, new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, ready.ToString()) }
            }
        };
    }

    public Lobby GetCurrentLobby()
    {
        return _currentLobby;
    }
}
