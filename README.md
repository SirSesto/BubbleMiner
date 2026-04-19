# BubbleMiner 🫧

A 2D Idle/Tycoon game built in **Unity 2022.3 LTS (C#)**.  
Buy miners, collect Bubbles passively, purchase upgrades, and rake in offline earnings!

> **Inspired by the Idle/Tycoon genre** — fully original assets, names and code.

---

## Requirements

| Tool | Version |
|------|---------|
| [Unity Hub](https://unity.com/unity-hub) | any recent |
| Unity Editor | **2022.3.x LTS** (tested on 2022.3.21f1) |
| Platform Module | **Windows Build Support (IL2CPP or Mono)** |

---

## First-time Setup (Opening the project)

1. **Clone / download** this repository.
2. Open **Unity Hub** → click **Open** → select the `BubbleMiner` folder.
3. Unity will import the project and compile all scripts (~30–60 s the first time).
4. In the Unity menu bar click **BubbleMiner → Setup Project**.  
   This will automatically:
   - Create all Miner and Upgrade ScriptableObject data assets in `Assets/Data/`
   - Create the prefabs (`ShopItem`, `UpgradeItem`) in `Assets/Data/`
   - Create and save `Assets/Scenes/MainScene.unity` with all UI wired up
   - Set the build target to **Windows x86_64**

   > ⚠️ If a dialog says "TMP Importer" – click **Import TMP Essentials** first, then re-run **BubbleMiner → Setup Project**.

5. Open `Assets/Scenes/MainScene.unity` in the editor (double-click).
6. Press **▶ Play** — the game runs immediately.

---

## How to Play

| Action | How |
|--------|-----|
| **Earn Bubbles** | Miners generate Bubbles/sec automatically |
| **Buy a Miner** | Click **Buy** next to a miner in the left panel |
| **Buy an Upgrade** | Click **Buy** in the right panel (upgrades multiply output) |
| **Offline Earnings** | Close the game, come back later — you'll see a popup with your offline haul (capped at 8 h) |
| **Save** | Auto-saves every 60 s and on quit |

### Miners (5 types)

| Miner | Base Cost | Output/s |
|-------|-----------|----------|
| Bubble Bot | 10 | 0.1 |
| Bubble Drill | 100 | 0.5 |
| Bubble Pump | 1 100 | 4.0 |
| Bubble Reactor | 12 000 | 20.0 |
| Bubble Singularity | 130 000 | 100.0 |

Costs scale by ×1.15 per unit owned.

### Upgrades (5 types)

| Upgrade | Cost | Effect |
|---------|------|--------|
| Bubble Surge I | 500 | ×2 all miners |
| Bubble Surge II | 50 000 | ×3 all miners |
| Bot Overclocking | 200 | ×4 Bubble Bots |
| Diamond Drill Tips | 2 000 | ×2 Bubble Drills |
| Turbo Compressor | 25 000 | ×3 Bubble Pumps |

---

## Building a Windows .exe

1. Make sure **Windows Build Support** is installed in Unity Hub.
2. In Unity: **File → Build Settings**
   - Platform: **PC, Mac & Linux Standalone**
   - Target Platform: **Windows**
   - Architecture: **x86_64**
   - Scenes in Build: `Assets/Scenes/MainScene.unity` (added automatically by setup)
3. Click **Build** → choose an output folder → the `.exe` is ready.

### Command-line build (optional)

```bash
"C:\Program Files\Unity\Hub\Editor\2022.3.21f1\Editor\Unity.exe" \
  -batchmode -quit \
  -projectPath "C:\path\to\BubbleMiner" \
  -buildWindows64Player "C:\path\to\Build\BubbleMiner.exe" \
  -logFile "build.log"
```

---

## Project Structure

```
Assets/
├── Editor/
│   └── ProjectSetup.cs       ← One-time scene & data generator (Editor only)
├── Scenes/
│   └── MainScene.unity       ← Created by ProjectSetup
├── Scripts/
│   ├── Core/
│   │   ├── GameManager.cs    ← Startup, autosave, quit-save
│   │   └── EconomyManager.cs ← Tick loop, BPS calculation, buy logic
│   ├── Data/
│   │   ├── MinerData.cs      ← ScriptableObject: miner definition
│   │   ├── UpgradeData.cs    ← ScriptableObject: upgrade definition
│   │   └── GameConfig.cs     ← ScriptableObject: global config
│   ├── Save/
│   │   ├── SaveData.cs       ← Serializable save snapshot
│   │   └── SaveSystem.cs     ← JSON read/write + offline earnings calc
│   └── UI/
│       ├── UIController.cs   ← Main UI singleton (stats, shop, upgrades)
│       ├── ShopItemUI.cs     ← Per-miner shop row
│       └── UpgradeItemUI.cs  ← Per-upgrade row
├── Data/                     ← Created by ProjectSetup
│   ├── GameConfig.asset
│   ├── ShopItemPrefab.prefab
│   ├── UpgradeItemPrefab.prefab
│   ├── Miners/
│   │   └── *.asset
│   └── Upgrades/
│       └── *.asset
Packages/
└── manifest.json             ← TMP + UGUI packages
ProjectSettings/              ← Unity project settings (Windows x64 target)
```

---

## Save File Location

```
%AppData%\..\LocalLow\SirSesto\BubbleMiner\bubbleminer_save.json
```
(i.e. `Application.persistentDataPath` on Windows)

---

## License

This project is original work, inspired by the Idle/Tycoon genre.  
No assets, code, or content from any third-party game is used.
