using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Data structure for one frame of ghost replay data.
/// Hayalet tekrarı için bir karenin veri yapısı.
/// </summary>
[System.Serializable]
public struct GhostFrameData : INetworkSerializable
{
    public float MoveInputX;    // -1, 0, 1 (Yatay hareket girdisi)
    public bool JumpPressed;    // Zıplama tuşuna basıldı mı?
    public float AimAngle;      // Nişan açısı (derece)
    public bool IsShooting;     // Ateş ediliyor mu?

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MoveInputX);
        serializer.SerializeValue(ref JumpPressed);
        serializer.SerializeValue(ref AimAngle);
        serializer.SerializeValue(ref IsShooting);
    }
}
