# SinkOrThink3D: Island Adventure

An educational 3D first-person boat and island exploration adventure built in **Unity 6.6 (`6000.6.9f1`, URP)** for Windows 64-bit. Sail across tropical, Nordic, and ancient seas, answer educational questions across **Math, Science, English, Filipino, and General Knowledge**, explore three themed islands on foot, unlock 18 treasure chests, and recover the **Legendary Golden Crown**.

---

## 1. Gameplay Loop & Island Progression

1. **Main Menu (`MainMenu.unity`)**
   - Click **PLAY** to start a fresh voyage, **HOW TO PLAY** for rules, or **SETTINGS** to adjust Master / Ambience / SFX volumes and Mouse Sensitivity.
2. **Ocean Voyage (`GameScene.unity`)**
   - Steer your wooden rowboat across the ocean toward the active island dock (`DiscoveryIsland_MainTerrain`, `VikingIslandOverview`, or `FinalIslandOverview`).
   - After sailing for 20 seconds, a **5-Question Ocean Quiz Round** appears on the boat's 3D wooden Quiz Board (15 seconds per question, randomized A–D choices).
   - Wrong answers or timeouts damage your boat (`-20 HP`); losing all `100 HP` sinks the boat and shows **Game Over**.
   - Completing at least **1 Ocean Quiz Round** unlocks docking at the active island!
3. **Docking & Reusable Island Exploration (`IslandScene.unity`)**
   - Approach the unlocked island's dock and press **`F`** to align the boat cleanly and transition into first-person exploration:
     - **Island 1 — Discovery Island (Easy)**: Tropical bamboo village, sandy shoreline, wooden bridges, campfire, and palm groves.
     - **Island 2 — Viking Island (Medium)**: Rugged Nordic fjord coastline, conifer forest, watchtower ridge, chimney-smoking longhouses, flickering torches, and waving banners.
     - **Island 3 — Final Treasure Island (Hard)**: Dusk-lit ancient temple ruins, cascading waterfall, towering cliffs, mossy stone arches, and the Sanctuary Chamber.
   - Explore on foot to find **5 Regular Chests** and **1 Special Chest** per island (**18 chests total**):
     - Each island's 5 Regular Chests contain **1 guaranteed Golden Key** plus randomized **Coins, Wooden Planks, and Repair Potions**.
     - Answer a chest's educational question to open it (wrong answers keep the chest locked so you can retry with a fresh question).
     - Bring the island's **Golden Key** to unlock its glowing **Special Chest**.
4. **Special Chest Rewards & Progression**
   - **Discovery Island Special Chest**: `+30 Coins` + reusable **Magic Compass** (HUD needle guiding you toward the nearest unopened chest, Special Chest when holding a Golden Key, or Dock Boat when finished).
   - **Viking Island Special Chest**: `+50 Coins` + `2 Repair Potions` + `3 Wooden Planks`.
   - **Final Treasure Island Special Chest**: `+100 Coins` + **Legendary Golden Crown** → celebratory confetti & **Victory Screen**!
5. **Boat Repairs & Returning to Sea**
   - **Key `1` (Wooden Plank)**: `+10 Boat HP`
   - **Key `2` (Repair Potion)**: `+25 Boat HP`
   - **Dock Repair Workbench (`E` on dock)**: Spend `10 Coins` for `+20 Boat HP`
   - Approach the docked boat on the island and press **`F`** to board and spawn right outside that island's dock back in `GameScene.unity`.

---

## 2. Controls

| Mode | Action | Input |
|---|---|---|
| **Ocean Boat** | Sail forward / reverse / steer | `W A S D` or `Arrow Keys` |
| **Ocean Boat** | Look around | Hold `Right Mouse Button` + Move Mouse |
| **Ocean Quiz** | Select 3D answer button A–D | `Left Mouse Click` |
| **Docking / Boarding** | Dock boat / Board boat to ocean | `F` |
| **Island On-Foot** | Walk / Sprint / Jump | `W A S D` / `Shift` / `Space` |
| **Island On-Foot** | Look around | `Mouse` |
| **Island Interaction** | Open chest / Use Dock Repair Bench | `E` |
| **Repairs & HUD** | Use Plank (`+10 HP`) / Use Potion (`+25 HP`) | `1` / `2` |
| **Inventory** | Open / Close Inventory modal | `I` |
| **Menus** | Pause Menu / Close modal | `Esc` |

---

## 3. One-Click Unity Editor Setup & Windows 64-Bit Build

All scenes, prefabs, lighting, audio clips, and optimizations are automated under **`Tools → SinkOrThink3D`**:

1. **Build, Optimize & Audit All 3 Scenes**:
   - Click **`Tools → SinkOrThink3D → Build & Verify All Scenes (Phases 9.5 -> 13)`**.
   - Rebuilds Discovery Island, Viking Island, Final Treasure Island, WAV audio clips (`Assets/Audio/`), MainMenu Settings button, drifting sky clouds, RTX 2050 terrain/shadow optimizations, and logs the **Phase 13 QA Audit Report** in the Unity Console.
2. **Build Windows 64-Bit Executable (`SinkOrThink3D.exe`)**:
   - Click **`Tools → SinkOrThink3D → Phase 14 - Build Windows 64-Bit Executable`**.
   - Outputs `Builds/Windows64/SinkOrThink3D.exe` (1920x1080 resizable window, `VSync` enabled, RTX 2050 URP tuning) and opens the build folder automatically.

---

## 4. Pushing Local Commits to Personal GitHub (`kylechristianybanez-star`)

If Windows Git Credential Manager still points to an older GitHub account (`kybanez1`), switch credentials before pushing:

```powershell
# Remove cached GitHub credential for github.com or sign out in Windows Credential Manager
git credential-manager github logout
# Next push will prompt you to sign in as kylechristianybanez-star
git push origin main
```

---

## 5. Additional Documentation

- See [SCRIPT_DOCUMENTATION.md](SCRIPT_DOCUMENTATION.md) for beginner-friendly explanations of all 36 gameplay scripts and 13 Editor tools.
