using System;
using System.Collections.Generic;

/// <summary>
/// Plain serializable data class that holds the complete game state for saving/loading.
/// Serialized to JSON via Unity's JsonUtility.
/// </summary>
[Serializable]
public class SaveData
{
    public double bubbles = 0.0;
    public List<MinerSaveEntry> miners = new List<MinerSaveEntry>();
    public List<string> purchasedUpgradeIds = new List<string>();

    /// <summary>
    /// UTC timestamp of when the save was written, in ISO 8601 round-trip format ("o").
    /// Used to calculate offline earnings on next load.
    /// </summary>
    public string lastSaveTimeUtc = DateTime.UtcNow.ToString("o");

    public double offlineEarningsCapHours = 8.0;

    [Serializable]
    public class MinerSaveEntry
    {
        public string minerName;
        public int count;
    }
}
