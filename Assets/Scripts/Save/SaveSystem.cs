using System;
using System.Globalization;
using System.IO;
using UnityEngine;

/// <summary>
/// Handles reading and writing the save file to Application.persistentDataPath
/// and calculating offline earnings from the elapsed time since the last save.
/// </summary>
public static class SaveSystem
{
    private const string SaveFileName = "bubbleminer_save.json";

    private static string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);

    /// <summary>Serializes <paramref name="data"/> to JSON and writes it to disk.</summary>
    public static void Save(SaveData data)
    {
        data.lastSaveTimeUtc = DateTime.UtcNow.ToString("o");
        try
        {
            string json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(SavePath, json);
            Debug.Log($"[SaveSystem] Saved → {SavePath}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
    }

    /// <summary>
    /// Reads the save file from disk and returns the deserialized <see cref="SaveData"/>,
    /// or <c>null</c> if no save file exists or the file is corrupt.
    /// </summary>
    public static SaveData Load()
    {
        if (!File.Exists(SavePath))
        {
            Debug.Log("[SaveSystem] No save file found — starting fresh.");
            return null;
        }

        try
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("[SaveSystem] Save loaded successfully.");
            return data;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
    }

    /// <summary>
    /// Calculates how many Bubbles the player earned while offline.
    /// </summary>
    /// <param name="data">Previously loaded save data (provides the last-save timestamp).</param>
    /// <param name="bubblesPerSecond">The player's current output rate AFTER upgrades are applied.</param>
    /// <param name="capHours">Maximum number of hours of offline earnings to award.</param>
    /// <returns>Total Bubbles earned offline (capped).</returns>
    public static double CalculateOfflineEarnings(SaveData data, double bubblesPerSecond, double capHours)
    {
        if (data == null || bubblesPerSecond <= 0) return 0.0;

        if (!DateTime.TryParse(data.lastSaveTimeUtc, null,
                DateTimeStyles.RoundtripKind, out DateTime lastSave))
        {
            Debug.LogWarning("[SaveSystem] Could not parse lastSaveTimeUtc — skipping offline earnings.");
            return 0.0;
        }

        TimeSpan elapsed = DateTime.UtcNow - lastSave;
        double cappedSeconds = Math.Min(elapsed.TotalSeconds, capHours * 3600.0);

        return bubblesPerSecond * cappedSeconds;
    }

    /// <returns><c>true</c> if a save file exists on disk.</returns>
    public static bool HasSaveFile() => File.Exists(SavePath);

    /// <summary>Deletes the save file (used for "New Game" / reset).</summary>
    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("[SaveSystem] Save file deleted.");
        }
    }
}
