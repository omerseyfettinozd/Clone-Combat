using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Records player actions each FixedUpdate for ghost replay.
/// Her FixedUpdate'te oyuncunun hareketlerini hayalet tekrarı için kaydeder.
/// </summary>
public class GhostRecorder : NetworkBehaviour
{
    private List<GhostFrameData> _recordedFrames = new List<GhostFrameData>();
    private PlayerController _playerController;
    private WeaponController _weaponController;
    private bool _isRecording;

    private void Awake()
    {
        _playerController = GetComponent<PlayerController>();
        _weaponController = GetComponent<WeaponController>();
    }

    public override void OnNetworkSpawn()
    {
        // Kayıt artık GameManager veya sahne yüklendiğinde başlayacak.
        _isRecording = false;
        if (IsOwner)
        {
            _recordedFrames.Clear();
        }
    }

    public override void OnNetworkDespawn()
    {
        _isRecording = false;
        _recordedFrames.Clear();
    }

    private void Update()
    {
        if (!IsOwner) return;
        
        // BattleArena sahnesindeysek ve kayıt kapalıysa kaydı başlat (otomatik)
        if (!_isRecording && UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "BattleArena")
        {
            StartNewRecording();
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !_isRecording) return;
        if (_playerController == null || _weaponController == null) return;
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "BattleArena") return;

        // Her FixedUpdate'te oyuncunun girdilerini (input) kaydet
        GhostFrameData frame = new GhostFrameData
        {
            MoveInputX = _playerController.GetMoveInput().x,
            JumpPressed = _playerController.ConsumeJumpFlag(), // Sadece basıldığı kare true döner
            AimAngle = _playerController.GetAimAngle(),
            IsShooting = _weaponController.IsShooting()
        };

        _recordedFrames.Add(frame);
    }

    /// <summary>
    /// Returns all recorded frames.
    /// Tüm kaydedilmiş kareleri döndürür.
    /// </summary>
    public GhostFrameData[] GetRecordedFrames()
    {
        return _recordedFrames.ToArray();
    }

    /// <summary>
    /// Called by the Server when the player dies. The owner client will then send its frames back.
    /// Oyuncu öldüğünde Sunucu tarafından çağrılır. Sahip olan istemci (Client) karelerini geri gönderir.
    /// </summary>
    [ClientRpc]
    public void RequestFramesAndSpawnGhostClientRpc()
    {
        if (!IsOwner) return;

        // Kaydı durdur
        _isRecording = false;

        // Silah indeksini bul
        int weaponIndex = -1;
        if (_weaponController != null && _weaponController.CurrentWeapon != null && GameManager.Instance != null)
        {
            var weapons = GameManager.Instance.AvailableWeapons;
            for (int i = 0; i < weapons.Length; i++)
            {
                if (weapons[i] == _weaponController.CurrentWeapon)
                {
                    weaponIndex = i;
                    break;
                }
            }
        }

        // Sunucuya kareleri gönder
        SendFramesToServerRpc(GetRecordedFrames(), OwnerClientId, weaponIndex);
    }

    [ServerRpc]
    private void SendFramesToServerRpc(GhostFrameData[] frames, ulong ownerClientId, int weaponIndex)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.SpawnGhostFromServerRpc(frames, ownerClientId, weaponIndex);
        }
    }

    /// <summary>
    /// Starts fresh recording locally.
    /// Kaydedilmiş verileri temizler ve yerel olarak yeni kayda başlar.
    /// </summary>
    public void StartNewRecording()
    {
        if (!IsOwner) return;
        _recordedFrames.Clear();
        _isRecording = true;
    }

    /// <summary>
    /// Clears recorded data and starts fresh recording. Called by Server.
    /// Kaydedilmiş verileri temizler ve yeni kayda başlar. Server tarafından çağrılır.
    /// </summary>
    [ClientRpc]
    public void StartNewRecordingClientRpc()
    {
        StartNewRecording();
    }
}
