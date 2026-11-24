using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Idempotent Phase 11 Editor Builder:
/// Creates reusable textured Ancient Ruin & Waterfall prefabs (AncientTempleArch,
/// AncientStonePillar, AncientTreasureChamber, AncientWaterfall), builds the matching physical
/// Final Treasure Island Overview + BoatDockPoint_Final in GameScene.unity, and builds the
/// detailed FinalTreasureIsland (twilight dusk lighting, fog, 6 accessible Final Chests,
/// Hard quizzes, Ancient Treasure Chamber for Special Chest & Golden Crown, Dock Repair Bench,
/// and Return Boat) inside reusable IslandScene.unity.
/// </summary>
public static class Phase11FinalIslandSetup
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string FinalTerrainPath = "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 3.asset";

    private const string Cliff2TexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Cliff_2/Cliff_2_Diffuse.tif";
    private const string MossRockTexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Mossy_Rock/Mossy_Rock_Diffuse.tif";
    private const string PlankTexPath = "Assets/Textures/Planks023A_1K-JPG_Color.jpg";
    private const string WaterfallTexPath = "Assets/Textures/WaterfallFoamPattern.png";

    private const string DockPrefabPath = "Assets/Island/Prefabs/Dock.prefab";
    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string OldTextPrefabPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Oldtext.prefab";
    private const string PortalSpherePrefabPath = "Assets/POLYART_Ancient Village/Prefabs/PA_PortalSphere.prefab";
    private const string BridgePrefabPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Bridge.prefab";
    private const string FireFxPath = "Assets/POLYART_Ancient Village/Prefabs/PA_FireFX_1.prefab";
    private const string BushPath = "Assets/ToonTropical/Prefabs/SM_Island_Bush_A.prefab";
    private const string GrassPath = "Assets/ToonTropical/Prefabs/SM_Island_GrassTuft_A.prefab";

    private const string ArchPrefabPath = "Assets/Prefabs/AncientTempleArch.prefab";
    private const string PillarPrefabPath = "Assets/Prefabs/AncientStonePillar.prefab";
    private const string ChamberPrefabPath = "Assets/Prefabs/AncientTreasureChamber.prefab";
    private const string WaterfallPrefabPath = "Assets/Prefabs/AncientWaterfall.prefab";

    private static readonly string[] TreePaths = new string[]
    {
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile2.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile4.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile7.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile9.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet1_Pile11.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_TreeSet2_Pile1.prefab"
    };

    private static readonly string[] RockPaths = new string[]
    {
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile2.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile4.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet2_Pile3.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet3_Pile1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_Mountainset1_hill2.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_mountainset2_hill1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_mountainset2_hill3.prefab"
    };

    [MenuItem("Tools/SinkOrThink3D/Phase 11 - Build Final Treasure Island (GameScene + IslandScene)")]
    public static void BuildFinalTreasureIsland()
    {
        RunBuild(true);
    }

    public static void RunBuild(bool showDialog)
    {
        if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Textures");

        // Build/refresh reusable chest & ancient ruin prefabs
        Phase6DiscoveryIslandSetup.BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);
        BuildFinalRuinsPrefabs(
            out GameObject archPrefab,
            out GameObject pillarPrefab,
            out GameObject chamberPrefab,
            out GameObject waterfallPrefab
        );

        // Part A: Build physical Final Island Overview + Dock in GameScene.unity
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        BuildFinalOverviewInGameScene(gameScene, archPrefab, pillarPrefab, chamberPrefab, waterfallPrefab);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        // Part B: Build Detailed FinalTreasureIsland inside reusable IslandScene.unity
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        BuildDetailedFinalInIslandScene(
            islandScene,
            archPrefab,
            pillarPrefab,
            chamberPrefab,
            waterfallPrefab,
            regularChestPrefab,
            specialChestPrefab
        );
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        AssetDatabase.SaveAssets();

        Debug.Log("Phase 11 Complete! Built Final Treasure Island overview in GameScene.unity and detailed FinalTreasureIsland in IslandScene.unity.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase 11 Final Treasure Island Complete",
                "Built reusable textured Ancient Ruin & Waterfall prefabs (AncientTempleArch, AncientStonePillar, AncientTreasureChamber, AncientWaterfall) and updated BOTH scenes:\n" +
                "- GameScene.unity: Added FinalIslandOverview + locked/unlockable BoatDockPoint_Final (Terrain 3)\n" +
                "- IslandScene.unity: Built detailed FinalTreasureIsland (inactive by default; auto-activated on dock), 6 Final Chests (Hard quizzes -> Legendary Golden Crown & Victory), Ancient Waterfall, Dock Repair Bench & Return Boat.",
                "OK"
            );
        }
    }

    private static void BuildFinalRuinsPrefabs(
        out GameObject archPrefab,
        out GameObject pillarPrefab,
        out GameObject chamberPrefab,
        out GameObject waterfallPrefab)
    {
        Texture2D cliffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Cliff2TexPath);
        Texture2D mossTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MossRockTexPath);
        Texture2D plankTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlankTexPath);
        Texture2D waterfallTex = EnsureWaterfallFoamTexture();

        Material mossyStoneMat = GetOrCreateLitMaterial("FinalMossyStone.mat", mossTex != null ? mossTex : cliffTex, new Color(0.58f, 0.64f, 0.56f, 1f), 0f, 0.14f);
        Material darkCliffMat = GetOrCreateLitMaterial("FinalDarkCliff.mat", cliffTex, new Color(0.45f, 0.44f, 0.48f, 1f), 0f, 0.1f);
        Material ancientGoldMat = GetOrCreateLitMaterial("FinalAncientGold.mat", null, new Color(0.86f, 0.68f, 0.24f, 1f), 0.88f, 0.62f);
        Material dockTimberMat = GetOrCreateLitMaterial("FinalAncientTimber.mat", plankTex, new Color(0.42f, 0.3f, 0.2f, 1f), 0f, 0.16f);
        Material arcaneGlowMat = GetOrCreateEmissiveMaterial("FinalArcaneGlow.mat", new Color(0.32f, 0.92f, 0.86f, 1f), new Color(0.24f, 0.88f, 0.82f, 1f) * 1.85f);
        Material torchGlowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material waterSheetMat = GetOrCreateWaterfallMaterial("FinalWaterfallWater.mat", waterfallTex);
        Material mistMat = GetOrCreateParticleMaterial("FinalWaterfallMist.mat", new Color(0.82f, 0.95f, 1f, 0.55f));

        // 1. AncientTempleArch (walkable clearance between pillars)
        GameObject arch = new GameObject("AncientTempleArch");
        CreatePart(arch.transform, "PillarBase_L", PrimitiveType.Cube, new Vector3(-2.65f, 0.45f, 0f), Vector3.zero, new Vector3(1.5f, 0.9f, 1.5f), darkCliffMat, true);
        CreatePart(arch.transform, "PillarBase_R", PrimitiveType.Cube, new Vector3(2.65f, 0.45f, 0f), Vector3.zero, new Vector3(1.5f, 0.9f, 1.5f), darkCliffMat, true);
        CreatePart(arch.transform, "PillarShaft_L", PrimitiveType.Cube, new Vector3(-2.65f, 2.8f, 0f), Vector3.zero, new Vector3(1.15f, 3.8f, 1.15f), mossyStoneMat, true);
        CreatePart(arch.transform, "PillarShaft_R", PrimitiveType.Cube, new Vector3(2.65f, 2.8f, 0f), Vector3.zero, new Vector3(1.15f, 3.8f, 1.15f), mossyStoneMat, true);
        CreatePart(arch.transform, "LintelBeam", PrimitiveType.Cube, new Vector3(0f, 5.15f, 0f), Vector3.zero, new Vector3(7.4f, 1.0f, 1.45f), mossyStoneMat);
        CreatePart(arch.transform, "CrownCapstone", PrimitiveType.Cube, new Vector3(0f, 5.95f, 0f), Vector3.zero, new Vector3(4.6f, 0.7f, 1.1f), darkCliffMat);
        CreatePart(arch.transform, "GoldTrim", PrimitiveType.Cube, new Vector3(0f, 5.15f, 0.76f), Vector3.zero, new Vector3(5.8f, 0.24f, 0.08f), ancientGoldMat);
        CreatePart(arch.transform, "SconceOrb_L", PrimitiveType.Sphere, new Vector3(-2.0f, 2.95f, 0.65f), Vector3.zero, Vector3.one * 0.36f, arcaneGlowMat);
        CreatePart(arch.transform, "SconceOrb_R", PrimitiveType.Sphere, new Vector3(2.0f, 2.95f, 0.65f), Vector3.zero, Vector3.one * 0.36f, arcaneGlowMat);
        archPrefab = SavePrefabClean(arch, ArchPrefabPath);

        // 2. AncientStonePillar
        GameObject pillar = new GameObject("AncientStonePillar");
        BoxCollider pCol = pillar.AddComponent<BoxCollider>();
        pCol.center = new Vector3(0f, 2.4f, 0f);
        pCol.size = new Vector3(1.5f, 4.8f, 1.5f);
        CreatePart(pillar.transform, "Plinth", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(1.55f, 0.9f, 1.55f), darkCliffMat);
        CreatePart(pillar.transform, "ColumnShaft", PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f), Vector3.zero, new Vector3(1.12f, 1.8f, 1.12f), mossyStoneMat);
        CreatePart(pillar.transform, "Capital", PrimitiveType.Cube, new Vector3(0f, 4.45f, 0f), Vector3.zero, new Vector3(1.48f, 0.42f, 1.48f), mossyStoneMat);
        CreatePart(pillar.transform, "RunicOrb", PrimitiveType.Sphere, new Vector3(0f, 4.92f, 0f), Vector3.zero, Vector3.one * 0.42f, arcaneGlowMat);
        pillarPrefab = SavePrefabClean(pillar, PillarPrefabPath);

        // 3. AncientTreasureChamber (open sanctuary housing Final Special Chest)
        GameObject chamber = new GameObject("AncientTreasureChamber");
        CreatePart(chamber.transform, "SanctuaryDais", PrimitiveType.Cube, new Vector3(0f, 0.18f, 0f), Vector3.zero, new Vector3(9.6f, 0.36f, 9.6f), darkCliffMat, true);
        CreatePart(chamber.transform, "GoldInlayRing", PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), Vector3.zero, new Vector3(4.2f, 0.04f, 4.2f), ancientGoldMat);
        CreatePart(chamber.transform, "FrontStair_Low", PrimitiveType.Cube, new Vector3(0f, 0.09f, -5.25f), Vector3.zero, new Vector3(3.6f, 0.18f, 1.1f), mossyStoneMat, true);
        CreatePart(chamber.transform, "FrontStair_Mid", PrimitiveType.Cube, new Vector3(0f, 0.18f, -4.55f), Vector3.zero, new Vector3(3.6f, 0.24f, 0.9f), mossyStoneMat, true);

        Vector3[] cornerPos = new Vector3[]
        {
            new Vector3(-3.8f, 0f, -3.8f),
            new Vector3(3.8f, 0f, -3.8f),
            new Vector3(-3.8f, 0f, 3.8f),
            new Vector3(3.8f, 0f, 3.8f)
        };
        for (int i = 0; i < cornerPos.Length; i++)
        {
            CreatePart(chamber.transform, "CornerPillar_" + (i + 1), PrimitiveType.Cube, cornerPos[i] + new Vector3(0f, 3.1f, 0f), Vector3.zero, new Vector3(1.15f, 5.8f, 1.15f), mossyStoneMat, true);
            CreatePart(chamber.transform, "CornerCap_" + (i + 1), PrimitiveType.Cube, cornerPos[i] + new Vector3(0f, 6.1f, 0f), Vector3.zero, new Vector3(1.45f, 0.45f, 1.45f), darkCliffMat);
        }

        // Ruined roof lintels high overhead
        CreatePart(chamber.transform, "TopBeam_North", PrimitiveType.Cube, new Vector3(0f, 6.45f, 3.8f), Vector3.zero, new Vector3(9.0f, 0.72f, 1.25f), mossyStoneMat);
        CreatePart(chamber.transform, "TopBeam_West", PrimitiveType.Cube, new Vector3(-3.8f, 6.45f, 0f), Vector3.zero, new Vector3(1.25f, 0.72f, 9.0f), mossyStoneMat);
        CreatePart(chamber.transform, "TopBeam_East", PrimitiveType.Cube, new Vector3(3.8f, 6.45f, 0f), Vector3.zero, new Vector3(1.25f, 0.72f, 9.0f), mossyStoneMat);
        CreatePart(chamber.transform, "BrokenArch_FrontL", PrimitiveType.Cube, new Vector3(-2.3f, 6.45f, -3.8f), new Vector3(0f, 0f, -7f), new Vector3(3.8f, 0.7f, 1.2f), darkCliffMat);
        CreatePart(chamber.transform, "BrokenArch_FrontR", PrimitiveType.Cube, new Vector3(2.3f, 6.45f, -3.8f), new Vector3(0f, 0f, 7f), new Vector3(3.8f, 0.7f, 1.2f), darkCliffMat);

        // Inscribed ancient tablet at rear & floating portal orb overhead
        GameObject oldTextAsset = AssetDatabase.LoadAssetAtPath<GameObject>(OldTextPrefabPath);
        if (oldTextAsset != null)
        {
            GameObject tablet = (GameObject)PrefabUtility.InstantiatePrefab(oldTextAsset, chamber.transform);
            tablet.name = "AncientInscriptionTablet";
            tablet.transform.localPosition = new Vector3(0f, 0.36f, 3.3f);
            tablet.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
            EnsureSolidColliders(tablet, false);
        }

        GameObject portalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(PortalSpherePrefabPath);
        if (portalAsset != null)
        {
            GameObject orb = (GameObject)PrefabUtility.InstantiatePrefab(portalAsset, chamber.transform);
            orb.name = "SanctuaryMysticOrb";
            orb.transform.localPosition = new Vector3(0f, 5.1f, 0f);
            orb.transform.localScale = Vector3.one * 0.85f;
            GentleFloat orbFloat = orb.AddComponent<GentleFloat>();
            orbFloat.bobAmplitude = 0.22f;
            orbFloat.tiltAngle = 3f;
            orbFloat.spinSpeed = 28f;
        }
        else
        {
            GameObject orb = CreatePart(chamber.transform, "SanctuaryMysticOrb", PrimitiveType.Sphere, new Vector3(0f, 5.1f, 0f), Vector3.zero, Vector3.one * 1.1f, arcaneGlowMat);
            GentleFloat orbFloat = orb.AddComponent<GentleFloat>();
            orbFloat.bobAmplitude = 0.22f;
            orbFloat.spinSpeed = 28f;
        }

        // Front flickering sanctuary braziers (zero shadow overhead)
        for (int i = 0; i < 2; i++)
        {
            float sideX = i == 0 ? -2.4f : 2.4f;
            CreatePart(chamber.transform, "BrazierBase_" + (i + 1), PrimitiveType.Cylinder, new Vector3(sideX, 0.85f, -3.6f), Vector3.zero, new Vector3(0.62f, 0.55f, 0.62f), darkCliffMat);
            CreatePart(chamber.transform, "BrazierFlame_" + (i + 1), PrimitiveType.Sphere, new Vector3(sideX, 1.55f, -3.6f), Vector3.zero, new Vector3(0.36f, 0.48f, 0.36f), torchGlowMat);
            AddFlickeringPointLight(chamber.transform, "ChamberBrazierLight_" + (i + 1), new Vector3(sideX, 1.85f, -3.6f), new Color(1f, 0.72f, 0.28f), 2.4f, 16f, true);
        }

        chamberPrefab = SavePrefabClean(chamber, ChamberPrefabPath);

        // 4. AncientWaterfall (cliff backing + UV-scrolling cascading water + plunge pool + mist)
        GameObject wf = new GameObject("AncientWaterfall");
        BoxCollider wfCol = wf.AddComponent<BoxCollider>();
        wfCol.center = new Vector3(0f, 5.5f, 1.2f);
        wfCol.size = new Vector3(6.8f, 11f, 3.2f);

        CreatePart(wf.transform, "CliffBacking", PrimitiveType.Cube, new Vector3(0f, 5.5f, 1.3f), Vector3.zero, new Vector3(7.4f, 11.4f, 2.8f), darkCliffMat);
        CreatePart(wf.transform, "CliffLedge_L", PrimitiveType.Cube, new Vector3(-3.3f, 4.8f, 0.3f), new Vector3(0f, 14f, 0f), new Vector3(2.2f, 10.0f, 2.2f), mossyStoneMat);
        CreatePart(wf.transform, "CliffLedge_R", PrimitiveType.Cube, new Vector3(3.3f, 4.8f, 0.3f), new Vector3(0f, -14f, 0f), new Vector3(2.2f, 10.0f, 2.2f), mossyStoneMat);

        GameObject sheet = CreatePart(wf.transform, "WaterfallSheet", PrimitiveType.Quad, new Vector3(0f, 5.5f, -0.25f), new Vector3(6f, 0f, 0f), new Vector3(3.8f, 11.2f, 1f), waterSheetMat);
        WaterfallAnimator wfAnim = sheet.AddComponent<WaterfallAnimator>();
        wfAnim.scrollSpeedY = -0.95f;
        wfAnim.foamPulseSpeed = 2.6f;
        wfAnim.foamPulseAmount = 0.09f;

        GameObject pool = CreatePart(wf.transform, "PlungePool", PrimitiveType.Cylinder, new Vector3(0f, 0.12f, -1.6f), Vector3.zero, new Vector3(6.2f, 0.12f, 4.4f), waterSheetMat);
        WaterfallAnimator poolAnim = pool.AddComponent<WaterfallAnimator>();
        poolAnim.scrollSpeedY = -0.25f;
        poolAnim.foamPulseAmount = 0.06f;

        GameObject mistGo = new GameObject("WaterfallMist");
        mistGo.transform.SetParent(wf.transform, false);
        mistGo.transform.localPosition = new Vector3(0f, 0.55f, -1.0f);
        mistGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem mistPs = mistGo.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule mMain = mistPs.main;
        mMain.playOnAwake = true;
        mMain.loop = true;
        mMain.startLifetime = 2.0f;
        mMain.startSpeed = 1.6f;
        mMain.startSize = 1.15f;
        mMain.startColor = new Color(0.84f, 0.96f, 1f, 0.5f);
        mMain.gravityModifier = 0.05f;
        mMain.maxParticles = 32;
        ParticleSystem.EmissionModule mEm = mistPs.emission;
        mEm.rateOverTime = 14f;
        ParticleSystem.ShapeModule mShape = mistPs.shape;
        mShape.shapeType = ParticleSystemShapeType.Box;
        mShape.scale = new Vector3(3.8f, 1.4f, 0.4f);
        ParticleSystemRenderer mPr = mistGo.GetComponent<ParticleSystemRenderer>();
        if (mPr != null && mistMat != null) mPr.sharedMaterial = mistMat;

        // Touch plank mat reference cleanly to ensure dock wood asset stays available
        if (dockTimberMat != null)
            EditorUtility.SetDirty(dockTimberMat);

        waterfallPrefab = SavePrefabClean(wf, WaterfallPrefabPath);
    }

    private static void BuildFinalOverviewInGameScene(
        Scene gameScene,
        GameObject archPrefab,
        GameObject pillarPrefab,
        GameObject chamberPrefab,
        GameObject waterfallPrefab)
    {
        DestroyRootIfExists(gameScene, "FinalIslandOverview");

        Terrain discoveryTerrain = FindSceneObjectByName(gameScene, "DiscoveryIsland_MainTerrain")?.GetComponent<Terrain>();
        TerrainData finalData = AssetDatabase.LoadAssetAtPath<TerrainData>(FinalTerrainPath);
        float finalWidth = finalData != null ? finalData.size.x : 500f;
        float safeX = discoveryTerrain != null ? (discoveryTerrain.transform.position.x - finalWidth - 25f) : -860f;
        float safeZ = discoveryTerrain != null ? (discoveryTerrain.transform.position.z + 35f) : 185f;

        GameObject root = new GameObject("FinalIslandOverview");
        SceneManager.MoveGameObjectToScene(root, gameScene);
        root.transform.position = new Vector3(safeX, -9f, safeZ);

        GameObject terrainGo = new GameObject("FinalIsland_OverviewTerrain");
        terrainGo.transform.SetParent(root.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = finalData;
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = finalData;

        Vector3 dockWorld = FindSouthernShorelineWorld(terrain);
        InstantiateAssetAtWorld(DockPrefabPath, root.transform, "FinalDock_Overview", dockWorld, 90f);

        // BoatDockPoint_Final wired to BoatDocking + shared HealthCanvas HUD
        BoatDocking discoveryDock = FindDiscoveryDock(gameScene);
        GameObject dockPointGo = new GameObject("BoatDockPoint_Final");
        dockPointGo.transform.SetParent(root.transform, false);
        dockPointGo.transform.position = new Vector3(dockWorld.x, 0.6f, dockWorld.z - 6.0f);
        dockPointGo.transform.rotation = Quaternion.identity;

        BoatDocking finalDocking = dockPointGo.AddComponent<BoatDocking>();
        finalDocking.islandIndex = 2;
        finalDocking.autoMatchCurrentIsland = false;
        finalDocking.islandDisplayName = "Final Treasure Island";
        finalDocking.targetIslandSceneName = "IslandScene";
        finalDocking.transitionToIslandScene = true;
        finalDocking.dockingDistance = 12f;
        finalDocking.boat = discoveryDock != null ? discoveryDock.boat : FindSceneObjectByName(gameScene, "OldWoodenRowboat_Complete");
        finalDocking.quizManager = discoveryDock != null ? discoveryDock.quizManager : Object.FindFirstObjectByType<QuizManager>();
        if (discoveryDock != null)
        {
            finalDocking.promptPanel = discoveryDock.promptPanel;
            finalDocking.promptText = discoveryDock.promptText;
            finalDocking.distanceGuideText = discoveryDock.distanceGuideText;
        }

        GameObject parkGo = new GameObject("DockParkingPoint");
        parkGo.transform.SetParent(dockPointGo.transform, false);
        parkGo.transform.localPosition = new Vector3(0f, -0.55f, -2.0f);
        parkGo.transform.localRotation = Quaternion.identity;
        finalDocking.dockParkingPoint = parkGo.transform;

        GameObject retSpawnGo = new GameObject("BoatReturnSpawnPoint");
        retSpawnGo.transform.SetParent(dockPointGo.transform, false);
        retSpawnGo.transform.localPosition = new Vector3(0f, -0.55f, -7.2f);
        retSpawnGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        finalDocking.boatReturnSpawnPoint = retSpawnGo.transform;

        // Floating Final Guide Buoy & dock guardian pillars
        Material buoyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DockBuoyRed.mat");
        Material brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientGold.mat");
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalArcaneGlow.mat");

        GameObject buoy = new GameObject("FinalGuideBuoy");
        buoy.transform.SetParent(dockPointGo.transform, false);
        buoy.transform.localPosition = new Vector3(-4.5f, -0.55f, -5.0f);
        buoy.AddComponent<GentleFloat>();
        CreatePart(buoy.transform, "Base", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.85f, 0.32f, 0.85f), buoyMat);
        CreatePart(buoy.transform, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f), brassMat);
        CreatePart(buoy.transform, "Beacon", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.zero, Vector3.one * 0.34f, glowMat);

        InstantiatePrefabAtWorld(pillarPrefab, root.transform, "Overview_DockPillar_L", new Vector3(dockWorld.x - 2.8f, dockWorld.y + 0.2f, dockWorld.z + 0.8f), 0f);
        InstantiatePrefabAtWorld(pillarPrefab, root.transform, "Overview_DockPillar_R", new Vector3(dockWorld.x + 2.8f, dockWorld.y + 0.2f, dockWorld.z + 0.8f), 0f);

        // Shared Final ruins & waterfall silhouette matching IslandScene
        PopulateSharedFinalEnvironment(root.transform, terrain, dockWorld, archPrefab, pillarPrefab, chamberPrefab, waterfallPrefab, true);
        DisableDecorativePointShadows(gameScene);
    }

    private static void BuildDetailedFinalInIslandScene(
        Scene islandScene,
        GameObject archPrefab,
        GameObject pillarPrefab,
        GameObject chamberPrefab,
        GameObject waterfallPrefab,
        GameObject regularChestPrefab,
        GameObject specialChestPrefab)
    {
        DestroyRootIfExists(islandScene, "FinalTreasureIsland");
        GameObject finalRoot = new GameObject("FinalTreasureIsland");
        SceneManager.MoveGameObjectToScene(finalRoot, islandScene);
        finalRoot.transform.position = new Vector3(0f, -9f, 0f);

        // Mysterious twilight golden-violet dusk atmosphere & fog
        IslandAtmosphereProfile atmo = finalRoot.AddComponent<IslandAtmosphereProfile>();
        atmo.sunColor = new Color(0.95f, 0.78f, 0.62f);
        atmo.sunIntensity = 1.05f;
        atmo.sunEulerAngles = new Vector3(28f, -130f, 0f);
        atmo.fogColor = new Color(0.28f, 0.3f, 0.42f, 1f);
        atmo.fogDensity = 0.0038f;
        atmo.ambientSkyColor = new Color(0.36f, 0.34f, 0.48f, 1f);

        TerrainData finalData = AssetDatabase.LoadAssetAtPath<TerrainData>(FinalTerrainPath);
        GameObject terrainGo = new GameObject("FinalIsland_Terrain");
        terrainGo.transform.SetParent(finalRoot.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = finalData;
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = finalData;

        Vector3 dockWorld = FindSouthernShorelineWorld(terrain);

        // Final Dock Area
        GameObject dockArea = new GameObject("FinalDockArea");
        dockArea.transform.SetParent(finalRoot.transform, false);

        InstantiateAssetAtWorld(DockPrefabPath, dockArea.transform, "FinalDock", dockWorld, 90f);
        InstantiatePrefabAtWorld(pillarPrefab, dockArea.transform, "DockPillar_L", new Vector3(dockWorld.x - 2.8f, dockWorld.y + 0.25f, dockWorld.z + 0.8f), 0f);
        InstantiatePrefabAtWorld(pillarPrefab, dockArea.transform, "DockPillar_R", new Vector3(dockWorld.x + 2.8f, dockWorld.y + 0.25f, dockWorld.z + 0.8f), 0f);

        Material timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientTimber.mat");
        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientGold.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalMossyStone.mat");
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        Vector3 rampEnd = FindWalkablePoint(terrain, dockWorld.x, dockWorld.z + 11.5f, 10f);
        Vector3 rampStart = new Vector3(dockWorld.x, dockWorld.y + 0.3f, dockWorld.z + 3.8f);
        Vector3 rampMid = (rampStart + rampEnd) * 0.5f - new Vector3(0f, 0.08f, 0f);
        float rampLen = Vector3.Distance(rampStart, rampEnd) + 1.0f;

        GameObject rampGo = CreatePart(dockArea.transform, "FinalDockRamp", PrimitiveType.Cube, rampMid, Vector3.zero, new Vector3(4.6f, 0.28f, rampLen), timberMat, true);
        rampGo.transform.position = rampMid;
        rampGo.transform.LookAt(rampEnd);
        CreatePart(rampGo.transform, "RampRail_L", PrimitiveType.Cube, new Vector3(-0.48f, 1.3f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), stoneMat, true);
        CreatePart(rampGo.transform, "RampRail_R", PrimitiveType.Cube, new Vector3(0.48f, 1.3f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), stoneMat, true);

        // Return Boat & Dock Repair Workbench
        GameObject returnBoat = InstantiateAssetAtWorld(BoatPrefabPath, dockArea.transform, "FinalDockedBoat_Return", new Vector3(dockWorld.x, 0.08f, dockWorld.z - 6.2f), 180f);
        BoatMovement bm = returnBoat.GetComponent<BoatMovement>(); if (bm != null) Object.DestroyImmediate(bm);
        BoatSink bs = returnBoat.GetComponent<BoatSink>(); if (bs != null) Object.DestroyImmediate(bs);
        GentleFloat gf = returnBoat.AddComponent<GentleFloat>();
        gf.bobAmplitude = 0.05f;
        gf.tiltAngle = 2.0f;
        IslandBoatReturn boatRet = returnBoat.AddComponent<IslandBoatReturn>();
        boatRet.islandIndex = 2;
        boatRet.oceanSceneName = "GameScene";
        boatRet.interactRange = 8.5f;

        BuildDockRepairBench(dockArea.transform, new Vector3(dockWorld.x + 2.15f, dockWorld.y + 0.38f, dockWorld.z + 1.5f), timberMat, goldMat, sparkleMat);

        GameObject spawnGo = new GameObject("FinalSpawnPoint");
        spawnGo.transform.SetParent(dockArea.transform, false);
        spawnGo.transform.position = new Vector3(dockWorld.x, dockWorld.y + 0.85f, dockWorld.z - 0.5f);
        spawnGo.transform.rotation = Quaternion.identity;

        // Shared + Detailed Ancient Ruin Environment
        Vector3 chamberPos = PopulateSharedFinalEnvironment(finalRoot.transform, terrain, dockWorld, archPrefab, pillarPrefab, chamberPrefab, waterfallPrefab, false);

        // 6 Accessible Final Treasure Chests (Hard Regular -> Final Special Chest in Ancient Treasure Chamber)
        GameObject chestsRoot = new GameObject("FinalChests");
        chestsRoot.transform.SetParent(finalRoot.transform, false);

        Vector2[] regOffsets = new Vector2[]
        {
            new Vector2(-18f, 20f),  // 1: Dockside Ruins Cove
            new Vector2(18f, 34f),   // 2: Fallen Pillar Courtyard
            new Vector2(-24f, 46f),  // 3: Waterfall Mist Clearing
            new Vector2(-32f, 70f),  // 4: Western Cliff Overlook
            new Vector2(24f, 64f)    // 5: Ancient Archway Terrace
        };
        float[] regYaws = new float[] { 135f, -135f, 110f, 145f, -150f };

        for (int i = 0; i < regOffsets.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + regOffsets[i].x, dockWorld.z + regOffsets[i].y, 16f);
            GameObject cObj = (GameObject)PrefabUtility.InstantiatePrefab(regularChestPrefab, chestsRoot.transform);
            cObj.name = "FinalChest_Regular_" + (i + 1);
            cObj.transform.position = p;
            cObj.transform.rotation = Quaternion.Euler(0f, regYaws[i], 0f);
            TreasureChest tcScript = cObj.GetComponent<TreasureChest>();
            tcScript.chestId = "final_regular_" + (i + 1);
            tcScript.islandIndex = 2;
            tcScript.isSpecialChest = false;
            EditorUtility.SetDirty(tcScript);
        }

        // Special Chest squarely inside AncientTreasureChamber dais (final_special_1 -> Golden Crown + Victory!)
        GameObject specialObj = (GameObject)PrefabUtility.InstantiatePrefab(specialChestPrefab, chestsRoot.transform);
        specialObj.name = "FinalChest_Special";
        specialObj.transform.position = chamberPos + Vector3.up * 0.36f;
        specialObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
        TreasureChest spTc = specialObj.GetComponent<TreasureChest>();
        spTc.chestId = "final_special_1";
        spTc.islandIndex = 2;
        spTc.isSpecialChest = true;
        EditorUtility.SetDirty(spTc);

        // Wire FinalTreasureIsland & FinalSpawnPoint into IslandSceneController; keep DiscoveryIsland active by default
        IslandSceneController sceneCtrl = Object.FindFirstObjectByType<IslandSceneController>();
        if (sceneCtrl != null)
        {
            sceneCtrl.finalTreasureIslandRoot = finalRoot;
            sceneCtrl.finalSpawnPoint = spawnGo.transform;
            EditorUtility.SetDirty(sceneCtrl);
        }

        finalRoot.SetActive(false);
        GameObject discoveryRoot = FindRootObject(islandScene, "DiscoveryIsland");
        if (discoveryRoot != null)
            discoveryRoot.SetActive(true);

        DisableDecorativePointShadows(islandScene);
    }

    private static Vector3 PopulateSharedFinalEnvironment(
        Transform parent,
        Terrain terrain,
        Vector3 dockWorld,
        GameObject archPrefab,
        GameObject pillarPrefab,
        GameObject chamberPrefab,
        GameObject waterfallPrefab,
        bool overviewOnly)
    {
        GameObject env = new GameObject(overviewOnly ? "FinalOverviewEnv" : "FinalEnvironment");
        env.transform.SetParent(parent, false);

        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalMossyStone.mat");

        // Sanctuary position (landmark matched between GameScene and IslandScene)
        Vector3 chamberPos = FindWalkablePoint(terrain, dockWorld.x + 2f, dockWorld.z + 82f, 18f);
        InstantiatePrefabAtWorld(chamberPrefab, env.transform, "AncientTreasureChamber_Sanctuary", chamberPos, 0f);

        // Temple Entry Archways along the sacred ascent
        Vector3 arch1Pos = FindWalkablePoint(terrain, dockWorld.x, dockWorld.z + 24f, 12f);
        InstantiatePrefabAtWorld(archPrefab, env.transform, "TempleArch_Lower", arch1Pos, 0f);

        Vector3 arch2Pos = FindWalkablePoint(terrain, dockWorld.x + 1f, dockWorld.z + 56f, 14f);
        InstantiatePrefabAtWorld(archPrefab, env.transform, "TempleArch_Upper", arch2Pos, 0f);

        // Cascading Ancient Waterfall flanking the western valley
        Vector3 waterfallPos = FindWalkablePoint(terrain, dockWorld.x - 28f, dockWorld.z + 54f, 18f);
        InstantiatePrefabAtWorld(waterfallPrefab, env.transform, "AncientWaterfall_Valley", waterfallPos, 28f);

        // Ancient Stone Bridge near lower ruins stream
        Vector3 bridgePos = FindWalkablePoint(terrain, dockWorld.x - 16f, dockWorld.z + 34f, 14f);
        GameObject bridge = InstantiateAssetAtWorld(BridgePrefabPath, env.transform, "AncientRuinBridge", bridgePos, -15f);
        EnsureSolidColliders(bridge, true);

        // Dense jungle/ancient trees with trunk colliders + WindSway
        Vector2[] treeOffsets = new Vector2[]
        {
            new Vector2(-22f, 18f), new Vector2(22f, 20f), new Vector2(-30f, 36f), new Vector2(28f, 38f),
            new Vector2(-36f, 52f), new Vector2(34f, 56f), new Vector2(-22f, 74f), new Vector2(26f, 76f),
            new Vector2(-12f, 92f), new Vector2(14f, 94f), new Vector2(-14f, 44f), new Vector2(16f, 46f),
            new Vector2(-38f, 26f), new Vector2(38f, 28f), new Vector2(-40f, 68f), new Vector2(38f, 72f),
            new Vector2(-10f, 22f), new Vector2(10f, 22f), new Vector2(-8f, 66f), new Vector2(12f, 68f)
        };
        int treeLimit = overviewOnly ? 10 : treeOffsets.Length;
        for (int i = 0; i < treeLimit; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + treeOffsets[i].x, dockWorld.z + treeOffsets[i].y, 12f);
            GameObject tree = InstantiateAssetAtWorld(TreePaths[i % TreePaths.Length], env.transform, "AncientTree_" + (i + 1), p, i * 41f);
            if (!overviewOnly) EnsureTreeTrunkCollider(tree);
            if (tree != null && tree.GetComponent<WindSway>() == null)
            {
                WindSway ws = tree.AddComponent<WindSway>();
                ws.swayAngle = 2.2f;
            }
        }

        // Dramatic Cliffs, Mountains & Ancient Boulders
        Vector2[] rockOffsets = new Vector2[]
        {
            new Vector2(-28f, 24f), new Vector2(28f, 26f), new Vector2(-36f, 46f), new Vector2(34f, 48f),
            new Vector2(-26f, 82f), new Vector2(28f, 84f), new Vector2(0f, 98f), new Vector2(-42f, 64f),
            new Vector2(40f, 66f), new Vector2(20f, 54f)
        };
        int rockLimit = overviewOnly ? 6 : rockOffsets.Length;
        for (int i = 0; i < rockLimit; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, dockWorld.x + rockOffsets[i].x, dockWorld.z + rockOffsets[i].y, 14f);
            GameObject rock = InstantiateAssetAtWorld(RockPaths[i % RockPaths.Length], env.transform, "AncientCliffRock_" + (i + 1), p, i * 57f);
            if (!overviewOnly) EnsureSolidColliders(rock, true);
        }

        if (overviewOnly)
            return chamberPos;

        // Scattered Ancient Temple Pillars & Relics
        Vector2[] pillarOffsets = new Vector2[]
        {
            new Vector2(-6f, 18f), new Vector2(6f, 18f),
            new Vector2(14f, 34f), new Vector2(21f, 38f),
            new Vector2(-10f, 52f), new Vector2(12f, 52f),
            new Vector2(-29f, 66f), new Vector2(22f, 62f)
        };
        for (int i = 0; i < pillarOffsets.Length; i++)
        {
            Vector3 pp = FindWalkablePoint(terrain, dockWorld.x + pillarOffsets[i].x, dockWorld.z + pillarOffsets[i].y, 10f);
            InstantiatePrefabAtWorld(pillarPrefab, env.transform, "TemplePillar_" + (i + 1), pp, i * 25f);
        }

        // Inscribed ancient ruin monoliths along overlook spots
        Vector3 tabletWest = FindWalkablePoint(terrain, dockWorld.x - 21f, dockWorld.z + 44f, 10f);
        GameObject tw = InstantiateAssetAtWorld(OldTextPrefabPath, env.transform, "AncientMonolith_West", tabletWest, 45f);
        EnsureSolidColliders(tw, false);

        Vector3 tabletEast = FindWalkablePoint(terrain, dockWorld.x + 24f, dockWorld.z + 58f, 10f);
        GameObject te = InstantiateAssetAtWorld(OldTextPrefabPath, env.transform, "AncientMonolith_East", tabletEast, -40f);
        EnsureSolidColliders(te, false);

        // Winding Mossy Flagstone Path from Dock to Sanctuary + Dense Bushes/Grass
        for (int i = 0; i < 11; i++)
        {
            float dz = 14f + i * 6.2f;
            float dx = Mathf.Sin(i * 0.5f) * 2.2f;
            Vector3 sp = FindWalkablePoint(terrain, dockWorld.x + dx, dockWorld.z + dz, 8f);
            CreatePart(env.transform, "AncientPathStone_" + (i + 1), PrimitiveType.Cylinder, sp + Vector3.up * 0.03f, new Vector3(0f, i * 33f, 0f), new Vector3(1.9f, 0.06f, 1.9f), stoneMat);

            if (i % 2 == 1)
            {
                Vector3 bushPos = FindWalkablePoint(terrain, sp.x + 3.2f, sp.z, 6f);
                GameObject bush = InstantiateAssetAtWorld(BushPath, env.transform, "JungleBush_" + (i + 1), bushPos, i * 45f);
                if (bush != null && bush.GetComponent<WindSway>() == null) bush.AddComponent<WindSway>();
            }
            else
            {
                Vector3 grassPos = FindWalkablePoint(terrain, sp.x - 3.0f, sp.z, 6f);
                GameObject grass = InstantiateAssetAtWorld(GrassPath, env.transform, "JungleGrass_" + (i + 1), grassPos, i * 35f);
                if (grass != null && grass.GetComponent<WindSway>() == null) grass.AddComponent<WindSway>();
            }
        }

        // Flickering fire braziers flanking the lower & upper paths (zero shadow overhead)
        Vector3 lowerFirePos = FindWalkablePoint(terrain, dockWorld.x + 8f, dockWorld.z + 28f, 10f);
        InstantiateFireFxNoShadows(env.transform, "PathFireFX_Lower", lowerFirePos);
        Vector3 upperFirePos = FindWalkablePoint(terrain, dockWorld.x - 8f, dockWorld.z + 62f, 10f);
        InstantiateFireFxNoShadows(env.transform, "PathFireFX_Upper", upperFirePos);

        return chamberPos;
    }

    private static void InstantiateFireFxNoShadows(Transform parent, string name, Vector3 pos)
    {
        GameObject fireAsset = AssetDatabase.LoadAssetAtPath<GameObject>(FireFxPath);
        if (fireAsset != null)
        {
            GameObject fireInst = (GameObject)PrefabUtility.InstantiatePrefab(fireAsset, parent);
            fireInst.name = name;
            fireInst.transform.position = pos;
            Light[] fls = fireInst.GetComponentsInChildren<Light>(true);
            for (int i = 0; i < fls.Length; i++)
            {
                fls[i].shadows = LightShadows.None;
                fls[i].range = 14f;
                if (fls[i].GetComponent<FlickeringLight>() == null)
                    fls[i].gameObject.AddComponent<FlickeringLight>();
            }
        }
        else
        {
            AddFlickeringPointLight(parent, name, pos + Vector3.up * 1.1f, new Color(1f, 0.65f, 0.25f), 2.3f, 14f, false);
        }
    }

    private static void BuildDockRepairBench(Transform parent, Vector3 worldPos, Material woodMat, Material goldMat, Material sparkleMat)
    {
        GameObject bench = new GameObject("FinalDockRepairWorkbench");
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
        CreatePart(bench.transform, "HammerHead", PrimitiveType.Cube, new Vector3(0.52f, 0.98f, 0.18f), new Vector3(0f, 25f, 0f), new Vector3(0.22f, 0.10f, 0.11f), goldMat);

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

    private static Texture2D EnsureWaterfallFoamTexture()
    {
        Texture2D existing = AssetDatabase.LoadAssetAtPath<Texture2D>(WaterfallTexPath);
        if (existing != null)
            return existing;

        const int w = 128;
        const int h = 128;
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w;
                float v = y / (float)h;
                float streak = Mathf.PerlinNoise(u * 14f, v * 4.5f);
                float foam = Mathf.Pow(streak, 1.6f);
                Color c = Color.Lerp(
                    new Color(0.18f, 0.68f, 0.84f, 0.76f),
                    new Color(0.86f, 0.98f, 1f, 0.94f),
                    foam
                );
                pixels[y * w + x] = c;
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        string fullPath = Path.GetFullPath(WaterfallTexPath);
        File.WriteAllBytes(fullPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(WaterfallTexPath, ImportAssetOptions.ForceUpdate);
        return AssetDatabase.LoadAssetAtPath<Texture2D>(WaterfallTexPath);
    }

    private static Material GetOrCreateWaterfallMaterial(string fileName, Texture2D foamTex)
    {
        string path = "Assets/Materials/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        mat = new Material(shader);
        if (foamTex != null && mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", foamTex);
        if (foamTex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", foamTex);
        if (mat.HasProperty("_BaseMap")) mat.SetTextureScale("_BaseMap", new Vector2(2f, 3.5f));
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.36f, 0.84f, 0.94f, 0.86f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.36f, 0.84f, 0.94f, 0.86f));
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.82f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", new Color(0.08f, 0.34f, 0.42f, 1f));
        AssetDatabase.CreateAsset(mat, path);
        return mat;
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

    private static void AddFlickeringPointLight(Transform parent, string name, Vector3 pos, Color color, float intensity, float range, bool localPos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        if (localPos)
            go.transform.localPosition = pos;
        else
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
        if (mainTex != null && mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
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
