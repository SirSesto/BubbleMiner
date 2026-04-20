using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the game's economy: tracks Bubble balance, miner counts, purchased upgrades,
/// and calculates the current Bubbles-per-second rate.
/// Income is added each frame via <c>Time.deltaTime</c> for smooth accumulation.
/// </summary>
public class EconomyManager : MonoBehaviour
{
    public static EconomyManager Instance { get; private set; }

    // ── Public read-only state ──────────────────────────────────────────────
    public double Bubbles { get; private set; } = 0.0;
    public double BubblesPerSecond { get; private set; } = 0.0;

    // ── Private state ───────────────────────────────────────────────────────
    private GameConfig _config;
    private readonly Dictionary<string, int> _minerCounts = new Dictionary<string, int>();
    private readonly HashSet<string> _purchasedUpgrades = new HashSet<string>();

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

    private void Update()
    {
        if (_config == null) return;
        Bubbles += BubblesPerSecond * Time.deltaTime;
    }

    // ── Initialization ──────────────────────────────────────────────────────

    /// <summary>Must be called by <see cref="GameManager"/> after the config is available.</summary>
    public void Initialize(GameConfig config)
    {
        _config = config;
        RecalculateBPS();
    }

    // ── Economy calculations ────────────────────────────────────────────────

    /// <summary>
    /// Recomputes BubblesPerSecond from current miner counts and purchased upgrades.
    /// Call after any state change (buy miner, buy upgrade, load save).
    /// </summary>
    public void RecalculateBPS()
    {
        if (_config == null) return;

        double globalMultiplier = 1.0;
        double total = 0.0;

        // Collect global upgrade multipliers
        foreach (var upgrade in _config.upgrades)
        {
            if (upgrade == null || !_purchasedUpgrades.Contains(upgrade.upgradeId)) continue;
            if (upgrade.isGlobal)
                globalMultiplier *= upgrade.multiplier;
        }

        // Sum miner outputs, applying per-miner upgrades
        foreach (var miner in _config.miners)
        {
            if (miner == null) continue;
            int count = GetMinerCount(miner.minerName);
            if (count <= 0) continue;

            double output = miner.GetTotalOutput(count);

            // Apply targeted upgrades for this miner
            foreach (var upgrade in _config.upgrades)
            {
                if (upgrade == null || upgrade.isGlobal) continue;
                if (upgrade.targetMiner != miner) continue;
                if (!_purchasedUpgrades.Contains(upgrade.upgradeId)) continue;
                output *= upgrade.multiplier;
            }

            total += output;
        }

        BubblesPerSecond = total * globalMultiplier;
    }

    // ── Purchases ───────────────────────────────────────────────────────────

    /// <summary>
    /// Attempts to buy one unit of <paramref name="miner"/>.
    /// Returns <c>true</c> on success and deducts the cost from <see cref="Bubbles"/>.
    /// </summary>
    public bool TryBuyMiner(MinerData miner)
    {
        int current = GetMinerCount(miner.minerName);
        double cost = miner.GetCost(current);

        if (Bubbles < cost) return false;

        Bubbles -= cost;
        _minerCounts[miner.minerName] = current + 1;
        RecalculateBPS();
        return true;
    }

    /// <summary>
    /// Attempts to purchase <paramref name="upgrade"/>.
    /// Returns <c>false</c> if already owned or not enough Bubbles.
    /// </summary>
    public bool TryBuyUpgrade(UpgradeData upgrade)
    {
        if (_purchasedUpgrades.Contains(upgrade.upgradeId)) return false;
        if (Bubbles < upgrade.cost) return false;

        Bubbles -= upgrade.cost;
        _purchasedUpgrades.Add(upgrade.upgradeId);
        RecalculateBPS();
        return true;
    }

    // ── Queries ─────────────────────────────────────────────────────────────

    public bool IsUpgradePurchased(string upgradeId) =>
        _purchasedUpgrades.Contains(upgradeId);

    public int GetMinerCount(string minerName) =>
        _minerCounts.TryGetValue(minerName, out int count) ? count : 0;

    /// <summary>Directly adds Bubbles to the balance (used for offline earnings).</summary>
    public void AddBubbles(double amount) => Bubbles += amount;

    // ── Save / Load helpers ─────────────────────────────────────────────────

    /// <summary>Packages current state into a <see cref="SaveData"/> snapshot.</summary>
    public SaveData CreateSaveData()
    {
        var data = new SaveData
        {
            bubbles = Bubbles,
            offlineEarningsCapHours = _config != null ? _config.offlineEarningsCapHours : 8.0
        };

        foreach (var kvp in _minerCounts)
            data.miners.Add(new SaveData.MinerSaveEntry { minerName = kvp.Key, count = kvp.Value });

        foreach (var id in _purchasedUpgrades)
            data.purchasedUpgradeIds.Add(id);

        return data;
    }

    /// <summary>Restores state from a previously loaded <see cref="SaveData"/> snapshot.</summary>
    public void LoadFromSaveData(SaveData data)
    {
        if (data == null) return;

        Bubbles = data.bubbles;

        _minerCounts.Clear();
        foreach (var entry in data.miners)
            _minerCounts[entry.minerName] = entry.count;

        _purchasedUpgrades.Clear();
        foreach (var id in data.purchasedUpgradeIds)
            _purchasedUpgrades.Add(id);

        RecalculateBPS();
    }
}
