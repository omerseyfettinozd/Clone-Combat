using UnityEngine;

/// <summary>
/// ScriptableObject that defines weapon stats for each soldier class.
/// Silah istatistiklerini tutan ScriptableObject.
/// </summary>
[CreateAssetMenu(fileName = "NewWeapon", menuName = "CloneCombat/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity / Kimlik")]
    public string weaponName;          // Silah adı
    public Sprite weaponSprite;        // Silah görseli
    public Sprite bulletSprite;        // Mermi görseli

    [Header("Stats / İstatistikler")]
    [Min(0f)] public float damage = 10f;         // Mermi başına hasar
    [Min(0.1f)] public float bulletSpeed = 15f;  // Mermi hızı
    [Min(0.01f)] public float fireRate = 0.5f;   // İki atış arası bekleme süresi (saniye)
    [Min(0.1f)] public float bulletLifetime = 3f; // Merminin yok olmadan önce yaşam süresi

    [Header("Economy / Ekonomi")]
    [Min(0)] public int cost = 0;                // Bu silahı almak için gereken coin miktarı

    [Header("Visuals / Görseller")]
    public Color bulletColor = Color.yellow;     // Mermi rengi
}
