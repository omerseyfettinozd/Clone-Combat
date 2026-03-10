using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Menu UI with two panels: Menu panel and Lobby panel.
/// Ana Menü: Menü paneli ve Lobi paneli olmak üzere iki panelden oluşur.
/// </summary>
public class MainMenuUI : MonoBehaviour
{
    [Header("Menu Panel / Menü Paneli")]
    [SerializeField] private GameObject _menuPanel;
    [SerializeField] private Button _createLobbyBtn;
    [SerializeField] private Button _joinLobbyBtn;
    [SerializeField] private TMP_InputField _lobbyCodeInput;

    [Header("Lobby Panel / Lobi Paneli")]
    [SerializeField] private GameObject _lobbyPanel;
    [SerializeField] private TextMeshProUGUI _lobbyCodeText;
    [SerializeField] private TextMeshProUGUI _readyStatusText;
    [SerializeField] private Button _readyBtn;
    [SerializeField] private Button _leaveLobbyBtn;

    private bool _isReady;
    private bool _inLobbyPanel;

    private void Awake()
    {
        // Başlangıçta menü paneli açık, lobi paneli kapalı
        if (_menuPanel != null) _menuPanel.SetActive(true);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);

        if (_createLobbyBtn != null) _createLobbyBtn.onClick.AddListener(OnCreateRoom);
        if (_joinLobbyBtn != null) _joinLobbyBtn.onClick.AddListener(OnJoinRoom);
        if (_readyBtn != null) _readyBtn.onClick.AddListener(OnReadyClicked);
        if (_leaveLobbyBtn != null) _leaveLobbyBtn.onClick.AddListener(OnLeaveLobby);
    }

    private void OnDestroy()
    {
        // Button listener temizliği
        if (_createLobbyBtn != null) _createLobbyBtn.onClick.RemoveAllListeners();
        if (_joinLobbyBtn != null) _joinLobbyBtn.onClick.RemoveAllListeners();
        if (_readyBtn != null) _readyBtn.onClick.RemoveAllListeners();
        if (_leaveLobbyBtn != null) _leaveLobbyBtn.onClick.RemoveAllListeners();
    }

    private void OnCreateRoom()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogWarning("LobbyManager not found!");
            return;
        }
        LobbyManager.Instance.CreateLobby("CloneCombatRoom", 2);
    }

    private void OnJoinRoom()
    {
        if (LobbyManager.Instance == null)
        {
            Debug.LogWarning("LobbyManager not found!");
            return;
        }

        if (_lobbyCodeInput != null && !string.IsNullOrEmpty(_lobbyCodeInput.text))
        {
            LobbyManager.Instance.JoinLobbyByCode(_lobbyCodeInput.text.Trim());
        }
        else
        {
            Debug.LogWarning("Please enter a Lobby Code first!");
        }
    }

    private void OnReadyClicked()
    {
        if (LobbyManager.Instance == null) return;

        _isReady = !_isReady;
        LobbyManager.Instance.SetReady(_isReady);

        if (_readyBtn != null)
        {
            TextMeshProUGUI btnText = _readyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null)
            {
                btnText.text = _isReady ? "CANCEL" : "READY";
            }
        }
    }

    private void OnLeaveLobby()
    {
        if (LobbyManager.Instance != null)
        {
            LobbyManager.Instance.LeaveLobby();
        }
        ShowMenuPanel();
    }

    private void Update()
    {
        if (LobbyManager.Instance == null) return;

        bool inLobby = LobbyManager.Instance.IsInLobby();

        // Lobi varsa ve panel hâlâ kapalıysa, lobi paneline geç
        if (inLobby && !_inLobbyPanel)
        {
            ShowLobbyPanel();
        }

        // Lobiden çıkıldıysa menü paneline dön
        if (!inLobby && _inLobbyPanel)
        {
            ShowMenuPanel();
        }

        // Lobi paneli açıkken bilgileri güncelle
        if (_inLobbyPanel && inLobby)
        {
            UpdateLobbyInfo();
        }
    }

    private void ShowLobbyPanel()
    {
        if (_menuPanel != null) _menuPanel.SetActive(false);
        if (_lobbyPanel != null) _lobbyPanel.SetActive(true);
        _inLobbyPanel = true;
        _isReady = false;

        if (_readyBtn != null)
        {
            TextMeshProUGUI btnText = _readyBtn.GetComponentInChildren<TextMeshProUGUI>();
            if (btnText != null) btnText.text = "READY";
        }
    }

    public void ShowMenuPanel()
    {
        if (_lobbyPanel != null) _lobbyPanel.SetActive(false);
        if (_menuPanel != null) _menuPanel.SetActive(true);
        _inLobbyPanel = false;
        _isReady = false;
    }

    private void UpdateLobbyInfo()
    {
        if (LobbyManager.Instance == null) return;

        var lobby = LobbyManager.Instance.GetCurrentLobby();
        if (lobby == null) return;

        if (_lobbyCodeText != null)
        {
            _lobbyCodeText.text = "Room Code: " + lobby.LobbyCode;
        }

        if (_readyStatusText != null)
        {
            int playerCount = lobby.Players.Count;
            bool hostReady = LobbyManager.Instance.IsHostReady();
            bool clientReady = LobbyManager.Instance.IsClientReady();

            _readyStatusText.text = $"Players: {playerCount}/2\nHost: {(hostReady ? "READY" : "---")} | Client: {(clientReady ? "READY" : "---")}";
        }
    }
}
