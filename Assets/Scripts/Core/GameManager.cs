using UnityEngine;

/// <summary>
/// Central game manager singleton.
/// Orchestrates startup (load save → apply offline earnings → initialize UI),
/// autosave tick, and quit save.
/// Attach to a persistent GameObject in the scene and assign the GameConfig asset.
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Tooltip("Assign the GameConfig ScriptableObject asset created in Assets/Data.")]
    [SerializeField] private GameConfig gameConfig;

    private float _autosaveTimer = 0f;

    // ── Unity lifecycle ─────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (gameConfig == null)
        {
            Debug.LogError("[GameManager] GameConfig is not assigned! " +
                           "Please assign it in the Inspector.");
            return;
        }

        // 1. Init economy with config (must happen before loading save)
        EconomyManager.Instance.Initialize(gameConfig);

        // 2. Load save
        SaveData saveData = SaveSystem.Load();

        if (saveData != null)
        {
            // 3. Restore miner counts / upgrades so BPS is correct
            EconomyManager.Instance.LoadFromSaveData(saveData);

            // 4. Calculate and award offline earnings
            double bps = EconomyManager.Instance.BubblesPerSecond;
            double offlineEarnings = SaveSystem.CalculateOfflineEarnings(
                saveData, bps, gameConfig.offlineEarningsCapHours);

            if (offlineEarnings > 0.0)
            {
                EconomyManager.Instance.AddBubbles(offlineEarnings);
                UIController.Instance?.ShowOfflineEarnings(offlineEarnings);
            }
        }

        // 5. Build UI
        UIController.Instance?.Initialize(gameConfig);
    }

    private void Update()
    {
        if (gameConfig == null) return;

        _autosaveTimer += Time.deltaTime;
        if (_autosaveTimer >= gameConfig.autosaveIntervalSeconds)
        {
            _autosaveTimer = 0f;
            SaveGame();
        }
    }

    private void OnApplicationQuit()
    {
        SaveGame();
    }

    // ── Public API ──────────────────────────────────────────────────────────

    public void SaveGame()
    {
        if (EconomyManager.Instance == null) return;
        SaveSystem.Save(EconomyManager.Instance.CreateSaveData());
    }

    public GameConfig Config => gameConfig;
}
