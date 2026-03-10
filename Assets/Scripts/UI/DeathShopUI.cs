using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Death shop UI. Opens when player dies to let them pick a weapon class.
/// Ölüm mağazası arayüzü. Oyuncu öldüğünde silah sınıfı seçtirmek için açılır.
/// </summary>
public class DeathShopUI : MonoBehaviour
{
    [Header("Buttons / Butonlar")]
    [SerializeField] private Button _pistolBtn;
    [SerializeField] private Button _assaultBtn;
    [SerializeField] private Button _sniperBtn;

    [Header("Price Texts / Fiyat Metinleri")]
    [SerializeField] private TextMeshProUGUI _pistolPriceText;
    [SerializeField] private TextMeshProUGUI _assaultPriceText;
    [SerializeField] private TextMeshProUGUI _sniperPriceText;

    [Header("Coin Display / Coin Gösterimi")]
    [SerializeField] private TextMeshProUGUI _coinText;

    [Header("Weapon Costs / Silah Fiyatları")]
    [SerializeField] private int _pistolCost = 0;
    [SerializeField] private int _assaultCost = 50;
    [SerializeField] private int _sniperCost = 100;

    [Header("Panel")]
    [SerializeField] private GameObject _shopPanel;

    private void Awake()
    {
        if (_pistolBtn != null) _pistolBtn.onClick.AddListener(() => PurchaseWeapon(0));
        if (_assaultBtn != null) _assaultBtn.onClick.AddListener(() => PurchaseWeapon(1));
        if (_sniperBtn != null) _sniperBtn.onClick.AddListener(() => PurchaseWeapon(2));

        if (_shopPanel != null) _shopPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // Button listener temizliği
        if (_pistolBtn != null) _pistolBtn.onClick.RemoveAllListeners();
        if (_assaultBtn != null) _assaultBtn.onClick.RemoveAllListeners();
        if (_sniperBtn != null) _sniperBtn.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// Shows the shop panel, updates prices and button states.
    /// Mağaza panelini gösterir, fiyatları ve buton durumlarını günceller.
    /// </summary>
    public void ShowShop()
    {
        if (_shopPanel == null) return;
        _shopPanel.SetActive(true);

        int currentCoins = 0;
        if (CoinManager.LocalInstance != null)
        {
            currentCoins = CoinManager.LocalInstance.Coins;
        }

        // Coin miktarını göster
        if (_coinText != null)
        {
            _coinText.text = $"Coins: {currentCoins}";
        }

        // Fiyat metinlerini ayarla
        if (_pistolPriceText != null)
            _pistolPriceText.text = _pistolCost == 0 ? "FREE" : $"{_pistolCost} Coin";
        if (_assaultPriceText != null)
            _assaultPriceText.text = $"{_assaultCost} Coin";
        if (_sniperPriceText != null)
            _sniperPriceText.text = $"{_sniperCost} Coin";

        // Parası yetmeyenlerin butonlarını kapat
        if (_pistolBtn != null)
            _pistolBtn.interactable = _pistolCost == 0 || currentCoins >= _pistolCost;
        if (_assaultBtn != null)
            _assaultBtn.interactable = currentCoins >= _assaultCost;
        if (_sniperBtn != null)
            _sniperBtn.interactable = currentCoins >= _sniperCost;
    }

    /// <summary>
    /// Sends purchase request to server. Respawn is now handled by GameManager after purchase.
    /// Satın alma isteğini sunucuya gönderir. Respawn artık satın alma sonrası GameManager tarafından yapılır.
    /// </summary>
    private void PurchaseWeapon(int weaponIndex)
    {
        if (CoinManager.LocalInstance != null)
        {
            CoinManager.LocalInstance.RequestPurchaseServerRpc(weaponIndex);
        }

        // NOT: HideShop artık burada çağrılmıyor!
        // Satın alma başarısız olursa shop açık kalmalı ki oyuncu başka silah seçebilsin.
        // Shop, GameManager.HandleWeaponPurchase başarılı olduktan sonra
        // ShowPlayerClientRpc ile birlikte kapatılacak.
    }

    public void HideShop()
    {
        if (_shopPanel != null)
        {
            _shopPanel.SetActive(false);
        }
    }
}
