using TMPro;
using UnityEngine;

/// <summary>
/// Battle HUD showing base health and coin count.
/// Savaş arayüzü: Base canı ve coin sayısını gösterir.
/// Event-driven: UI sadece değer değiştiğinde güncellenir.
/// </summary>
public class BattleHUD : MonoBehaviour
{
    [Header("Base Health UI / Üs Can Göstergesi")]
    [SerializeField] private TextMeshProUGUI _baseHPLeftText;
    [SerializeField] private TextMeshProUGUI _baseHPRightText;

    [Header("Base References / Üs Referansları")]
    [SerializeField] private BaseHealth _baseLeft;
    [SerializeField] private BaseHealth _baseRight;

    [Header("Coin UI / Coin Göstergesi")]
    [SerializeField] private TextMeshProUGUI _coinText;

    private void OnEnable()
    {
        // Base sağlık olaylarını dinle
        if (_baseLeft != null)
        {
            _baseLeft.OnHealthChanged += OnBaseLeftHealthChanged;
            // Başlangıç değerini hemen göster
            UpdateBaseLeftText(_baseLeft.CurrentHealth, _baseLeft.MaxHealth);
        }

        if (_baseRight != null)
        {
            _baseRight.OnHealthChanged += OnBaseRightHealthChanged;
            UpdateBaseRightText(_baseRight.CurrentHealth, _baseRight.MaxHealth);
        }

        // Coin olayını dinle
        if (CoinManager.LocalInstance != null)
        {
            CoinManager.LocalInstance.OnCoinsChanged += OnCoinsChanged;
            UpdateCoinText(CoinManager.LocalInstance.Coins);
        }
    }

    private void OnDisable()
    {
        // Olay dinleyicilerini temizle
        if (_baseLeft != null)
        {
            _baseLeft.OnHealthChanged -= OnBaseLeftHealthChanged;
        }

        if (_baseRight != null)
        {
            _baseRight.OnHealthChanged -= OnBaseRightHealthChanged;
        }

        if (CoinManager.LocalInstance != null)
        {
            CoinManager.LocalInstance.OnCoinsChanged -= OnCoinsChanged;
        }
    }

    private void Update()
    {
        // CoinManager geç spawn olabilir, henüz bağlanmadıysa bağlan
        if (_coinText != null && CoinManager.LocalInstance != null)
        {
            // İlk bağlantıyı kontrol et
            CoinManager.LocalInstance.OnCoinsChanged -= OnCoinsChanged; // Çift kayıt engelle
            CoinManager.LocalInstance.OnCoinsChanged += OnCoinsChanged;
            UpdateCoinText(CoinManager.LocalInstance.Coins);
        }
    }

    private void OnBaseLeftHealthChanged(float current, float max)
    {
        UpdateBaseLeftText(current, max);
    }

    private void OnBaseRightHealthChanged(float current, float max)
    {
        UpdateBaseRightText(current, max);
    }

    private void OnCoinsChanged(int newCoins)
    {
        UpdateCoinText(newCoins);
    }

    private void UpdateBaseLeftText(float current, float max)
    {
        if (_baseHPLeftText != null)
        {
            _baseHPLeftText.text = $"Base HP: {current:F0}/{max:F0}";
        }
    }

    private void UpdateBaseRightText(float current, float max)
    {
        if (_baseHPRightText != null)
        {
            _baseHPRightText.text = $"Base HP: {current:F0}/{max:F0}";
        }
    }

    private void UpdateCoinText(int coins)
    {
        if (_coinText != null)
        {
            _coinText.text = $"Coins: {coins}";
        }
    }
}
