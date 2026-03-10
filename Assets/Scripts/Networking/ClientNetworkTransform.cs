using Unity.Netcode.Components;
using UnityEngine;

/// <summary>
/// Owner-authoritative NetworkTransform with optimized interpolation settings.
/// Optimize edilmiş interpolasyon ayarlarıyla sahip-yetkili NetworkTransform.
/// </summary>
[DisallowMultipleComponent]
public class ClientNetworkTransform : NetworkTransform
{
    protected override bool OnIsServerAuthoritative()
    {
        return false; // Client (sahip) yetkili, sunucu değil
    }
}
