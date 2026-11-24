using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 13 RTX 2050 Optimization & Automated Full QA Audit Tool:
/// Applies non-destructive Terrain/Shadow/MeshRenderer optimizations across MainMenu,
/// GameScene, and IslandScene, and verifies zero missing scripts, zero pink shaders,
/// zero shadow-casting point lights, 3 island overviews/docks, and all 18 Treasure Chests.
/// </summary>
public static class Phase13OptimizationAndQATool
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string PcRpAssetPath = "Assets/Settings/PC_RPAsset.asset";

    [MenuItem("Tools/SinkOrThink3D/Phase 13 - Optimize Scenes & Run Full QA Audit")]
    public static void RunPhase13Tool()
    {
        RunOptimizationAndAudit(true);
    }

    public static void RunOptimizationAndAudit(bool showDialog)
    {
        if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        StringBuilder report = new StringBuilder();
        int totalWarnings = 0;

        // 1. Verify Build Settings scenes
        EditorBuildSettingsScene[] buildScenes = EditorBuildSettings.scenes;
        bool hasMain = false, hasGame = false, hasIsland = false;
        for (int i = 0; i < buildScenes.Length; i++)
        {
            if (!buildScenes[i].enabled) continue;
            if (buildScenes[i].path == MainMenuPath) hasMain = true;
            else if (buildScenes[i].path == GameScenePath) hasGame = true;
            else if (buildScenes[i].path == IslandScenePath) hasIsland = true;
        }
        report.AppendLine("Build Settings (MainMenu / GameScene / IslandScene): " + (hasMain && hasGame && hasIsland ? "PASS" : "MISSING SCENES"));
        if (!hasMain || !hasGame || !hasIsland) totalWarnings++;

        // 2. Verify PC_RPAsset RTX 2050 tuning
        Object pcRpObj = AssetDatabase.LoadAssetAtPath<Object>(PcRpAssetPath);
        if (pcRpObj != null)
        {
            SerializedObject so = new SerializedObject(pcRpObj);
            int msaa = so.FindProperty("m_MSAA")?.intValue ?? -1;
            float shadowDist = so.FindProperty("m_ShadowDistance")?.floatValue ?? -1f;
            int cascades = so.FindProperty("m_ShadowCascadeCount")?.intValue ?? -1;
            bool urpOk = msaa <= 2 && shadowDist <= 80f && cascades <= 2;
            report.AppendLine("URP PC_RPAsset (MSAA=" + msaa + ", ShadowDist=" + shadowDist + "m, Cascades=" + cascades + "): " + (urpOk ? "OPTIMIZED" : "REVIEW"));
            if (!urpOk) totalWarnings++;
        }

        // 3. Optimize & audit all 3 build scenes
        totalWarnings += OptimizeAndAuditScene(MainMenuPath, report, checkMainMenu: true);
        totalWarnings += OptimizeAndAuditScene(GameScenePath, report, checkGameScene: true);
        totalWarnings += OptimizeAndAuditScene(IslandScenePath, report, checkIslandScene: true);

        AssetDatabase.SaveAssets();

        string summary = report.ToString();
        Debug.Log("=== PHASE 13 QA AUDIT REPORT (Warnings: " + totalWarnings + ") ===\n" + summary);

        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                totalWarnings == 0 ? "Phase 13 QA Audit: ALL PASSED" : "Phase 13 QA Audit: " + totalWarnings + " Issue(s)",
                summary,
                "OK"
            );
        }
    }

    private static int OptimizeAndAuditScene(
        string scenePath,
        StringBuilder report,
        bool checkMainMenu = false,
        bool checkGameScene = false,
        bool checkIslandScene = false)
    {
        int warnings = 0;
        Scene scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
        bool dirty = false;

        // Terrain tuning
        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < terrains.Length; i++)
        {
            if (terrains[i].gameObject.scene != scene) continue;
            if (!Mathf.Approximately(terrains[i].heightmapPixelError, 8f) || !Mathf.Approximately(terrains[i].basemapDistance, 450f))
            {
                terrains[i].heightmapPixelError = 8f;
                terrains[i].basemapDistance = 450f;
                EditorUtility.SetDirty(terrains[i]);
                dirty = true;
            }
        }

        // Non-directional lights must never cast realtime shadows (prevents Shadow Atlas overflow)
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        int shadowPointLights = 0;
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].gameObject.scene != scene || lights[i].type == LightType.Directional) continue;
            if (lights[i].shadows != LightShadows.None)
            {
                lights[i].shadows = LightShadows.None;
                EditorUtility.SetDirty(lights[i]);
                dirty = true;
            }
            if (lights[i].shadows != LightShadows.None)
                shadowPointLights++;
        }

        // Scan all objects for missing scripts, pink materials, and tiny-prop shadow casts
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int missingScriptCount = 0;
        int pinkMaterialCount = 0;

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go.scene != scene) continue;

            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);

            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                string n = go.name;
                bool isSmallProp = n.Contains("Grass") || n.Contains("Seashell") || n.Contains("Starfish") ||
                                   n.Contains("PathStone") || n.StartsWith("Puff_") || n.Contains("Cloud");
                if (isSmallProp && r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    EditorUtility.SetDirty(r);
                    dirty = true;
                }

                Material[] mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null || mats[m].shader == null || mats[m].shader.name.Contains("Error"))
                        pinkMaterialCount++;
                }
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        string shortName = System.IO.Path.GetFileName(scenePath);
        report.AppendLine("[" + shortName + "] MissingScripts=" + missingScriptCount +
                          ", PinkMats=" + pinkMaterialCount +
                          ", ShadowPointLights=" + shadowPointLights);

        if (missingScriptCount > 0 || pinkMaterialCount > 0 || shadowPointLights > 0)
            warnings++;

        if (checkMainMenu)
        {
            bool hasMenuMgr = Object.FindFirstObjectByType<MainMenuManager>() != null;
            bool hasAudioMgr = Object.FindFirstObjectByType<GameAudioManager>() != null;
            bool hasSettingsBtn = GameObject.Find("SettingsButton") != null;
            report.AppendLine("  MainMenu Wiring (MenuMgr=" + hasMenuMgr + ", AudioMgr=" + hasAudioMgr + ", SettingsBtn=" + hasSettingsBtn + ")");
            if (!hasMenuMgr || !hasAudioMgr || !hasSettingsBtn) warnings++;
        }

        if (checkGameScene)
        {
            bool hasDisc = GameObject.Find("DiscoveryIsland_MainTerrain") != null;
            bool hasVik = GameObject.Find("VikingIslandOverview") != null;
            bool hasFin = GameObject.Find("FinalIslandOverview") != null;
            BoatDocking[] docks = Object.FindObjectsByType<BoatDocking>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            HashSet<int> dockIndices = new HashSet<int>();
            for (int i = 0; i < docks.Length; i++)
            {
                if (docks[i].gameObject.scene == scene)
                    dockIndices.Add(docks[i].islandIndex);
            }
            bool allDocks = dockIndices.Contains(0) && dockIndices.Contains(1) && dockIndices.Contains(2);
            report.AppendLine("  GameScene Islands (Disc=" + hasDisc + ", Vik=" + hasVik + ", Fin=" + hasFin + ", 3 Docks=" + allDocks + ")");
            if (!hasDisc || !hasVik || !hasFin || !allDocks) warnings++;
        }

        if (checkIslandScene)
        {
            IslandSceneController ctrl = Object.FindFirstObjectByType<IslandSceneController>();
            bool rootsWired = ctrl != null && ctrl.discoveryIslandRoot != null && ctrl.vikingIslandRoot != null && ctrl.finalTreasureIslandRoot != null;

            TreasureChest[] chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            HashSet<string> chestIds = new HashSet<string>();
            int regularCount = 0;
            int specialCount = 0;
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i].gameObject.scene != scene) continue;
                if (chestIds.Add(chests[i].chestId))
                {
                    if (chests[i].isSpecialChest) specialCount++;
                    else regularCount++;
                }
            }
            bool chestsOk = chestIds.Count == 18 && regularCount == 15 && specialCount == 3;
            report.AppendLine("  IslandScene Roots=" + rootsWired + ", UniqueChests=" + chestIds.Count + "/18 (Reg=" + regularCount + ", Spec=" + specialCount + ")");
            if (!rootsWired || !chestsOk) warnings++;
        }

        return warnings;
    }
}
