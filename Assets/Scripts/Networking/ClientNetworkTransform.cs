using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Owner-authoritative NetworkTransform. Allows the owning client to move their character.
/// Sahip-yetkili NetworkTransform. Sahip olan client'ın kendi karakterini hareket ettirmesini sağlar.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client (sahip) yetkili, sunucu değil
    }
}
