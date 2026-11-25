# SinkOrThink3D: Island Adventure — Beginner-Friendly Script & Architecture Guide

This guide explains every runtime script (`Assets/Scripts/`) and Editor tool (`Assets/Editor/`) in plain, easy-to-follow terms for presentation and grading.

---

## How to Explain the Architecture in Your Presentation

1. **Single Persistent State (`GameStateManager`)**:
   - Instead of losing coins, keys, boat health, or opened chests whenever Unity loads a new scene, `GameStateManager` calls `DontDestroyOnLoad(gameObject)` so one manager survives from `MainMenu` → `GameScene` → `IslandScene` → back again.
2. **Reusable 3-in-1 `IslandScene.unity`**:
   - Instead of duplicating UI, player prefabs, and EventSystems across 3 separate island `.unity` scenes, `IslandScene.unity` holds three parent GameObjects: `DiscoveryIsland`, `VikingIsland`, and `FinalTreasureIsland`.
   - `IslandSceneController` activates only the selected island at `Start()`, keeping inactive islands disabled to save memory and draw calls.
3. **Event-Driven UI Updates**:
   - When coins, keys, planks, potions, or boat health change, `GameStateManager.NotifyStateChanged()` fires `OnStateChanged`, so the HUD, Bottom Hotbar, Inventory, and Magic Compass refresh immediately without expensive per-frame searching.
4. **RTX 2050 Performance Budget**:
   - Decorative torches/campfires have realtime point-light shadows turned off (`LightShadows.None`) to prevent URP Shadow Atlas overflow, URP uses `2x MSAA` + `75m` 2-cascade shadows + half-res SSAO (`Downsample: 1`), and UI modal scripts cache scene references on `Start`/`OnSceneLoaded`.

---

## 1. Core State, Questions & Scene Transitions (`Assets/Scripts/`)

| Script | What It Does |
|---|---|
| `GameStateManager.cs` | Central `DontDestroyOnLoad` save-in-memory state: tracks `boatHealth`, `coins`, `planks`, `potions`, `hasMagicCompass`, `hasGoldenCrown`, per-island Golden Keys (`hasIslandKey[3]`), opened chest IDs (`openedChestIds`), non-rerolling chest reward assignments (`EnsureIslandRewardsAssigned`), island unlock index (`currentIslandIndex`), and repair helpers (`TryUsePlank`, `TryUsePotion`, `TryCoinRepair`). |
| `QuestionBank.cs` | Static bank holding **90 educational multiple-choice questions**: 30 **Easy**, 30 **Medium**, and 30 **Hard** across Math, Science, English, Filipino, and General Knowledge (`QuizDifficulty`). |
| `ScreenFader.cs` | `DontDestroyOnLoad` full-screen fade curtain (`FadeAndLoadScene`) and top arrival/status banner (`ShowArrivalBanner`) with `raycastTarget = false` so transparent overlays never block clicks. |
| `OceanBoatSpawner.cs` | Runs when `GameScene` loads after exploring an island (`returningFromIsland == true`); places the boat at the matching dock's `boatReturnSpawnPoint` and shows a sailing banner. |
| `IslandSceneController.cs` | Reads `GameStateManager.selectedIslandIndex` on load, activates `DiscoveryIsland`, `VikingIsland`, or `FinalTreasureIsland`, teleports the player to that island's dock spawn, removes duplicate `AudioListener` / `EventSystem` objects, refreshes the HUD and Magic Compass, and starts island ambience audio. |

---

## 2. Boat Controls, Health, Sinking & Ocean Quiz (`Assets/Scripts/`)

| Script | What It Does |
|---|---|
| `BoatMovement.cs` | Reads `Vertical`/`Horizontal` axes (`W/S/A/D` or arrows) and moves/rotates the boat's kinematic `Rigidbody` in `FixedUpdate` using collision sweep checks so the boat never clips through islands or docks. |
| `BoatSink.cs` | Manages `currentHealth` (synced with `GameStateManager`), smoothly animates the green-to-red health bar fill, triggers boat tilt & bounded sinking (`maxSinkDepth`) at `0 HP`, and supports healing via `SyncFromGameState`. |
| `BoatAnimator.cs` | Adds visual wave bobbing, rocking, turn banking, oar rowing on `Paddle` & `Paddle (1)`, wake/bow splashes, and damage/repair particle bursts on top of `BoatMovement` without interfering with physics. |
| `FirstPersonMouseLook.cs` | Rotates the boat camera while holding **Right Mouse Button** (clamping pitch `-70°..70°` and keeping Z roll at `0`), leaving Left Click free for 3D quiz buttons. Reads saved mouse sensitivity from `PauseAndSettingsUI`. |
| `QuizManager.cs` | Runs the ocean loop: 20s sailing countdown → 5-question round (15s/question, randomized question order + randomized A–D choices). Uses a single authoritative `Physics.RaycastAll` check on Left Click (ignoring UI clicks), updates boat damage/particles, unlocks docking after round completion (`CanDockNow()`), and shows Game Over at `0 HP`. |
| `QuizBoardAnimator.cs` | Animates the 3D wooden Quiz Board dropping into view on the boat bow, pulsing on each new question, celebrating correct answers, and shaking on wrong answers. |
| `AnswerButton3D.cs` | Attached to `AnswerButton3D_A..D`; handles hover/press scale animation and calls `QuizManager.CheckAnswer(answerNumber)` when triggered. |
| `MouseClickDebugger.cs` | Optional click logger (disabled by default via `enableDebugLogs = false`) that avoids duplicate quiz submissions. |
| `BoatDocking.cs` | Measures horizontal distance from boat to each island's `DockParkingPoint`, gates locked islands and uncompleted ocean quiz rounds, displays `"Press F to Dock at ..."`, smoothly aligns the boat dockside on `F`, plays docking audio, and transitions to `IslandScene.unity`. |

---

## 3. First-Person Island Exploration & Chest Systems (`Assets/Scripts/`)

| Script | What It Does |
|---|---|
| `IslandPlayerController.cs` | Uses Unity's `CharacterController` for first-person `WASD` walk, `Shift` sprint, `Space` jump, mouse look (synced with Settings sensitivity), head bob, landing dip, deep-water safety respawn, raycast surface footstep detection (wood vs. terrain), and `E`/`F` interactable prompts. |
| `IslandInteractable.cs` | Clean base class (`GetPromptText`, `GetInteractKey`, `CanInteract`, `GetInteractRange`, `Interact`) shared by treasure chests, the dock repair bench, and the return boat. |
| `TreasureChest.cs` | Controls regular and special chests: smoothly hinges `Chest_top` open when unlocked, rattles when locked, bursts sparkles on open, plays chest open/locked SFX, and restores already-opened state across scene transitions. |
| `ChestQuizManager.cs` | Listens for chest `E` interactions; checks Golden Key requirement on Special Chests (`"You need a Golden Key to unlock this chest."`), opens a 4-choice parchment quiz panel (`Easy`/`Medium`/`Hard` per island/special chest), grants persistent rewards via `GameStateManager`, spawns 3D `RewardRevealEffect` props, and triggers `VictoryScreenUI` after opening Final Island's Special Chest. |
| `RewardRevealEffect.cs` | Spawns an animated 3D treasure mesh (Coins, Planks, Potion, Golden Key, Magic Compass, or sparkling Golden Crown) above the opened chest that rises, spins, and flies smoothly toward the player camera. |
| `DockRepairStation.cs` | Dock repair workbench (`E`) allowing players to spend `10 Coins` to repair `+20 Boat HP` with green repair burst particles and sound. |
| `IslandBoatReturn.cs` | Attached to each island's docked boat; displays `"Press F to Board Boat"`, advances `currentIslandIndex` when the island's Special Chest is complete, sets `returningFromIsland = true`, and loads `GameScene.unity`. |

---

## 4. HUD, Inventory, Compass, Menus & Victory (`Assets/Scripts/`)

| Script | What It Does |
|---|---|
| `InventoryHotbarUI.cs` | Persistent bottom hotbar (`[1]` Plank `+10 HP`, `[2]` Potion `+25 HP`, Coins, Golden Key status, Compass status) plus `[I]` Inventory modal with slot dimming and pulse animations. Blocks hotkeys during quizzes, menus, Victory, or Game Over. |
| `MagicCompassUI.cs` | Hidden until Discovery Island's Special Chest unlocks `hasMagicCompass`. Smoothly rotates its brass compass needle toward the nearest unopened chest on the active island (`selectedIslandIndex`), prioritizes the Special Chest once holding the Golden Key, and points back to the Boat once all 6 chests are opened. |
| `PauseAndSettingsUI.cs` | Persistent `Escape` Pause Menu (`Resume`, `Settings`, `Main Menu`, `Quit Game`) and shared Settings panel (`Master`, `Ambience`, `SFX` volumes + `Mouse Sensitivity`, saved in `PlayerPrefs`). Guards against double-triggering on the same frame Inventory or a Chest Quiz closes. |
| `VictoryScreenUI.cs` | Celebratory Victory overlay triggered upon recovering the Legendary Golden Crown on Final Treasure Island; spawns golden confetti particles and displays Islands Conquered, Total Chests Opened (`/ 18`), Coins, Supplies, Boat Health, Magic Compass status, and **Play Again** / **Main Menu** buttons. |
| `MainMenuManager.cs` | Handles `MainMenu.unity` **PLAY**, **HOW TO PLAY**, **SETTINGS** (`OpenSettings`), and **QUIT** buttons with click sound feedback. |

---

## 5. Audio, Atmosphere & Environmental Animations (`Assets/Scripts/`)

| Script | What It Does |
|---|---|
| `GameAudioManager.cs` | `DontDestroyOnLoad` audio hub managing `ambientSource` (ocean, Discovery surf/birds, Viking wind, Final mysterious ambience) and `sfxSource` (correct/wrong, chest open/locked, collect, repair, dock, ground vs. wood footsteps, victory, game over, UI click) with automatic procedural fallback clips. |
| `IslandAtmosphereProfile.cs` | Attached to each island parent in `IslandScene.unity`; applies that island's sun color/angle, ambient tint, and fog color/density whenever activated. |
| `OceanWaterAnimator.cs` | Scrolls URP ocean water texture offsets and applies a gentle vertical ocean swell. |
| `GentleFloat.cs` | Bobs and gently tilts floating objects (buoys, moored return boats, and the Sanctuary floating orb). |
| `WindSway.cs` | Gently sways palm trees, conifers, bushes, and grass tufts using world-position phase offsets so foliage moves naturally rather than identically. |
| `FlagWaveAnimator.cs` | Ripples Viking crimson/gold cloth banners smoothly in the wind. |
| `FlickeringLight.cs` | Animates campfire, torch, and brazier point light intensity via `Mathf.PerlinNoise` while enforcing `LightShadows.None` for GPU performance. |
| `WaterfallAnimator.cs` | Scrolls UVs and pulses foam brightness on Final Treasure Island's cascading waterfall using a per-instance material. |
| `CloudDriftAnimator.cs` | Drifts sky cloud puffs across `GameScene` and `IslandScene` horizons and wraps them seamlessly around the map. |
| `BeachCrabWander.cs` | Moves shoreline crabs back and forth across Discovery Island beaches. |

---

## 6. Automated Unity Editor Setup & QA Tools (`Assets/Editor/`)

All Editor builders live under `Tools → SinkOrThink3D` and are idempotent (safe to re-run cleanly):

- **`Phase3BoatAndBoardSetup.cs` – `Phase9CompassAndProgressionSetup.cs`**: Modular setup tools built progressively across Phases 3–9 (boat/board animations, ocean fog/docking, Discovery Island base, 6 chests, inventory/hotbar, and Magic Compass).
- **`Phase9_5DiscoveryIslandPolish.cs`**: Polishes Discovery Island across both `GameScene.unity` and `IslandScene.unity`, adds missing colliders on trees/rocks/buildings/bridges, aligns the dock berth, and organizes `IslandScene` into `DiscoveryIsland`, `VikingIsland`, and `FinalTreasureIsland`.
- **`Phase10VikingIslandSetup.cs`**: Creates 5 textured Viking prefabs (`VikingLonghouse` with chimney smoke, `VikingWatchtower`, `VikingBannerFlag`, `VikingTorchPost`, `VikingFenceSegment`), builds `VikingIslandOverview` + `BoatDockPoint_Viking` in `GameScene.unity`, and populates detailed `VikingIsland` + 6 chests in `IslandScene.unity`.
- **`Phase11FinalIslandSetup.cs`**: Creates 4 Ancient Ruin/Waterfall prefabs (`AncientTempleArch`, `AncientStonePillar`, `AncientTreasureChamber`, `AncientWaterfall`), builds `FinalIslandOverview` + `BoatDockPoint_Final` in `GameScene.unity`, and populates detailed `FinalTreasureIsland` + 6 Hard chests in `IslandScene.unity`.
- **`Phase12PolishAndAudioSetup.cs`**: Generates 17 WAV audio clips under `Assets/Audio/`, wires `GameAudioManager` & `SettingsButton` into `MainMenu.unity`, adds drifting `SkyCloudLayer` puffs to `GameScene.unity` and `IslandScene.unity`, and provides the 1-click **`Build & Verify All Scenes (Phases 9.5 -> 13)`** workflow.
- **`Phase13OptimizationAndQATool.cs`**: Tunes Terrain LOD error & tiny-prop shadows, guarantees 0 shadow-casting point lights, and runs an automated QA audit across all 3 scenes (missing scripts, pink shaders, 3 docks, all 18 chests).
- **`Phase14WindowsBuildTool.cs`**: Validates Build Settings and builds **`Builds/Windows64/SinkOrThink3D.exe`** (`StandaloneWindows64`).
