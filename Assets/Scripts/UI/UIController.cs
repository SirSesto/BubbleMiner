using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Main UI controller singleton.
/// Builds the shop and upgrade lists from the GameConfig at startup,
/// and refreshes stat displays every frame.
/// </summary>
public class UIController : MonoBehaviour
{
    public static UIController Instance { get; private set; }

    // ── Inspector references ────────────────────────────────────────────────

    [Header("Stats Panel")]
    [SerializeField] private TextMeshProUGUI balanceText;
    [SerializeField] private TextMeshProUGUI bpsText;

    [Header("Shop Panel")]
    [SerializeField] private Transform shopItemContainer;
    [SerializeField] private GameObject shopItemPrefab;

    [Header("Upgrades Panel")]
    [SerializeField] private Transform upgradeItemContainer;
    [SerializeField] private GameObject upgradeItemPrefab;

    [Header("Offline Earnings Popup")]
    [SerializeField] private GameObject offlineEarningsPanel;
    [SerializeField] private TextMeshProUGUI offlineEarningsText;
    [SerializeField] private Button offlineEarningsOkButton;

    // ── Private state ───────────────────────────────────────────────────────
    private GameConfig _config;

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    // ── Initialization ──────────────────────────────────────────────────────

    /// <summary>Called by <see cref="GameManager"/> after the economy is fully loaded.</summary>
    public void Initialize(GameConfig config)
    {
        _config = config;
        BuildShop();
        BuildUpgrades();

        if (offlineEarningsPanel != null)
            offlineEarningsPanel.SetActive(false);

        if (offlineEarningsOkButton != null)
            offlineEarningsOkButton.onClick.AddListener(() => offlineEarningsPanel.SetActive(false));
    }

    // ── Per-frame updates ───────────────────────────────────────────────────

    private void Update()
    {
        if (_config == null) return;
        UpdateStats();
        UpdateShopButtons();
        UpdateUpgradeButtons();
    }

    private void UpdateStats()
    {
        if (EconomyManager.Instance == null) return;
        if (balanceText != null)
            balanceText.text = $"{EconomyManager.Instance.Bubbles:F0} {_config.currencyName}";
        if (bpsText != null)
            bpsText.text = $"{FormatNumber(EconomyManager.Instance.BubblesPerSecond)}/sec";
    }

    private void UpdateShopButtons()
    {
        if (shopItemContainer == null) return;
        foreach (Transform child in shopItemContainer)
            child.GetComponent<ShopItemUI>()?.Refresh();
    }

    private void UpdateUpgradeButtons()
    {
        if (upgradeItemContainer == null) return;
        foreach (Transform child in upgradeItemContainer)
            child.GetComponent<UpgradeItemUI>()?.Refresh();
    }

    // ── UI building ─────────────────────────────────────────────────────────

    private void BuildShop()
    {
        if (_config == null || shopItemContainer == null || shopItemPrefab == null) return;
        foreach (Transform child in shopItemContainer) Destroy(child.gameObject);

        foreach (var miner in _config.miners)
        {
            if (miner == null) continue;
            var go   = Instantiate(shopItemPrefab, shopItemContainer);
            var item = go.GetComponent<ShopItemUI>();
            item?.Setup(miner);
        }
    }

    private void BuildUpgrades()
    {
        if (_config == null || upgradeItemContainer == null || upgradeItemPrefab == null) return;
        foreach (Transform child in upgradeItemContainer) Destroy(child.gameObject);

        foreach (var upgrade in _config.upgrades)
        {
            if (upgrade == null) continue;
            var go   = Instantiate(upgradeItemPrefab, upgradeItemContainer);
            var item = go.GetComponent<UpgradeItemUI>();
            item?.Setup(upgrade);
        }
    }

    // ── Offline earnings popup ──────────────────────────────────────────────

    /// <summary>Shows the offline earnings popup with the given amount.</summary>
    public void ShowOfflineEarnings(double amount)
    {
        if (offlineEarningsPanel == null) return;
        offlineEarningsPanel.SetActive(true);
        if (offlineEarningsText != null)
            offlineEarningsText.text =
                $"Welcome back!\nYou earned {FormatNumber(amount)} {(_config != null ? _config.currencyName : "Bubbles")} while away!";
    }

    // ── Number formatting ───────────────────────────────────────────────────

    /// <summary>
    /// Converts large numbers to human-readable shorthand (K, M, B, T, Q…).
    /// </summary>
    public static string FormatNumber(double value)
    {
        if (value >= 1e18) return $"{value / 1e18:F2}Qi";
        if (value >= 1e15) return $"{value / 1e15:F2}Q";
        if (value >= 1e12) return $"{value / 1e12:F2}T";
        if (value >= 1e9)  return $"{value / 1e9:F2}B";
        if (value >= 1e6)  return $"{value / 1e6:F2}M";
        if (value >= 1e3)  return $"{value / 1e3:F2}K";
        return $"{value:F1}";
    }
}
