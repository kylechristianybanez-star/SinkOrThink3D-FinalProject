using System.Collections.Generic;
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
    private const string SourceFinalTerrainPath = "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 3.asset";
    private const string FinalTerrainPath = "Assets/Terrain/FinalTreasureTerrain.asset";

    private const string Cliff2TexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Cliff_2/Cliff_2_Diffuse.tif";
    private const string MossRockTexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Mossy_Rock/Mossy_Rock_Diffuse.tif";
    private const string PlankTexPath = "Assets/Textures/Planks023A_1K-JPG_Color.jpg";
    private const string WaterfallTexPath = "Assets/Textures/WaterfallFoamPattern.png";

    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string OldTextPrefabPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Oldtext.prefab";
    private const string PortalSpherePrefabPath = "Assets/POLYART_Ancient Village/Prefabs/PA_PortalSphere.prefab";
    private const string BridgePrefabPath = "Assets/Prefabs/AncientStoneBridge.prefab";
    private const string CampfirePath = "Assets/ToonTropical/Prefabs/SM_Island_Campfire.prefab";
    private const string EmbersPath = "Assets/ToonTropical/Prefabs/FX/FX_Embers.prefab";
    private const string BushPath = "Assets/ToonTropical/Prefabs/SM_Island_Bush_A.prefab";
    private const string GrassPath = "Assets/ToonTropical/Prefabs/SM_Island_GrassTuft_A.prefab";
    private const string BarrelPath = "Assets/ToonTropical/Prefabs/SM_Island_Barrel.prefab";
    private const string CratePath = "Assets/ToonTropical/Prefabs/SM_Island_Crate.prefab";

    private const string ArchPrefabPath = "Assets/Prefabs/AncientTempleArch.prefab";
    private const string PillarPrefabPath = "Assets/Prefabs/AncientStonePillar.prefab";
    private const string ChamberPrefabPath = "Assets/Prefabs/AncientTreasureChamber.prefab";
    private const string WaterfallPrefabPath = "Assets/Prefabs/AncientWaterfall.prefab";
    private const string RuinHousePrefabPath = "Assets/Prefabs/AncientRuinHouse.prefab";

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
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet3_Pile1.prefab"
    };

    private static readonly string[] MountainPaths = new string[]
    {
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
        EnsureFolder("Assets/Terrain");

        // Reset FinalTreasureTerrain from clean source once before flattening temple pads
        IslandPlacementUtility.ResetTerrainFromSource(SourceFinalTerrainPath, FinalTerrainPath, new Vector3(480f, 76f, 480f));

        // Build/refresh reusable chest & ancient ruin prefabs
        Phase6DiscoveryIslandSetup.BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);
        BuildFinalRuinsPrefabs(
            out GameObject archPrefab,
            out GameObject pillarPrefab,
            out GameObject chamberPrefab,
            out GameObject waterfallPrefab,
            out GameObject ruinHousePrefab,
            out GameObject stoneBridgePrefab
        );

        // Part A: Build Detailed FinalTreasureIsland FIRST in IslandScene.unity (flattens pads into shared terrain)
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        BuildDetailedFinalInIslandScene(
            islandScene,
            archPrefab,
            pillarPrefab,
            chamberPrefab,
            waterfallPrefab,
            ruinHousePrefab,
            stoneBridgePrefab,
            regularChestPrefab,
            specialChestPrefab
        );
        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(islandScene);
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);
        AssetDatabase.SaveAssets();

        // Part B: Build physical Final Island Overview + Dock in GameScene.unity using leveled terrain
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        BuildFinalOverviewInGameScene(gameScene, archPrefab, pillarPrefab, chamberPrefab, waterfallPrefab, ruinHousePrefab, stoneBridgePrefab);
        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(gameScene);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

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
        out GameObject waterfallPrefab,
        out GameObject ruinHousePrefab,
        out GameObject stoneBridgePrefab)
    {
        Texture2D cliffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(Cliff2TexPath);
        Texture2D mossTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MossRockTexPath);
        Texture2D plankTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlankTexPath);
        Texture2D waterfallTex = EnsureWaterfallFoamTexture();

        Material mossyStoneMat = GetOrCreateLitMaterial("FinalMossyStone.mat", mossTex != null ? mossTex : cliffTex, new Color(0.88f, 0.94f, 0.86f, 1f), 0f, 0.14f);
        Material darkCliffMat = GetOrCreateLitMaterial("FinalDarkCliff.mat", cliffTex, new Color(0.78f, 0.78f, 0.82f, 1f), 0f, 0.1f);
        Material ancientGoldMat = GetOrCreateLitMaterial("FinalAncientGold.mat", null, new Color(0.86f, 0.68f, 0.24f, 1f), 0.88f, 0.62f);
        Material dockTimberMat = GetOrCreateLitMaterial("FinalAncientTimber.mat", plankTex, new Color(0.82f, 0.68f, 0.52f, 1f), 0f, 0.16f);
        Material arcaneGlowMat = GetOrCreateEmissiveMaterial("FinalArcaneGlow.mat", new Color(0.32f, 0.92f, 0.86f, 1f), new Color(0.24f, 0.88f, 0.82f, 1f) * 1.85f);
        Material torchGlowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material waterSheetMat = GetOrCreateWaterfallMaterial("FinalWaterfallWater.mat", waterfallTex);
        Material mistMat = GetOrCreateParticleMaterial("FinalWaterfallMist.mat", new Color(0.82f, 0.95f, 1f, 0.55f));

        // 1. AncientTempleArch (walkable clearance between pillars + deep stone pillar foundations)
        GameObject arch = new GameObject("AncientTempleArch");
        CreatePart(arch.transform, "PillarFoundation_L", PrimitiveType.Cube, new Vector3(-2.65f, -1.25f, 0f), Vector3.zero, new Vector3(1.75f, 2.8f, 1.75f), darkCliffMat, true);
        CreatePart(arch.transform, "PillarFoundation_R", PrimitiveType.Cube, new Vector3(2.65f, -1.25f, 0f), Vector3.zero, new Vector3(1.75f, 2.8f, 1.75f), darkCliffMat, true);
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

        // 2. AncientStonePillar (with deep stone footing)
        GameObject pillar = new GameObject("AncientStonePillar");
        BoxCollider pCol = pillar.AddComponent<BoxCollider>();
        pCol.center = new Vector3(0f, 2.4f, 0f);
        pCol.size = new Vector3(1.5f, 4.8f, 1.5f);
        CreatePart(pillar.transform, "PillarFoundationSkirt", PrimitiveType.Cube, new Vector3(0f, -1.25f, 0f), Vector3.zero, new Vector3(1.75f, 2.8f, 1.75f), darkCliffMat);
        CreatePart(pillar.transform, "Plinth", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(1.55f, 0.9f, 1.55f), darkCliffMat);
        CreatePart(pillar.transform, "ColumnShaft", PrimitiveType.Cylinder, new Vector3(0f, 2.5f, 0f), Vector3.zero, new Vector3(1.12f, 1.8f, 1.12f), mossyStoneMat);
        CreatePart(pillar.transform, "Capital", PrimitiveType.Cube, new Vector3(0f, 4.45f, 0f), Vector3.zero, new Vector3(1.48f, 0.42f, 1.48f), mossyStoneMat);
        CreatePart(pillar.transform, "RunicOrb", PrimitiveType.Sphere, new Vector3(0f, 4.92f, 0f), Vector3.zero, Vector3.one * 0.42f, arcaneGlowMat);
        pillarPrefab = SavePrefabClean(pillar, PillarPrefabPath);

        // 3. AncientTreasureChamber (open sanctuary housing Final Special Chest + deep cliff foundation skirt)
        GameObject chamber = new GameObject("AncientTreasureChamber");
        CreatePart(chamber.transform, "ChamberFoundationSkirt", PrimitiveType.Cube, new Vector3(0f, -1.35f, 0f), Vector3.zero, new Vector3(10.0f, 3.0f, 10.0f), darkCliffMat, true);
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

        CreatePart(wf.transform, "WaterfallFoundationSkirt", PrimitiveType.Cube, new Vector3(0f, -1.35f, 1.3f), Vector3.zero, new Vector3(7.8f, 3.0f, 3.4f), darkCliffMat);
        CreatePart(wf.transform, "PlungePoolFoundation", PrimitiveType.Cylinder, new Vector3(0f, -0.95f, -1.6f), Vector3.zero, new Vector3(6.4f, 1.0f, 4.6f), darkCliffMat, true);
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
        mistPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        // 5. AncientRuinHouse (weathered mossy-stone temple dwelling with deep foundation skirt & arcane lanterns)
        GameObject house = new GameObject("AncientRuinHouse");
        BoxCollider hCol = house.AddComponent<BoxCollider>();
        hCol.center = new Vector3(0f, 2.2f, 0f);
        hCol.size = new Vector3(5.2f, 4.4f, 4.2f);
        CreatePart(house.transform, "FoundationSkirt", PrimitiveType.Cube, new Vector3(0f, -1.35f, 0f), Vector3.zero, new Vector3(5.7f, 3.0f, 4.7f), darkCliffMat, true);
        CreatePart(house.transform, "FloorPlinth", PrimitiveType.Cube, new Vector3(0f, 0.25f, 0f), Vector3.zero, new Vector3(5.4f, 0.5f, 4.4f), darkCliffMat);
        CreatePart(house.transform, "Wall_North", PrimitiveType.Cube, new Vector3(0f, 1.85f, 1.95f), Vector3.zero, new Vector3(5.0f, 2.7f, 0.42f), mossyStoneMat);
        CreatePart(house.transform, "Wall_West", PrimitiveType.Cube, new Vector3(-2.32f, 1.85f, 0f), Vector3.zero, new Vector3(0.42f, 2.7f, 4.0f), mossyStoneMat);
        CreatePart(house.transform, "Wall_East", PrimitiveType.Cube, new Vector3(2.32f, 1.85f, 0f), Vector3.zero, new Vector3(0.42f, 2.7f, 4.0f), mossyStoneMat);
        CreatePart(house.transform, "Wall_SouthL", PrimitiveType.Cube, new Vector3(-1.55f, 1.85f, -1.95f), Vector3.zero, new Vector3(1.8f, 2.7f, 0.42f), darkCliffMat);
        CreatePart(house.transform, "Wall_SouthR", PrimitiveType.Cube, new Vector3(1.55f, 1.85f, -1.95f), Vector3.zero, new Vector3(1.8f, 2.7f, 0.42f), darkCliffMat);
        CreatePart(house.transform, "DoorLintel", PrimitiveType.Cube, new Vector3(0f, 2.95f, -1.95f), Vector3.zero, new Vector3(5.2f, 0.52f, 0.52f), mossyStoneMat);
        CreatePart(house.transform, "DoorGoldTrim", PrimitiveType.Cube, new Vector3(0f, 2.95f, -2.23f), Vector3.zero, new Vector3(2.4f, 0.18f, 0.08f), ancientGoldMat);
        CreatePart(house.transform, "Roof_Left", PrimitiveType.Cube, new Vector3(-1.45f, 3.85f, 0f), new Vector3(0f, 0f, 26f), new Vector3(3.35f, 0.3f, 4.65f), dockTimberMat);
        CreatePart(house.transform, "Roof_Right", PrimitiveType.Cube, new Vector3(1.45f, 3.85f, 0f), new Vector3(0f, 0f, -26f), new Vector3(3.35f, 0.3f, 4.65f), dockTimberMat);
        CreatePart(house.transform, "RoofRidgeBeam", PrimitiveType.Cube, new Vector3(0f, 4.55f, 0f), Vector3.zero, new Vector3(0.42f, 0.42f, 4.9f), ancientGoldMat);
        CreatePart(house.transform, "SconceOrb_L", PrimitiveType.Sphere, new Vector3(-1.35f, 2.0f, -2.24f), Vector3.zero, Vector3.one * 0.28f, arcaneGlowMat);
        CreatePart(house.transform, "SconceOrb_R", PrimitiveType.Sphere, new Vector3(1.35f, 2.0f, -2.24f), Vector3.zero, Vector3.one * 0.28f, torchGlowMat);
        ruinHousePrefab = SavePrefabClean(house, RuinHousePrefabPath);

        // 6. AncientStoneBridge (grounded walkable stone ruin bridge replacing PA_Bridge with -28m stilts)
        GameObject stBr = new GameObject("AncientStoneBridge");
        CreatePart(stBr.transform, "BridgeDeck", PrimitiveType.Cube, new Vector3(0f, 0.72f, 0f), Vector3.zero, new Vector3(3.4f, 0.34f, 8.6f), mossyStoneMat, true);
        CreatePart(stBr.transform, "ApproachRamp_S", PrimitiveType.Cube, new Vector3(0f, 0.16f, -5.6f), new Vector3(-20f, 0f, 0f), new Vector3(3.4f, 0.28f, 3.4f), darkCliffMat, true);
        CreatePart(stBr.transform, "ApproachRamp_N", PrimitiveType.Cube, new Vector3(0f, 0.16f, 5.6f), new Vector3(20f, 0f, 0f), new Vector3(3.4f, 0.28f, 3.4f), darkCliffMat, true);
        CreatePart(stBr.transform, "Parapet_L", PrimitiveType.Cube, new Vector3(-1.55f, 1.24f, 0f), Vector3.zero, new Vector3(0.28f, 0.72f, 8.6f), darkCliffMat, true);
        CreatePart(stBr.transform, "Parapet_R", PrimitiveType.Cube, new Vector3(1.55f, 1.24f, 0f), Vector3.zero, new Vector3(0.28f, 0.72f, 8.6f), darkCliffMat, true);
        for (int p = -1; p <= 1; p++)
        {
            float pz = p * 3.8f;
            CreatePart(stBr.transform, "StonePier_L_" + (p + 2), PrimitiveType.Cube, new Vector3(-1.55f, -0.75f, pz), Vector3.zero, new Vector3(0.46f, 3.4f, 0.46f), mossyStoneMat, true);
            CreatePart(stBr.transform, "StonePier_R_" + (p + 2), PrimitiveType.Cube, new Vector3(1.55f, -0.75f, pz), Vector3.zero, new Vector3(0.46f, 3.4f, 0.46f), mossyStoneMat, true);
        }
        stoneBridgePrefab = SavePrefabClean(stBr, BridgePrefabPath);
    }

    private static void BuildFinalOverviewInGameScene(
        Scene gameScene,
        GameObject archPrefab,
        GameObject pillarPrefab,
        GameObject chamberPrefab,
        GameObject waterfallPrefab,
        GameObject ruinHousePrefab,
        GameObject stoneBridgePrefab)
    {
        DestroyRootIfExists(gameScene, "FinalIslandOverview");

        GameObject discTerrainGo = FindSceneObjectByName(gameScene, "DiscoveryIsland_MainTerrain");
        Terrain discoveryTerrain = discTerrainGo != null ? discTerrainGo.GetComponent<Terrain>() : null;
        TerrainData finalData = GetOrCreateRightSizedFinalTerrain();
        float finalWidth = finalData != null ? finalData.size.x : 480f;
        float safeX = discoveryTerrain != null ? (discoveryTerrain.transform.position.x - finalWidth - 120f) : -740f;
        float safeZ = discoveryTerrain != null ? (discoveryTerrain.transform.position.z + 40f) : 100f;

        GameObject root = new GameObject("FinalIslandOverview");
        SceneManager.MoveGameObjectToScene(root, gameScene);
        root.transform.position = new Vector3(safeX, -9f, safeZ);

        GameObject terrainGo = new GameObject("FinalIsland_OverviewTerrain");
        terrainGo.transform.SetParent(root.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = finalData;
        terrain.materialTemplate = IslandPlacementUtility.GetOrCreateUrpTerrainMaterial();
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = finalData;

        IslandPlacementUtility.FindClearWaterShorelineDock(terrain, out Vector3 shoreEdge, out Vector3 inlandDir);
        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 rightDir = Vector3.Cross(Vector3.up, inlandDir).normalized;
        float dockYaw = dockRot.eulerAngles.y;

        Material timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientTimber.mat");
        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientGold.mat");
        Material ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardRope.mat");
        if (ropeMat == null) ropeMat = timberMat;
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalArcaneGlow.mat");
        Material buoyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DockBuoyRed.mat");

        IslandPlacementUtility.BuildRealisticHarborPier(
            root.transform,
            "FinalHarborPier_Overview",
            shoreEdge,
            inlandDir,
            rightDir,
            terrain,
            timberMat,
            goldMat,
            ropeMat,
            glowMat,
            BarrelPath,
            CratePath,
            true,
            out Vector3 pierOuterTip,
            out Vector3 pierDeckCenter,
            out Vector3 beachEntryPos
        );

        // BoatDockPoint_Final wired to BoatDocking + shared HealthCanvas HUD off pierhead
        BoatDocking discoveryDock = FindDiscoveryDock(gameScene);
        GameObject dockPointGo = new GameObject("BoatDockPoint_Final");
        dockPointGo.transform.SetParent(root.transform, false);
        dockPointGo.transform.position = pierOuterTip - inlandDir * 3.2f;
        dockPointGo.transform.rotation = dockRot;

        BoatDocking finalDocking = dockPointGo.AddComponent<BoatDocking>();
        finalDocking.islandIndex = 2;
        finalDocking.autoMatchCurrentIsland = false;
        finalDocking.islandDisplayName = "Final Treasure Island";
        finalDocking.targetIslandSceneName = "IslandScene";
        finalDocking.transitionToIslandScene = true;
        finalDocking.dockingDistance = 12f;
        finalDocking.boat = discoveryDock != null ? discoveryDock.boat : FindSceneObjectByName(gameScene, "OldWoodenRowboat_Complete");
        finalDocking.quizManager = discoveryDock != null ? discoveryDock.quizManager : Object.FindAnyObjectByType<QuizManager>();
        if (discoveryDock != null)
        {
            finalDocking.promptPanel = discoveryDock.promptPanel;
            finalDocking.promptText = discoveryDock.promptText;
            finalDocking.distanceGuideText = discoveryDock.distanceGuideText;
        }

        GameObject parkGo = new GameObject("DockParkingPoint");
        parkGo.transform.SetParent(dockPointGo.transform, false);
        parkGo.transform.localPosition = new Vector3(0f, -0.52f, -1.8f);
        parkGo.transform.localRotation = Quaternion.identity;
        finalDocking.dockParkingPoint = parkGo.transform;

        GameObject retSpawnGo = new GameObject("BoatReturnSpawnPoint");
        retSpawnGo.transform.SetParent(dockPointGo.transform, false);
        retSpawnGo.transform.localPosition = new Vector3(0f, -0.52f, -6.5f);
        retSpawnGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        finalDocking.boatReturnSpawnPoint = retSpawnGo.transform;

        // Floating Final Guide Buoy & dock guardian pillars framing beach plaza entry
        GameObject buoy = new GameObject("FinalGuideBuoy");
        buoy.transform.SetParent(dockPointGo.transform, false);
        buoy.transform.localPosition = new Vector3(-4.5f, -0.55f, -3.5f);
        buoy.AddComponent<GentleFloat>();
        CreatePart(buoy.transform, "Base", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.85f, 0.32f, 0.85f), buoyMat);
        CreatePart(buoy.transform, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f), goldMat);
        CreatePart(buoy.transform, "Beacon", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.zero, Vector3.one * 0.34f, glowMat);

        GameObject ovPilL = InstantiatePrefabAtWorld(pillarPrefab, root.transform, "Overview_DockPillar_L", beachEntryPos - rightDir * 3.4f, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(ovPilL, terrain, 0.9f, 0.9f, 0.04f);
        GameObject ovPilR = InstantiatePrefabAtWorld(pillarPrefab, root.transform, "Overview_DockPillar_R", beachEntryPos + rightDir * 3.4f, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(ovPilR, terrain, 0.9f, 0.9f, 0.04f);

        // Shared Final ruins & waterfall silhouette matching IslandScene (terrain pads already leveled)
        PopulateSharedFinalEnvironment(root.transform, terrain, beachEntryPos, inlandDir, rightDir, archPrefab, pillarPrefab, chamberPrefab, waterfallPrefab, ruinHousePrefab, stoneBridgePrefab, true, false);
        DisableDecorativePointShadows(gameScene);
    }

    private static void BuildDetailedFinalInIslandScene(
        Scene islandScene,
        GameObject archPrefab,
        GameObject pillarPrefab,
        GameObject chamberPrefab,
        GameObject waterfallPrefab,
        GameObject ruinHousePrefab,
        GameObject stoneBridgePrefab,
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
        atmo.ambientSkyColor = new Color(0.58f, 0.56f, 0.70f, 1f);

        TerrainData finalData = GetOrCreateRightSizedFinalTerrain();
        GameObject terrainGo = new GameObject("FinalIsland_Terrain");
        terrainGo.transform.SetParent(finalRoot.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = finalData;
        terrain.materialTemplate = IslandPlacementUtility.GetOrCreateUrpTerrainMaterial();
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = finalData;

        IslandPlacementUtility.FindClearWaterShorelineDock(terrain, out Vector3 shoreEdge, out Vector3 inlandDir);
        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 rightDir = Vector3.Cross(Vector3.up, inlandDir).normalized;
        float dockYaw = dockRot.eulerAngles.y;

        // Realistic 15m Ancient Temple Harbor Pier + Return Boat + Repair Workbench + Pillars
        GameObject dockArea = new GameObject("FinalDockArea");
        dockArea.transform.SetParent(finalRoot.transform, false);

        Material timberMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientTimber.mat");
        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalAncientGold.mat");
        Material ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardRope.mat");
        if (ropeMat == null) ropeMat = timberMat;
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalArcaneGlow.mat");
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        IslandPlacementUtility.BuildRealisticHarborPier(
            dockArea.transform,
            "FinalHarborPier",
            shoreEdge,
            inlandDir,
            rightDir,
            terrain,
            timberMat,
            goldMat,
            ropeMat,
            glowMat,
            BarrelPath,
            CratePath,
            true,
            out Vector3 pierOuterTip,
            out Vector3 pierDeckCenter,
            out Vector3 beachEntryPos
        );

        GameObject dPilL = InstantiatePrefabAtWorld(pillarPrefab, dockArea.transform, "DockPillar_L", beachEntryPos - rightDir * 3.4f, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(dPilL, terrain, 0.9f, 0.9f, 0.04f);
        GameObject dPilR = InstantiatePrefabAtWorld(pillarPrefab, dockArea.transform, "DockPillar_R", beachEntryPos + rightDir * 3.4f, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(dPilR, terrain, 0.9f, 0.9f, 0.04f);

        // Return Boat & Dock Repair Workbench
        Vector3 retPos = pierOuterTip - inlandDir * 3.0f;
        retPos.y = 0.08f;
        GameObject returnBoat = InstantiateAssetAtWorld(BoatPrefabPath, dockArea.transform, "FinalDockedBoat_Return", retPos, dockYaw + 180f);
        BoatMovement bm = returnBoat.GetComponent<BoatMovement>(); if (bm != null) Object.DestroyImmediate(bm);
        BoatSink bs = returnBoat.GetComponent<BoatSink>(); if (bs != null) Object.DestroyImmediate(bs);
        GentleFloat gf = returnBoat.AddComponent<GentleFloat>();
        gf.bobAmplitude = 0.05f;
        gf.tiltAngle = 2.0f;
        IslandBoatReturn boatRet = returnBoat.AddComponent<IslandBoatReturn>();
        boatRet.islandIndex = 2;
        boatRet.oceanSceneName = "GameScene";
        boatRet.interactRange = 8.5f;

        BuildDockRepairBench(dockArea.transform, pierDeckCenter + rightDir * 1.85f, dockRot, timberMat, goldMat, sparkleMat);

        GameObject spawnGo = new GameObject("FinalSpawnPoint");
        spawnGo.transform.SetParent(dockArea.transform, false);
        spawnGo.transform.position = pierDeckCenter + Vector3.up * 0.06f;
        spawnGo.transform.rotation = dockRot;

        // Shared + Detailed Ancient Ruin Environment (populated first so OverlapCapsule + footprints guarantee zero overlap!)
        Vector3 chamberPos = PopulateSharedFinalEnvironment(finalRoot.transform, terrain, beachEntryPos, inlandDir, rightDir, archPrefab, pillarPrefab, chamberPrefab, waterfallPrefab, ruinHousePrefab, stoneBridgePrefab, false, true);
        Physics.SyncTransforms();

        Vector3 orinSpot = IslandPlacementUtility.FindUnobstructedSpot(terrain, beachEntryPos.x - rightDir.x * 4.2f + inlandDir.x * 8.0f, beachEntryPos.z - rightDir.z * 4.2f + inlandDir.z * 8.0f, 8f, 1.0f);
        IslandPlacementUtility.BuildIslandGuideNpc(
            finalRoot.transform,
            terrain,
            orinSpot,
            dockYaw + 180f,
            2,
            "Scholar Orin",
            "You made it to Final Treasure Island! The Legendary Golden Crown awaits at the top of Pilgrim Avenue.",
            "Tip: Answer Hard challenges carefully—remember you can spend 10 Coins on a 50/50 Hint anytime!",
            25,
            new Color(0.26f, 0.24f, 0.56f, 1f),
            new Color(0.84f, 0.66f, 0.22f, 1f)
        );

        // 6 Well-Spaced, Accessible Final Treasure Chests + 8-Spot Respawn Pool
        GameObject chestsRoot = new GameObject("FinalChests");
        chestsRoot.transform.SetParent(finalRoot.transform, false);

        List<IslandPlacementUtility.PlacedFootprint> chestSpots = new List<IslandPlacementUtility.PlacedFootprint>();
        Vector2[] respawnOffsets = new Vector2[]
        {
            new Vector2(-20f, 16f),
            new Vector2(20f, 32f),
            new Vector2(-24f, 44f),
            new Vector2(-28f, 68f),
            new Vector2(26f, 62f),
            new Vector2(-10f, 34f),
            new Vector2(12f, 48f),
            new Vector2(-16f, 82f)
        };
        Vector3[] respawnPool = new Vector3[respawnOffsets.Length];
        for (int i = 0; i < respawnOffsets.Length; i++)
        {
            Vector3 rp = beachEntryPos + rightDir * respawnOffsets[i].x + inlandDir * respawnOffsets[i].y;
            respawnPool[i] = FindSpacedChestSpot(terrain, rp.x, rp.z, 20f, 8.5f, chestSpots);
        }

        Vector3 aveCenter = beachEntryPos + inlandDir * 48f;
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = respawnPool[i];
            Vector3 look = aveCenter - p;
            look.y = 0f;
            Quaternion cRot = look.sqrMagnitude > 0.1f ? Quaternion.LookRotation(look.normalized, Vector3.up) : dockRot;

            GameObject cObj = (GameObject)PrefabUtility.InstantiatePrefab(regularChestPrefab, chestsRoot.transform);
            cObj.name = "FinalChest_Regular_" + (i + 1);
            cObj.transform.position = p;
            cObj.transform.rotation = cRot;
            IslandPlacementUtility.GroundObjectExactToTerrain(cObj, terrain, 0.04f);
            TreasureChest tcScript = cObj.GetComponent<TreasureChest>();
            tcScript.chestId = "final_regular_" + (i + 1);
            tcScript.islandIndex = 2;
            tcScript.isSpecialChest = false;
            tcScript.respawnCandidateSpots = respawnPool;
            EditorUtility.SetDirty(tcScript);
        }

        // Special Chest squarely inside AncientTreasureChamber dais (final_special_1 -> Golden Crown + Victory!)
        GameObject specialObj = (GameObject)PrefabUtility.InstantiatePrefab(specialChestPrefab, chestsRoot.transform);
        specialObj.name = "FinalChest_Special";
        specialObj.transform.position = chamberPos + Vector3.up * 0.38f;
        specialObj.transform.rotation = Quaternion.Euler(0f, dockYaw + 180f, 0f);
        TreasureChest spTc = specialObj.GetComponent<TreasureChest>();
        spTc.chestId = "final_special_1";
        spTc.islandIndex = 2;
        spTc.isSpecialChest = true;
        spTc.respawnCandidateSpots = respawnPool;
        EditorUtility.SetDirty(spTc);

        // Wire FinalTreasureIsland & FinalSpawnPoint into IslandSceneController; keep DiscoveryIsland active by default
        IslandSceneController sceneCtrl = Object.FindAnyObjectByType<IslandSceneController>();
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
        Vector3 beachEntryPos,
        Vector3 inlandDir,
        Vector3 rightDir,
        GameObject archPrefab,
        GameObject pillarPrefab,
        GameObject chamberPrefab,
        GameObject waterfallPrefab,
        GameObject ruinHousePrefab,
        GameObject stoneBridgePrefab,
        bool overviewOnly,
        bool flattenPads)
    {
        GameObject env = new GameObject(overviewOnly ? "FinalOverviewEnv" : "FinalEnvironment");
        env.transform.SetParent(parent, false);

        float dockYaw = Quaternion.LookRotation(inlandDir, Vector3.up).eulerAngles.y;
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalMossyStone.mat");

        List<IslandPlacementUtility.PlacedFootprint> placed = new List<IslandPlacementUtility.PlacedFootprint>();

        // 1. Reserve Harbor Plaza & Flagstone Pilgrim Avenue from Beach to Sanctuary (92m inland)
        placed.Add(new IslandPlacementUtility.PlacedFootprint(beachEntryPos, 11.5f));
        Vector3[] aveNodes = new Vector3[14];
        for (int i = 0; i < aveNodes.Length; i++)
        {
            float dist = 6f + i * 6.2f;
            float sway = Mathf.Sin(i * 0.4f) * 2.2f;
            Vector3 at = beachEntryPos + rightDir * sway + inlandDir * dist;
            aveNodes[i] = IslandPlacementUtility.FindFlatWalkablePoint(terrain, at.x, at.z, 8f, 22f, 0.75f);
            placed.Add(new IslandPlacementUtility.PlacedFootprint(aveNodes[i], 5.0f));
        }

        // 2. AncientTreasureChamber Sanctuary at Avenue Summit (leveled pad + deep cliff skirt)
        Vector3 chTarget = beachEntryPos + inlandDir * 92f;
        Vector3 chamberPos;
        if (IslandPlacementUtility.FindFlatBuildingPad(terrain, chTarget.x, chTarget.z, 26f, 5.4f, 5.4f, 1.8f, 1.2f, placed, out Vector3 foundCh, out float chPadY))
        {
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, foundCh, 5.4f, 5.4f, dockYaw, 4.5f, chPadY);
            chamberPos = foundCh;
        }
        else
        {
            chamberPos = IslandPlacementUtility.FindFlatWalkablePoint(terrain, chTarget.x, chTarget.z, 24f, 22f, 1.0f);
            placed.Add(new IslandPlacementUtility.PlacedFootprint(chamberPos, 8.5f));
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, chamberPos, 5.4f, 5.4f, dockYaw, 4.5f, chamberPos.y);
        }
        GameObject chamberGo = InstantiatePrefabAtWorld(chamberPrefab, env.transform, "AncientTreasureChamber_Sanctuary", chamberPos, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(chamberGo, terrain, 5.0f, 5.0f, 0.04f);
        IslandPlacementUtility.EnsureExactSolidColliders(chamberGo, false, true);
        chamberPos = chamberGo.transform.position;

        // 3. Temple Entry Archways straddling Pilgrim Avenue + Western Valley Waterfall
        Vector3 a1t = beachEntryPos + inlandDir * 22f;
        Vector3 arch1Pos = IslandPlacementUtility.FindFlatWalkablePoint(terrain, a1t.x, a1t.z, 10f, 20f, 0.9f);
        if (flattenPads)
            IslandPlacementUtility.FlattenTerrainPad(terrain, arch1Pos, 3.8f, 1.4f, dockYaw, 3.5f, arch1Pos.y);
        GameObject arch1 = InstantiatePrefabAtWorld(archPrefab, env.transform, "TempleArch_Lower", arch1Pos, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(arch1, terrain, 3.5f, 1.2f, 0.04f);

        Vector3 a2t = beachEntryPos + inlandDir * 60f;
        Vector3 arch2Pos = IslandPlacementUtility.FindFlatWalkablePoint(terrain, a2t.x, a2t.z, 10f, 20f, 0.9f);
        if (flattenPads)
            IslandPlacementUtility.FlattenTerrainPad(terrain, arch2Pos, 3.8f, 1.4f, dockYaw, 3.5f, arch2Pos.y);
        GameObject arch2 = InstantiatePrefabAtWorld(archPrefab, env.transform, "TempleArch_Upper", arch2Pos, dockYaw);
        IslandPlacementUtility.GroundBuildingWithFoundation(arch2, terrain, 3.5f, 1.2f, 0.04f);

        Vector3 wft = beachEntryPos - rightDir * 32f + inlandDir * 56f;
        if (IslandPlacementUtility.FindFlatBuildingPad(terrain, wft.x, wft.z, 22f, 4.4f, 3.8f, 2.0f, 1.2f, placed, out Vector3 waterfallPos, out float wfPadY))
        {
            float wfYaw = dockYaw + 28f;
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, waterfallPos, 4.4f, 3.8f, wfYaw, 4.0f, wfPadY);
            GameObject wfGo = InstantiatePrefabAtWorld(waterfallPrefab, env.transform, "AncientWaterfall_Valley", waterfallPos, wfYaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(wfGo, terrain, 4.0f, 3.2f, 0.06f);
        }

        // 4. 8 Ancient Temple Dwellings on verified flat pads facing Pilgrim Avenue
        Vector2[] villageOffsets = new Vector2[]
        {
            new Vector2(-15f, 24f),
            new Vector2(15f, 24f),
            new Vector2(-16f, 44f),
            new Vector2(16f, 44f),
            new Vector2(-16f, 64f),
            new Vector2(16f, 64f),
            new Vector2(-25f, 34f),
            new Vector2(25f, 76f)
        };
        int houseLimit = overviewOnly ? 5 : villageOffsets.Length;
        for (int i = 0; i < houseLimit; i++)
        {
            Vector3 ht = beachEntryPos + rightDir * villageOffsets[i].x + inlandDir * villageOffsets[i].y;
            if (!IslandPlacementUtility.FindFlatBuildingPad(terrain, ht.x, ht.z, 20f, 3.3f, 2.8f, 1.6f, 1.1f, placed, out Vector3 hp, out float hPadY))
                continue;

            float distAlongAve = Vector3.Dot(hp - beachEntryPos, inlandDir);
            Vector3 aveRef = beachEntryPos + inlandDir * Mathf.Clamp(distAlongAve, 14f, 84f);
            Vector3 faceDir = aveRef - hp;
            faceDir.y = 0f;
            float yaw = faceDir.sqrMagnitude > 0.1f ? Quaternion.LookRotation(faceDir.normalized, Vector3.up).eulerAngles.y + 180f : dockYaw + 180f;

            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, hp, 3.3f, 2.8f, yaw, 3.8f, hPadY);

            GameObject house = InstantiatePrefabAtWorld(ruinHousePrefab, env.transform, "AncientRuinHouse_" + (i + 1), hp, yaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(house, terrain, 2.8f, 2.3f, 0.04f);
            IslandPlacementUtility.EnsureExactSolidColliders(house, false, false);
        }

        // 5. 2 Grounded AncientStoneBridge Spans (1 overview, 2 detailed)
        Vector3 b1t = beachEntryPos - rightDir * 16f + inlandDir * 32f;
        if (IslandPlacementUtility.FindFlatBuildingPad(terrain, b1t.x, b1t.z, 16f, 2.2f, 6.2f, 1.2f, 1.0f, placed, out Vector3 bridgePos, out float b1PadY))
        {
            float b1Yaw = dockYaw - 15f;
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, bridgePos, 2.2f, 6.2f, b1Yaw, 3.6f, b1PadY);
            GameObject bridge = InstantiatePrefabAtWorld(stoneBridgePrefab, env.transform, "AncientRuinBridge", bridgePos, b1Yaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(bridge, terrain, 1.8f, 5.8f, 0.04f);
            IslandPlacementUtility.EnsureExactSolidColliders(bridge, false, true);
        }

        if (!overviewOnly)
        {
            Vector3 b2t = beachEntryPos + rightDir * 16f + inlandDir * 54f;
            if (IslandPlacementUtility.FindFlatBuildingPad(terrain, b2t.x, b2t.z, 16f, 2.2f, 6.2f, 1.2f, 1.0f, placed, out Vector3 northBridgePos, out float b2PadY))
            {
                float b2Yaw = dockYaw + 18f;
                if (flattenPads)
                    IslandPlacementUtility.FlattenTerrainPad(terrain, northBridgePos, 2.2f, 6.2f, b2Yaw, 3.6f, b2PadY);
                GameObject northBridge = InstantiatePrefabAtWorld(stoneBridgePrefab, env.transform, "AncientRuinBridge_North", northBridgePos, b2Yaw);
                IslandPlacementUtility.GroundBuildingWithFoundation(northBridge, terrain, 1.8f, 5.8f, 0.04f);
                IslandPlacementUtility.EnsureExactSolidColliders(northBridge, false, true);
            }
        }

        Physics.SyncTransforms();

        // 6. Outer Scenic Mountains Strictly Outside Temple Ruins & Corridors
        int mountainLimit = overviewOnly ? 4 : 6;
        Vector3 hubCenter = beachEntryPos + inlandDir * 55f;
        for (int i = 0; i < mountainLimit; i++)
        {
            float angle = (-55f + i * (290f / mountainLimit)) * Mathf.Deg2Rad;
            float radius = 108f + (i % 2) * 28f;
            Vector3 mt = hubCenter + rightDir * (Mathf.Cos(angle) * radius) + inlandDir * (Mathf.Sin(angle) * radius);
            if (IslandPlacementUtility.ReserveFlatSpot(terrain, mt.x, mt.z, 18f, 26.0f, 35f, 0.8f, placed, out Vector3 mp))
            {
                GameObject mtn = InstantiateAssetAtWorld(MountainPaths[i % MountainPaths.Length], env.transform, "AncientMountain_" + (i + 1), mp, i * 59f);
                IslandPlacementUtility.GroundObjectExactToTerrain(mtn, terrain, 1.2f);
                IslandPlacementUtility.EnsureExactSolidColliders(mtn, false, false);
            }
        }

        Physics.SyncTransforms();

        // 7. Dramatic Ancient Boulders (strictly outside houses & mountains)
        int rockLimit = overviewOnly ? 18 : 34;
        Vector3 origin = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        for (int i = 0; i < rockLimit; i++)
        {
            float angle = i * 31f * Mathf.Deg2Rad;
            float radius = 34f + (i % 5) * 16f;
            Vector3 rt = (i < 22)
                ? beachEntryPos + rightDir * (Mathf.Cos(angle) * radius) + inlandDir * (46f + Mathf.Sin(angle) * radius)
                : new Vector3(origin.x + (0.14f + ((i * 19) % 10) * 0.078f) * tSize.x, 0f, origin.z + (0.18f + ((i * 23) % 10) * 0.072f) * tSize.z);

            if (IslandPlacementUtility.ReserveFlatSpot(terrain, rt.x, rt.z, 16f, 4.4f, 26f, 0.65f, placed, out Vector3 p)
                && IslandPlacementUtility.IsSpotClearOfObstacles(p, 2.0f))
            {
                GameObject rock = InstantiateAssetAtWorld(RockPaths[i % RockPaths.Length], env.transform, "AncientCliffRock_" + (i + 1), p, i * 57f);
                IslandPlacementUtility.GroundObjectExactToTerrain(rock, terrain, 0.25f);
                IslandPlacementUtility.EnsureExactSolidColliders(rock, false, false);
            }
        }

        Physics.SyncTransforms();

        // 8. Ancient Jungle Trees (strictly clear of buildings & mountains!)
        int treeLimit = overviewOnly ? 45 : 80;
        for (int i = 0; i < treeLimit; i++)
        {
            float tx;
            float tz;
            if (i < 42)
            {
                float angle = i * 24f * Mathf.Deg2Rad;
                float radius = 28f + (i % 6) * 14f;
                Vector3 ring = beachEntryPos + rightDir * (Mathf.Cos(angle) * radius) + inlandDir * (48f + Mathf.Sin(angle) * radius);
                tx = ring.x;
                tz = ring.z;
            }
            else
            {
                int idx = i - 42;
                float nx = 0.14f + ((idx * 17) % 10) * 0.076f;
                float nz = 0.16f + ((idx * 13) % 10) * 0.074f;
                tx = origin.x + nx * tSize.x;
                tz = origin.z + nz * tSize.z;
            }

            if (IslandPlacementUtility.ReserveFlatSpot(terrain, tx, tz, 14f, 3.4f, 24f, 0.85f, placed, out Vector3 p)
                && IslandPlacementUtility.IsSpotClearOfObstacles(p, 1.6f))
            {
                GameObject tree = InstantiateAssetAtWorld(TreePaths[i % TreePaths.Length], env.transform, "AncientTree_" + (i + 1), p, i * 41f);
                IslandPlacementUtility.GroundObjectExactToTerrain(tree, terrain, 0.18f, true);
                IslandPlacementUtility.EnsureExactSolidColliders(tree, true, false);
                if (tree != null && tree.GetComponent<WindSway>() == null)
                {
                    WindSway ws = tree.AddComponent<WindSway>();
                    ws.swayAngle = 2.2f;
                }
            }
        }

        if (overviewOnly)
            return chamberPos;

        Physics.SyncTransforms();

        // 9. Paired Temple Pillar Colonnade (10 flanking Pilgrim Avenue) + 4 Overlook Pillars
        Vector2[] pillarOffsets = new Vector2[]
        {
            new Vector2(-4.8f, 16f), new Vector2(4.8f, 16f),
            new Vector2(-4.8f, 30f), new Vector2(4.8f, 30f),
            new Vector2(-4.8f, 42f), new Vector2(4.8f, 42f),
            new Vector2(-4.8f, 54f), new Vector2(4.8f, 54f),
            new Vector2(-4.8f, 70f), new Vector2(4.8f, 70f),
            new Vector2(-24f, 48f), new Vector2(22f, 62f),
            new Vector2(-18f, 78f), new Vector2(18f, 78f)
        };
        for (int i = 0; i < pillarOffsets.Length; i++)
        {
            Vector3 pt = beachEntryPos + rightDir * pillarOffsets[i].x + inlandDir * pillarOffsets[i].y;
            Vector3 pp = IslandPlacementUtility.FindFlatWalkablePoint(terrain, pt.x, pt.z, 8f, 22f, 0.8f);
            if (!IslandPlacementUtility.IsSpotClearOfObstacles(pp, 1.2f))
                pp = IslandPlacementUtility.FindUnobstructedSpot(terrain, pp.x, pp.z, 8f, 1.2f);
            GameObject pil = InstantiatePrefabAtWorld(pillarPrefab, env.transform, "TemplePillar_" + (i + 1), pp, dockYaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(pil, terrain, 0.9f, 0.9f, 0.04f);
        }

        // Inscribed ancient ruin monoliths along overlook spots
        Vector3 twt = beachEntryPos - rightDir * 21f + inlandDir * 44f;
        Vector3 tabletWest = IslandPlacementUtility.FindUnobstructedSpot(terrain, twt.x, twt.z, 10f, 1.5f);
        GameObject tw = InstantiateAssetAtWorld(OldTextPrefabPath, env.transform, "AncientMonolith_West", tabletWest, dockYaw + 45f);
        IslandPlacementUtility.GroundObjectExactToTerrain(tw, terrain, 0.14f);
        IslandPlacementUtility.EnsureExactSolidColliders(tw, false, false);

        Vector3 tet = beachEntryPos + rightDir * 24f + inlandDir * 58f;
        Vector3 tabletEast = IslandPlacementUtility.FindUnobstructedSpot(terrain, tet.x, tet.z, 10f, 1.5f);
        GameObject te = InstantiateAssetAtWorld(OldTextPrefabPath, env.transform, "AncientMonolith_East", tabletEast, dockYaw - 40f);
        IslandPlacementUtility.GroundObjectExactToTerrain(te, terrain, 0.14f);
        IslandPlacementUtility.EnsureExactSolidColliders(te, false, false);

        // Flagstone Pilgrim Avenue + Dense Bushes/Grass on leveled terrain
        for (int i = 0; i < aveNodes.Length; i++)
        {
            Vector3 sp = aveNodes[i];
            if (!IslandPlacementUtility.IsSpotClearOfObstacles(sp, 1.0f))
                sp = IslandPlacementUtility.FindUnobstructedSpot(terrain, sp.x, sp.z, 6f, 1.0f);
            sp.y = terrain.SampleHeight(sp) + terrain.transform.position.y;
            CreatePart(env.transform, "AncientPathStone_" + (i + 1), PrimitiveType.Cylinder, sp + Vector3.up * 0.01f, new Vector3(0f, i * 33f, 0f), new Vector3(1.8f, 0.04f, 1.8f), stoneMat);

            if (i % 2 == 1)
            {
                Vector3 bushPos = IslandPlacementUtility.FindUnobstructedSpot(terrain, sp.x + rightDir.x * 3.4f, sp.z + rightDir.z * 3.4f, 6f, 0.8f);
                GameObject bush = InstantiateAssetAtWorld(BushPath, env.transform, "JungleBush_" + (i + 1), bushPos, i * 45f);
                IslandPlacementUtility.GroundObjectExactToTerrain(bush, terrain, 0.08f, true);
                if (bush != null && bush.GetComponent<WindSway>() == null) bush.AddComponent<WindSway>();
            }
            else
            {
                Vector3 grassPos = IslandPlacementUtility.FindUnobstructedSpot(terrain, sp.x - rightDir.x * 3.2f, sp.z - rightDir.z * 3.2f, 6f, 0.8f);
                GameObject grass = InstantiateAssetAtWorld(GrassPath, env.transform, "JungleGrass_" + (i + 1), grassPos, i * 35f);
                IslandPlacementUtility.GroundObjectExactToTerrain(grass, terrain, 0.06f, true);
                if (grass != null && grass.GetComponent<WindSway>() == null) grass.AddComponent<WindSway>();
            }
        }

        // Safe flickering stone fire braziers flanking the Pilgrim Avenue (zero bloom blowout)
        Vector3 lfTarget = beachEntryPos + rightDir * 6.5f + inlandDir * 24f;
        Vector3 lowerFirePos = IslandPlacementUtility.FindUnobstructedSpot(terrain, lfTarget.x, lfTarget.z, 10f, 1.1f);
        InstantiateFireFxNoShadows(env.transform, "PathFireFX_Lower", lowerFirePos, terrain);
        Vector3 ufTarget = beachEntryPos - rightDir * 6.5f + inlandDir * 58f;
        Vector3 upperFirePos = IslandPlacementUtility.FindUnobstructedSpot(terrain, ufTarget.x, ufTarget.z, 10f, 1.1f);
        InstantiateFireFxNoShadows(env.transform, "PathFireFX_Upper", upperFirePos, terrain);

        return chamberPos;
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

    private static void InstantiateFireFxNoShadows(Transform parent, string name, Vector3 pos, Terrain terrain)
    {
        GameObject brazier = new GameObject(name);
        brazier.transform.SetParent(parent, false);
        brazier.transform.position = pos;

        BoxCollider col = brazier.AddComponent<BoxCollider>();
        col.center = new Vector3(0f, 0.65f, 0f);
        col.size = new Vector3(1.1f, 1.3f, 1.1f);

        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/FinalMossyStone.mat");
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatLanternGlow.mat");
        CreatePart(brazier.transform, "PedestalSkirt", PrimitiveType.Cylinder, new Vector3(0f, -0.55f, 0f), Vector3.zero, new Vector3(1.05f, 0.65f, 1.05f), stoneMat);
        CreatePart(brazier.transform, "Pedestal", PrimitiveType.Cylinder, new Vector3(0f, 0.45f, 0f), Vector3.zero, new Vector3(0.95f, 0.45f, 0.95f), stoneMat);
        CreatePart(brazier.transform, "EmberCore", PrimitiveType.Sphere, new Vector3(0f, 0.98f, 0f), Vector3.zero, new Vector3(0.45f, 0.35f, 0.45f), glowMat);

        GameObject campAsset = AssetDatabase.LoadAssetAtPath<GameObject>(CampfirePath);
        if (campAsset != null)
        {
            GameObject camp = (GameObject)PrefabUtility.InstantiatePrefab(campAsset, brazier.transform);
            camp.transform.localPosition = new Vector3(0f, 0.9f, 0f);
            camp.transform.localScale = Vector3.one * 0.8f;
        }
        GameObject emberAsset = AssetDatabase.LoadAssetAtPath<GameObject>(EmbersPath);
        if (emberAsset != null)
        {
            GameObject em = (GameObject)PrefabUtility.InstantiatePrefab(emberAsset, brazier.transform);
            em.transform.localPosition = new Vector3(0f, 1.12f, 0f);
        }

        AddFlickeringPointLight(brazier.transform, name + "_Light", new Vector3(0f, 1.35f, 0f), new Color(1f, 0.65f, 0.25f), 1.8f, 14f, true);
        if (terrain != null)
            IslandPlacementUtility.GroundBuildingWithFoundation(brazier, terrain, 0.7f, 0.7f, 0.06f);
    }

    private static void BuildDockRepairBench(Transform parent, Vector3 worldPos, Quaternion dockRot, Material woodMat, Material goldMat, Material sparkleMat)
    {
        GameObject bench = new GameObject("FinalDockRepairWorkbench");
        bench.transform.SetParent(parent, false);
        bench.transform.position = worldPos;
        bench.transform.rotation = dockRot * Quaternion.Euler(0f, -90f, 0f);

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
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
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

    private static TerrainData GetOrCreateRightSizedFinalTerrain()
    {
        TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(FinalTerrainPath);
        if (existing == null)
        {
            AssetDatabase.CopyAsset(SourceFinalTerrainPath, FinalTerrainPath);
            AssetDatabase.Refresh();
            existing = AssetDatabase.LoadAssetAtPath<TerrainData>(FinalTerrainPath);
        }

        if (existing != null)
        {
            existing.size = new Vector3(480f, 76f, 480f);
            EditorUtility.SetDirty(existing);
            return existing;
        }

        return AssetDatabase.LoadAssetAtPath<TerrainData>(SourceFinalTerrainPath);
    }

    private static BoatDocking FindDiscoveryDock(Scene gameScene)
    {
        BoatDocking[] docks = Object.FindObjectsByType<BoatDocking>();
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

    private static bool HasNonTriggerCollider(GameObject root)
    {
        if (root == null) return false;
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && !cols[i].isTrigger)
                return true;
        }
        return false;
    }

    private static void EnsureSolidColliders(GameObject root, bool useMeshColliderPerRenderer)
    {
        if (HasNonTriggerCollider(root))
            return;

        if (useMeshColliderPerRenderer)
        {
            MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
            for (int i = 0; i < filters.Length; i++)
            {
                if (filters[i].sharedMesh != null && filters[i].sharedMesh.vertexCount <= 8000 && filters[i].GetComponent<Collider>() == null)
                    filters[i].gameObject.AddComponent<MeshCollider>();
            }
            if (HasNonTriggerCollider(root))
                return;
        }

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
            return;

        Bounds bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++)
            bounds.Encapsulate(renderers[i].bounds);

        Vector3 ls = root.transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(ls.x));
        float sy = Mathf.Max(0.01f, Mathf.Abs(ls.y));
        float sz = Mathf.Max(0.01f, Mathf.Abs(ls.z));

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(bounds.size.x / sx, bounds.size.y / sy, bounds.size.z / sz);
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
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
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

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
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

        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Particles/Standard Unlit");
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
