using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI entry for a single Miner type in the shop list.
/// Displays name, cost, output, owned count, and a buy button.
/// Refresh() is called every frame by UIController to keep the button state current.
/// </summary>
public class ShopItemUI : MonoBehaviour
{
    [Header("UI References (auto-assigned by Editor setup or manually)")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI costText;
    [SerializeField] private TextMeshProUGUI outputText;
    [SerializeField] private TextMeshProUGUI countText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Image iconImage;

    private MinerData _miner;

    public void Setup(MinerData miner)
    {
        _miner = miner;

        if (nameText != null)  nameText.text  = miner.minerName;
        if (iconImage != null && miner.icon != null) iconImage.sprite = miner.icon;
        if (buyButton != null) buyButton.onClick.AddListener(OnBuyClicked);

        Refresh();
    }

    /// <summary>Updates cost/count labels and button interactability.</summary>
    public void Refresh()
    {
        if (_miner == null || EconomyManager.Instance == null) return;

        int count = EconomyManager.Instance.GetMinerCount(_miner.minerName);
        double cost = _miner.GetCost(count);

        if (costText   != null) costText.text   = $"Cost: {UIController.FormatNumber(cost)}";
        if (outputText != null) outputText.text = $"+{UIController.FormatNumber(_miner.baseOutputPerSecond)}/s each";
        if (countText  != null) countText.text  = $"Owned: {count}";

        if (buyButton != null)
            buyButton.interactable = EconomyManager.Instance.Bubbles >= cost;
    }

    private void OnBuyClicked()
    {
        if (_miner == null) return;
        EconomyManager.Instance.TryBuyMiner(_miner);
        Refresh();
    }
}
