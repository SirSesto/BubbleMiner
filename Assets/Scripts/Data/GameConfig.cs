using UnityEngine;

/// <summary>
/// Central configuration asset that holds all Miner and Upgrade definitions,
/// as well as global gameplay settings.
/// </summary>
[CreateAssetMenu(fileName = "GameConfig", menuName = "BubbleMiner/Game Config", order = 0)]
public class GameConfig : ScriptableObject
{
    [Header("Currency")]
    public string currencyName = "Bubbles";

    [Header("Miners")]
    [Tooltip("All available miner types, shown in the shop in order.")]
    public MinerData[] miners;

    [Header("Upgrades")]
    [Tooltip("All available upgrades, shown in the upgrade panel in order.")]
    public UpgradeData[] upgrades;

    [Header("Offline Earnings")]
    [Tooltip("Maximum hours of offline earnings the player can collect on login.")]
    public double offlineEarningsCapHours = 8.0;

    [Header("Autosave")]
    [Tooltip("How often (in seconds) the game autosaves.")]
    public float autosaveIntervalSeconds = 60f;
}
