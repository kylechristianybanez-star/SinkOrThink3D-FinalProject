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
    private const string TerrainDataPath = "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 1.asset";
    private const string SkyMaterialPath = "Assets/Free Island Collection/Environment/Skies/Sky Material.mat";
    private const string GameSceneWaterMatGuid = "379410282482c54498410674d0c4391d";
    private const string FallbackWaterMatPath = "Assets/Island/Textures/Materials/Water.mat";

    private const string DockPrefabPath = "Assets/Island/Prefabs/Dock.prefab";
    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string House1Path = "Assets/POLYART_Ancient Village/Prefabs/PA_BambooHouse_1.prefab";
    private const string House2Path = "Assets/POLYART_Ancient Village/Prefabs/PA_BambooHouse_2.prefab";
    private const string BridgePath = "Assets/POLYART_Ancient Village/Prefabs/PA_BambooBridge_1.prefab";
    private const string LampPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Bamboolamp_Set1.prefab";
    private const string RockPile1Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile1.prefab";
    private const string RockPile3Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile3.prefab";
    private const string RockPile6Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile6.prefab";

    private const string PalmPack1Path = "Assets/PalmTreePack/Prefabs/1/Palm_1_1.prefab";
    private const string PalmPack2Path = "Assets/PalmTreePack/Prefabs/2/Palm_2_1.prefab";
    private const string PalmTallPath = "Assets/ToonTropical/Prefabs/SM_Island_PalmTree_Tall.prefab";
    private const string PalmShortPath = "Assets/ToonTropical/Prefabs/SM_Island_PalmTree_Short.prefab";
    private const string BushPath = "Assets/ToonTropical/Prefabs/SM_Island_Bush_A.prefab";
    private const string GrassPath = "Assets/ToonTropical/Prefabs/SM_Island_GrassTuft_A.prefab";
    private const string CampfirePath = "Assets/ToonTropical/Prefabs/SM_Island_Campfire.prefab";
    private const string EmbersPath = "Assets/ToonTropical/Prefabs/FX/FX_Embers.prefab";
    private const string BarrelPath = "Assets/ToonTropical/Prefabs/SM_Island_Barrel.prefab";
    private const string CratePath = "Assets/ToonTropical/Prefabs/SM_Island_Crate.prefab";
    private const string DriftwoodPath = "Assets/ToonTropical/Prefabs/SM_Island_Driftwood.prefab";
    private const string SeashellPath = "Assets/ToonTropical/Prefabs/SM_Island_Seashell.prefab";
    private const string StarfishPath = "Assets/ToonTropical/Prefabs/SM_Island_Starfish.prefab";
    private const string CrabPath = "Assets/ToonTropical/Prefabs/SK_Island_Crab.prefab";

    [MenuItem("Tools/SinkOrThink3D/Phase 9.5 - Build & Polish Discovery Island (GameScene + IslandScene)")]
    public static void BuildAndPolishDiscoveryIsland()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // Build/refresh reusable chest prefabs first
        Phase6DiscoveryIslandSetup.BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);

        // Part A: Polish GameScene (Boat, QuizBoard, Ocean, Dock alignment, Shadow Atlas, Discovery Overview)
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        PolishGameScene(gameScene);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        // Part B: Polish reusable IslandScene with DiscoveryIsland + placeholders for Viking & Final
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        PolishIslandScene(islandScene, regularChestPrefab, specialChestPrefab);
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        AssetDatabase.SaveAssets();

        Debug.Log("Phase 9.5 Complete! Polished Discovery Island in both GameScene.unity and IslandScene.unity.");
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

    private static void PolishGameScene(Scene gameScene)
    {
        Phase3BoatAndBoardSetup.ApplyToScene(gameScene);
        Phase4OceanAndDockSetup.ApplyToScene(gameScene);

        // Configure OceanBoatSpawner on rowboat
        GameObject boat = FindSceneObjectByName(gameScene, "OldWoodenRowboat_Complete");
        if (boat != null && boat.GetComponent<OceanBoatSpawner>() == null)
            boat.AddComponent<OceanBoatSpawner>();

        // Configure Discovery BoatDocking with closer berth and return spawn point
        GameObject dockPoint = FindSceneObjectByName(gameScene, "BoatDockPoint");
        if (dockPoint != null)
        {
            BoatDocking docking = dockPoint.GetComponent<BoatDocking>();
            if (docking != null)
            {
                Undo.RecordObject(docking, "Polish Discovery BoatDocking");
                docking.islandIndex = 0;
                docking.autoMatchCurrentIsland = false;
                docking.islandDisplayName = "Discovery Island";
                docking.targetIslandSceneName = "IslandScene";
                docking.transitionToIslandScene = true;
                docking.dockingDistance = 11f;

                Transform park = dockPoint.transform.Find("DockParkingPoint");
                if (park != null)
                {
                    park.localPosition = new Vector3(0f, -0.58f, -2.0f);
                    park.localRotation = Quaternion.identity;
                    docking.dockParkingPoint = park;
                }

                GameObject returnSpawn = GetOrCreateChild(dockPoint.transform, "BoatReturnSpawnPoint");
                returnSpawn.transform.localPosition = new Vector3(0f, -0.58f, -6.5f);
                returnSpawn.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
                docking.boatReturnSpawnPoint = returnSpawn.transform;

                EditorUtility.SetDirty(docking);
            }
        }

        // Enhance Discovery overview inside IslandDestination so it visually matches IslandScene
        GameObject islandDest = FindSceneObjectByName(gameScene, "IslandDestination");
        Terrain overviewTerrain = FindSceneObjectByName(gameScene, "DiscoveryIsland_MainTerrain")?.GetComponent<Terrain>();
        if (islandDest != null && overviewTerrain != null)
        {
            Transform oldOverview = islandDest.transform.Find("DiscoveryOverviewPolish");
            if (oldOverview != null)
                Object.DestroyImmediate(oldOverview.gameObject);

            GameObject polishRoot = new GameObject("DiscoveryOverviewPolish");
            polishRoot.transform.SetParent(islandDest.transform, false);

            // Note: overviewTerrain in GameScene is at local (-312, -9, 50) -> world (-312, -9, 150)
            // which matches IslandScene's DiscoveryIsland_Terrain position (-312, -9, 150)!
            Vector3 bridgePos = FindWalkablePoint(overviewTerrain, 15f, 174f, 8f);
            InstantiateGrounded(BridgePath, polishRoot.transform, "Overview_BambooBridge", bridgePos, 0f, 1f);

            Vector3 campPos = FindWalkablePoint(overviewTerrain, 16f, 194f, 10f);
            InstantiateGrounded(CampfirePath, polishRoot.transform, "Overview_Campfire", campPos, 0f, 1.2f);
            InstantiateGrounded(EmbersPath, polishRoot.transform, "Overview_Embers", campPos + Vector3.up * 0.25f, 0f, 1f);

            Vector2[] overviewPalms = new Vector2[]
            {
                new Vector2(2f, 158f), new Vector2(28f, 160f), new Vector2(-8f, 172f),
                new Vector2(36f, 176f), new Vector2(-12f, 192f), new Vector2(40f, 196f)
            };
            for (int i = 0; i < overviewPalms.Length; i++)
            {
                Vector3 p = FindWalkablePoint(overviewTerrain, overviewPalms[i].x, overviewPalms[i].y, 8f);
                GameObject palm = InstantiateGrounded(i % 2 == 0 ? PalmTallPath : PalmPack1Path, polishRoot.transform, "Overview_Palm_" + (i + 1), p, i * 52f, 1f);
                if (palm != null && palm.GetComponent<WindSway>() == null)
                    palm.AddComponent<WindSway>();
            }
        }

        // Guarantee zero shadow-casting point/spot lights in GameScene
        DisableDecorativePointShadows(gameScene);
    }

    private static void PolishIslandScene(Scene scene, GameObject regularChestPrefab, GameObject specialChestPrefab)
    {
        // Clean up legacy single-island roots if upgrading from Phase 5/6/8
        string[] legacyRoots = new string[]
        {
            "Main Camera", "DiscoveryIsland_Terrain", "DiscoveryDock", "DockBeachRamp",
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

        Light dirLight = Object.FindFirstObjectByType<Light>();
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
        oceanGo.transform.position = Vector3.zero;
        oceanGo.transform.rotation = Quaternion.identity;
        oceanGo.transform.localScale = new Vector3(300f, 1f, 300f);
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

        // Create Reusable 3-Island Parents
        GameObject discoveryRoot = CreateSceneRoot(scene, "DiscoveryIsland", true);
        GameObject vikingRoot = CreateSceneRoot(scene, "VikingIsland", false);
        GameObject finalRoot = CreateSceneRoot(scene, "FinalTreasureIsland", false);

        // Build Detailed Discovery Island inside DiscoveryIsland parent
        Transform discoverySpawn = BuildDetailedDiscoveryIsland(discoveryRoot.transform, regularChestPrefab, specialChestPrefab);

        // Ensure Player, HUD, EventSystem, ChestQuizManager, and IslandSceneController
        IslandPlayerController playerCtrl = EnsurePlayerAndHud(scene, discoverySpawn.position, discoverySpawn.rotation, out TMP_Text statusText);

        GameObject ctrlGo = FindSceneObjectByName(scene, "IslandSceneController") ?? CreateSceneRoot(scene, "IslandSceneController", true);
        IslandSceneController sceneCtrl = ctrlGo.GetComponent<IslandSceneController>() ?? ctrlGo.AddComponent<IslandSceneController>();
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

        GameObject quizMgrGo = FindSceneObjectByName(scene, "ChestQuizManager") ?? CreateSceneRoot(scene, "ChestQuizManager", true);
        ChestQuizManager chestQuiz = quizMgrGo.GetComponent<ChestQuizManager>() ?? quizMgrGo.AddComponent<ChestQuizManager>();
        chestQuiz.islandIndex = 0;
        EditorUtility.SetDirty(chestQuiz);

        DisableDecorativePointShadows(scene);
    }

    private static Transform BuildDetailedDiscoveryIsland(Transform parent, GameObject regularChestPrefab, GameObject specialChestPrefab)
    {
        Material woodMat = GetOrCreateLitMaterial("QuizBoardFrameWood.mat", new Color(0.42f, 0.27f, 0.16f, 1f), 0f, 0.18f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material ropeMat = GetOrCreateLitMaterial("QuizBoardRope.mat", new Color(0.66f, 0.54f, 0.36f, 1f), 0f, 0.1f);
        Material glowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material stoneMat = GetOrCreateLitMaterial("IslandPathStone.mat", new Color(0.56f, 0.53f, 0.47f, 1f), 0f, 0.15f);
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        // 1. Terrain
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        GameObject terrainGo = new GameObject("DiscoveryIsland_Terrain");
        terrainGo.transform.SetParent(parent, false);
        terrainGo.transform.position = new Vector3(-312f, -9f, 150f);
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = terrainData;
        TerrainCollider terrainCol = terrainGo.AddComponent<TerrainCollider>();
        terrainCol.terrainData = terrainData;

        // 2. Dock Area + Spawn + Ramp + Return Boat + Repair Bench
        GameObject dockArea = new GameObject("DiscoveryDockArea");
        dockArea.transform.SetParent(parent, false);

        GameObject dockGo = InstantiateAsset(DockPrefabPath, dockArea.transform, "DiscoveryDock");
        dockGo.transform.position = new Vector3(14.0f, 0.6f, 144.19f);
        dockGo.transform.rotation = Quaternion.Euler(0f, 94f, 0f);

        // Mooring posts + glowing lanterns along dock edges (no colliders)
        Vector3[] postPos = new Vector3[]
        {
            new Vector3(11.3f, 0.9f, 141.2f), new Vector3(16.7f, 0.9f, 141.2f),
            new Vector3(11.3f, 0.9f, 149.2f), new Vector3(16.7f, 0.9f, 149.2f)
        };
        for (int i = 0; i < postPos.Length; i++)
        {
            CreatePrimitive(dockArea.transform, "DockPost_" + (i + 1), PrimitiveType.Cylinder, postPos[i], Vector3.zero, new Vector3(0.28f, 1.15f, 0.28f), woodMat, false);
            CreatePrimitive(dockArea.transform, "DockRope_" + (i + 1), PrimitiveType.Cylinder, postPos[i] + new Vector3(0f, 0.35f, 0f), Vector3.zero, new Vector3(0.34f, 0.12f, 0.34f), ropeMat, false);
            CreatePrimitive(dockArea.transform, "DockCap_" + (i + 1), PrimitiveType.Cylinder, postPos[i] + new Vector3(0f, 1.18f, 0f), Vector3.zero, new Vector3(0.30f, 0.04f, 0.30f), brassMat, false);
        }
        CreatePrimitive(dockArea.transform, "DockLantern_L", PrimitiveType.Cube, new Vector3(11.3f, 2.28f, 141.2f), Vector3.zero, new Vector3(0.26f, 0.34f, 0.26f), glowMat, false);
        CreatePrimitive(dockArea.transform, "DockLantern_R", PrimitiveType.Cube, new Vector3(16.7f, 2.28f, 141.2f), Vector3.zero, new Vector3(0.26f, 0.34f, 0.26f), glowMat, false);
        AddFlickeringPointLight(dockArea.transform, "DockWarmLight", new Vector3(14.0f, 2.9f, 142.0f), new Color(1f, 0.78f, 0.45f), 2.3f, 16f);

        // Smooth walkable DockBeachRamp with wooden side rails
        float beachTargetY = terrainData != null
            ? terrain.SampleHeight(new Vector3(14f, 0f, 157.5f)) + terrainGo.transform.position.y
            : 0.45f;
        Vector3 rampStart = new Vector3(14.0f, 0.85f, 150.2f);
        Vector3 rampEnd = new Vector3(14.0f, Mathf.Max(0.25f, beachTargetY), 157.8f);
        Vector3 rampMid = (rampStart + rampEnd) * 0.5f - new Vector3(0f, 0.08f, 0f);
        float rampLen = Vector3.Distance(rampStart, rampEnd) + 0.8f;

        GameObject rampGo = CreatePrimitive(dockArea.transform, "DockBeachRamp", PrimitiveType.Cube, rampMid, Vector3.zero, new Vector3(4.6f, 0.28f, rampLen), woodMat, true);
        rampGo.transform.LookAt(rampEnd);
        CreatePrimitive(rampGo.transform, "RampRail_L", PrimitiveType.Cube, new Vector3(-0.48f, 1.3f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), woodMat, true);
        CreatePrimitive(rampGo.transform, "RampRail_R", PrimitiveType.Cube, new Vector3(0.48f, 1.3f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), woodMat, true);

        // Docked Return Boat right beside dock water edge
        GameObject returnBoat = InstantiateAsset(BoatPrefabPath, dockArea.transform, "DockedBoat_Return");
        returnBoat.transform.position = new Vector3(13.8f, 0.08f, 138.0f);
        returnBoat.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
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

        // Dock Repair Workbench right on the dock deck
        BuildDockRepairBench(dockArea.transform, new Vector3(16.15f, 0.96f, 146.5f), woodMat, brassMat, sparkleMat);

        // Spawn point cleanly on dock deck facing inland
        GameObject spawnGo = new GameObject("DiscoverySpawnPoint");
        spawnGo.transform.SetParent(dockArea.transform, false);
        spawnGo.transform.position = new Vector3(14.0f, 1.42f, 144.8f);
        spawnGo.transform.rotation = Quaternion.identity;

        // 3. Detailed Environment (Houses, Bridge, Campfire, Paths, Trees & Rocks with Colliders, Crabs)
        GameObject envRoot = new GameObject("DiscoveryEnvironment");
        envRoot.transform.SetParent(parent, false);

        // 5 Bamboo Houses with guaranteed solid colliders
        Vector2[] houseSpots = new Vector2[]
        {
            new Vector2(2f, 186f),
            new Vector2(29f, 188f),
            new Vector2(4f, 208f),
            new Vector2(31f, 209f),
            new Vector2(44f, 178f)
        };
        float[] houseYaws = new float[] { 65f, -72f, 118f, -118f, -135f };
        for (int i = 0; i < houseSpots.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, houseSpots[i].x, houseSpots[i].y, 12f);
            GameObject house = InstantiateGrounded(i % 2 == 0 ? House1Path : House2Path, envRoot.transform, "VillageHouse_" + (i + 1), p, houseYaws[i], 1f);
            EnsureSolidColliders(house, false);
        }

        // Scenic walkable Bamboo Bridge along coastal trail
        Vector3 bridgePos = FindWalkablePoint(terrain, 14.5f, 171f, 8f);
        GameObject bridge = InstantiateGrounded(BridgePath, envRoot.transform, "VillageBambooBridge", bridgePos, 0f, 1f);
        EnsureSolidColliders(bridge, true);

        // Village Gathering Campfire + Embers + Cargo
        Vector3 campPos = FindWalkablePoint(terrain, 16f, 195f, 10f);
        GameObject camp = InstantiateGrounded(CampfirePath, envRoot.transform, "VillageCampfire", campPos, 0f, 1.25f);
        EnsureSolidColliders(camp, false);
        InstantiateGrounded(EmbersPath, envRoot.transform, "VillageCampfireEmbers", campPos + Vector3.up * 0.25f, 0f, 1f);
        AddFlickeringPointLight(envRoot.transform, "VillageCampfireLight", campPos + Vector3.up * 1.15f, new Color(1f, 0.58f, 0.2f), 2.5f, 15f);
        InstantiateGrounded(BarrelPath, envRoot.transform, "VillageBarrel_1", FindWalkablePoint(terrain, 12.2f, 193f, 6f), 25f, 1f);
        InstantiateGrounded(CratePath, envRoot.transform, "VillageCrate_1", FindWalkablePoint(terrain, 19.6f, 193f, 6f), -30f, 1f);

        // Natural Stepping Stone Pathways & Bamboo Lanterns connecting Dock -> Bridge -> Village -> Shrine & Beach
        Vector2[] mainTrail = new Vector2[]
        {
            new Vector2(14f, 160f), new Vector2(14.3f, 165.5f), new Vector2(15f, 177f), new Vector2(15.6f, 183f),
            new Vector2(16f, 189f), new Vector2(18.5f, 203f), new Vector2(21.5f, 212f), new Vector2(24f, 221f),
            new Vector2(26f, 230f), new Vector2(24f, 163f), new Vector2(31f, 164f), new Vector2(5f, 188f), new Vector2(-4f, 189f)
        };
        for (int i = 0; i < mainTrail.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, mainTrail[i].x, mainTrail[i].y, 6f);
            GameObject stone = CreatePrimitive(envRoot.transform, "PathStone_" + (i + 1), PrimitiveType.Cylinder, p + Vector3.up * 0.03f, new Vector3(0f, i * 28f, 0f), new Vector3(1.65f, 0.05f, 1.65f), stoneMat, false);
            if (i % 2 == 0 && i < 9)
            {
                Vector3 lp = FindWalkablePoint(terrain, mainTrail[i].x + 2.3f, mainTrail[i].y, 4f);
                GameObject lamp = InstantiateGrounded(LampPath, envRoot.transform, "PathLamp_" + (i + 1), lp, 0f, 1f);
                EnsureSolidColliders(lamp, false);
            }
        }

        // ~28 Swaying Palm Trees ALL WITH TRUNK COLLIDERS
        Vector2[] palmSpots = new Vector2[]
        {
            new Vector2(-6f, 166f), new Vector2(28f, 164f), new Vector2(36f, 174f), new Vector2(-12f, 182f),
            new Vector2(40f, 192f), new Vector2(-14f, 204f), new Vector2(44f, 212f), new Vector2(-6f, 222f),
            new Vector2(38f, 230f), new Vector2(10f, 236f), new Vector2(-20f, 176f), new Vector2(52f, 184f),
            new Vector2(-22f, 194f), new Vector2(48f, 224f), new Vector2(2f, 158f), new Vector2(26f, 158f),
            new Vector2(-18f, 216f), new Vector2(34f, 242f), new Vector2(12f, 216f), new Vector2(-24f, 208f),
            new Vector2(56f, 204f), new Vector2(20f, 246f), new Vector2(-4f, 244f), new Vector2(46f, 162f),
            new Vector2(-16f, 164f), new Vector2(30f, 200f), new Vector2(-2f, 198f), new Vector2(16f, 226f)
        };
        string[] palmPrefabs = new string[] { PalmPack1Path, PalmPack2Path, PalmTallPath, PalmShortPath };
        for (int i = 0; i < palmSpots.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, palmSpots[i].x, palmSpots[i].y, 8f);
            GameObject palm = InstantiateGrounded(palmPrefabs[i % palmPrefabs.Length], envRoot.transform, "SwayingPalm_" + (i + 1), p, i * 43f, 0.95f + (i % 3) * 0.1f);
            if (palm != null)
            {
                EnsureTreeTrunkCollider(palm);
                if (palm.GetComponent<WindSway>() == null)
                {
                    WindSway sway = palm.AddComponent<WindSway>();
                    sway.swayAngle = 2.4f;
                    sway.swaySpeed = 1.2f + (i % 4) * 0.1f;
                }
            }
        }

        // ~24 Swaying Tropical Bushes & Grass Tufts
        for (int i = 0; i < 24; i++)
        {
            float angle = i * 15f * Mathf.Deg2Rad;
            float radius = 13f + (i % 4) * 10f;
            Vector3 p = FindWalkablePoint(terrain, 16f + Mathf.Cos(angle) * radius, 196f + Mathf.Sin(angle) * radius, 8f);
            GameObject veg = InstantiateGrounded(i % 2 == 0 ? BushPath : GrassPath, envRoot.transform, "IslandFoliage_" + (i + 1), p, i * 31f, 1f + (i % 2) * 0.25f);
            if (veg != null && veg.GetComponent<WindSway>() == null)
            {
                WindSway sway = veg.AddComponent<WindSway>();
                sway.swayAngle = 4.0f;
                sway.swaySpeed = 1.8f;
            }
        }

        // ~10 Rock Clusters ALL WITH SOLID COLLIDERS
        Vector2[] rockSpots = new Vector2[]
        {
            new Vector2(-18f, 186f), new Vector2(46f, 198f), new Vector2(-10f, 232f), new Vector2(36f, 246f),
            new Vector2(2f, 224f), new Vector2(54f, 172f), new Vector2(-24f, 170f), new Vector2(42f, 232f),
            new Vector2(8f, 246f), new Vector2(-16f, 218f)
        };
        string[] rockPaths = new string[] { RockPile1Path, RockPile3Path, RockPile6Path };
        for (int i = 0; i < rockSpots.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, rockSpots[i].x, rockSpots[i].y, 10f);
            GameObject rock = InstantiateGrounded(rockPaths[i % rockPaths.Length], envRoot.transform, "RockCluster_" + (i + 1), p, i * 55f, 1f);
            EnsureSolidColliders(rock, true);
        }

        // Shoreline Driftwood, Shells, Starfish & 3 Animated Crabs
        Vector3 shore1 = FindWalkablePoint(terrain, 34f, 161f, 8f);
        Vector3 shore2 = FindWalkablePoint(terrain, -2f, 161f, 8f);
        Vector3 shore3 = FindWalkablePoint(terrain, 44f, 168f, 8f);
        InstantiateGrounded(DriftwoodPath, envRoot.transform, "ShoreDriftwood_1", shore1 + Vector3.left * 2f, 35f, 1.15f);
        InstantiateGrounded(SeashellPath, envRoot.transform, "ShoreShell_1", shore1 + Vector3.right * 1.5f, 15f, 1.5f);
        InstantiateGrounded(StarfishPath, envRoot.transform, "ShoreStarfish_1", shore2 + Vector3.left * 1.5f, 70f, 1.5f);

        AddCrab(envRoot.transform, "BeachCrab_East", shore1, 0f);
        AddCrab(envRoot.transform, "BeachCrab_West", shore2, 90f);
        AddCrab(envRoot.transform, "BeachCrab_Cove", shore3, 180f);

        // 4. Six Scenic, Accessible Treasure Chests
        GameObject chestsRoot = new GameObject("DiscoveryChests");
        chestsRoot.transform.SetParent(parent, false);

        Vector2[] regularCoords = new Vector2[]
        {
            new Vector2(36f, 164f),   // 1: Driftwood Beach Cove
            new Vector2(21f, 196f),   // 2: Bamboo Village Courtyard
            new Vector2(8f, 176f),    // 3: Bamboo Bridge Crossing
            new Vector2(-12f, 192f),  // 4: Western Palm Oasis
            new Vector2(42f, 218f)    // 5: Coastal Rock Overlook
        };
        float[] regularYaws = new float[] { -145f, -160f, 150f, 115f, -125f };

        for (int i = 0; i < regularCoords.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, regularCoords[i].x, regularCoords[i].y, 10f);
            GameObject chestObj = (GameObject)PrefabUtility.InstantiatePrefab(regularChestPrefab, chestsRoot.transform);
            chestObj.name = "DiscoveryChest_Regular_" + (i + 1);
            chestObj.transform.position = p;
            chestObj.transform.rotation = Quaternion.Euler(0f, regularYaws[i], 0f);

            TreasureChest tc = chestObj.GetComponent<TreasureChest>();
            tc.chestId = "discovery_regular_" + (i + 1);
            tc.islandIndex = 0;
            tc.isSpecialChest = false;
            EditorUtility.SetDirty(tc);
        }

        // Special Chest on raised Ancient Discovery Shrine Dais
        Vector3 shrinePos = FindWalkablePoint(terrain, 26f, 236f, 12f);
        GameObject plinth = CreatePrimitive(chestsRoot.transform, "SpecialShrinePlinth", PrimitiveType.Cylinder, shrinePos + Vector3.up * 0.14f, Vector3.zero, new Vector3(4.8f, 0.28f, 4.8f), stoneMat, true);
        CreatePrimitive(chestsRoot.transform, "SpecialShrineStep", PrimitiveType.Cube, shrinePos + new Vector3(0f, 0.08f, -2.5f), Vector3.zero, new Vector3(2.4f, 0.18f, 1.2f), stoneMat, true);

        Vector3[] lampOffsets = new Vector3[]
        {
            new Vector3(-2.5f, 0f, -2.5f), new Vector3(2.5f, 0f, -2.5f),
            new Vector3(-2.5f, 0f, 2.5f), new Vector3(2.5f, 0f, 2.5f)
        };
        for (int i = 0; i < lampOffsets.Length; i++)
        {
            Vector3 lp = shrinePos + lampOffsets[i];
            lp.y = terrain.SampleHeight(lp) + terrainGo.transform.position.y;
            GameObject shrineLamp = InstantiateGrounded(LampPath, chestsRoot.transform, "ShrineLamp_" + (i + 1), lp, 0f, 1.1f);
            EnsureSolidColliders(shrineLamp, false);
        }

        GameObject specialObj = (GameObject)PrefabUtility.InstantiatePrefab(specialChestPrefab, chestsRoot.transform);
        specialObj.name = "DiscoveryChest_Special";
        specialObj.transform.position = shrinePos + Vector3.up * 0.28f;
        specialObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        TreasureChest specialTc = specialObj.GetComponent<TreasureChest>();
        specialTc.chestId = "discovery_special_1";
        specialTc.islandIndex = 0;
        specialTc.isSpecialChest = true;
        EditorUtility.SetDirty(specialTc);

        return spawnGo.transform;
    }

    private static void BuildDockRepairBench(Transform parent, Vector3 worldPos, Material woodMat, Material brassMat, Material sparkleMat)
    {
        GameObject bench = new GameObject("DockRepairWorkbench");
        bench.transform.SetParent(parent, false);
        bench.transform.position = worldPos;
        bench.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

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
        GameObject playerGo = FindSceneObjectByName(scene, "IslandPlayer") ?? CreateSceneRoot(scene, "IslandPlayer", true);
        playerGo.transform.position = spawnPos;
        playerGo.transform.rotation = spawnRot;

        CharacterController cc = playerGo.GetComponent<CharacterController>() ?? playerGo.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.45f;
        cc.slopeLimit = 50f;

        IslandPlayerController playerCtrl = playerGo.GetComponent<IslandPlayerController>() ?? playerGo.AddComponent<IslandPlayerController>();

        Transform camChild = playerGo.transform.Find("PlayerCamera");
        GameObject camGo = camChild != null ? camChild.gameObject : new GameObject("PlayerCamera");
        if (camChild == null)
            camGo.transform.SetParent(playerGo.transform, false);
        camGo.tag = "MainCamera";
        camGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        camGo.transform.localRotation = Quaternion.identity;

        if (camGo.GetComponent<Camera>() == null) camGo.AddComponent<Camera>();
        if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
        if (camGo.GetComponent<UniversalAdditionalCameraData>() == null) camGo.AddComponent<UniversalAdditionalCameraData>();
        playerCtrl.cameraTransform = camGo.transform;

        EventSystem[] existingSystems = Object.FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        if (existingSystems.Length == 0)
        {
            GameObject esGo = CreateSceneRoot(scene, "EventSystem", true);
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        GameObject hudGo = FindSceneObjectByName(scene, "IslandHUDCanvas") ?? CreateSceneRoot(scene, "IslandHUDCanvas", true);
        Canvas hudCanvas = hudGo.GetComponent<Canvas>() ?? hudGo.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = hudGo.GetComponent<CanvasScaler>() ?? hudGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        if (hudGo.GetComponent<GraphicRaycaster>() == null) hudGo.AddComponent<GraphicRaycaster>();

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
            "WASD Move  |  Shift Sprint  |  Space Jump  |  E Interact  |  I Inventory  |  F Board Boat",
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
        Image crossImg = crosshairGo.GetComponent<Image>() ?? crosshairGo.AddComponent<Image>();
        crossImg.color = new Color(1f, 1f, 1f, 0.85f);
        crossImg.raycastTarget = false;

        GameObject promptPanel = GetOrCreateChild(hudGo.transform, "InteractionPromptPanel");
        RectTransform promptRect = EnsureRectTransform(promptPanel);
        promptRect.anchorMin = new Vector2(0.5f, 0.14f);
        promptRect.anchorMax = new Vector2(0.5f, 0.14f);
        promptRect.sizeDelta = new Vector2(680f, 58f);
        promptRect.anchoredPosition = Vector2.zero;
        Image promptBg = promptPanel.GetComponent<Image>() ?? promptPanel.AddComponent<Image>();
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
        return playerCtrl;
    }

    private static void EnsureTreeTrunkCollider(GameObject tree)
    {
        if (tree == null)
            return;

        if (tree.GetComponentsInChildren<Collider>(true).Length > 0)
            return;

        CapsuleCollider col = tree.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 2.5f, 0f);
        col.radius = 0.42f;
        col.height = 5.5f;
    }

    private static void EnsureSolidColliders(GameObject root, bool useMeshColliderPerRenderer)
    {
        if (root == null)
            return;

        if (root.GetComponentsInChildren<Collider>(true).Length > 0)
            return;

        if (useMeshColliderPerRenderer)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh != null && filters[i].GetComponent<Collider>() == null)
                    filters[i].gameObject.AddComponent<MeshCollider>();
            }
            if (root.GetComponentsInChildren<Collider>(true).Length > 0)
                return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = bounds.size;
    }

    private static void AddCrab(Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject crab = InstantiateGrounded(CrabPath, parent, name, pos, yaw, 1.25f);
        if (crab != null && crab.GetComponent<BeachCrabWander>() == null)
            crab.AddComponent<BeachCrabWander>();
    }

    private static Vector3 FindWalkablePoint(Terrain terrain, float preferredX, float preferredZ, float searchRadius)
    {
        float terrainBaseY = terrain.transform.position.y;
        TerrainData data = terrain.terrainData;

        if (IsWalkable(terrain, data, terrainBaseY, preferredX, preferredZ, out float bestY))
            return new Vector3(preferredX, bestY, preferredZ);

        for (float r = 1.5f; r <= searchRadius; r += 1.5f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (IsWalkable(terrain, data, terrainBaseY, tx, tz, out float sampleY))
                    return new Vector3(tx, sampleY, tz);
            }
        }

        float fallbackY = terrain.SampleHeight(new Vector3(preferredX, 0f, preferredZ)) + terrainBaseY;
        return new Vector3(preferredX, Mathf.Max(0.55f, fallbackY), preferredZ);
    }

    private static bool IsWalkable(Terrain terrain, TerrainData data, float baseY, float worldX, float worldZ, out float worldY)
    {
        Vector3 origin = terrain.transform.position;
        float normX = (worldX - origin.x) / data.size.x;
        float normZ = (worldZ - origin.z) / data.size.z;
        worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + baseY;

        if (normX < 0.05f || normX > 0.95f || normZ < 0.05f || normZ > 0.95f)
            return false;
        if (worldY < 0.55f)
            return false;

        return data.GetSteepness(normX, normZ) < 30f;
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

    private static void AddFlickeringPointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = pos;
        Light l = go.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        l.shadows = LightShadows.None;
        FlickeringLight fl = go.AddComponent<FlickeringLight>();
        fl.baseIntensity = intensity;
    }

    private static void DisableDecorativePointShadows(Scene scene)
    {
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
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
        return go.GetComponent<RectTransform>() ?? go.AddComponent<RectTransform>();
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

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>() ?? go.AddComponent<TextMeshProUGUI>();
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

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
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
}
