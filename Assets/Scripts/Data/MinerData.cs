using System;
using UnityEngine;

/// <summary>
/// ScriptableObject defining a Miner type (e.g., Bubble Bot, Bubble Drill).
/// Each miner generates a passive income per second.
/// </summary>
[CreateAssetMenu(fileName = "MinerData", menuName = "BubbleMiner/Miner Data", order = 1)]
public class MinerData : ScriptableObject
{
    [Header("Identity")]
    public string minerName = "Bubble Bot";
    [TextArea(2, 4)]
    public string description = "A basic bubble mining bot.";
    public Sprite icon;

    [Header("Economy")]
    [Tooltip("Base purchase cost for the first unit.")]
    public double baseCost = 10.0;
    [Tooltip("Bubbles produced per second per unit owned.")]
    public double baseOutputPerSecond = 0.1;
    [Tooltip("Cost multiplier applied for each additional unit owned.")]
    public double costScalingMultiplier = 1.15;

    /// <summary>Returns the purchase cost for the next unit given how many are already owned.</summary>
    public double GetCost(int currentCount)
    {
        return baseCost * Math.Pow(costScalingMultiplier, currentCount);
    }

    /// <summary>Returns the total output per second for the given count, before upgrades.</summary>
    public double GetTotalOutput(int count)
    {
        return baseOutputPerSecond * count;
    }
}
