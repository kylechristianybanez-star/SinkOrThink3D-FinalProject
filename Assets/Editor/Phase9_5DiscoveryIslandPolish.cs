using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Phase 9.5 Master Setup & Polish Tool:
/// Unifies and polishes Discovery Island across BOTH GameScene.unity and IslandScene.unity,
/// adds missing colliders on palms/rocks/bridges/houses, fixes dock parking proximity,
/// organizes reusable IslandScene parents (DiscoveryIsland, VikingIsland, FinalTreasureIsland),
/// and fixes the 27-point-light shadow atlas overflow.
/// </summary>
public static class Phase9_5DiscoveryIslandPolish
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string PolyartSourceTerrainPath = "Assets/POLYART_Ancient Village/Terrain/TerrainData.asset";
    private const string DiscoveryTerrainPath = "Assets/Terrain/DiscoveryIslandTerrain.asset";
    private const string SkyMaterialPath = "Assets/Free Island Collection/Environment/Skies/Sky Material.mat";
    private const string GameSceneWaterMatGuid = "379410282482c54498410674d0c4391d";
    private const string FallbackWaterMatPath = "Assets/Island/Textures/Materials/Water.mat";

    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string LampPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Bamboolamp_Set1.prefab";
    private const string RockPile1Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile1.prefab";
    private const string RockPile3Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile3.prefab";
    private const string RockPile6Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile6.prefab";

    private const string PalmPack1Path = "Assets/PalmTreePack/Prefabs/1/Palm_1_1.prefab";
    private const string PalmTallPath = "Assets/ToonTropical/Prefabs/SM_Island_PalmTree_Tall.prefab";
    private const string PalmShortPath = "Assets/ToonTropical/Prefabs/SM_Island_PalmTree_Short.prefab";
    private const string BarrelPath = "Assets/ToonTropical/Prefabs/SM_Island_Barrel.prefab";
    private const string CratePath = "Assets/ToonTropical/Prefabs/SM_Island_Crate.prefab";
    private const string DriftwoodPath = "Assets/ToonTropical/Prefabs/SM_Island_Driftwood.prefab";
    private const string SeashellPath = "Assets/ToonTropical/Prefabs/SM_Island_Seashell.prefab";
    private const string StarfishPath = "Assets/ToonTropical/Prefabs/SM_Island_Starfish.prefab";
    private const string CrabPath = "Assets/ToonTropical/Prefabs/SK_Island_Crab.prefab";

    [MenuItem("Tools/SinkOrThink3D/Phase 9.5 - Build & Polish Discovery Island (GameScene + IslandScene)")]
    public static void BuildAndPolishDiscoveryIsland()
    {
        RunBuild(true);
    }

    public static void RunBuild(bool showDialog)
    {
        if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Terrain"))
            AssetDatabase.CreateFolder("Assets", "Terrain");

        // Reset shared DiscoveryIslandTerrain from Polyart source once before flattening bamboo house pads
        TerrainData discoveryTerrainData = IslandPlacementUtility.ResetTerrainFromSourceKeepSize(PolyartSourceTerrainPath, DiscoveryTerrainPath);

        // Build/refresh reusable chest prefabs first
        Phase6DiscoveryIslandSetup.BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);

        // Part A: Polish IslandScene FIRST (flattens bamboo house pads into DiscoveryIslandTerrain.asset)
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        PolishIslandScene(islandScene, regularChestPrefab, specialChestPrefab, discoveryTerrainData);
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);
        AssetDatabase.SaveAssets();

        // Part B: Polish GameScene SECOND using the already-leveled DiscoveryIslandTerrain.asset
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        PolishGameScene(gameScene, discoveryTerrainData);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        AssetDatabase.SaveAssets();

        Debug.Log("Phase 9.5 Complete! Polished Discovery Island in both GameScene.unity and IslandScene.unity.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase 9.5 Discovery Island Polish Complete",
                "Updated and saved BOTH GameScene.unity and IslandScene.unity:\n" +
                "- Fixed Shadow Atlas overflow, Quiz Board frame, Boat animations & Ocean fog\n" +
                "- Fixed close Boat Dock alignment & Return-to-Dock spawning\n" +
                "- Built reusable IslandScene (DiscoveryIsland active; VikingIsland & FinalTreasureIsland placeholders ready)\n" +
                "- Added solid colliders to all Palm Trees, Rocks, Bamboo Houses, Bridge & Ramp\n" +
                "- Placed 6 scenic, reachable Discovery Chests + Dock Repair Workbench",
                "OK"
            );
        }
    }

    private static void PolishGameScene(Scene gameScene, TerrainData discoveryTerrainData)
    {
        Phase3BoatAndBoardSetup.ApplyToScene(gameScene);
        Phase4OceanAndDockSetup.ApplyToScene(gameScene);

        // OceanSurface must NOT carry a physics collider and should span 5000m across all 3 islands
        GameObject oceanObj = FindSceneObjectByName(gameScene, "OceanSurface");
        if (oceanObj != null)
        {
            oceanObj.transform.position = new Vector3(0f, 0f, 180f);
            oceanObj.transform.localScale = new Vector3(500f, 1f, 500f);
            RemoveCollidersOnObject(oceanObj);
        }

        GameObject islandDest = FindSceneObjectByName(gameScene, "IslandDestination");
        if (islandDest == null)
            islandDest = CreateSceneRoot(gameScene, "IslandDestination", true);

        // Preserve BoatDockPoint reference before rebuilding IslandDestination children
        GameObject dockPoint = FindSceneObjectByName(gameScene, "BoatDockPoint");
        if (dockPoint != null && dockPoint.transform.IsChildOf(islandDest.transform))
            dockPoint.transform.SetParent(null, true);

        for (int i = islandDest.transform.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(islandDest.transform.GetChild(i).gameObject);

        islandDest.transform.position = Vector3.zero;
        islandDest.transform.rotation = Quaternion.identity;
        islandDest.transform.localScale = Vector3.one;

        // Import handcrafted POLYART_Ancient Village island into IslandDestination using shared leveled terrain
        IslandPlacementUtility.CopyPolyartAncientVillage(
            gameScene,
            islandDest.transform,
            new Vector3(0f, 0f, 220f),
            "DiscoveryIsland_MainTerrain",
            out Terrain overviewTerrain,
            discoveryTerrainData,
            false
        );

        Vector3 shoreEdge = new Vector3(14f, 0f, 144f);
        Vector3 inlandDir = Vector3.forward;
        if (overviewTerrain != null)
            IslandPlacementUtility.FindClearWaterShorelineDock(overviewTerrain, out shoreEdge, out inlandDir);

        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 rightDir = Vector3.Cross(Vector3.up, inlandDir).normalized;

        Material woodMat = GetOrCreateLitMaterial("QuizBoardFrameWood.mat", new Color(0.42f, 0.27f, 0.16f, 1f), 0f, 0.18f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material ropeMat = GetOrCreateLitMaterial("QuizBoardRope.mat", new Color(0.66f, 0.54f, 0.36f, 1f), 0f, 0.1f);
        Material glowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.4f);

        GameObject harborRoot = new GameObject("DiscoveryHarborOverview");
        harborRoot.transform.SetParent(islandDest.transform, false);

        IslandPlacementUtility.BuildRealisticHarborPier(
            harborRoot.transform,
            "DiscoveryHarborPier_Overview",
            shoreEdge,
            inlandDir,
            rightDir,
            overviewTerrain,
            woodMat,
            brassMat,
            ropeMat,
            glowMat,
            BarrelPath,
            CratePath,
            true,
            out Vector3 pierOuterTip,
            out Vector3 pierDeckCenter,
            out Vector3 beachEntryPos
        );

        Vector3 archSpot = beachEntryPos + inlandDir * 2.5f;
        if (overviewTerrain != null)
            archSpot = IslandPlacementUtility.FindFlatWalkablePoint(overviewTerrain, archSpot.x, archSpot.z, 8f, 20f, 0.65f);
        GameObject overviewArch = BuildWoodenVillageArch(harborRoot.transform, "Overview_VillageArch", archSpot, dockRot, woodMat, glowMat);
        IslandPlacementUtility.GroundBuildingWithFoundation(overviewArch, overviewTerrain, 2.4f, 0.8f, 0.06f);

        // Coastal harbor bay palms & boulders flanking the cove without overlapping the pier or village
        BuildDiscoveryBayAmbience(harborRoot.transform, overviewTerrain, beachEntryPos, inlandDir, rightDir, 10, 6);

        // Configure OceanBoatSpawner, sailing speed, and clear approach line toward Discovery Harbor Pier
        GameObject boat = FindSceneObjectByName(gameScene, "OldWoodenRowboat_Complete");
        if (boat != null)
        {
            if (boat.GetComponent<OceanBoatSpawner>() == null)
                boat.AddComponent<OceanBoatSpawner>();
            BoatMovement bm = boat.GetComponent<BoatMovement>();
            if (bm != null)
            {
                bm.moveSpeed = 15f;
                bm.turnSpeed = 68f;
                EditorUtility.SetDirty(bm);
            }
            Vector3 boatStartPos = pierOuterTip - inlandDir * 110f;
            boatStartPos.y = 0.08f;
            boat.transform.position = boatStartPos;
            boat.transform.rotation = dockRot;
            EditorUtility.SetDirty(boat);
        }

        // Configure Discovery BoatDocking aligned cleanly off the outer tip of the realistic pier
        if (dockPoint != null)
        {
            dockPoint.transform.SetParent(islandDest.transform, true);
            dockPoint.transform.position = pierOuterTip - inlandDir * 3.2f;
            dockPoint.transform.rotation = dockRot;

            // Remove legacy floating DockVisualDetails posts/lanterns created by Phase4OceanAndDockSetup
            Transform legacyVisuals = dockPoint.transform.Find("DockVisualDetails");
            if (legacyVisuals != null)
                Object.DestroyImmediate(legacyVisuals.gameObject);

            Transform oldBuoy = dockPoint.transform.Find("DiscoveryGuideBuoy");
            if (oldBuoy != null)
                Object.DestroyImmediate(oldBuoy.gameObject);

            Material buoyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DockBuoyRed.mat");
            GameObject buoy = new GameObject("DiscoveryGuideBuoy");
            buoy.transform.SetParent(dockPoint.transform, false);
            buoy.transform.localPosition = new Vector3(-4.5f, -0.55f, -3.5f);
            buoy.AddComponent<GentleFloat>();
            CreatePrimitive(buoy.transform, "Base", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.85f, 0.32f, 0.85f), buoyMat, false);
            CreatePrimitive(buoy.transform, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f), brassMat, false);
            CreatePrimitive(buoy.transform, "Beacon", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.zero, Vector3.one * 0.32f, glowMat, false);

            BoatDocking docking = dockPoint.GetComponent<BoatDocking>();
            if (docking != null)
            {
                Undo.RecordObject(docking, "Polish Discovery BoatDocking");
                docking.islandIndex = 0;
                docking.autoMatchCurrentIsland = false;
                docking.islandDisplayName = "Discovery Island";
                docking.targetIslandSceneName = "IslandScene";
                docking.transitionToIslandScene = true;
                docking.dockingDistance = 12f;

                Transform park = dockPoint.transform.Find("DockParkingPoint");
                if (park != null)
                {
                    park.localPosition = new Vector3(0f, -0.52f, -1.8f);
                    park.localRotation = Quaternion.identity;
                    docking.dockParkingPoint = park;
                }

                GameObject returnSpawn = GetOrCreateChild(dockPoint.transform, "BoatReturnSpawnPoint");
                returnSpawn.transform.localPosition = new Vector3(0f, -0.52f, -6.5f);
                returnSpawn.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                docking.boatReturnSpawnPoint = returnSpawn.transform;

                EditorUtility.SetDirty(docking);
            }
        }

        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(gameScene);
        DisableDecorativePointShadows(gameScene);
    }

    private static void PolishIslandScene(Scene scene, GameObject regularChestPrefab, GameObject specialChestPrefab, TerrainData discoveryTerrainData)
    {
        // Clean up legacy single-island roots if upgrading from Phase 5/6/8
        string[] legacyRoots = new string[]
        {
            "Main Camera", "IslandPlayer", "FirstPersonPlayer", "DiscoveryIsland_Terrain", "DiscoveryDock", "DockBeachRamp",
            "DockedBoat_Return", "DockRepairWorkbench", "IslandSpawnPoint",
            "DiscoveryEnvironment", "DiscoveryChests", "DiscoveryIsland", "VikingIsland", "FinalTreasureIsland"
        };
        for (int i = 0; i < legacyRoots.Length; i++)
            DestroyRootIfExists(scene, legacyRoots[i]);

        // Shared sky, sun, and coastal fog
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (skyMat != null)
            RenderSettings.skybox = skyMat;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.67f, 0.83f, 0.93f, 1f);
        RenderSettings.fogDensity = 0.0018f;

        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.intensity = 1.3f;
            dirLight.color = new Color(1f, 0.95f, 0.86f);
            dirLight.transform.rotation = Quaternion.Euler(46f, -35f, 0f);
        }

        // Shared OceanSurface
        GameObject oceanGo = FindSceneObjectByName(scene, "OceanSurface");
        if (oceanGo == null)
        {
            oceanGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanGo.name = "OceanSurface";
            SceneManager.MoveGameObjectToScene(oceanGo, scene);
        }
        oceanGo.transform.position = new Vector3(0f, 0f, 180f);
        oceanGo.transform.rotation = Quaternion.identity;
        oceanGo.transform.localScale = new Vector3(500f, 1f, 500f);
        MeshRenderer oceanMr = oceanGo.GetComponent<MeshRenderer>();
        if (oceanMr != null)
        {
            oceanMr.shadowCastingMode = ShadowCastingMode.Off;
            string waterPath = AssetDatabase.GUIDToAssetPath(GameSceneWaterMatGuid);
            Material waterMat = !string.IsNullOrEmpty(waterPath)
                ? AssetDatabase.LoadAssetAtPath<Material>(waterPath)
                : AssetDatabase.LoadAssetAtPath<Material>(FallbackWaterMatPath);
            if (waterMat != null)
                oceanMr.sharedMaterial = waterMat;
        }
        if (oceanGo.GetComponent<OceanWaterAnimator>() == null)
            oceanGo.AddComponent<OceanWaterAnimator>();
        RemoveCollidersOnObject(oceanGo);

        // Create Reusable 3-Island Parents
        GameObject discoveryRoot = CreateSceneRoot(scene, "DiscoveryIsland", true);
        GameObject vikingRoot = CreateSceneRoot(scene, "VikingIsland", false);
        GameObject finalRoot = CreateSceneRoot(scene, "FinalTreasureIsland", false);

        // Build Detailed Discovery Island inside DiscoveryIsland parent
        Transform discoverySpawn = BuildDetailedDiscoveryIsland(discoveryRoot.transform, regularChestPrefab, specialChestPrefab, discoveryTerrainData);

        // Ensure Player, HUD, EventSystem, ChestQuizManager, and IslandSceneController (explicit null checks for Unity Objects)
        IslandPlayerController playerCtrl = EnsurePlayerAndHud(scene, discoverySpawn.position, discoverySpawn.rotation, out TMP_Text statusText);

        GameObject ctrlGo = FindSceneObjectByName(scene, "IslandSceneController");
        if (ctrlGo == null) ctrlGo = CreateSceneRoot(scene, "IslandSceneController", true);
        IslandSceneController sceneCtrl = GetOrAdd<IslandSceneController>(ctrlGo);
        sceneCtrl.islandIndex = 0;
        sceneCtrl.islandDisplayName = "Discovery Island";
        sceneCtrl.player = playerCtrl;
        sceneCtrl.islandStatusText = statusText;
        sceneCtrl.discoveryIslandRoot = discoveryRoot;
        sceneCtrl.vikingIslandRoot = vikingRoot;
        sceneCtrl.finalTreasureIslandRoot = finalRoot;
        sceneCtrl.discoverySpawnPoint = discoverySpawn;
        sceneCtrl.playerSpawnPoint = discoverySpawn;
        EditorUtility.SetDirty(sceneCtrl);

        GameObject quizMgrGo = FindSceneObjectByName(scene, "ChestQuizManager");
        if (quizMgrGo == null) quizMgrGo = CreateSceneRoot(scene, "ChestQuizManager", true);
        ChestQuizManager chestQuiz = GetOrAdd<ChestQuizManager>(quizMgrGo);
        chestQuiz.islandIndex = 0;
        EditorUtility.SetDirty(chestQuiz);

        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(scene);
        DisableDecorativePointShadows(scene);
    }

    private static Transform BuildDetailedDiscoveryIsland(Transform parent, GameObject regularChestPrefab, GameObject specialChestPrefab, TerrainData discoveryTerrainData)
    {
        Scene targetScene = parent.gameObject.scene;
        Material woodMat = GetOrCreateLitMaterial("QuizBoardFrameWood.mat", new Color(0.62f, 0.44f, 0.28f, 1f), 0f, 0.18f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material ropeMat = GetOrCreateLitMaterial("QuizBoardRope.mat", new Color(0.66f, 0.54f, 0.36f, 1f), 0f, 0.1f);
        Material glowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.4f);
        Material stoneMat = GetOrCreateTexturedStoneMaterial("IslandPathStone.mat", "Assets/Free Island Collection/Environment/Terrain/Textures/Cliff_1/Cliff_1_Diffuse.tif", new Color(0.76f, 0.72f, 0.64f, 1f));
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        // 1. Handcrafted POLYART_Ancient Village island (flattens bamboo house pads into DiscoveryIslandTerrain.asset)
        IslandPlacementUtility.CopyPolyartAncientVillage(
            targetScene,
            parent,
            new Vector3(0f, 0f, 220f),
            "DiscoveryIsland_Terrain",
            out Terrain terrain,
            discoveryTerrainData,
            true
        );

        Vector3 shoreEdge = new Vector3(14f, 0f, 144f);
        Vector3 inlandDir = Vector3.forward;
        if (terrain != null)
            IslandPlacementUtility.FindClearWaterShorelineDock(terrain, out shoreEdge, out inlandDir);

        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 rightDir = Vector3.Cross(Vector3.up, inlandDir).normalized;

        // 2. Realistic 15m Harbor Pier + Spawn + Return Boat + Repair Bench
        GameObject dockArea = new GameObject("DiscoveryDockArea");
        dockArea.transform.SetParent(parent, false);

        IslandPlacementUtility.BuildRealisticHarborPier(
            dockArea.transform,
            "DiscoveryHarborPier",
            shoreEdge,
            inlandDir,
            rightDir,
            terrain,
            woodMat,
            brassMat,
            ropeMat,
            glowMat,
            BarrelPath,
            CratePath,
            true,
            out Vector3 pierOuterTip,
            out Vector3 pierDeckCenter,
            out Vector3 beachEntryPos
        );

        // Docked Return Boat moored cleanly beside outer tip of pier
        GameObject returnBoat = InstantiateAsset(BoatPrefabPath, dockArea.transform, "DockedBoat_Return");
        Vector3 returnPos = pierOuterTip - inlandDir * 3.0f;
        returnPos.y = 0.08f;
        returnBoat.transform.position = returnPos;
        returnBoat.transform.rotation = dockRot * Quaternion.Euler(0f, 180f, 0f);
        BoatMovement bm = returnBoat.GetComponent<BoatMovement>();
        if (bm != null) Object.DestroyImmediate(bm);
        BoatSink bs = returnBoat.GetComponent<BoatSink>();
        if (bs != null) Object.DestroyImmediate(bs);
        GentleFloat gf = returnBoat.AddComponent<GentleFloat>();
        gf.bobAmplitude = 0.05f;
        gf.tiltAngle = 2.0f;
        IslandBoatReturn boatRet = returnBoat.AddComponent<IslandBoatReturn>();
        boatRet.islandIndex = 0;
        boatRet.oceanSceneName = "GameScene";
        boatRet.interactRange = 8.5f;

        // Dock Repair Workbench neatly on right edge of pier deck
        Vector3 benchPos = pierDeckCenter + rightDir * 1.85f;
        BuildDockRepairBench(dockArea.transform, benchPos, dockRot, woodMat, brassMat, sparkleMat);

        // Spawn point cleanly on center of pier deck facing inland down the gangplank
        GameObject spawnGo = new GameObject("DiscoverySpawnPoint");
        spawnGo.transform.SetParent(dockArea.transform, false);
        spawnGo.transform.position = pierDeckCenter + Vector3.up * 0.06f;
        spawnGo.transform.rotation = dockRot;

        // 3. Shoreline Harbor Archway + Stepping Trail + Bay Palms/Rocks + Animated Beach Crabs
        GameObject envRoot = new GameObject("DiscoveryEnvironment");
        envRoot.transform.SetParent(parent, false);

        Vector3 archSpot = beachEntryPos + inlandDir * 2.5f;
        if (terrain != null)
            archSpot = IslandPlacementUtility.FindFlatWalkablePoint(terrain, archSpot.x, archSpot.z, 8f, 20f, 0.65f);
        GameObject villageArch = BuildWoodenVillageArch(envRoot.transform, "VillageEntranceArch", archSpot, dockRot, woodMat, glowMat);
        IslandPlacementUtility.GroundBuildingWithFoundation(villageArch, terrain, 2.4f, 0.8f, 0.06f);

        // Stepping stone trail leading from beach plaza into the POLYART Ancient Village
        for (int i = 0; i < 10; i++)
        {
            float dist = 5.5f + i * 4.8f;
            float swayX = Mathf.Sin(i * 0.5f) * 1.6f;
            Vector3 spTarget = beachEntryPos + rightDir * swayX + inlandDir * dist;
            Vector3 sp = IslandPlacementUtility.FindFlatWalkablePoint(terrain, spTarget.x, spTarget.z, 6f, 24f, 0.55f);
            if (!IslandPlacementUtility.IsSpotClearOfObstacles(sp, 1.1f))
                sp = IslandPlacementUtility.FindUnobstructedSpot(terrain, sp.x, sp.z, 6f, 1.1f);
            if (terrain != null)
                sp.y = terrain.SampleHeight(sp) + terrain.transform.position.y;
            CreatePrimitive(envRoot.transform, "HarborTrailStone_" + (i + 1), PrimitiveType.Cylinder, sp + Vector3.up * 0.02f, new Vector3(0f, i * 32f, 0f), new Vector3(1.55f, 0.04f, 1.55f), stoneMat, false);
        }

        BuildDiscoveryBayAmbience(envRoot.transform, terrain, beachEntryPos, inlandDir, rightDir, 14, 10);

        Vector3 shore1 = IslandPlacementUtility.FindFlatWalkablePoint(terrain, beachEntryPos.x + rightDir.x * 16f, beachEntryPos.z + rightDir.z * 16f, 12f, 22f, 0.55f);
        Vector3 shore2 = IslandPlacementUtility.FindFlatWalkablePoint(terrain, beachEntryPos.x - rightDir.x * 16f, beachEntryPos.z - rightDir.z * 16f, 12f, 22f, 0.55f);
        Vector3 shore3 = IslandPlacementUtility.FindFlatWalkablePoint(terrain, beachEntryPos.x + rightDir.x * 26f + inlandDir.x * 6f, beachEntryPos.z + rightDir.z * 26f + inlandDir.z * 6f, 12f, 22f, 0.55f);

        GameObject drift = InstantiateGrounded(DriftwoodPath, envRoot.transform, "ShoreDriftwood_1", shore1 - rightDir * 2f, 35f, 1.15f);
        IslandPlacementUtility.GroundObjectExactToTerrain(drift, terrain, 0.08f);
        IslandPlacementUtility.EnsureExactSolidColliders(drift, false, false);
        GameObject shell = InstantiateGrounded(SeashellPath, envRoot.transform, "ShoreShell_1", shore1 + rightDir * 1.5f, 15f, 1.5f);
        IslandPlacementUtility.GroundObjectExactToTerrain(shell, terrain, 0.04f);
        GameObject star = InstantiateGrounded(StarfishPath, envRoot.transform, "ShoreStarfish_1", shore2 - rightDir * 1.5f, 70f, 1.5f);
        IslandPlacementUtility.GroundObjectExactToTerrain(star, terrain, 0.04f);

        AddCrab(envRoot.transform, "BeachCrab_East", shore1, 0f);
        AddCrab(envRoot.transform, "BeachCrab_West", shore2, 90f);
        AddCrab(envRoot.transform, "BeachCrab_Cove", shore3, 180f);

        Vector3 elderSpot = IslandPlacementUtility.FindUnobstructedSpot(terrain, archSpot.x + rightDir.x * 3.8f, archSpot.z + rightDir.z * 3.8f, 8f, 1.0f);
        IslandPlacementUtility.BuildIslandGuideNpc(
            envRoot.transform,
            terrain,
            elderSpot,
            dockRot.eulerAngles.y + 180f,
            0,
            "Elder Kael",
            "Welcome to Discovery Island, sailor! Five bamboo huts shelter treasure chests across this cove.",
            "Tip: Find the Golden Key inside a regular chest, then unlock the Hilltop Shrine's Special Chest for the Magic Compass! Press M for your Map.",
            15,
            new Color(0.22f, 0.54f, 0.62f, 1f),
            new Color(0.68f, 0.52f, 0.24f, 1f)
        );

        // 4. Six Scenic, Well-Spaced Unobstructed Treasure Chests + 8-Spot Respawn Pool
        GameObject chestsRoot = new GameObject("DiscoveryChests");
        chestsRoot.transform.SetParent(parent, false);

        Vector3 villageHub = IslandPlacementUtility.FindFlatWalkablePoint(
            terrain,
            beachEntryPos.x + inlandDir.x * 46f,
            beachEntryPos.z + inlandDir.z * 46f,
            24f,
            20f,
            0.9f
        );

        List<IslandPlacementUtility.PlacedFootprint> chestSpots = new List<IslandPlacementUtility.PlacedFootprint>();
        Vector2[] respawnRel = new Vector2[]
        {
            new Vector2(22f, -24f), new Vector2(-22f, -20f), new Vector2(20f, 6f), new Vector2(-22f, 10f),
            new Vector2(30f, 28f), new Vector2(-30f, 30f), new Vector2(4f, 36f), new Vector2(8f, -12f)
        };
        Vector3[] respawnPool = new Vector3[respawnRel.Length];
        for (int r = 0; r < respawnRel.Length; r++)
        {
            Vector3 target = villageHub + rightDir * respawnRel[r].x + inlandDir * respawnRel[r].y;
            respawnPool[r] = FindSpacedChestSpot(terrain, target.x, target.z, 22f, 9.0f, chestSpots);
        }

        Vector2[] regularRel = new Vector2[]
        {
            new Vector2(22f, -24f),
            new Vector2(-22f, -20f),
            new Vector2(20f, 6f),
            new Vector2(-22f, 10f),
            new Vector2(30f, 28f)
        };

        for (int i = 0; i < regularRel.Length; i++)
        {
            Vector3 p = respawnPool[i];
            Vector3 toHub = villageHub - p;
            toHub.y = 0f;
            Quaternion lookHub = toHub.sqrMagnitude > 0.1f ? Quaternion.LookRotation(toHub.normalized, Vector3.up) : dockRot;

            GameObject chestObj = (GameObject)PrefabUtility.InstantiatePrefab(regularChestPrefab, chestsRoot.transform);
            chestObj.name = "DiscoveryChest_Regular_" + (i + 1);
            chestObj.transform.position = p;
            chestObj.transform.rotation = lookHub;
            IslandPlacementUtility.GroundObjectExactToTerrain(chestObj, terrain, 0.04f);

            TreasureChest tc = chestObj.GetComponent<TreasureChest>();
            tc.chestId = "discovery_regular_" + (i + 1);
            tc.islandIndex = 0;
            tc.isSpecialChest = false;
            tc.respawnCandidateSpots = respawnPool;
            EditorUtility.SetDirty(tc);
        }

        // Special Chest on raised Ancient Discovery Shrine Dais with deep stone foundation skirt
        Vector3 shrineTarget = villageHub + inlandDir * 44f;
        Vector3 shrinePos = FindSpacedChestSpot(terrain, shrineTarget.x, shrineTarget.z, 24f, 10.0f, chestSpots);
        if (terrain != null)
        {
            float maxCornerY = shrinePos.y;
            for (int ox = -1; ox <= 1; ox++)
            {
                for (int oz = -1; oz <= 1; oz++)
                {
                    Vector3 sample = shrinePos + new Vector3(ox * 2.5f, 0f, oz * 2.5f);
                    float sy = terrain.SampleHeight(sample) + terrain.transform.position.y;
                    if (sy > maxCornerY)
                        maxCornerY = sy;
                }
            }
            shrinePos.y = maxCornerY;
        }
        CreatePrimitive(chestsRoot.transform, "SpecialShrineBaseSkirt", PrimitiveType.Cylinder, shrinePos - Vector3.up * 1.18f, Vector3.zero, new Vector3(5.2f, 1.2f, 5.2f), stoneMat, true);
        CreatePrimitive(chestsRoot.transform, "SpecialShrinePlinth", PrimitiveType.Cylinder, shrinePos + Vector3.up * 0.14f, Vector3.zero, new Vector3(4.8f, 0.14f, 4.8f), stoneMat, true);
        CreatePrimitive(chestsRoot.transform, "SpecialShrineStep", PrimitiveType.Cube, shrinePos - inlandDir * 2.6f + Vector3.up * 0.08f, new Vector3(0f, dockRot.eulerAngles.y, 0f), new Vector3(2.4f, 0.22f, 1.2f), stoneMat, true);

        Vector3[] lampOffsets = new Vector3[]
        {
            new Vector3(-2.5f, 0f, -2.5f), new Vector3(2.5f, 0f, -2.5f),
            new Vector3(-2.5f, 0f, 2.5f), new Vector3(2.5f, 0f, 2.5f)
        };
        for (int i = 0; i < lampOffsets.Length; i++)
        {
            Vector3 lp = shrinePos + lampOffsets[i];
            if (terrain != null)
                lp.y = terrain.SampleHeight(lp) + terrain.transform.position.y;
            GameObject shrineLamp = InstantiateGrounded(LampPath, chestsRoot.transform, "ShrineLamp_" + (i + 1), lp, 0f, 1.1f);
            IslandPlacementUtility.GroundObjectExactToTerrain(shrineLamp, terrain, 0.12f);
            IslandPlacementUtility.EnsureExactSolidColliders(shrineLamp, false, false);
        }

        GameObject specialObj = (GameObject)PrefabUtility.InstantiatePrefab(specialChestPrefab, chestsRoot.transform);
        specialObj.name = "DiscoveryChest_Special";
        specialObj.transform.position = shrinePos + Vector3.up * 0.28f;
        specialObj.transform.rotation = dockRot * Quaternion.Euler(0f, 180f, 0f);

        TreasureChest specialTc = specialObj.GetComponent<TreasureChest>();
        specialTc.chestId = "discovery_special_1";
        specialTc.islandIndex = 0;
        specialTc.isSpecialChest = true;
        specialTc.respawnCandidateSpots = respawnPool;
        EditorUtility.SetDirty(specialTc);

        return spawnGo.transform;
    }

    private static Vector3 FindSpacedChestSpot(
        Terrain terrain,
        float preferredX,
        float preferredZ,
        float searchRadius,
        float footprintRadius,
        List<IslandPlacementUtility.PlacedFootprint> placed)
    {
        Vector3 best = IslandPlacementUtility.FindUnobstructedSpot(terrain, preferredX, preferredZ, searchRadius, 1.8f);
        if (IslandPlacementUtility.IsClearOfFootprints(best, footprintRadius, placed))
        {
            placed.Add(new IslandPlacementUtility.PlacedFootprint(best, footprintRadius));
            return best;
        }

        for (float r = 4f; r <= searchRadius + 16f; r += 4f)
        {
            for (int deg = 0; deg < 360; deg += 45)
            {
                float rad = deg * Mathf.Deg2Rad;
                Vector3 cand = IslandPlacementUtility.FindUnobstructedSpot(terrain, preferredX + Mathf.Cos(rad) * r, preferredZ + Mathf.Sin(rad) * r, 8f, 1.8f);
                if (IslandPlacementUtility.IsClearOfFootprints(cand, footprintRadius, placed))
                {
                    placed.Add(new IslandPlacementUtility.PlacedFootprint(cand, footprintRadius));
                    return cand;
                }
            }
        }

        placed.Add(new IslandPlacementUtility.PlacedFootprint(best, footprintRadius));
        return best;
    }

    private static void BuildDiscoveryBayAmbience(
        Transform parent,
        Terrain terrain,
        Vector3 beachEntryPos,
        Vector3 inlandDir,
        Vector3 rightDir,
        int palmCount,
        int rockCount)
    {
        if (terrain == null)
            return;

        List<IslandPlacementUtility.PlacedFootprint> bayPlaced = new List<IslandPlacementUtility.PlacedFootprint>();
        bayPlaced.Add(new IslandPlacementUtility.PlacedFootprint(beachEntryPos, 7.5f));
        bayPlaced.Add(new IslandPlacementUtility.PlacedFootprint(beachEntryPos + inlandDir * 8f, 5.5f));

        string[] palmPaths = new string[] { PalmTallPath, PalmShortPath, PalmPack1Path };
        for (int i = 0; i < palmCount; i++)
        {
            float sideSign = (i % 2 == 0) ? -1f : 1f;
            float rx = sideSign * (11f + (i / 2) * 5.2f);
            float rz = -2f + (i % 4) * 5.5f;
            Vector3 target = beachEntryPos + rightDir * rx + inlandDir * rz;
            if (IslandPlacementUtility.ReserveFlatSpot(terrain, target.x, target.z, 12f, 3.0f, 26f, 0.7f, bayPlaced, out Vector3 p))
            {
                p = IslandPlacementUtility.FindUnobstructedSpot(terrain, p.x, p.z, 6f, 1.3f);
                GameObject palm = InstantiateGrounded(palmPaths[i % palmPaths.Length], parent, "BayPalm_" + (i + 1), p, i * 47f, 0.95f + (i % 3) * 0.1f);
                IslandPlacementUtility.GroundObjectExactToTerrain(palm, terrain, 0.18f, true);
                IslandPlacementUtility.EnsureExactSolidColliders(palm, true, false);
                if (palm != null && palm.GetComponent<WindSway>() == null)
                    palm.AddComponent<WindSway>();
            }
        }

        string[] rockPaths = new string[] { RockPile1Path, RockPile3Path, RockPile6Path };
        for (int i = 0; i < rockCount; i++)
        {
            float sideSign = (i % 2 == 0) ? 1f : -1f;
            float rx = sideSign * (13f + (i / 2) * 7.5f);
            float rz = -4f + (i % 3) * 5.0f;
            Vector3 target = beachEntryPos + rightDir * rx + inlandDir * rz;
            if (IslandPlacementUtility.ReserveFlatSpot(terrain, target.x, target.z, 12f, 3.6f, 28f, 0.45f, bayPlaced, out Vector3 rp))
            {
                rp = IslandPlacementUtility.FindUnobstructedSpot(terrain, rp.x, rp.z, 6f, 1.6f);
                GameObject rock = InstantiateGrounded(rockPaths[i % rockPaths.Length], parent, "BayRock_" + (i + 1), rp, i * 63f, 1.05f);
                IslandPlacementUtility.GroundObjectExactToTerrain(rock, terrain, 0.26f);
                IslandPlacementUtility.EnsureExactSolidColliders(rock, false, false);
            }
        }
    }

    private static void BuildDockRepairBench(Transform parent, Vector3 worldPos, Quaternion dockRot, Material woodMat, Material brassMat, Material sparkleMat)
    {
        GameObject bench = new GameObject("DockRepairWorkbench");
        bench.transform.SetParent(parent, false);
        bench.transform.position = worldPos;
        bench.transform.rotation = dockRot * Quaternion.Euler(0f, -90f, 0f);

        BoxCollider col = bench.AddComponent<BoxCollider>();
        col.isTrigger = false;
        col.center = new Vector3(0f, 0.55f, 0f);
        col.size = new Vector3(1.8f, 1.1f, 1.0f);

        CreatePrimitive(bench.transform, "TableTop", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), Vector3.zero, new Vector3(1.7f, 0.14f, 0.9f), woodMat, false);
        CreatePrimitive(bench.transform, "Leg_FL", PrimitiveType.Cube, new Vector3(-0.72f, 0.4f, 0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat, false);
        CreatePrimitive(bench.transform, "Leg_FR", PrimitiveType.Cube, new Vector3(0.72f, 0.4f, 0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat, false);
        CreatePrimitive(bench.transform, "Leg_BL", PrimitiveType.Cube, new Vector3(-0.72f, 0.4f, -0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat, false);
        CreatePrimitive(bench.transform, "Leg_BR", PrimitiveType.Cube, new Vector3(0.72f, 0.4f, -0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat, false);
        CreatePrimitive(bench.transform, "SparePlank_1", PrimitiveType.Cube, new Vector3(-0.35f, 0.97f, 0f), new Vector3(0f, 12f, 0f), new Vector3(0.85f, 0.07f, 0.25f), woodMat, false);
        CreatePrimitive(bench.transform, "HammerHead", PrimitiveType.Cube, new Vector3(0.52f, 0.98f, 0.18f), new Vector3(0f, 25f, 0f), new Vector3(0.22f, 0.10f, 0.11f), brassMat, false);

        GameObject fxGo = new GameObject("RepairWorkbenchParticles");
        fxGo.transform.SetParent(bench.transform, false);
        fxGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        fxGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.8f;
        main.startLifetime = 0.75f;
        main.startSpeed = 2.2f;
        main.startSize = 0.14f;
        main.startColor = new Color(0.45f, 1f, 0.55f, 1f);
        main.maxParticles = 30;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });
        ParticleSystemRenderer pr = fxGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null && sparkleMat != null)
            pr.sharedMaterial = sparkleMat;

        DockRepairStation station = bench.AddComponent<DockRepairStation>();
        station.repairBurstParticles = ps;
        station.interactRange = 4.4f;
    }

    private static IslandPlayerController EnsurePlayerAndHud(Scene scene, Vector3 spawnPos, Quaternion spawnRot, out TMP_Text statusText)
    {
        DestroyRootIfExists(scene, "IslandPlayer");
        DestroyRootIfExists(scene, "FirstPersonPlayer");

        GameObject playerGo = CreateSceneRoot(scene, "FirstPersonPlayer", true);
        playerGo.transform.position = spawnPos;
        playerGo.transform.rotation = spawnRot;

        CharacterController cc = GetOrAdd<CharacterController>(playerGo);
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.45f;
        cc.slopeLimit = 50f;

        IslandPlayerController playerCtrl = GetOrAdd<IslandPlayerController>(playerGo);

        GameObject camGo = new GameObject("Main Camera");
        camGo.transform.SetParent(playerGo.transform, false);
        camGo.tag = "MainCamera";
        camGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        camGo.transform.localRotation = Quaternion.identity;

        GetOrAdd<Camera>(camGo);
        GetOrAdd<AudioListener>(camGo);
        GetOrAdd<UniversalAdditionalCameraData>(camGo);
        playerCtrl.cameraTransform = camGo.transform;

        EventSystem[] existingSystems = Object.FindObjectsByType<EventSystem>();
        if (existingSystems.Length == 0)
        {
            GameObject esGo = CreateSceneRoot(scene, "EventSystem", true);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        GameObject hudGo = FindSceneObjectByName(scene, "IslandHUDCanvas");
        if (hudGo == null) hudGo = CreateSceneRoot(scene, "IslandHUDCanvas", true);
        Canvas hudCanvas = GetOrAdd<Canvas>(hudGo);
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = GetOrAdd<CanvasScaler>(hudGo);
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        GetOrAdd<GraphicRaycaster>(hudGo);

        statusText = CreateOrUpdateHudText(
            hudGo.transform,
            "IslandStatusText",
            new Vector2(0f, 1f),
            new Vector2(20f, -18f),
            new Vector2(860f, 64f),
            new Vector2(0f, 1f),
            "Discovery Island  |  Boat HP: 100/100",
            20f,
            TextAlignmentOptions.Left,
            new Color(1f, 0.94f, 0.78f, 1f)
        );

        CreateOrUpdateHudText(
            hudGo.transform,
            "ControlsHintText",
            new Vector2(1f, 1f),
            new Vector2(-20f, -18f),
            new Vector2(760f, 40f),
            new Vector2(1f, 1f),
            "WASD Move  |  Shift Sprint  |  Space Jump  |  E Interact  |  M Map  |  I Inventory  |  F Board Boat",
            17f,
            TextAlignmentOptions.Right,
            new Color(0.92f, 0.92f, 0.92f, 0.9f)
        );

        GameObject crosshairGo = GetOrCreateChild(hudGo.transform, "CrosshairDot");
        RectTransform crossRect = EnsureRectTransform(crosshairGo);
        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.sizeDelta = new Vector2(8f, 8f);
        crossRect.anchoredPosition = Vector2.zero;
        Image crossImg = GetOrAdd<Image>(crosshairGo);
        crossImg.color = new Color(1f, 1f, 1f, 0.85f);
        crossImg.raycastTarget = false;

        GameObject promptPanel = GetOrCreateChild(hudGo.transform, "InteractionPromptPanel");
        RectTransform promptRect = EnsureRectTransform(promptPanel);
        promptRect.anchorMin = new Vector2(0.5f, 0.14f);
        promptRect.anchorMax = new Vector2(0.5f, 0.14f);
        promptRect.sizeDelta = new Vector2(680f, 58f);
        promptRect.anchoredPosition = Vector2.zero;
        Image promptBg = GetOrAdd<Image>(promptPanel);
        promptBg.color = new Color(0.14f, 0.09f, 0.05f, 0.88f);
        promptBg.raycastTarget = false;

        TMP_Text promptText = CreateOrUpdateHudText(
            promptPanel.transform,
            "InteractionPromptText",
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(640f, 48f),
            new Vector2(0.5f, 0.5f),
            "Press E to Interact",
            22f,
            TextAlignmentOptions.Center,
            new Color(1f, 0.9f, 0.6f, 1f)
        );
        promptPanel.SetActive(false);

        playerCtrl.interactionPromptPanel = promptPanel;
        playerCtrl.interactionPromptText = promptText;
        playerCtrl.crosshairObject = crosshairGo;
        EditorUtility.SetDirty(playerCtrl);
        return playerCtrl;
    }

    private static void RemoveCollidersOnObject(GameObject go)
    {
        if (go == null) return;
        Collider[] cols = go.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
            Object.DestroyImmediate(cols[i]);
    }

    private static GameObject BuildWoodenVillageArch(Transform parent, string name, Vector3 worldPos, Quaternion rot, Material woodMat, Material glowMat)
    {
        GameObject arch = new GameObject(name);
        arch.transform.SetParent(parent, false);
        arch.transform.position = worldPos;
        arch.transform.rotation = rot;
        CreatePrimitive(arch.transform, "ArchPost_L", PrimitiveType.Cylinder, new Vector3(-2.4f, 1.25f, 0f), Vector3.zero, new Vector3(0.38f, 2.75f, 0.38f), woodMat, true);
        CreatePrimitive(arch.transform, "ArchPost_R", PrimitiveType.Cylinder, new Vector3(2.4f, 1.25f, 0f), Vector3.zero, new Vector3(0.38f, 2.75f, 0.38f), woodMat, true);
        CreatePrimitive(arch.transform, "ArchCrossbeam", PrimitiveType.Cube, new Vector3(0f, 3.85f, 0f), Vector3.zero, new Vector3(5.6f, 0.42f, 0.42f), woodMat, false);
        CreatePrimitive(arch.transform, "ArchLantern_L", PrimitiveType.Cube, new Vector3(-2.1f, 3.35f, 0.3f), Vector3.zero, new Vector3(0.28f, 0.36f, 0.28f), glowMat, false);
        CreatePrimitive(arch.transform, "ArchLantern_R", PrimitiveType.Cube, new Vector3(2.1f, 3.35f, 0.3f), Vector3.zero, new Vector3(0.28f, 0.36f, 0.28f), glowMat, false);
        return arch;
    }

    private static T GetOrAdd<T>(GameObject go) where T : Component
    {
        T comp = go.GetComponent<T>();
        if (comp == null)
            comp = go.AddComponent<T>();
        return comp;
    }

    private static void AddCrab(Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject crab = InstantiateGrounded(CrabPath, parent, name, pos, yaw, 1.25f);
        if (crab != null && crab.GetComponent<BeachCrabWander>() == null)
            crab.AddComponent<BeachCrabWander>();
    }

    private static GameObject InstantiateGrounded(string assetPath, Transform parent, string name, Vector3 pos, float yaw, float scale)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            return null;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.position = pos;
        instance.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        instance.transform.localScale = Vector3.one * scale;
        return instance;
    }

    private static GameObject InstantiateAsset(string assetPath, Transform parent, string name)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            return new GameObject(name);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        return instance;
    }

    private static void DisableDecorativePointShadows(Scene scene)
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].gameObject.scene == scene && lights[i].type != LightType.Directional)
                lights[i].shadows = LightShadows.None;
        }
    }

    private static GameObject CreatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat, bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;

        if (!keepCollider)
        {
            Collider col = go.GetComponent<Collider>();
            if (col != null)
                Object.DestroyImmediate(col);
        }

        if (mat != null && go.GetComponent<MeshRenderer>() != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        return go;
    }

    private static GameObject CreateSceneRoot(Scene scene, string name, bool active)
    {
        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        go.SetActive(active);
        return go;
    }

    private static void DestroyRootIfExists(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == rootName)
                Object.DestroyImmediate(roots[i]);
        }
    }

    private static GameObject FindSceneObjectByName(Scene scene, string targetName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].scene == scene && allObjects[i].name == targetName)
                return allObjects[i];
        }
        return null;
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        return GetOrAdd<RectTransform>(go);
    }

    private static TMP_Text CreateOrUpdateHudText(Transform parent, string name, Vector2 anchor, Vector2 anchoredPos, Vector2 sizeDelta, Vector2 pivot, string text, float fontSize, TextAlignmentOptions align, Color color)
    {
        GameObject go = GetOrCreateChild(parent, name);
        RectTransform rect = EnsureRectTransform(go);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = GetOrAdd<TextMeshProUGUI>(go);
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Material GetOrCreateLitMaterial(string fileName, Color color, float metallic, float smoothness)
    {
        string path = "Assets/Materials/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static Material GetOrCreateEmissiveMaterial(string fileName, Color baseColor, Color emissionColor)
    {
        string path = "Assets/Materials/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        mat = GetOrCreateLitMaterial(fileName, baseColor, 0f, 0.45f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateTexturedStoneMaterial(string fileName, string texturePath, Color tintColor)
    {
        Material mat = GetOrCreateLitMaterial(fileName, tintColor, 0f, 0.14f);
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
        if (tex != null)
        {
            if (mat.HasProperty("_BaseMap") && mat.GetTexture("_BaseMap") == null)
            {
                mat.SetTexture("_BaseMap", tex);
                mat.SetTextureScale("_BaseMap", new Vector2(0.75f, 0.75f));
                EditorUtility.SetDirty(mat);
            }
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", tintColor);
        }
        return mat;
    }
}
