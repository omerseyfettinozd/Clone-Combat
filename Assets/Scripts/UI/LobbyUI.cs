using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// In-game lobby UI for ready status display. Scene transition is handled by LobbyManager.
/// Oyun içi lobi arayüzü, hazır durumunu gösterir. Sahne geçişi LobbyManager tarafından yapılır.
/// </summary>
public class LobbyUI : NetworkBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI _lobbyCodeText;
    [SerializeField] private Button _readyBtn;
    [SerializeField] private TextMeshProUGUI _readyStatusText;

    private NetworkVariable<bool> _isHostReady = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> _isClientReady = new NetworkVariable<bool>(false);

    private bool _codeDisplayed;

    public override void OnNetworkSpawn()
    {
        // NetworkVariable değişikliklerini dinle (event-driven)
        _isHostReady.OnValueChanged += OnReadyStateChanged;
        _isClientReady.OnValueChanged += OnReadyStateChanged;
    }

    public override void OnNetworkDespawn()
    {
        _isHostReady.OnValueChanged -= OnReadyStateChanged;
        _isClientReady.OnValueChanged -= OnReadyStateChanged;
    }

    private void Start()
    {
        if (_readyBtn != null)
        {
            _readyBtn.onClick.AddListener(OnReadyButtonClicked);
        }
    }

    public override void OnDestroy()
    {
        if (_readyBtn != null)
        {
            _readyBtn.onClick.RemoveAllListeners();
        }
    }

    private void Update()
    {
        // Oda kodu henüz gösterilmediyse ve lobi varsa göster
        if (!_codeDisplayed && LobbyManager.Instance != null && LobbyManager.Instance.GetCurrentLobby() != null)
        {
            string code = LobbyManager.Instance.GetCurrentLobby().LobbyCode;
            if (!string.IsNullOrEmpty(code) && _lobbyCodeText != null)
            {
                _lobbyCodeText.text = "Code: " + code;
                _codeDisplayed = true;
                Debug.Log("Lobby Code displayed: " + code);
            }
        }
    }

    private void OnReadyButtonClicked()
    {
        SetReadyServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetReadyServerRpc(ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId)
        {
            _isHostReady.Value = !_isHostReady.Value; // Toggle
        }
        else
        {
            _isClientReady.Value = !_isClientReady.Value; // Toggle
        }
        
        UpdateReadyStatusClientRpc(_isHostReady.Value, _isClientReady.Value);
    }

    private void OnReadyStateChanged(bool oldValue, bool newValue)
    {
        // NetworkVariable değiştiğinde durumu güncelle
        UpdateReadyDisplay(_isHostReady.Value, _isClientReady.Value);
    }

    [ClientRpc]
    private void UpdateReadyStatusClientRpc(bool hostReady, bool clientReady)
    {
        UpdateReadyDisplay(hostReady, clientReady);
    }

    private void UpdateReadyDisplay(bool hostReady, bool clientReady)
    {
        if (_readyStatusText != null)
        {
            _readyStatusText.text = $"Host Ready: {hostReady} | Client Ready: {clientReady}";
        }
    }

    // NOT: Sahne yükleme buradan kaldırıldı!
    // Daha önce CheckAllReady() hem burada hem LobbyManager'da sahne yüklüyordu (çift yükleme hatası).
    // Sahne geçişi artık yalnızca LobbyManager.CheckGameStart() tarafından kontrol edilir.
}
