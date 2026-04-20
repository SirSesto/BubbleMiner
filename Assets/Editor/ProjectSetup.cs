using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// One-time project setup script.
/// Run via menu: BubbleMiner → Setup Project
/// Or it triggers automatically the first time the project is opened (if Data/GameConfig.asset is missing).
/// </summary>
[InitializeOnLoad]
public static class ProjectSetup
{
    // ── Auto-run on domain reload ───────────────────────────────────────────

    static ProjectSetup()
    {
        EditorApplication.delayCall += AutoSetupIfNeeded;
    }

    private static void AutoSetupIfNeeded()
    {
        // Only run once: check if GameConfig asset already exists
        if (File.Exists("Assets/Data/GameConfig.asset")) return;
        RunSetup();
    }

    // ── Manual menu item ────────────────────────────────────────────────────

    [MenuItem("BubbleMiner/Setup Project")]
    public static void RunSetup()
    {
        Debug.Log("[BubbleMiner] Running project setup…");

        AssetDatabase.StartAssetEditing();
        try
        {
            EnsureDirectories();
            var miners   = CreateMinerAssets();
            var upgrades = CreateUpgradeAssets(miners);
            var config   = CreateGameConfig(miners, upgrades);
            CreateMainScene(config);
        }
        finally
        {
            AssetDatabase.StopAssetEditing();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        SetupBuildSettings();
        Debug.Log("[BubbleMiner] Setup complete! Open MainScene and press Play.");
        EditorUtility.DisplayDialog("BubbleMiner Setup", "Project setup complete!\n\nOpen Assets/Scenes/MainScene.unity and press Play.", "OK");
    }

    // ── Directory setup ─────────────────────────────────────────────────────

    private static void EnsureDirectories()
    {
        string[] dirs =
        {
            "Assets/Data",
            "Assets/Data/Miners",
            "Assets/Data/Upgrades",
            "Assets/Scenes"
        };
        foreach (var dir in dirs)
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
    }

    // ── Miner ScriptableObjects ─────────────────────────────────────────────

    private static MinerData[] CreateMinerAssets()
    {
        var defs = new[]
        {
            // (name, description, baseCost, outputPerSec, costScale)
            ("Bubble Bot",         "A basic bot that pops small bubbles.",                       10.0,      0.1,   1.15),
            ("Bubble Drill",       "Drills deeper layers for denser bubble deposits.",           100.0,     0.5,   1.15),
            ("Bubble Pump",        "High-pressure pump extracts bubbles at great speed.",        1_100.0,   4.0,   1.15),
            ("Bubble Reactor",     "Fusion-powered chamber generates bubbles continuously.",     12_000.0,  20.0,  1.15),
            ("Bubble Singularity", "Quantum device harvests bubbles from parallel dimensions.", 130_000.0, 100.0,  1.15),
        };

        var miners = new MinerData[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var (name, desc, cost, bps, scale) = defs[i];
            string path = $"Assets/Data/Miners/{SanitizeFilename(name)}.asset";
            miners[i] = LoadOrCreate<MinerData>(path, m =>
            {
                m.minerName              = name;
                m.description            = desc;
                m.baseCost               = cost;
                m.baseOutputPerSecond    = bps;
                m.costScalingMultiplier  = scale;
            });
        }
        return miners;
    }

    // ── Upgrade ScriptableObjects ───────────────────────────────────────────

    private static UpgradeData[] CreateUpgradeAssets(MinerData[] miners)
    {
        // (id, name, description, cost, multiplier, isGlobal, targetIndex)
        var defs = new[]
        {
            ("global_boost_1",       "Bubble Surge I",      "Doubles output of all miners.",         500.0,    2.0, true,  -1),
            ("global_boost_2",       "Bubble Surge II",     "Triples output of all miners.",         50_000.0, 3.0, true,  -1),
            ("bot_boost",            "Bot Overclocking",    "Quadruples Bubble Bot output.",         200.0,    4.0, false,  0),
            ("drill_boost",          "Diamond Drill Tips",  "Doubles Bubble Drill output.",          2_000.0,  2.0, false,  1),
            ("pump_boost",           "Turbo Compressor",    "Triples Bubble Pump output.",           25_000.0, 3.0, false,  2),
        };

        var upgrades = new UpgradeData[defs.Length];
        for (int i = 0; i < defs.Length; i++)
        {
            var (id, name, desc, cost, mult, isGlobal, targetIdx) = defs[i];
            string path = $"Assets/Data/Upgrades/{SanitizeFilename(name)}.asset";
            upgrades[i] = LoadOrCreate<UpgradeData>(path, u =>
            {
                u.upgradeId   = id;
                u.upgradeName = name;
                u.description = desc;
                u.cost        = cost;
                u.multiplier  = mult;
                u.isGlobal    = isGlobal;
                u.targetMiner = (!isGlobal && targetIdx >= 0 && targetIdx < miners.Length)
                                    ? miners[targetIdx]
                                    : null;
            });
        }
        return upgrades;
    }

    // ── GameConfig ScriptableObject ─────────────────────────────────────────

    private static GameConfig CreateGameConfig(MinerData[] miners, UpgradeData[] upgrades)
    {
        return LoadOrCreate<GameConfig>("Assets/Data/GameConfig.asset", cfg =>
        {
            cfg.currencyName             = "Bubbles";
            cfg.miners                   = miners;
            cfg.upgrades                 = upgrades;
            cfg.offlineEarningsCapHours  = 8.0;
            cfg.autosaveIntervalSeconds  = 60f;
        });
    }

    // ── Main Scene ──────────────────────────────────────────────────────────

    private static void CreateMainScene(GameConfig config)
    {
        const string scenePath = "Assets/Scenes/MainScene.unity";

        // Only create if not already there
        if (File.Exists(scenePath))
        {
            Debug.Log("[BubbleMiner] MainScene already exists – skipping scene creation.");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // ── Camera ─────────────────────────────────────────────────────────
        var cameraGO = new GameObject("Main Camera");
        var cam      = cameraGO.AddComponent<Camera>();
        cam.clearFlags       = CameraClearFlags.SolidColor;
        cam.backgroundColor  = new Color(0.08f, 0.08f, 0.15f);
        cam.orthographic     = true;
        cameraGO.AddComponent<AudioListener>();

        // ── EventSystem ────────────────────────────────────────────────────
        var eventSysGO = new GameObject("EventSystem");
        eventSysGO.AddComponent<EventSystem>();
        eventSysGO.AddComponent<StandaloneInputModule>();

        // ── Canvas (Screen-Space Overlay) ──────────────────────────────────
        var canvasGO = new GameObject("Canvas");
        var canvas   = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode            = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution    = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight     = 0.5f;
        canvasGO.AddComponent<GraphicRaycaster>();

        // ── Background panel ───────────────────────────────────────────────
        var bgGO  = CreatePanel(canvasGO.transform, "Background",
                                new Color(0.08f, 0.08f, 0.15f), Vector2.zero, Vector2.one,
                                Vector2.zero, Vector2.zero);

        // ── Stats bar (top) ────────────────────────────────────────────────
        var statsBar = CreatePanel(canvasGO.transform, "StatsBar",
                                   new Color(0.05f, 0.05f, 0.1f, 0.9f),
                                   new Vector2(0f, 1f), new Vector2(1f, 1f),
                                   new Vector2(0f, -60f), new Vector2(0f, 0f));
        SetRect(statsBar, 0, 60, 0, 0);

        var balanceText = CreateTMPLabel(statsBar.transform, "BalanceText",
                                         "0 Bubbles", 32,
                                         new Vector2(0f, 0f), new Vector2(0.5f, 1f));
        var bpsText = CreateTMPLabel(statsBar.transform, "BPSText",
                                     "0.0/sec", 24,
                                     new Vector2(0.5f, 0f), new Vector2(1f, 1f));

        // ── Left panel: Shop ───────────────────────────────────────────────
        var shopPanel = CreatePanel(canvasGO.transform, "ShopPanel",
                                    new Color(0.1f, 0.1f, 0.2f, 0.95f),
                                    new Vector2(0f, 0f), new Vector2(0.5f, 1f),
                                    new Vector2(0f, 60f), new Vector2(0f, 0f));

        CreateTMPLabel(shopPanel.transform, "ShopTitle",
                       "MINERS", 28,
                       new Vector2(0f, 1f), new Vector2(1f, 1f),
                       height: 50f);

        var shopScrollRect = CreateScrollView(shopPanel.transform, "ShopScrollView",
                                              new Vector2(0f, 0f), new Vector2(1f, 1f),
                                              topOffset: 50f);

        // ── Right panel: Upgrades ──────────────────────────────────────────
        var upgradePanel = CreatePanel(canvasGO.transform, "UpgradePanel",
                                       new Color(0.1f, 0.2f, 0.1f, 0.95f),
                                       new Vector2(0.5f, 0f), new Vector2(1f, 1f),
                                       new Vector2(0f, 60f), new Vector2(0f, 0f));

        CreateTMPLabel(upgradePanel.transform, "UpgradeTitle",
                       "UPGRADES", 28,
                       new Vector2(0f, 1f), new Vector2(1f, 1f),
                       height: 50f);

        var upgradeScrollRect = CreateScrollView(upgradePanel.transform, "UpgradeScrollView",
                                                 new Vector2(0f, 0f), new Vector2(1f, 1f),
                                                 topOffset: 50f);

        // ── Offline earnings popup ─────────────────────────────────────────
        var offlinePanel = CreatePanel(canvasGO.transform, "OfflineEarningsPanel",
                                       new Color(0f, 0f, 0f, 0.85f),
                                       Vector2.zero, Vector2.one,
                                       Vector2.zero, Vector2.zero);
        var offlineCard = CreatePanel(offlinePanel.transform, "Card",
                                      new Color(0.15f, 0.15f, 0.3f),
                                      new Vector2(0.25f, 0.3f), new Vector2(0.75f, 0.7f),
                                      Vector2.zero, Vector2.zero);

        var offlineText = CreateTMPLabel(offlineCard.transform, "OfflineText",
                                          "Offline earnings…", 26,
                                          new Vector2(0f, 0.2f), new Vector2(1f, 1f));

        var okBtn = CreateButton(offlineCard.transform, "OkButton", "Collect!",
                                 new Vector2(0.2f, 0f), new Vector2(0.8f, 0.25f));
        offlinePanel.SetActive(false);

        // ── Shop item prefab ───────────────────────────────────────────────
        var shopItemPrefab     = CreateShopItemPrefab();
        var upgradeItemPrefab  = CreateUpgradeItemPrefab();

        // ── GameManager GameObject ─────────────────────────────────────────
        var gmGO = new GameObject("GameManager");
        var gm   = gmGO.AddComponent<GameManager>();
        SetPrivateField(gm, "gameConfig", config);
        gmGO.AddComponent<EconomyManager>();

        // ── UIController GameObject ────────────────────────────────────────
        var uiGO = new GameObject("UIController");
        var uic  = uiGO.AddComponent<UIController>();
        SetPrivateField(uic, "balanceText",           balanceText.GetComponent<TextMeshProUGUI>());
        SetPrivateField(uic, "bpsText",               bpsText.GetComponent<TextMeshProUGUI>());
        SetPrivateField(uic, "shopItemContainer",     shopScrollRect.content);
        SetPrivateField(uic, "shopItemPrefab",        shopItemPrefab);
        SetPrivateField(uic, "upgradeItemContainer",  upgradeScrollRect.content);
        SetPrivateField(uic, "upgradeItemPrefab",     upgradeItemPrefab);
        SetPrivateField(uic, "offlineEarningsPanel",  offlinePanel);
        SetPrivateField(uic, "offlineEarningsText",   offlineText.GetComponent<TextMeshProUGUI>());
        SetPrivateField(uic, "offlineEarningsOkButton", okBtn.GetComponent<Button>());

        EditorSceneManager.SaveScene(scene, scenePath);
        Debug.Log($"[BubbleMiner] Scene saved to {scenePath}");
    }

    // ── Prefab helpers ──────────────────────────────────────────────────────

    private static GameObject CreateShopItemPrefab()
    {
        var root = new GameObject("ShopItem");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 110f);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.15f, 0.15f, 0.28f);

        root.AddComponent<ShopItemUI>();

        var vl = root.AddComponent<VerticalLayoutGroup>();
        vl.padding    = new RectOffset(10, 10, 8, 8);
        vl.spacing    = 4f;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        vl.childControlHeight     = true;

        var nameLbl    = CreateChildTMP(root.transform, "NameText",    "Miner Name", 20, FontStyles.Bold);
        var costLbl    = CreateChildTMP(root.transform, "CostText",    "Cost: 0",    16);
        var outputLbl  = CreateChildTMP(root.transform, "OutputText",  "+0/s each",  14);
        var countLbl   = CreateChildTMP(root.transform, "CountText",   "Owned: 0",   14);
        var btn        = CreateChildButton(root.transform, "BuyButton", "Buy");

        var item = root.GetComponent<ShopItemUI>();
        SetPrivateField(item, "nameText",   nameLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "costText",   costLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "outputText", outputLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "countText",  countLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "buyButton",  btn.GetComponent<Button>());

        string prefabPath = "Assets/Data/ShopItemPrefab.prefab";
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out saved);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateUpgradeItemPrefab()
    {
        var root = new GameObject("UpgradeItem");
        var rt   = root.AddComponent<RectTransform>();
        rt.sizeDelta = new Vector2(0f, 100f);
        var bg = root.AddComponent<Image>();
        bg.color = new Color(0.12f, 0.2f, 0.12f);

        root.AddComponent<UpgradeItemUI>();

        var vl = root.AddComponent<VerticalLayoutGroup>();
        vl.padding    = new RectOffset(10, 10, 8, 8);
        vl.spacing    = 4f;
        vl.childForceExpandWidth  = true;
        vl.childForceExpandHeight = false;
        vl.childControlHeight     = true;

        var nameLbl  = CreateChildTMP(root.transform, "NameText",   "Upgrade Name", 20, FontStyles.Bold);
        var descLbl  = CreateChildTMP(root.transform, "DescText",   "Description",  14);
        var costLbl  = CreateChildTMP(root.transform, "CostText",   "Cost: 0",      16);
        var btn      = CreateChildButton(root.transform, "BuyButton", "Buy");

        var item = root.GetComponent<UpgradeItemUI>();
        SetPrivateField(item, "nameText",        nameLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "descriptionText", descLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "costText",        costLbl.GetComponent<TextMeshProUGUI>());
        SetPrivateField(item, "buyButton",       btn.GetComponent<Button>());

        string prefabPath = "Assets/Data/UpgradeItemPrefab.prefab";
        bool saved;
        var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out saved);
        Object.DestroyImmediate(root);
        return prefab;
    }

    // ── Small UI factory helpers ────────────────────────────────────────────

    private static GameObject CreatePanel(Transform parent, string name, Color color,
        Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var go  = new GameObject(name);
        go.transform.SetParent(parent, false);
        var img = go.AddComponent<Image>();
        img.color = color;
        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin  = anchorMin;
        rt.anchorMax  = anchorMax;
        rt.offsetMin  = offsetMin;
        rt.offsetMax  = offsetMax;
        return go;
    }

    private static void SetRect(GameObject go, float left, float top, float right, float bottom)
    {
        var rt = go.GetComponent<RectTransform>();
        if (rt == null) return;
        rt.offsetMin = new Vector2(left,  -bottom);
        rt.offsetMax = new Vector2(-right, top);
    }

    private static GameObject CreateTMPLabel(Transform parent, string name, string text,
        int fontSize, Vector2 anchorMin, Vector2 anchorMax, float height = 0f)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        if (height > 0f) rt.sizeDelta = new Vector2(0f, height);

        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text      = text;
        tmp.fontSize  = fontSize;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    private static ScrollRect CreateScrollView(Transform parent, string name,
        Vector2 anchorMin, Vector2 anchorMax, float topOffset = 0f)
    {
        // Viewport
        var viewport = new GameObject(name);
        viewport.transform.SetParent(parent, false);
        var vrt = viewport.AddComponent<RectTransform>();
        vrt.anchorMin = anchorMin;
        vrt.anchorMax = anchorMax;
        vrt.offsetMin = new Vector2(0f, 0f);
        vrt.offsetMax = new Vector2(0f, -topOffset);
        viewport.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0f);
        viewport.AddComponent<Mask>().showMaskGraphic = false;

        // Content
        var content = new GameObject("Content");
        content.transform.SetParent(viewport.transform, false);
        var crt = content.AddComponent<RectTransform>();
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(1f, 1f);
        crt.pivot     = new Vector2(0.5f, 1f);
        crt.sizeDelta = new Vector2(0f, 0f);

        var vlg = content.AddComponent<VerticalLayoutGroup>();
        vlg.spacing               = 8f;
        vlg.padding               = new RectOffset(8, 8, 8, 8);
        vlg.childForceExpandWidth = true;
        vlg.childControlHeight    = true;
        vlg.childForceExpandHeight = false;

        content.AddComponent<ContentSizeFitter>().verticalFit =
            ContentSizeFitter.FitMode.PreferredSize;

        // ScrollRect on viewport
        var sr = viewport.AddComponent<ScrollRect>();
        sr.content         = crt;
        sr.viewport        = vrt;
        sr.horizontal      = false;
        sr.vertical        = true;
        sr.scrollSensitivity = 30f;
        sr.movementType    = ScrollRect.MovementType.Clamped;

        return sr;
    }

    private static GameObject CreateButton(Transform parent, string name, string label,
        Vector2 anchorMin, Vector2 anchorMax)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.2f);
        go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 22;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;

        return go;
    }

    private static GameObject CreateChildTMP(Transform parent, string name, string text,
        int fontSize, FontStyles style = FontStyles.Normal)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = fontSize + 8f;
        var tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text       = text;
        tmp.fontSize   = fontSize;
        tmp.fontStyle  = style;
        tmp.color      = Color.white;
        tmp.alignment  = TextAlignmentOptions.Left;
        return go;
    }

    private static GameObject CreateChildButton(Transform parent, string name, string label)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        var le  = go.AddComponent<LayoutElement>();
        le.preferredHeight = 36f;
        var img = go.AddComponent<Image>();
        img.color = new Color(0.2f, 0.6f, 0.2f);
        go.AddComponent<Button>();

        var textGO = new GameObject("Text");
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.AddComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = Vector2.zero;
        trt.offsetMax = Vector2.zero;
        var tmp = textGO.AddComponent<TextMeshProUGUI>();
        tmp.text      = label;
        tmp.fontSize  = 18;
        tmp.color     = Color.white;
        tmp.alignment = TextAlignmentOptions.Center;
        return go;
    }

    // ── Build settings ──────────────────────────────────────────────────────

    private static void SetupBuildSettings()
    {
        const string scenePath = "Assets/Scenes/MainScene.unity";

        var scenes = new EditorBuildSettingsScene[]
        {
            new EditorBuildSettingsScene(scenePath, true)
        };
        EditorBuildSettings.scenes = scenes;

        // Default to Windows Standalone
        EditorUserBuildSettings.SwitchActiveBuildTarget(
            BuildTargetGroup.Standalone,
            BuildTarget.StandaloneWindows64);

        Debug.Log("[BubbleMiner] Build target set to Windows x64.");
    }

    // ── Utility ─────────────────────────────────────────────────────────────

    /// <summary>Loads an existing asset or creates a new one and applies initializer.</summary>
    private static T LoadOrCreate<T>(string path, System.Action<T> init) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset != null) return asset;

        asset = ScriptableObject.CreateInstance<T>();
        init(asset);
        AssetDatabase.CreateAsset(asset, path);
        return asset;
    }

    private static string SanitizeFilename(string name) =>
        name.Replace(" ", "_").Replace("/", "_");

    /// <summary>Uses reflection to set a serialized private field (works in Editor only).</summary>
    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(target, value);
    }
}
