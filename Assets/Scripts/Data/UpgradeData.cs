using UnityEngine;

/// <summary>
/// ScriptableObject defining an Upgrade.
/// Upgrades apply a multiplier either globally or to a specific miner type.
/// </summary>
[CreateAssetMenu(fileName = "UpgradeData", menuName = "BubbleMiner/Upgrade Data", order = 2)]
public class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("Unique identifier used in save data. Must be unique across all upgrades.")]
    public string upgradeId = "upgrade_001";
    public string upgradeName = "Bubble Boost";
    [TextArea(2, 4)]
    public string description = "Doubles the output of all miners.";
    public Sprite icon;

    [Header("Economy")]
    public double cost = 100.0;
    [Tooltip("Output multiplier applied when this upgrade is purchased.")]
    public double multiplier = 2.0;

    [Header("Target")]
    [Tooltip("If true, multiplier applies to ALL miners. If false, only to targetMiner.")]
    public bool isGlobal = true;
    [Tooltip("Only used when isGlobal is false.")]
    public MinerData targetMiner;
}
