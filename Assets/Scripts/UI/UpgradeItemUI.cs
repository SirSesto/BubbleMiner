using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI entry for a single Upgrade in the upgrade panel.
/// Grays out and shows a checkmark once the upgrade has been purchased.
/// </summary>
public class UpgradeItemUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image iconImage;

    private UpgradeData _upgrade;
    private string _baseName;

    public void Setup(UpgradeData upgrade)
    {
        _upgrade  = upgrade;
        _baseName = upgrade.upgradeName;

        if (nameText        != null) nameText.text        = upgrade.upgradeName;
        if (descriptionText != null) descriptionText.text = upgrade.description;
        if (costText        != null) costText.text        = $"Cost: {UIController.FormatNumber(upgrade.cost)}";
        if (iconImage       != null && upgrade.icon != null) iconImage.sprite = upgrade.icon;
        if (buyButton       != null) buyButton.onClick.AddListener(OnBuyClicked);

        Refresh();
    }

    /// <summary>Updates button state based on current balance and purchase status.</summary>
    public void Refresh()
    {
        if (_upgrade == null || EconomyManager.Instance == null) return;

        bool purchased = EconomyManager.Instance.IsUpgradePurchased(_upgrade.upgradeId);
        bool canAfford = EconomyManager.Instance.Bubbles >= _upgrade.cost;

        if (buyButton != null)
            buyButton.interactable = !purchased && canAfford;

        if (nameText != null)
            nameText.text = purchased ? $"✓ {_baseName}" : _baseName;
    }

    private void OnBuyClicked()
    {
        if (_upgrade == null) return;
        if (EconomyManager.Instance.TryBuyUpgrade(_upgrade))
            Refresh();
    }
}
