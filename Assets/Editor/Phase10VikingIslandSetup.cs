using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Idempotent Phase 10 Editor Builder:
/// Creates reusable textured Viking prefabs (Longhouse with chimney smoke, Watchtower,
/// animated BannerFlag, flickering TorchPost, Palisade Fence), builds the matching physical
/// Viking Island Overview + Dock in GameScene.unity, and builds the detailed VikingIsland
/// (with solid colliders, 6 accessible Viking chests, Medium quizzes, Dock Repair Bench,
/// and Return Boat) inside reusable IslandScene.unity.
/// </summary>
public static class Phase10VikingIslandSetup
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string VikingTerrainPath = "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 2.asset";

    private const string PlankTexPath = "Assets/Textures/Planks023A_1K-JPG_Color.jpg";
    private const string CliffTexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Cliff_1/Cliff_1_Diffuse.tif";
    private const string MossRockTexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Mossy_Rock/Mossy_Rock_Diffuse.tif";

    private const string DockPrefabPath = "Assets/Island/Prefabs/Dock.prefab";
    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string BridgePrefabPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Bridge.prefab";
    private const string FireFxPath = "Assets/POLYART_Ancient Village/Prefabs/PA_FireFX_1.prefab";
    private const string CampfirePath = "Assets/ToonTropical/Prefabs/SM_Island_Campfire.prefab";
    private const string EmbersPath = "Assets/ToonTropical/Prefabs/FX/FX_Embers.prefab";
    private const string BarrelPath = "Assets/ToonTropical/Prefabs/SM_Island_Barrel.prefab";
    private const string CratePath = "Assets/ToonTropical/Prefabs/SM_Island_Crate.prefab";

    private const string LonghousePrefabPath = "Assets/Prefabs/VikingLonghouse.prefab";
    private const string WatchtowerPrefabPath = "Assets/Prefabs/VikingWatchtower.prefab";
    private const string BannerPrefabPath = "Assets/Prefabs/VikingBannerFlag.prefab";
    private const string TorchPrefabPath = "Assets/Prefabs/VikingTorchPost.prefab";
    private const string FencePrefabPath = "Assets/Prefabs/VikingFenceSegment.prefab";

    private static readonly string[] TreePaths = new string[]
    {
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile3.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile5.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile8.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile10.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet2_Pile1.prefab"
    };

    private static readonly string[] RockPaths = new string[]
    {
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet2_Pile1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet2_Pile2.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet2_Pile4.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet3_Pile1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_Mountainset1_hill1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_Mountainset1_hill3.prefab"
    };

    [MenuItem("Tools/SinkOrThink3D/Phase 10 - Build Viking Island (GameScene + IslandScene)")]
    public static void BuildVikingIsland()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");

        // Build/refresh reusable chest & textured Viking prefabs
        Phase6DiscoveryIslandSetup.BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);
        BuildVikingPrefabs(
            out GameObject longhousePrefab,
            out GameObject watchtowerPrefab,
            out GameObject bannerPrefab,
            out GameObject torchPrefab,
            out GameObject fencePrefab
        );

        // Part A: Build physical Viking Island Overview + Dock in GameScene.unity
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        BuildVikingOverviewInGameScene(gameScene, longhousePrefab, watchtowerPrefab, bannerPrefab, torchPrefab, fencePrefab);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        // Part B: Build Detailed VikingIsland inside reusable IslandScene.unity
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        BuildDetailedVikingInIslandScene(
            islandScene,
            longhousePrefab,
            watchtowerPrefab,
            bannerPrefab,
            torchPrefab,
            fencePrefab,
            regularChestPrefab,
            specialChestPrefab
        );
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        AssetDatabase.SaveAssets();

        Debug.Log("Phase 10 Complete! Built Viking Island overview in GameScene.unity and detailed VikingIsland in IslandScene.unity.");
        EditorUtility.DisplayDialog(
            "Phase 10 Viking Island Complete",
            "Built reusable textured Viking prefabs (with chimney smoke, waving flags, flickering torches, and solid colliders) and updated BOTH scenes:\n" +
            "- GameScene.unity: Added VikingIslandOverview + locked/unlockable BoatDockPoint_Viking (Terrain 2)\n" +
            "- IslandScene.unity: Built detailed VikingIsland (inactive by default; auto-activated on dock), 6 Viking Chests (Medium/Hard quizzes), Viking Dock Repair Bench & Return Boat.",
            "OK"
        );
    }

    private static void BuildVikingPrefabs(
        out GameObject longhousePrefab,
        out GameObject watchtowerPrefab,
        out GameObject bannerPrefab,
        out GameObject torchPrefab,
        out GameObject fencePrefab)
    {
        Texture2D plankTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlankTexPath);
        Texture2D cliffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CliffTexPath);
        Texture2D mossTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MossRockTexPath);

        Material darkTimberMat = GetOrCreateLitMaterial("VikingDarkTimber.mat", plankTex, new Color(0.46f, 0.32f, 0.2f, 1f), 0f, 0.16f);
        Material roofTimberMat = GetOrCreateLitMaterial("VikingRoofShingle.mat", plankTex, new Color(0.28f, 0.2f, 0.14f, 1f), 0f, 0.12f);
        Material stoneMat = GetOrCreateLitMaterial("VikingStoneWall.mat", cliffTex, new Color(0.66f, 0.68f, 0.72f, 1f), 0f, 0.12f);
        Material runeStoneMat = GetOrCreateLitMaterial("VikingRunestone.mat", mossTex != null ? mossTex : cliffTex, new Color(0.56f, 0.62f, 0.66f, 1f), 0.05f, 0.2f);
        Material bannerRedMat = GetOrCreateLitMaterial("VikingBannerCrimson.mat", null, new Color(0.64f, 0.14f, 0.12f, 1f), 0f, 0.15f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", null, new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material glowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material smokeMat = GetOrCreateParticleMaterial("VikingSmokeParticle.mat", new Color(0.78f, 0.8f, 0.84f, 0.45f));

        // 1. VikingLonghouse
        GameObject lh = new GameObject("VikingLonghouse");
        BoxCollider lhCol = lh.AddComponent<BoxCollider>();
        lhCol.center = new Vector3(0f, 2.2f, 0f);
        lhCol.size = new Vector3(7.2f, 4.4f, 11.4f);

        CreatePart(lh.transform, "StoneBase", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(7.1f, 0.9f, 11.2f), stoneMat);
        CreatePart(lh.transform, "TimberBody", PrimitiveType.Cube, new Vector3(0f, 1.95f, 0f), Vector3.zero, new Vector3(6.6f, 2.2f, 10.6f), darkTimberMat);
        CreatePart(lh.transform, "RoofSlope_L", PrimitiveType.Cube, new Vector3(-1.95f, 3.85f, 0f), new Vector3(0f, 0f, 36f), new Vector3(4.9f, 0.25f, 11.4f), roofTimberMat);
        CreatePart(lh.transform, "RoofSlope_R", PrimitiveType.Cube, new Vector3(1.95f, 3.85f, 0f), new Vector3(0f, 0f, -36f), new Vector3(4.9f, 0.25f, 11.4f), roofTimberMat);
        CreatePart(lh.transform, "RidgeBeam", PrimitiveType.Cube, new Vector3(0f, 5.2f, 0f), Vector3.zero, new Vector3(0.45f, 0.45f, 12.1f), darkTimberMat);
        CreatePart(lh.transform, "GableHorn_FL", PrimitiveType.Cube, new Vector3(-0.35f, 5.65f, 5.85f), new Vector3(0f, 0f, -24f), new Vector3(0.22f, 1.25f, 0.22f), brassMat);
        CreatePart(lh.transform, "GableHorn_FR", PrimitiveType.Cube, new Vector3(0.35f, 5.65f, 5.85f), new Vector3(0f, 0f, 24f), new Vector3(0.22f, 1.25f, 0.22f), brassMat);
        CreatePart(lh.transform, "FrontDoor", PrimitiveType.Cube, new Vector3(0f, 1.45f, 5.36f), Vector3.zero, new Vector3(1.65f, 2.2f, 0.16f), roofTimberMat);
        CreatePart(lh.transform, "PorchBeam_L", PrimitiveType.Cube, new Vector3(-1.35f, 1.5f, 5.7f), Vector3.zero, new Vector3(0.28f, 3.0f, 0.28f), darkTimberMat);
        CreatePart(lh.transform, "PorchBeam_R", PrimitiveType.Cube, new Vector3(1.35f, 1.5f, 5.7f), Vector3.zero, new Vector3(0.28f, 3.0f, 0.28f), darkTimberMat);
        CreatePart(lh.transform, "WindowGlow_L", PrimitiveType.Cube, new Vector3(-3.33f, 2.0f, 2.0f), Vector3.zero, new Vector3(0.08f, 0.65f, 0.9f), glowMat);
        CreatePart(lh.transform, "WindowGlow_R", PrimitiveType.Cube, new Vector3(3.33f, 2.0f, -2.0f), Vector3.zero, new Vector3(0.08f, 0.65f, 0.9f), glowMat);
        CreatePart(lh.transform, "StoneChimney", PrimitiveType.Cube, new Vector3(1.5f, 4.9f, -2.4f), Vector3.zero, new Vector3(1.05f, 2.4f, 1.05f), runeStoneMat);

        // Chimney smoke ParticleSystem
        GameObject smokeGo = new GameObject("ChimneySmoke");
        smokeGo.transform.SetParent(lh.transform, false);
        smokeGo.transform.localPosition = new Vector3(1.5f, 6.15f, -2.4f);
        smokeGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem smokePs = smokeGo.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule sMain = smokePs.main;
        sMain.playOnAwake = true;
        sMain.loop = true;
        sMain.startLifetime = 3.2f;
        sMain.startSpeed = 1.15f;
        sMain.startSize = 0.75f;
        sMain.startColor = new Color(0.84f, 0.86f, 0.9f, 0.45f);
        sMain.gravityModifier = -0.06f;
        sMain.maxParticles = 24;
        ParticleSystem.EmissionModule sEm = smokePs.emission;
        sEm.rateOverTime = 5f;
        ParticleSystem.ShapeModule sShape = smokePs.shape;
        sShape.shapeType = ParticleSystemShapeType.Cone;
        sShape.angle = 14f;
        sShape.radius = 0.25f;
        ParticleSystemRenderer sPr = smokeGo.GetComponent<ParticleSystemRenderer>();
        if (sPr != null && smokeMat != null) sPr.sharedMaterial = smokeMat;

        longhousePrefab = SavePrefabClean(lh, LonghousePrefabPath);

        // 2. VikingWatchtower
        GameObject wt = new GameObject("VikingWatchtower");
        BoxCollider wtCol = wt.AddComponent<BoxCollider>();
        wtCol.center = new Vector3(0f, 3.6f, 0f);
        wtCol.size = new Vector3(4.0f, 7.2f, 4.0f);
        CreatePart(wt.transform, "TowerStoneBase", PrimitiveType.Cube, new Vector3(0f, 0.6f, 0f), Vector3.zero, new Vector3(3.9f, 1.2f, 3.9f), stoneMat);
        Vector3[] legPos = new Vector3[]
        {
            new Vector3(-1.45f, 3.0f, -1.45f), new Vector3(1.45f, 3.0f, -1.45f),
            new Vector3(-1.45f, 3.0f, 1.45f), new Vector3(1.45f, 3.0f, 1.45f)
        };
        for (int i = 0; i < legPos.Length; i++)
            CreatePart(wt.transform, "TowerPillar_" + (i + 1), PrimitiveType.Cylinder, legPos[i], Vector3.zero, new Vector3(0.35f, 2.4f, 0.35f), darkTimberMat);
        CreatePart(wt.transform, "LookoutDeck", PrimitiveType.Cube, new Vector3(0f, 4.8f, 0f), Vector3.zero, new Vector3(4.3f, 0.3f, 4.3f), darkTimberMat);
        CreatePart(wt.transform, "TowerRoof_L", PrimitiveType.Cube, new Vector3(-1.1f, 6.7f, 0f), new Vector3(0f, 0f, 34f), new Vector3(2.8f, 0.22f, 4.5f), roofTimberMat);
        CreatePart(wt.transform, "TowerRoof_R", PrimitiveType.Cube, new Vector3(1.1f, 6.7f, 0f), new Vector3(0f, 0f, -34f), new Vector3(2.8f, 0.22f, 4.5f), roofTimberMat);
        CreatePart(wt.transform, "TowerBeacon", PrimitiveType.Cube, new Vector3(0f, 5.45f, 0f), Vector3.zero, new Vector3(0.5f, 0.5f, 0.5f), glowMat);
        watchtowerPrefab = SavePrefabClean(wt, WatchtowerPrefabPath);

        // 3. VikingBannerFlag (animated cloth)
        GameObject bf = new GameObject("VikingBannerFlag");
        CreatePart(bf.transform, "Pole", PrimitiveType.Cylinder, new Vector3(0f, 2.1f, 0f), Vector3.zero, new Vector3(0.14f, 2.1f, 0.14f), darkTimberMat);
        CreatePart(bf.transform, "PoleCap", PrimitiveType.Sphere, new Vector3(0f, 4.25f, 0f), Vector3.zero, Vector3.one * 0.26f, brassMat);
        GameObject clothPivot = new GameObject("BannerClothPivot");
        clothPivot.transform.SetParent(bf.transform, false);
        clothPivot.transform.localPosition = new Vector3(0f, 3.45f, 0.08f);
        clothPivot.AddComponent<FlagWaveAnimator>();
        CreatePart(clothPivot.transform, "Crossbar", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.08f, 0.72f, 0.08f), darkTimberMat);
        CreatePart(clothPivot.transform, "ClothBody", PrimitiveType.Cube, new Vector3(0f, -0.35f, 0.02f), Vector3.zero, new Vector3(1.15f, 1.75f, 0.04f), bannerRedMat);
        CreatePart(clothPivot.transform, "ClothGoldStripe", PrimitiveType.Cube, new Vector3(0f, -0.35f, 0.03f), Vector3.zero, new Vector3(0.22f, 1.75f, 0.05f), brassMat);
        bannerPrefab = SavePrefabClean(bf, BannerPrefabPath);

        // 4. VikingTorchPost (flickering, zero shadow overhead)
        GameObject tp = new GameObject("VikingTorchPost");
        CreatePart(tp.transform, "Post", PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0f), Vector3.zero, new Vector3(0.18f, 1.25f, 0.18f), darkTimberMat);
        CreatePart(tp.transform, "BrazierBowl", PrimitiveType.Cylinder, new Vector3(0f, 2.55f, 0f), Vector3.zero, new Vector3(0.45f, 0.16f, 0.45f), stoneMat);
        CreatePart(tp.transform, "FlameEmberCore", PrimitiveType.Sphere, new Vector3(0f, 2.75f, 0f), Vector3.zero, new Vector3(0.32f, 0.42f, 0.32f), glowMat);
        GameObject fireAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FireFxPath);
        if (fireAsset != null)
        {
            GameObject fireInst = (GameObject)PrefabUtility.InstantiatePrefab(fireAsset, tp.transform);
            fireInst.transform.localPosition = new Vector3(0f, 2.78f, 0f);
            Light[] fls = fireInst.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < fls.Length; i++)
            {
                fls[i].shadows = LightShadows.None;
                fls[i].range = 12f;
                if (fls[i].GetComponent<FlickeringLight>() == null)
                    fls[i].gameObject.AddComponent<FlickeringLight>();
            }
        }
        else
        {
            GameObject lightGo = new GameObject("TorchLight");
            lightGo.transform.SetParent(tp.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 2.95f, 0f);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.62f, 0.22f);
            l.intensity = 2.2f;
            l.range = 12f;
            l.shadows = LightShadows.None;
            lightGo.AddComponent<FlickeringLight>();
        }
        torchPrefab = SavePrefabClean(tp, TorchPrefabPath);

        // 5. VikingFenceSegment
        GameObject fnc = new GameObject("VikingFenceSegment");
        BoxCollider fCol = fnc.AddComponent<BoxCollider>();
        fCol.center = new Vector3(0f, 0.85f, 0f);
        fCol.size = new Vector3(3.8f, 1.7f, 0.45f);
        for (int i = -1; i <= 1; i++)
            CreatePart(fnc.transform, "Stake_" + (i + 2), PrimitiveType.Cylinder, new Vector3(i * 1.3f, 0.8f, 0f), Vector3.zero, new Vector3(0.22f, 0.85f, 0.22f), darkTimberMat);
        CreatePart(fnc.transform, "Rail_Top", PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f), Vector3.zero, new Vector3(3.8f, 0.16f, 0.16f), roofTimberMat);
        CreatePart(fnc.transform, "Rail_Bot", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(3.8f, 0.16f, 0.16f), roofTimberMat);
        fencePrefab = SavePrefabClean(fnc, FencePrefabPath);
    }

    private static void BuildVikingOverviewInGameScene(
        Scene gameScene,
        GameObject longhousePrefab,
        GameObject watchtowerPrefab,
        GameObject bannerPrefab,
        GameObject torchPrefab,
        GameObject fencePrefab)
    {
        DestroyRootIfExists(gameScene, "VikingIslandOverview");

        Terrain discoveryTerrain = FindSceneObjectByName(gameScene, "DiscoveryIsland_MainTerrain")?.GetComponent<Terrain>();
        float safeX = (discoveryTerrain != null && discoveryTerrain.terrainData != null)
            ? discoveryTerrain.transform.position.x + discoveryTerrain.terrainData.size.x + 25f
            : 360f;
        float safeZ = discoveryTerrain != null ? discoveryTerrain.transform.position.z : 150f;

        GameObject root = new GameObject("VikingIslandOverview");
        SceneManager.MoveGameObjectToScene(root, gameScene);
        root.transform.position = new Vector3(safeX, -9f, safeZ);

        TerrainData vikingData = AssetDatabase.LoadAssetAtPath<TerrainData>(VikingTerrainPath);
        GameObject terrainGo = new GameObject("VikingIsland_OverviewTerrain");
        terrainGo.transform.SetParent(root.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = vikingData;
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = vikingData;

        Vector3 dockWorld = FindSouthernShorelineWorld(terrain);
        GameObject dockGo = InstantiateAssetAtWorld(DockPrefabPath, root.transform, "VikingDock_Overview", dockWorld, 90f);

        // BoatDockPoint_Viking wired to BoatDocking + shared HealthCanvas HUD
        BoatDocking discoveryDock = FindDiscoveryDock(gameScene);
        GameObject dockPointGo = new GameObject("BoatDockPoint_Viking");
        dockPointGo.transform.SetParent(root.transform, false);
        dockPointGo.transform.position = new Vector3(dockWorld.x, 0.6f, dockWorld.z - 6.0f);
        dockPointGo.transform.rotation = Quaternion.identity;

        BoatDocking vikingDocking = dockPointGo.AddComponent<BoatDocking>();
        vikingDocking.islandIndex = 1;
        vikingDocking.autoMatchCurrentIsland = false;
        vikingDocking.islandDisplayName = "Viking Island";
        vikingDocking.targetIslandSceneName = "IslandScene";
        vikingDocking.transitionToIslandScene = true;
        vikingDocking.dockingDistance = 12f;
        vikingDocking.boat = discoveryDock != null ? discoveryDock.boat : FindSceneObjectByName(gameScene, "OldWoodenRowboat_Complete");
        vikingDocking.quizManager = discoveryDock != null ? discoveryDock.quizManager : Object.FindFirstObjectByType<QuizManager>();
        if (discoveryDock != null)
        {
            vikingDocking.promptPanel = discoveryDock.promptPanel;
            vikingDocking.promptText = discoveryDock.promptText;
            vikingDocking.distanceGuideText = discoveryDock.distanceGuideText;
        }

        GameObject parkGo = new GameObject("DockParkingPoint");
        parkGo.transform.SetParent(dockPointGo.transform, false);
        parkGo.transform.localPosition = new Vector3(0f, -0.55f, -2.0f);
        parkGo.transform.localRotation = Quaternion.identity;
        vikingDocking.dockParkingPoint = parkGo.transform;

        GameObject retSpawnGo = new GameObject("BoatReturnSpawnPoint");
        retSpawnGo.transform.SetParent(dockPointGo.transform, false);
        retSpawnGo.transform.localPosition = new Vector3(0f, -0.55f, -7.2f);
        retSpawnGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        vikingDocking.boatReturnSpawnPoint = retSpawnGo.transform;

        // Floating Viking Guide Buoy & dock torches/banners
        Material buoyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DockBuoyRed.mat");
        Material brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardBrass.mat");
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatLanternGlow.mat");
        GameObject buoy = new GameObject("VikingGuideBuoy");
        buoy.transform.SetParent(dockPointGo.transform, false);
        buoy.transform.localPosition = new Vector3(-4.5f, -0.55f, -5.0f);
        buoy.AddComponent<GentleFloat>();
        CreatePart(buoy.transform, "Base", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.85f, 0.32f, 0.85f), buoyMat);
        CreatePart(buoy.transform, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f), brassMat);
        CreatePart(buoy.transform, "Beacon", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.zero, Vector3.one * 0.32f, glowMat);

        InstantiatePrefabAtWorld(torchPrefab, root.transform, "Overview_Torch_L", new Vector3(dockWorld.x - 2.6f, dockWorld.y + 0.3f, dockWorld.z - 2.8f), 0f);
        InstantiatePrefabAtWorld(torchPrefab, root.transform, "Overview_Torch_R", new Vector3(dockWorld.x + 2.6f, dockWorld.y + 0.3f, dockWorld.z - 2.8f), 0f);
        InstantiatePrefabAtWorld(bannerPrefab, root.transform, "Overview_Banner_L", new Vector3(dockWorld.x - 3.1f, dockWorld.y + 0.3f, dockWorld.z + 1.5f), 0f);
        InstantiatePrefabAtWorld(bannerPrefab, root.transform, "Overview_Banner_R", new Vector3(dockWorld.x + 3.1f, dockWorld.y + 0.3f, dockWorld.z + 1.5f), 0f);

        // Shared 1:1 Viking village overview matching IslandScene
        PopulateSharedVikingEnvironment(root.transform, terrain, dockWorld, longhousePrefab, watchtowerPrefab, bannerPrefab, torchPrefab, fencePrefab, true);
        DisableDecorativePointShadows(gameScene);
    }

    private static void BuildDetailedVikingInIslandScene(
        Scene islandScene,
        GameObject longhousePrefab,
        GameObject watchtowerPrefab,
        GameObject bannerPrefab,
        GameObject torchPrefab,
        GameObject fencePrefab,
        GameObject regularChestPrefab,
        GameObject specialChestPrefab)
    {
        // Ensure DiscoveryIsland has warm tropical atmosphere profile
        GameObject discoveryRoot = FindRootObject(islandScene, "DiscoveryIsland");
        if (discoveryRoot != null)
        {
            IslandAtmosphereProfile discAtmo = discoveryRoot.GetComponent<IslandAtmosphereProfile>() ?? discoveryRoot.AddComponent<IslandAtmosphereProfile>();
            discAtmo.sunColor = new Color(1f, 0.95f, 0.86f);
            discAtmo.sunIntensity = 1.3f;
            discAtmo.fogColor = new Color(0.67f, 0.83f, 0.93f, 1f);
            discAtmo.fogDensity = 0.0018f;
            discAtmo.ambientSkyColor = new Color(0.55f, 0.65f, 0.78f, 1f);
        }

        // Rebuild VikingIsland root cleanly
        DestroyRootIfExists(islandScene, "VikingIsland");
        GameObject vikingRoot = new GameObject("VikingIsland");
        SceneManager.MoveGameObjectToScene(vikingRoot, islandScene);
        vikingRoot.transform.position = new Vector3(0f, -9f, 0f);

        IslandAtmosphereProfile vikAtmo = vikingRoot.AddComponent<IslandAtmosphereProfile>();
        vikAtmo.sunColor = new Color(0.86f, 0.92f, 1f);
        vikAtmo.sunIntensity = 1.15f;
        vikAtmo.sunEulerAngles = new Vector3(38f, -48f, 0f);
        vikAtmo.fogColor = new Color(0.54f, 0.64f, 0.74f, 1f);
        vikAtmo.fogDensity = 0.0032f;
        vikAtmo.ambientSkyColor = new Color(0.45f, 0.54f, 0.66f, 1f);

        TerrainData vikingData = AssetDatabase.LoadAssetAtPath<TerrainData>(VikingTerrainPath);
        GameObject terrainGo = new GameObject("VikingIsland_Terrain");
        terrainGo.transform.SetParent(vikingRoot.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = vikingData;
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = vikingData;

        Vector3 dockWorld = FindSouthernShorelineWorld(terrain);

        // Viking Dock Area
        GameObject dockArea = new GameObject("VikingDockArea");
        dockArea.transform.SetParent(vikingRoot.transform, false);

        InstantiateAssetAtWorld(DockPrefabPath, dockArea.transform, "VikingDock", dockWorld, 90f);
        InstantiatePrefabAtWorld(torchPrefab, dockArea.transform, "DockTorch_L", new Vector3(dockWorld.x - 2.6f, dockWorld.y + 0.3f, dockWorld.z - 2.8f), 0f);
        InstantiatePrefabAtWorld(torchPrefab, dockArea.transform, "DockTorch_R", new Vector3(dockWorld.x + 2.6f, dockWorld.y + 0.3f, dockWorld.z - 2.8f), 0f);
        InstantiatePrefabAtWorld(bannerPrefab, dockArea.transform, "DockBanner_L", new Vector3(dockWorld.x - 3.0f, dockWorld.y + 0.3f, dockWorld.z + 2.0f), 0f);
        InstantiatePrefabAtWorld(bannerPrefab, dockArea.transform, "DockBanner_R", new Vector3(dockWorld.x + 3.0f, dockWorld.y + 0.3f, dockWorld.z + 2.0f), 0f);

        // Walkable railed wooden ramp connecting VikingDock to shore
        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingDarkTimber.mat");
        Material brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardBrass.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingStoneWall.mat");
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        Vector3 rampEnd = FindWalkablePoint(terrain, dockWorld.x, dockWorld.z + 11.5f, 10f);
        Vector3 rampStart = new Vector3(dockWorld.x, dockWorld.y + 0.3f, dockWorld.z + 3.8f);
        Vector3 rampMid = (rampStart + rampEnd) * 0.5f - new Vector3(0f, 0.08f, 0f);
        float rampLen = Vector3.Distance(rampStart, rampEnd) + 1.0f;

        GameObject rampGo = CreatePart(dockArea.transform, "VikingDockRamp", PrimitiveType.Cube, rampMid, Vector3.zero, new Vector3(4.6f, 0.28f, rampLen), woodMat, true);
        rampGo.transform.position = rampMid;
        rampGo.transform.LookAt(rampEnd);
        CreatePart(rampGo.transform, "RampRail_L", PrimitiveType.Cube, new Vector3(-0.48f, 1.3f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), woodMat, true);
        CreatePart(rampGo.transform, "RampRail_R", PrimitiveType.Cube, new Vector3(0.48f, 1.3f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), woodMat, true);

        // Return Boat & Repair Workbench
        GameObject returnBoat = InstantiateAssetAtWorld(BoatPrefabPath, dockArea.transform, "VikingDockedBoat_Return", new Vector3(dockWorld.x, 0.08f, dockWorld.z - 6.2f), 180f);
        BoatMovement bm = returnBoat.GetComponent<BoatMovement>(); if (bm != null) Object.DestroyImmediate(bm);
        BoatSink bs = returnBoat.GetComponent<BoatSink>(); if (bs != null) Object.DestroyImmediate(bs);
        GentleFloat gf = returnBoat.AddComponent<GentleFloat>();
        gf.bobAmplitude = 0.05f;
        gf.tiltAngle = 2.0f;
        IslandBoatReturn boatRet = returnBoat.AddComponent<IslandBoatReturn>();
        boatRet.islandIndex = 1;
        boatRet.oceanSceneName = "GameScene";
        boatRet.interactRange = 8.5f;

        BuildDockRepairBench(dockArea.transform, new Vector3(dockWorld.x + 2.15f, dockWorld.y + 0.38f, dockWorld.z + 1.5f), woodMat, brassMat, sparkleMat);

        GameObject spawnGo = new GameObject("VikingSpawnPoint");
        spawnGo.transform.SetParent(dockArea.transform, false);
        spawnGo.transform.position = new Vector3(dockWorld.x, dockWorld.y + 0.85f, dockWorld.z - 0.5f);
        spawnGo.transform.rotation = Quaternion.identity;

        // Shared + Detailed Viking Environment
        PopulateSharedVikingEnvironment(vikingRoot.transform, terrain, dockWorld, longhousePrefab, watchtowerPrefab, bannerPrefab, torchPrefab, fencePrefab, false);

        // 6 Accessible Viking Chests (Medium Regular, Hard Special)
        GameObject chestsRoot = new GameObject("VikingChests");
        chestsRoot.transform.SetParent(vikingRoot.transform, false);

        Vector2[] regOffsets = new Vector2[]
        {
            new Vector2(18f, 18f),   // 1: Harbor Cove
            new Vector2(6f, 40f),    // 2: Longhouse Courtyard
            new Vector2(-20f, 34f),  // 3: Timber Bridge Clearing
            new Vector2(-28f, 66f),  // 4: Northern Conifer Grove
            new Vector2(30f, 62f)    // 5: Watchtower Ridge
        };
        float[] regYaws = new float[] { -145f, 180f, 125f, 145f, -135f };

        for (int i = 0; i < regOffsets.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + regOffsets[i].x, dockWorld.z + regOffsets[i].y, 16f);
            GameObject cObj = (GameObject)PrefabUtility.InstantiatePrefab(regularChestPrefab, chestsRoot.transform);
            cObj.name = "VikingChest_Regular_" + (i + 1);
            cObj.transform.position = p;
            cObj.transform.rotation = Quaternion.Euler(0f, regYaws[i], 0f);
            TreasureChest tcScript = cObj.GetComponent<TreasureChest>();
            tcScript.chestId = "viking_regular_" + (i + 1);
            tcScript.islandIndex = 1;
            tcScript.isSpecialChest = false;
            EditorUtility.SetDirty(tcScript);
        }

        // Special Chest at Jarl's Runestone Shrine
        Vector3 shrinePos = FindWalkablePoint(terrain, dockWorld.x + 4f, dockWorld.z + 82f, 18f);
        CreatePart(chestsRoot.transform, "JarlShrineDais", PrimitiveType.Cylinder, shrinePos + Vector3.up * 0.16f, Vector3.zero, new Vector3(5.2f, 0.32f, 5.2f), stoneMat, true);
        CreatePart(chestsRoot.transform, "JarlShrineStep", PrimitiveType.Cube, shrinePos + new Vector3(0f, 0.08f, -2.7f), Vector3.zero, new Vector3(2.5f, 0.18f, 1.3f), stoneMat, true);

        Vector3[] shrineCorners = new Vector3[]
        {
            new Vector3(-2.6f, 0f, -2.6f), new Vector3(2.6f, 0f, -2.6f),
            new Vector3(-2.6f, 0f, 2.6f), new Vector3(2.6f, 0f, 2.6f)
        };
        for (int i = 0; i < shrineCorners.Length; i++)
        {
            Vector3 lp = shrinePos + shrineCorners[i];
            lp.y = terrain.SampleHeight(lp) + terrain.transform.position.y;
            InstantiatePrefabAtWorld(i % 2 == 0 ? torchPrefab : bannerPrefab, chestsRoot.transform, "ShrineMarker_" + (i + 1), lp, 0f);
        }

        GameObject specialObj = (GameObject)PrefabUtility.InstantiatePrefab(specialChestPrefab, chestsRoot.transform);
        specialObj.name = "VikingChest_Special";
        specialObj.transform.position = shrinePos + Vector3.up * 0.32f;
        specialObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        TreasureChest spTc = specialObj.GetComponent<TreasureChest>();
        spTc.chestId = "viking_special_1";
        spTc.islandIndex = 1;
        spTc.isSpecialChest = true;
        EditorUtility.SetDirty(spTc);

        // Wire VikingIsland & VikingSpawnPoint into IslandSceneController; keep DiscoveryIsland active by default
        IslandSceneController sceneCtrl = Object.FindFirstObjectByType<IslandSceneController>();
        if (sceneCtrl != null)
        {
            sceneCtrl.vikingIslandRoot = vikingRoot;
            sceneCtrl.vikingSpawnPoint = spawnGo.transform;
            EditorUtility.SetDirty(sceneCtrl);
        }

        vikingRoot.SetActive(false);
        if (discoveryRoot != null)
            discoveryRoot.SetActive(true);

        DisableDecorativePointShadows(islandScene);
    }

    private static void PopulateSharedVikingEnvironment(
        Transform parent,
        Terrain terrain,
        Vector3 dockWorld,
        GameObject longhousePrefab,
        GameObject watchtowerPrefab,
        GameObject bannerPrefab,
        GameObject torchPrefab,
        GameObject fencePrefab,
        bool overviewOnly)
    {
        GameObject env = new GameObject(overviewOnly ? "VikingOverviewEnv" : "VikingEnvironment");
        env.transform.SetParent(parent, false);

        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingStoneWall.mat");

        // Longhouses arranged around the coastal village square
        Vector2[] houseOffsets = new Vector2[]
        {
            new Vector2(-14f, 38f),
            new Vector2(15f, 40f),
            new Vector2(-13f, 58f),
            new Vector2(16f, 60f)
        };
        float[] houseYaws = new float[] { 70f, -72f, 115f, -115f };
        int houseLimit = overviewOnly ? 3 : houseOffsets.Length;
        for (int i = 0; i < houseLimit; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + houseOffsets[i].x, dockWorld.z + houseOffsets[i].y, 14f);
            InstantiatePrefabAtWorld(longhousePrefab, env.transform, "VikingLonghouse_" + (i + 1), p, houseYaws[i]);
        }

        // Coastal Watchtower on Ridge
        Vector3 towerPos = FindWalkablePoint(terrain, dockWorld.x + 28f, dockWorld.z + 56f, 14f);
        InstantiatePrefabAtWorld(watchtowerPrefab, env.transform, "VikingWatchtower_Ridge", towerPos, -140f);

        // Walkable Wooden Bridge
        Vector3 bridgePos = FindWalkablePoint(terrain, dockWorld.x - 16f, dockWorld.z + 30f, 12f);
        GameObject bridge = InstantiateAssetAtWorld(BridgePrefabPath, env.transform, "VikingWoodenBridge", bridgePos, 20f);
        EnsureSolidColliders(bridge, true);

        // Trees (conifer/forest clusters with trunk colliders + WindSway)
        Vector2[] treeOffsets = new Vector2[]
        {
            new Vector2(-24f, 22f), new Vector2(24f, 24f), new Vector2(-30f, 44f), new Vector2(32f, 42f),
            new Vector2(-34f, 64f), new Vector2(36f, 68f), new Vector2(-18f, 76f), new Vector2(22f, 78f),
            new Vector2(-8f, 88f), new Vector2(14f, 92f), new Vector2(-26f, 52f), new Vector2(26f, 84f),
            new Vector2(-40f, 36f), new Vector2(40f, 34f), new Vector2(-36f, 78f), new Vector2(38f, 82f),
            new Vector2(-12f, 20f), new Vector2(12f, 20f), new Vector2(-4f, 72f), new Vector2(8f, 68f)
        };
        int treeLimit = overviewOnly ? 10 : treeOffsets.Length;
        for (int i = 0; i < treeLimit; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + treeOffsets[i].x, dockWorld.z + treeOffsets[i].y, 12f);
            GameObject tree = InstantiateAssetAtWorld(TreePaths[i % TreePaths.Length], env.transform, "VikingTree_" + (i + 1), p, i * 47f);
            if (!overviewOnly) EnsureTreeTrunkCollider(tree);
            if (tree != null && tree.GetComponent<WindSway>() == null)
            {
                WindSway ws = tree.AddComponent<WindSway>();
                ws.swayAngle = 2.0f;
            }
        }

        // Rocky Mountains & Cliffs
        Vector2[] rockOffsets = new Vector2[]
        {
            new Vector2(-28f, 28f), new Vector2(28f, 30f), new Vector2(-34f, 56f), new Vector2(34f, 52f),
            new Vector2(-22f, 84f), new Vector2(24f, 86f), new Vector2(0f, 96f), new Vector2(-38f, 70f),
            new Vector2(38f, 74f), new Vector2(-14f, 68f)
        };
        int rockLimit = overviewOnly ? 6 : rockOffsets.Length;
        for (int i = 0; i < rockLimit; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + rockOffsets[i].x, dockWorld.z + rockOffsets[i].y, 14f);
            GameObject rock = InstantiateAssetAtWorld(RockPaths[i % RockPaths.Length], env.transform, "VikingRock_" + (i + 1), p, i * 63f);
            if (!overviewOnly) EnsureSolidColliders(rock, true);
        }

        if (overviewOnly)
            return;

        // Detailed Viking Village Props: Campfire Courtyard, Palisade Fences, Cobblestones, Torches & Flags
        Vector3 campPos = FindWalkablePoint(terrain, dockWorld.x + 1f, dockWorld.z + 46f, 10f);
        GameObject camp = InstantiateAssetAtWorld(CampfirePath, env.transform, "VikingFirepit", campPos, 0f);
        camp.transform.localScale = Vector3.one * 1.35f;
        EnsureSolidColliders(camp, false);
        InstantiateAssetAtWorld(EmbersPath, env.transform, "VikingFirepitEmbers", campPos + Vector3.up * 0.25f, 0f);
        AddFlickeringPointLight(env.transform, "VikingFirepitLight", campPos + Vector3.up * 1.2f, new Color(1f, 0.56f, 0.18f), 2.5f, 16f);
        InstantiateAssetAtWorld(BarrelPath, env.transform, "VikingBarrel_1", FindWalkablePoint(terrain, campPos.x - 3.5f, campPos.z - 2f, 5f), 15f);
        InstantiateAssetAtWorld(CratePath, env.transform, "VikingCrate_1", FindWalkablePoint(terrain, campPos.x + 3.5f, campPos.z - 2f, 5f), -25f);

        // Palisade fences framing village entrance
        Vector2[] fenceOffsets = new Vector2[]
        {
            new Vector2(-6.5f, 24f), new Vector2(-10.5f, 24f),
            new Vector2(6.5f, 24f), new Vector2(10.5f, 24f),
            new Vector2(-16f, 48f), new Vector2(16f, 48f)
        };
        for (int i = 0; i < fenceOffsets.Length; i++)
        {
            Vector3 fp = FindWalkablePoint(terrain, dockWorld.x + fenceOffsets[i].x, dockWorld.z + fenceOffsets[i].y, 8f);
            InstantiatePrefabAtWorld(fencePrefab, env.transform, "VikingFence_" + (i + 1), fp, i >= 4 ? 90f : 0f);
        }

        // Cobblestone pathway stones + flickering torches + waving flags
        for (int i = 0; i < 10; i++)
        {
            float dz = 16f + i * 6.5f;
            float dx = Mathf.Sin(i * 0.55f) * 2.4f;
            Vector3 sp = FindWalkablePoint(terrain, dockWorld.x + dx, dockWorld.z + dz, 8f);
            CreatePart(env.transform, "VikingPathStone_" + (i + 1), PrimitiveType.Cylinder, sp + Vector3.up * 0.03f, new Vector3(0f, i * 31f, 0f), new Vector3(1.8f, 0.05f, 1.8f), stoneMat);

            if (i % 2 == 0)
            {
                Vector3 tp = FindWalkablePoint(terrain, sp.x + 2.5f, sp.z, 5f);
                InstantiatePrefabAtWorld(torchPrefab, env.transform, "TrailTorch_" + (i + 1), tp, 0f);
            }
            if (i % 3 == 0)
            {
                Vector3 bp = FindWalkablePoint(terrain, sp.x - 2.8f, sp.z, 5f);
                InstantiatePrefabAtWorld(bannerPrefab, env.transform, "TrailBanner_" + (i + 1), bp, 15f * i);
            }
        }
    }

    private static Vector3 FindSouthernShorelineWorld(Terrain terrain)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;

        float bestScore = float.MaxValue;
        Vector3 bestPos = new Vector3(origin.x + data.size.x * 0.5f, 0.6f, origin.z + data.size.z * 0.25f);

        for (float nx = 0.25f; nx <= 0.75f; nx += 0.05f)
        {
            for (float nz = 0.04f; nz <= 0.45f; nz += 0.02f)
            {
                float wx = origin.x + nx * data.size.x;
                float wz = origin.z + nz * data.size.z;
                float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + origin.y;

                if (wy >= 0.45f && wy <= 1.8f)
                {
                    float seaY = terrain.SampleHeight(new Vector3(wx, 0f, wz - 10f)) + origin.y;
                    float slope = data.GetSteepness(nx, nz);
                    if (seaY < 0.2f && slope < 28f)
                    {
                        float centerDist = Mathf.Abs(nx - 0.5f);
                        float score = centerDist * 20f + slope * 0.1f;
                        if (score < bestScore)
                        {
                            bestScore = score;
                            bestPos = new Vector3(wx, 0.6f, wz);
                        }
                    }
                    break;
                }
            }
        }

        return bestPos;
    }

    private static Vector3 FindWalkablePoint(Terrain terrain, float preferredX, float preferredZ, float searchRadius)
    {
        float baseY = terrain.transform.position.y;
        TerrainData data = terrain.terrainData;

        if (IsWalkable(terrain, data, baseY, preferredX, preferredZ, out float bestY))
            return new Vector3(preferredX, bestY, preferredZ);

        for (float r = 1.5f; r <= searchRadius; r += 1.5f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (IsWalkable(terrain, data, baseY, tx, tz, out float sampleY))
                    return new Vector3(tx, sampleY, tz);
            }
        }

        float fallbackY = terrain.SampleHeight(new Vector3(preferredX, 0f, preferredZ)) + baseY;
        return new Vector3(preferredX, Mathf.Max(0.6f, fallbackY), preferredZ);
    }

    private static bool IsWalkable(Terrain terrain, TerrainData data, float baseY, float worldX, float worldZ, out float worldY)
    {
        Vector3 origin = terrain.transform.position;
        float nx = (worldX - origin.x) / data.size.x;
        float nz = (worldZ - origin.z) / data.size.z;
        worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + baseY;

        if (nx < 0.05f || nx > 0.95f || nz < 0.05f || nz > 0.95f)
            return false;
        if (worldY < 0.6f)
            return false;

        return data.GetSteepness(nx, nz) < 30f;
    }

    private static void BuildDockRepairBench(Transform parent, Vector3 worldPos, Material woodMat, Material brassMat, Material sparkleMat)
    {
        GameObject bench = new GameObject("VikingDockRepairWorkbench");
        bench.transform.SetParent(parent, false);
        bench.transform.position = worldPos;
        bench.transform.rotation = Quaternion.Euler(0f, -90f, 0f);

        BoxCollider col = bench.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.55f, 0f);
        col.size = new Vector3(1.8f, 1.1f, 1.0f);

        CreatePart(bench.transform, "TableTop", PrimitiveType.Cube, new Vector3(0f, 0.85f, 0f), Vector3.zero, new Vector3(1.7f, 0.14f, 0.9f), woodMat);
        CreatePart(bench.transform, "Leg_FL", PrimitiveType.Cube, new Vector3(-0.72f, 0.4f, 0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreatePart(bench.transform, "Leg_FR", PrimitiveType.Cube, new Vector3(0.72f, 0.4f, 0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreatePart(bench.transform, "Leg_BL", PrimitiveType.Cube, new Vector3(-0.72f, 0.4f, -0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreatePart(bench.transform, "Leg_BR", PrimitiveType.Cube, new Vector3(0.72f, 0.4f, -0.34f), Vector3.zero, new Vector3(0.14f, 0.8f, 0.14f), woodMat);
        CreatePart(bench.transform, "SparePlank", PrimitiveType.Cube, new Vector3(-0.35f, 0.97f, 0f), new Vector3(0f, 12f, 0f), new Vector3(0.85f, 0.07f, 0.25f), woodMat);
        CreatePart(bench.transform, "HammerHead", PrimitiveType.Cube, new Vector3(0.52f, 0.98f, 0.18f), new Vector3(0f, 25f, 0f), new Vector3(0.22f, 0.10f, 0.11f), brassMat);

        GameObject fxGo = new GameObject("RepairParticles");
        fxGo.transform.SetParent(bench.transform, false);
        fxGo.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        fxGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.startLifetime = 0.75f;
        main.startSpeed = 2.2f;
        main.startSize = 0.14f;
        main.startColor = new Color(0.45f, 1f, 0.55f, 1f);
        main.maxParticles = 30;
        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });
        ParticleSystemRenderer pr = fxGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null && sparkleMat != null) pr.sharedMaterial = sparkleMat;

        DockRepairStation station = bench.AddComponent<DockRepairStation>();
        station.repairBurstParticles = ps;
        station.interactRange = 4.4f;
    }

    private static BoatDocking FindDiscoveryDock(Scene gameScene)
    {
        BoatDocking[] docks = Object.FindObjectsByType<BoatDocking>(FindObjectsSortMode.None);
        for (int i = 0; i < docks.Length; i++)
        {
            if (docks[i].gameObject.scene == gameScene && docks[i].islandIndex == 0)
                return docks[i];
        }
        return docks.Length > 0 ? docks[0] : null;
    }

    private static GameObject SavePrefabClean(GameObject root, string assetPath)
    {
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, assetPath);
        Object.DestroyImmediate(root);
        return saved;
    }

    private static GameObject InstantiateAssetAtWorld(string path, Transform parent, string name, Vector3 pos, float yaw)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        return InstantiatePrefabAtWorld(prefab, parent, name, pos, yaw);
    }

    private static GameObject InstantiatePrefabAtWorld(GameObject prefab, Transform parent, string name, Vector3 pos, float yaw)
    {
        if (prefab == null)
            return new GameObject(name);

        GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        inst.name = name;
        inst.transform.position = pos;
        inst.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
        return inst;
    }

    private static GameObject CreatePart(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat, bool keepCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;

        if (!keepCollider)
        {
            Collider c = go.GetComponent<Collider>();
            if (c != null) Object.DestroyImmediate(c);
        }

        if (mat != null && go.GetComponent<MeshRenderer>() != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        return go;
    }

    private static void EnsureTreeTrunkCollider(GameObject tree)
    {
        if (tree == null || tree.GetComponentsInChildren<Collider>(true).Length > 0)
            return;

        CapsuleCollider col = tree.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 2.5f, 0f);
        col.radius = 0.45f;
        col.height = 5.5f;
    }

    private static void EnsureSolidColliders(GameObject root, bool useMeshColliderPerRenderer)
    {
        if (root == null || root.GetComponentsInChildren<Collider>(true).Length > 0)
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

    private static void DestroyRootIfExists(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == rootName)
                Object.DestroyImmediate(roots[i]);
        }
    }

    private static GameObject FindRootObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }
        return null;
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

    private static Material GetOrCreateLitMaterial(string fileName, Texture2D mainTex, Color color, float metallic, float smoothness)
    {
        string path = "Assets/Materials/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader);
        if (mainTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
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

        mat = GetOrCreateLitMaterial(fileName, null, baseColor, 0f, 0.45f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateParticleMaterial(string fileName, Color color)
    {
        string path = "Assets/Materials/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
