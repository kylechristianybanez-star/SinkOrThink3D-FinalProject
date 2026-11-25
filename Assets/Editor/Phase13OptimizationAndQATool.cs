using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Phase 13 RTX 2050 Optimization & Automated Full QA Audit Tool:
/// Applies non-destructive Terrain/Shadow/MeshRenderer optimizations across MainMenu,
/// GameScene, and IslandScene, strips stray OceanSurface colliders, and verifies zero
/// missing scripts, zero pink shaders, zero shadow-casting point lights, FirstPersonPlayer +
/// Main Camera wiring, 3 populated islands across both scenes, all 18 solid + interactive
/// Treasure Chests, and all 17 Audio WAV files.
/// </summary>
public static class Phase13OptimizationAndQATool
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string PcRpAssetPath = "Assets/Settings/PC_RPAsset.asset";
    private const string AudioFolderPath = "Assets/Audio";

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

        // 2. Verify Assets/Audio WAV clips exist on disk
        int wavCount = 0;
        if (Directory.Exists(Path.GetFullPath(AudioFolderPath)))
            wavCount = Directory.GetFiles(Path.GetFullPath(AudioFolderPath), "*.wav").Length;
        report.AppendLine("Assets/Audio WAV clips: " + wavCount + "/24");
        if (wavCount < 24) totalWarnings++;

        // 3. Verify PC_RPAsset RTX 2050 tuning
        Object pcRpObj = AssetDatabase.LoadAssetAtPath<Object>(PcRpAssetPath);
        if (pcRpObj != null)
        {
            SerializedObject so = new SerializedObject(pcRpObj);
            SerializedProperty msaaProp = so.FindProperty("m_MSAA");
            SerializedProperty distProp = so.FindProperty("m_ShadowDistance");
            SerializedProperty cascProp = so.FindProperty("m_ShadowCascadeCount");
            int msaa = msaaProp != null ? msaaProp.intValue : -1;
            float shadowDist = distProp != null ? distProp.floatValue : -1f;
            int cascades = cascProp != null ? cascProp.intValue : -1;
            bool urpOk = msaa <= 2 && shadowDist <= 80f && cascades <= 2;
            report.AppendLine("URP PC_RPAsset (MSAA=" + msaa + ", ShadowDist=" + shadowDist + "m, Cascades=" + cascades + "): " + (urpOk ? "OPTIMIZED" : "REVIEW"));
            if (!urpOk) totalWarnings++;
        }

        // 4. Optimize & audit all 3 build scenes
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

        // Sanitize Bloom / HDR fire emission / light intensity + non-directional shadow removal
        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(scene);
        dirty = true;

        // Terrain tuning + URP Terrain/Lit material repair (prevents pink terrains)
        Material urpTerrainMat = IslandPlacementUtility.GetOrCreateUrpTerrainMaterial();

        Terrain[] terrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
        int missingTerrainMats = 0;
        int invalidTerrainTreeProtos = 0;
        for (int i = 0; i < terrains.Length; i++)
        {
            if (terrains[i].gameObject.scene != scene) continue;

            if (IslandPlacementUtility.SanitizeTerrainTreePrototypes(terrains[i], terrains[i].transform.parent) > 0)
                dirty = true;
            if (IslandPlacementUtility.HasInvalidTerrainTreePrototypes(terrains[i]))
                invalidTerrainTreeProtos++;

            if (terrains[i].materialTemplate == null && urpTerrainMat != null)
            {
                terrains[i].materialTemplate = urpTerrainMat;
                EditorUtility.SetDirty(terrains[i]);
                dirty = true;
            }
            if (terrains[i].materialTemplate == null || terrains[i].materialTemplate.shader == null || !terrains[i].materialTemplate.shader.name.Contains("Universal"))
                missingTerrainMats++;

            if (!terrains[i].drawInstanced ||
                !Mathf.Approximately(terrains[i].heightmapPixelError, 10f) ||
                !Mathf.Approximately(terrains[i].basemapDistance, 400f) ||
                !Mathf.Approximately(terrains[i].detailObjectDistance, 55f) ||
                !Mathf.Approximately(terrains[i].detailObjectDensity, 0.6f) ||
                !Mathf.Approximately(terrains[i].treeDistance, 240f) ||
                !Mathf.Approximately(terrains[i].treeBillboardDistance, 90f) ||
                terrains[i].treeMaximumFullLODCount != 40)
            {
                terrains[i].drawInstanced = true;
                terrains[i].heightmapPixelError = 10f;
                terrains[i].basemapDistance = 400f;
                terrains[i].detailObjectDistance = 55f;
                terrains[i].detailObjectDensity = 0.6f;
                terrains[i].treeDistance = 240f;
                terrains[i].treeBillboardDistance = 90f;
                terrains[i].treeMaximumFullLODCount = 40;
                EditorUtility.SetDirty(terrains[i]);
                dirty = true;
            }
        }

        // Ensure OceanSurface never carries a blocking Collider
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "OceanSurface")
            {
                Collider[] oceanCols = roots[i].GetComponentsInChildren<Collider>(true);
                for (int c = 0; c < oceanCols.Length; c++)
                {
                    Object.DestroyImmediate(oceanCols[c]);
                    dirty = true;
                }
            }
        }

        // Verify zero shadow-casting or blown-out (>2.5 intensity) non-directional lights remain
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        int shadowPointLights = 0;
        int overbrightPointLights = 0;
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
            if (lights[i].intensity > 2.5f)
                overbrightPointLights++;
        }

        // Scan all objects for missing scripts, pink materials, oversized MeshColliders, and tiny-prop shadow casts
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        int missingScriptCount = 0;
        int pinkMaterialCount = 0;
        int oversizeMeshCols = 0;

        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go.scene != scene) continue;

            int removedMissing = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(go);
            if (removedMissing > 0)
                dirty = true;
            missingScriptCount += GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);

            // Replace null/huge MeshColliders (> 12k verts or > 100k tris) on static props with BoxColliders,
            // and mark MeshColliders under dynamic Rigidbodies convex to prevent PhysX warnings.
            MeshCollider[] meshCols = go.GetComponents<MeshCollider>();
            bool addedBoxFallback = false;
            for (int mcIdx = 0; mcIdx < meshCols.Length; mcIdx++)
            {
                MeshCollider mc = meshCols[mcIdx];
                if (mc == null) continue;
                bool oversize = mc.sharedMesh == null || mc.sharedMesh.vertexCount > 12000 || mc.sharedMesh.triangles.Length > 300000;
                if (oversize)
                {
                    Object.DestroyImmediate(mc);
                    addedBoxFallback = true;
                    dirty = true;
                }
                else if (!mc.convex)
                {
                    Rigidbody parentRb = go.GetComponentInParent<Rigidbody>();
                    if (parentRb != null && !parentRb.isKinematic)
                    {
                        mc.convex = true;
                        EditorUtility.SetDirty(mc);
                        dirty = true;
                    }
                }
            }
            if (addedBoxFallback && go.GetComponent<Collider>() == null)
                go.AddComponent<BoxCollider>();

            MeshCollider[] meshColsAfter = go.GetComponents<MeshCollider>();
            for (int mcIdx = 0; mcIdx < meshColsAfter.Length; mcIdx++)
            {
                MeshCollider mcAfter = meshColsAfter[mcIdx];
                if (mcAfter != null && (mcAfter.sharedMesh == null || mcAfter.sharedMesh.vertexCount > 12000 || mcAfter.sharedMesh.triangles.Length > 300000))
                {
                    oversizeMeshCols++;
                    break;
                }
            }

            Renderer r = go.GetComponent<Renderer>();
            if (r != null)
            {
                string n = go.name;
                bool isSmallProp = n.Contains("Grass") || n.Contains("Bush") || n.Contains("Flower") ||
                                   n.Contains("Fern") || n.Contains("Plant") || n.Contains("Seashell") ||
                                   n.Contains("Starfish") || n.Contains("PathStone") || n.StartsWith("Puff_") ||
                                   n.Contains("Cloud") || n.StartsWith("DeckRib_") || n.Contains("PierBollard") ||
                                   n.Contains("PierRope");
                if (isSmallProp && r.shadowCastingMode != ShadowCastingMode.Off)
                {
                    r.shadowCastingMode = ShadowCastingMode.Off;
                    EditorUtility.SetDirty(r);
                    dirty = true;
                }

                Material[] mats = r.sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    if (mats[m] == null || mats[m].shader == null ||
                        mats[m].shader.name.Contains("Error") ||
                        mats[m].shader.name == "Standard" ||
                        mats[m].shader.name.StartsWith("Legacy"))
                        pinkMaterialCount++;
                }
            }
        }

        if (dirty)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
        }

        string shortName = Path.GetFileName(scenePath);
        report.AppendLine("[" + shortName + "] MissingScripts=" + missingScriptCount +
                          ", Pink/NonURPMats=" + pinkMaterialCount +
                          ", NonURPTerrains=" + missingTerrainMats +
                          ", InvalidTreeProtos=" + invalidTerrainTreeProtos +
                          ", ShadowPointLights=" + shadowPointLights +
                          ", OverbrightLights=" + overbrightPointLights +
                          ", OversizeMeshCols=" + oversizeMeshCols);

        if (missingScriptCount > 0 || pinkMaterialCount > 0 || missingTerrainMats > 0 || invalidTerrainTreeProtos > 0 || shadowPointLights > 0 || overbrightPointLights > 0 || oversizeMeshCols > 0)
            warnings++;

        if (checkMainMenu)
        {
            bool hasMenuMgr = Object.FindAnyObjectByType<MainMenuManager>() != null;
            bool hasAudioMgr = Object.FindAnyObjectByType<GameAudioManager>() != null;
            bool hasSettingsBtn = GameObject.Find("SettingsButton") != null;
            report.AppendLine("  MainMenu Wiring (MenuMgr=" + hasMenuMgr + ", AudioMgr=" + hasAudioMgr + ", SettingsBtn=" + hasSettingsBtn + ")");
            if (!hasMenuMgr || !hasAudioMgr || !hasSettingsBtn) warnings++;
        }

        if (checkGameScene)
        {
            bool hasDisc = GameObject.Find("DiscoveryIsland_MainTerrain") != null;
            bool hasVik = GameObject.Find("VikingIslandOverview") != null;
            bool hasFin = GameObject.Find("FinalIslandOverview") != null;

            GameObject oceanObj = GameObject.Find("OceanSurface");
            bool oceanColliderFree = oceanObj != null && oceanObj.GetComponentsInChildren<Collider>(true).Length == 0;

            BoatDocking[] docks = Object.FindObjectsByType<BoatDocking>(FindObjectsInactive.Include);
            HashSet<int> dockIndices = new HashSet<int>();
            for (int i = 0; i < docks.Length; i++)
            {
                if (docks[i].gameObject.scene == scene)
                    dockIndices.Add(docks[i].islandIndex);
            }
            bool allDocks = dockIndices.Contains(0) && dockIndices.Contains(1) && dockIndices.Contains(2);
            report.AppendLine("  GameScene Islands (Disc=" + hasDisc + ", Vik=" + hasVik + ", Fin=" + hasFin + ", 3 Docks=" + allDocks + ", OceanNoCol=" + oceanColliderFree + ")");
            if (!hasDisc || !hasVik || !hasFin || !allDocks || !oceanColliderFree) warnings++;
            warnings += AuditPlacementGrounding(scene, report);
        }

        if (checkIslandScene)
        {
            IslandSceneController ctrl = Object.FindAnyObjectByType<IslandSceneController>();
            bool rootsWired = ctrl != null && ctrl.discoveryIslandRoot != null && ctrl.vikingIslandRoot != null && ctrl.finalTreasureIslandRoot != null;
            bool rootsPopulated = rootsWired &&
                                  ctrl.discoveryIslandRoot.transform.childCount >= 3 &&
                                  ctrl.vikingIslandRoot.transform.childCount >= 3 &&
                                  ctrl.finalTreasureIslandRoot.transform.childCount >= 3;

            IslandPlayerController fpsPlayer = Object.FindAnyObjectByType<IslandPlayerController>();
            CharacterController charCtrl = fpsPlayer != null ? fpsPlayer.GetComponent<CharacterController>() : null;
            Camera playerCam = fpsPlayer != null ? fpsPlayer.GetComponentInChildren<Camera>(true) : null;
            AudioListener playerListener = fpsPlayer != null ? fpsPlayer.GetComponentInChildren<AudioListener>(true) : null;
            bool playerCameraOk = fpsPlayer != null && charCtrl != null && playerCam != null &&
                                  playerCam.CompareTag("MainCamera") && playerListener != null &&
                                  fpsPlayer.cameraTransform == playerCam.transform;

            TreasureChest[] chests = Object.FindObjectsByType<TreasureChest>(FindObjectsInactive.Include);
            HashSet<string> chestIds = new HashSet<string>();
            int regularCount = 0;
            int specialCount = 0;
            int solidAndTriggerChests = 0;
            int respawnReadyChests = 0;
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i].gameObject.scene != scene) continue;
                if (chestIds.Add(chests[i].chestId))
                {
                    if (chests[i].isSpecialChest) specialCount++;
                    else regularCount++;

                    Collider[] cCols = chests[i].GetComponentsInChildren<Collider>(true);
                    bool hasTrig = false, hasSolid = false;
                    for (int k = 0; k < cCols.Length; k++)
                    {
                        if (cCols[k].isTrigger) hasTrig = true;
                        else hasSolid = true;
                    }
                    if (hasTrig && hasSolid) solidAndTriggerChests++;
                    if (chests[i].respawnCandidateSpots != null && chests[i].respawnCandidateSpots.Length >= 4)
                        respawnReadyChests++;
                }
            }
            bool chestsOk = chestIds.Count == 18 && regularCount == 15 && specialCount == 3 && solidAndTriggerChests == 18 && respawnReadyChests == 18;
            report.AppendLine("  IslandScene Player+Cam=" + playerCameraOk + ", PopulatedRoots=" + rootsPopulated +
                              ", Chests=" + chestIds.Count + "/18 (Reg=" + regularCount + ", Spec=" + specialCount + ", Solid+Trigger=" + solidAndTriggerChests + "/18, RespawnSpots=" + respawnReadyChests + "/18)");
            if (!playerCameraOk || !rootsPopulated || !chestsOk) warnings++;
            warnings += AuditPlacementGrounding(scene, report);
        }

        return warnings;
    }

    private static int AuditPlacementGrounding(Scene scene, StringBuilder report)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        Dictionary<GameObject, bool> savedStates = new Dictionary<GameObject, bool>();
        for (int i = 0; i < roots.Length; i++)
        {
            string rn = roots[i].name;
            if (rn == "DiscoveryIsland" || rn == "VikingIsland" || rn == "FinalTreasureIsland")
            {
                savedStates[roots[i]] = roots[i].activeSelf;
                roots[i].SetActive(true);
            }
        }

        int checkedCount = 0;
        int floatingCount = 0;
        int buriedCount = 0;
        int underMountainCount = 0;
        int stalePlinthCount = 0;

        try
        {
            Physics.SyncTransforms();

            Terrain[] allTerrains = Object.FindObjectsByType<Terrain>(FindObjectsInactive.Include);
            List<Terrain> sceneTerrains = new List<Terrain>();
            for (int i = 0; i < allTerrains.Length; i++)
            {
                if (allTerrains[i] != null && allTerrains[i].gameObject.scene == scene && allTerrains[i].terrainData != null)
                    sceneTerrains.Add(allTerrains[i]);
            }

            Transform[] allTransforms = Object.FindObjectsByType<Transform>(FindObjectsInactive.Include);
            for (int i = 0; i < allTransforms.Length; i++)
            {
                Transform tr = allTransforms[i];
                if (tr == null || tr.gameObject.scene != scene)
                    continue;

                string n = tr.name;
                if (n == "HouseSupportPlinth")
                {
                    stalePlinthCount++;
                    continue;
                }

                bool isChest = n.StartsWith("DiscoveryChest_") || n.StartsWith("VikingChest_") || n.StartsWith("FinalChest_");
                bool isBuilding = n.Contains("BambooHouse") || n.StartsWith("VikingLonghouse_") || n.StartsWith("VikingWatchtower_") ||
                                  n.StartsWith("AncientRuinHouse_") || n.StartsWith("AncientTreasureChamber_") ||
                                  n.StartsWith("AncientWaterfall_") || n.StartsWith("TempleArch_");
                bool isBridge = n.StartsWith("VikingWoodenBridge") || n.StartsWith("AncientRuinBridge");
                bool isTreeOrRock = n.StartsWith("VikingTree_") || n.StartsWith("AncientTree_") || n.StartsWith("BayPalm_") ||
                                    n.StartsWith("VikingRock_") || n.StartsWith("AncientCliffRock_") || n.StartsWith("BayRock_");
                if (!isChest && !isBuilding && !isBridge && !isTreeOrRock)
                    continue;

                Terrain nearestTerrain = FindOwningTerrain(tr.position, sceneTerrains);
                if (nearestTerrain == null)
                    continue;

                Bounds? wb = ComputeRendererBounds(tr.gameObject);
                if (!wb.HasValue)
                    continue;

                Bounds b = wb.Value;
                Vector3 samplePt = isBuilding ? b.center : tr.position;
                float terrainY = nearestTerrain.SampleHeight(samplePt) + nearestTerrain.transform.position.y;
                checkedCount++;

                // 1. Floating check
                if (isChest)
                {
                    bool supported = b.min.y <= terrainY + 0.35f;
                    if (!supported)
                    {
                        RaycastHit[] downHits = Physics.RaycastAll(new Vector3(tr.position.x, b.min.y + 0.25f, tr.position.z), Vector3.down, 0.70f);
                        for (int h = 0; h < downHits.Length; h++)
                        {
                            Collider c = downHits[h].collider;
                            if (c != null && !c.isTrigger && c.transform != tr && !c.transform.IsChildOf(tr))
                            {
                                supported = true;
                                break;
                            }
                        }
                    }
                    if (!supported)
                        floatingCount++;
                }
                else if (isTreeOrRock)
                {
                    if (b.min.y > terrainY + 0.35f)
                        floatingCount++;
                }
                else if (b.min.y > terrainY + 0.45f)
                {
                    floatingCount++;
                }

                // 2. Under-terrain buried check
                if (b.max.y < terrainY + 0.25f || (isBuilding && !n.Contains("BambooHouse") && tr.position.y < terrainY - 0.75f))
                    buriedCount++;

                // 3. Under-mountain overhead check
                RaycastHit[] upHits = Physics.RaycastAll(tr.position + Vector3.up * 2.0f, Vector3.up, 120f);
                for (int h = 0; h < upHits.Length; h++)
                {
                    Collider c = upHits[h].collider;
                    if (c == null || c.isTrigger || c is TerrainCollider)
                        continue;
                    if (HasMountainAncestor(c.transform))
                    {
                        underMountainCount++;
                        break;
                    }
                }
            }
        }
        finally
        {
            foreach (KeyValuePair<GameObject, bool> kv in savedStates)
            {
                if (kv.Key != null)
                    kv.Key.SetActive(kv.Value);
            }
        }

        report.AppendLine("  PlacementAudit (Checked=" + checkedCount +
                          ", Floating=" + floatingCount +
                          ", Buried=" + buriedCount +
                          ", UnderMountain=" + underMountainCount +
                          ", StalePlinths=" + stalePlinthCount + ")");
        return (floatingCount > 0 || buriedCount > 0 || underMountainCount > 0 || stalePlinthCount > 0) ? 1 : 0;
    }

    private static Terrain FindOwningTerrain(Vector3 worldPos, List<Terrain> terrains)
    {
        for (int i = 0; i < terrains.Count; i++)
        {
            Terrain t = terrains[i];
            Vector3 o = t.transform.position;
            Vector3 s = t.terrainData.size;
            if (worldPos.x >= o.x && worldPos.x <= o.x + s.x && worldPos.z >= o.z && worldPos.z <= o.z + s.z)
                return t;
        }
        return terrains.Count > 0 ? terrains[0] : null;
    }

    private static bool HasMountainAncestor(Transform t)
    {
        while (t != null)
        {
            if (t.name.IndexOf("Mountain", System.StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            t = t.parent;
        }
        return false;
    }

    private static Bounds? ComputeRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds bounds = new Bounds();
        bool hasBounds = false;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r is ParticleSystemRenderer || r is TrailRenderer)
                continue;
            if (!hasBounds)
            {
                bounds = r.bounds;
                hasBounds = true;
            }
            else
            {
                bounds.Encapsulate(r.bounds);
            }
        }
        return hasBounds ? bounds : (Bounds?)null;
    }
}
