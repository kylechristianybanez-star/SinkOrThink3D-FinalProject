using System.Collections.Generic;
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
    private const string SourceVikingTerrainPath = "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 2.asset";
    private const string VikingTerrainPath = "Assets/Terrain/VikingIslandTerrain.asset";

    private const string PlankTexPath = "Assets/Textures/Planks023A_1K-JPG_Color.jpg";
    private const string CliffTexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Cliff_1/Cliff_1_Diffuse.tif";
    private const string MossRockTexPath = "Assets/Free Island Collection/Environment/Terrain/Textures/Mossy_Rock/Mossy_Rock_Diffuse.tif";

    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string CampfirePath = "Assets/ToonTropical/Prefabs/SM_Island_Campfire.prefab";
    private const string EmbersPath = "Assets/ToonTropical/Prefabs/FX/FX_Embers.prefab";
    private const string BarrelPath = "Assets/ToonTropical/Prefabs/SM_Island_Barrel.prefab";
    private const string CratePath = "Assets/ToonTropical/Prefabs/SM_Island_Crate.prefab";

    private const string LonghousePrefabPath = "Assets/Prefabs/VikingLonghouse.prefab";
    private const string WatchtowerPrefabPath = "Assets/Prefabs/VikingWatchtower.prefab";
    private const string BannerPrefabPath = "Assets/Prefabs/VikingBannerFlag.prefab";
    private const string TorchPrefabPath = "Assets/Prefabs/VikingTorchPost.prefab";
    private const string FencePrefabPath = "Assets/Prefabs/VikingFenceSegment.prefab";
    private const string BridgePrefabPath = "Assets/Prefabs/VikingTimberBridge.prefab";

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
        "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet3_Pile1.prefab"
    };

    private static readonly string[] MountainPaths = new string[]
    {
        "Assets/POLYART_Ancient Village/Prefabs/PA_Mountainset1_hill1.prefab",
        "Assets/POLYART_Ancient Village/Prefabs/PA_Mountainset1_hill3.prefab"
    };

    [MenuItem("Tools/SinkOrThink3D/Phase 10 - Build Viking Island (GameScene + IslandScene)")]
    public static void BuildVikingIsland()
    {
        RunBuild(true);
    }

    public static void RunBuild(bool showDialog)
    {
        if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder("Assets/Prefabs");
        EnsureFolder("Assets/Materials");
        EnsureFolder("Assets/Terrain");

        // Reset VikingIslandTerrain from clean source once before flattening building pads
        IslandPlacementUtility.ResetTerrainFromSource(SourceVikingTerrainPath, VikingTerrainPath, new Vector3(480f, 68f, 480f));

        // Build/refresh reusable chest & textured Viking prefabs
        Phase6DiscoveryIslandSetup.BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);
        BuildVikingPrefabs(
            out GameObject longhousePrefab,
            out GameObject watchtowerPrefab,
            out GameObject bannerPrefab,
            out GameObject torchPrefab,
            out GameObject fencePrefab,
            out GameObject bridgePrefab
        );

        // Part A: Build Detailed VikingIsland FIRST in IslandScene.unity (flattens building pads into shared terrain)
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        BuildDetailedVikingInIslandScene(
            islandScene,
            longhousePrefab,
            watchtowerPrefab,
            bannerPrefab,
            torchPrefab,
            fencePrefab,
            bridgePrefab,
            regularChestPrefab,
            specialChestPrefab
        );
        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(islandScene);
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);
        AssetDatabase.SaveAssets();

        // Part B: Build physical Viking Island Overview + Dock in GameScene.unity using leveled terrain
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        BuildVikingOverviewInGameScene(gameScene, longhousePrefab, watchtowerPrefab, bannerPrefab, torchPrefab, fencePrefab, bridgePrefab);
        IslandPlacementUtility.SanitizeGlobalLightingAndMaterials(gameScene);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        AssetDatabase.SaveAssets();

        Debug.Log("Phase 10 Complete! Built Viking Island overview in GameScene.unity and detailed VikingIsland in IslandScene.unity.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase 10 Viking Island Complete",
                "Built reusable textured Viking prefabs (with chimney smoke, waving flags, flickering torches, and solid colliders) and updated BOTH scenes:\n" +
                "- GameScene.unity: Added VikingIslandOverview + locked/unlockable BoatDockPoint_Viking (Terrain 2)\n" +
                "- IslandScene.unity: Built detailed VikingIsland (inactive by default; auto-activated on dock), 6 Viking Chests (Medium/Hard quizzes), Viking Dock Repair Bench & Return Boat.",
                "OK"
            );
        }
    }

    private static void BuildVikingPrefabs(
        out GameObject longhousePrefab,
        out GameObject watchtowerPrefab,
        out GameObject bannerPrefab,
        out GameObject torchPrefab,
        out GameObject fencePrefab,
        out GameObject bridgePrefab)
    {
        Texture2D plankTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlankTexPath);
        Texture2D cliffTex = AssetDatabase.LoadAssetAtPath<Texture2D>(CliffTexPath);
        Texture2D mossTex = AssetDatabase.LoadAssetAtPath<Texture2D>(MossRockTexPath);

        Material darkTimberMat = GetOrCreateLitMaterial("VikingDarkTimber.mat", plankTex, new Color(0.82f, 0.66f, 0.48f, 1f), 0f, 0.16f);
        Material roofTimberMat = GetOrCreateLitMaterial("VikingRoofShingle.mat", plankTex, new Color(0.64f, 0.50f, 0.38f, 1f), 0f, 0.12f);
        Material stoneMat = GetOrCreateLitMaterial("VikingStoneWall.mat", cliffTex, new Color(0.90f, 0.92f, 0.95f, 1f), 0f, 0.12f);
        Material runeStoneMat = GetOrCreateLitMaterial("VikingRunestone.mat", mossTex != null ? mossTex : cliffTex, new Color(0.82f, 0.88f, 0.92f, 1f), 0.05f, 0.2f);
        Material bannerRedMat = GetOrCreateLitMaterial("VikingBannerCrimson.mat", null, new Color(0.64f, 0.14f, 0.12f, 1f), 0f, 0.15f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", null, new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material glowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material smokeMat = GetOrCreateParticleMaterial("VikingSmokeParticle.mat", new Color(0.78f, 0.8f, 0.84f, 0.45f));

        // 1. VikingLonghouse (with deep stone foundation skirt so hillside entries stay level & never float)
        GameObject lh = new GameObject("VikingLonghouse");
        BoxCollider lhCol = lh.AddComponent<BoxCollider>();
        lhCol.center = new Vector3(0f, 2.2f, 0f);
        lhCol.size = new Vector3(7.2f, 4.4f, 11.4f);

        CreatePart(lh.transform, "FoundationSkirt", PrimitiveType.Cube, new Vector3(0f, -1.35f, 0f), Vector3.zero, new Vector3(7.4f, 3.0f, 11.6f), stoneMat, true);
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
        smokePs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
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

        // 2. VikingWatchtower (with deep stone foundation skirt)
        GameObject wt = new GameObject("VikingWatchtower");
        BoxCollider wtCol = wt.AddComponent<BoxCollider>();
        wtCol.center = new Vector3(0f, 3.6f, 0f);
        wtCol.size = new Vector3(4.0f, 7.2f, 4.0f);
        CreatePart(wt.transform, "TowerFoundationSkirt", PrimitiveType.Cube, new Vector3(0f, -1.35f, 0f), Vector3.zero, new Vector3(4.2f, 3.0f, 4.2f), stoneMat, true);
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

        // 3. VikingBannerFlag (animated cloth + solid post collider)
        GameObject bf = new GameObject("VikingBannerFlag");
        BoxCollider bCol = bf.AddComponent<BoxCollider>();
        bCol.center = new Vector3(0f, 1.4f, 0f);
        bCol.size = new Vector3(0.42f, 2.8f, 0.42f);
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

        // 4. VikingTorchPost (safe warm glow + embers + flickering light, zero white bloom blowout)
        GameObject tp = new GameObject("VikingTorchPost");
        BoxCollider tpCol = tp.AddComponent<BoxCollider>();
        tpCol.center = new Vector3(0f, 1.35f, 0f);
        tpCol.size = new Vector3(0.6f, 2.7f, 0.6f);
        CreatePart(tp.transform, "Post", PrimitiveType.Cylinder, new Vector3(0f, 1.25f, 0f), Vector3.zero, new Vector3(0.18f, 1.25f, 0.18f), darkTimberMat);
        CreatePart(tp.transform, "BrazierBowl", PrimitiveType.Cylinder, new Vector3(0f, 2.55f, 0f), Vector3.zero, new Vector3(0.45f, 0.16f, 0.45f), stoneMat);
        CreatePart(tp.transform, "FlameEmberCore", PrimitiveType.Sphere, new Vector3(0f, 2.75f, 0f), Vector3.zero, new Vector3(0.32f, 0.42f, 0.32f), glowMat);
        GameObject embersPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(EmbersPath);
        if (embersPrefab != null)
        {
            GameObject emInst = (GameObject)PrefabUtility.InstantiatePrefab(embersPrefab, tp.transform);
            emInst.transform.localPosition = new Vector3(0f, 2.78f, 0f);
            emInst.transform.localScale = Vector3.one * 0.45f;
        }
        GameObject lightGo = new GameObject("TorchLight");
        lightGo.transform.SetParent(tp.transform, false);
        lightGo.transform.localPosition = new Vector3(0f, 2.95f, 0f);
        Light l = lightGo.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = new Color(1f, 0.62f, 0.22f);
        l.intensity = 1.8f;
        l.range = 14f;
        l.shadows = LightShadows.None;
        FlickeringLight tpFlicker = lightGo.AddComponent<FlickeringLight>();
        tpFlicker.baseIntensity = 1.8f;
        torchPrefab = SavePrefabClean(tp, TorchPrefabPath);

        // 5. VikingFenceSegment
        GameObject fnc = new GameObject("VikingFenceSegment");
        BoxCollider fCol = fnc.AddComponent<BoxCollider>();
        fCol.center = new Vector3(0f, 0.85f, 0f);
        fCol.size = new Vector3(3.8f, 1.7f, 0.45f);
        for (int i = -1; i <= 1; i++)
            CreatePart(fnc.transform, "Stake_" + (i + 2), PrimitiveType.Cylinder, new Vector3(i * 1.3f, 0.45f, 0f), Vector3.zero, new Vector3(0.22f, 1.2f, 0.22f), darkTimberMat);
        CreatePart(fnc.transform, "Rail_Top", PrimitiveType.Cube, new Vector3(0f, 1.15f, 0f), Vector3.zero, new Vector3(3.8f, 0.16f, 0.16f), roofTimberMat);
        CreatePart(fnc.transform, "Rail_Bot", PrimitiveType.Cube, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(3.8f, 0.16f, 0.16f), roofTimberMat);
        fencePrefab = SavePrefabClean(fnc, FencePrefabPath);

        // 6. VikingTimberBridge (grounded walkable bridge replacing PA_Bridge with -28m stilts)
        GameObject br = new GameObject("VikingTimberBridge");
        CreatePart(br.transform, "BridgeDeck", PrimitiveType.Cube, new Vector3(0f, 0.68f, 0f), Vector3.zero, new Vector3(3.2f, 0.28f, 8.5f), darkTimberMat, true);
        CreatePart(br.transform, "ApproachRamp_S", PrimitiveType.Cube, new Vector3(0f, 0.14f, -5.55f), new Vector3(-20f, 0f, 0f), new Vector3(3.2f, 0.24f, 3.3f), roofTimberMat, true);
        CreatePart(br.transform, "ApproachRamp_N", PrimitiveType.Cube, new Vector3(0f, 0.14f, 5.55f), new Vector3(20f, 0f, 0f), new Vector3(3.2f, 0.24f, 3.3f), roofTimberMat, true);
        CreatePart(br.transform, "SideRail_L", PrimitiveType.Cube, new Vector3(-1.48f, 1.35f, 0f), Vector3.zero, new Vector3(0.18f, 0.18f, 8.5f), roofTimberMat, true);
        CreatePart(br.transform, "SideRail_R", PrimitiveType.Cube, new Vector3(1.48f, 1.35f, 0f), Vector3.zero, new Vector3(0.18f, 0.18f, 8.5f), roofTimberMat, true);
        for (int p = -1; p <= 1; p++)
        {
            float pz = p * 3.8f;
            CreatePart(br.transform, "SupportPost_L_" + (p + 2), PrimitiveType.Cylinder, new Vector3(-1.48f, -0.75f, pz), Vector3.zero, new Vector3(0.28f, 1.85f, 0.28f), darkTimberMat, true);
            CreatePart(br.transform, "SupportPost_R_" + (p + 2), PrimitiveType.Cylinder, new Vector3(1.48f, -0.75f, pz), Vector3.zero, new Vector3(0.28f, 1.85f, 0.28f), darkTimberMat, true);
        }
        bridgePrefab = SavePrefabClean(br, BridgePrefabPath);
    }

    private static void BuildVikingOverviewInGameScene(
        Scene gameScene,
        GameObject longhousePrefab,
        GameObject watchtowerPrefab,
        GameObject bannerPrefab,
        GameObject torchPrefab,
        GameObject fencePrefab,
        GameObject bridgePrefab)
    {
        DestroyRootIfExists(gameScene, "VikingIslandOverview");

        GameObject discoveryObj = FindSceneObjectByName(gameScene, "DiscoveryIsland_MainTerrain");
        Terrain discoveryTerrain = discoveryObj != null ? discoveryObj.GetComponent<Terrain>() : null;
        float safeX = (discoveryTerrain != null && discoveryTerrain.terrainData != null)
            ? discoveryTerrain.transform.position.x + discoveryTerrain.terrainData.size.x + 120f
            : 460f;
        float safeZ = discoveryTerrain != null ? discoveryTerrain.transform.position.z + 40f : 100f;

        GameObject root = new GameObject("VikingIslandOverview");
        SceneManager.MoveGameObjectToScene(root, gameScene);
        root.transform.position = new Vector3(safeX, -9f, safeZ);

        TerrainData vikingData = GetOrCreateRightSizedVikingTerrain();
        GameObject terrainGo = new GameObject("VikingIsland_OverviewTerrain");
        terrainGo.transform.SetParent(root.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = vikingData;
        terrain.materialTemplate = IslandPlacementUtility.GetOrCreateUrpTerrainMaterial();
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = vikingData;

        IslandPlacementUtility.FindClearWaterShorelineDock(terrain, out Vector3 shoreEdge, out Vector3 inlandDir);
        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 rightDir = Vector3.Cross(Vector3.up, inlandDir).normalized;

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingDarkTimber.mat");
        Material ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardRope.mat");
        Material brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardBrass.mat");
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatLanternGlow.mat");

        IslandPlacementUtility.BuildRealisticHarborPier(
            root.transform,
            "VikingHarborPier_Overview",
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

        // BoatDockPoint_Viking wired to BoatDocking + shared HealthCanvas HUD off pierhead
        BoatDocking discoveryDock = FindDiscoveryDock(gameScene);
        GameObject dockPointGo = new GameObject("BoatDockPoint_Viking");
        dockPointGo.transform.SetParent(root.transform, false);
        dockPointGo.transform.position = pierOuterTip - inlandDir * 3.2f;
        dockPointGo.transform.rotation = dockRot;

        BoatDocking vikingDocking = dockPointGo.AddComponent<BoatDocking>();
        vikingDocking.islandIndex = 1;
        vikingDocking.autoMatchCurrentIsland = false;
        vikingDocking.islandDisplayName = "Viking Island";
        vikingDocking.targetIslandSceneName = "IslandScene";
        vikingDocking.transitionToIslandScene = true;
        vikingDocking.dockingDistance = 12f;
        vikingDocking.boat = discoveryDock != null ? discoveryDock.boat : FindSceneObjectByName(gameScene, "OldWoodenRowboat_Complete");
        vikingDocking.quizManager = discoveryDock != null ? discoveryDock.quizManager : Object.FindAnyObjectByType<QuizManager>();
        if (discoveryDock != null)
        {
            vikingDocking.promptPanel = discoveryDock.promptPanel;
            vikingDocking.promptText = discoveryDock.promptText;
            vikingDocking.distanceGuideText = discoveryDock.distanceGuideText;
        }

        GameObject parkGo = new GameObject("DockParkingPoint");
        parkGo.transform.SetParent(dockPointGo.transform, false);
        parkGo.transform.localPosition = new Vector3(0f, -0.52f, -1.8f);
        parkGo.transform.localRotation = Quaternion.identity;
        vikingDocking.dockParkingPoint = parkGo.transform;

        GameObject retSpawnGo = new GameObject("BoatReturnSpawnPoint");
        retSpawnGo.transform.SetParent(dockPointGo.transform, false);
        retSpawnGo.transform.localPosition = new Vector3(0f, -0.52f, -6.5f);
        retSpawnGo.transform.localRotation = Quaternion.Euler(0f, 180f, 0f);
        vikingDocking.boatReturnSpawnPoint = retSpawnGo.transform;

        // Floating Viking Guide Buoy & pier torches/banners
        Material buoyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/DockBuoyRed.mat");
        GameObject buoy = new GameObject("VikingGuideBuoy");
        buoy.transform.SetParent(dockPointGo.transform, false);
        buoy.transform.localPosition = new Vector3(-4.5f, -0.55f, -3.5f);
        buoy.AddComponent<GentleFloat>();
        CreatePart(buoy.transform, "Base", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.85f, 0.32f, 0.85f), buoyMat);
        CreatePart(buoy.transform, "Tower", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f), brassMat);
        CreatePart(buoy.transform, "Beacon", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.zero, Vector3.one * 0.32f, glowMat);

        InstantiatePrefabAtWorld(torchPrefab, root.transform, "Overview_Torch_L", pierOuterTip - rightDir * 2.25f + inlandDir * 0.6f + Vector3.up * 0.58f, dockRot.eulerAngles.y);
        InstantiatePrefabAtWorld(torchPrefab, root.transform, "Overview_Torch_R", pierOuterTip + rightDir * 2.25f + inlandDir * 0.6f + Vector3.up * 0.58f, dockRot.eulerAngles.y);
        InstantiatePrefabAtWorld(bannerPrefab, root.transform, "Overview_Banner_L", pierDeckCenter - rightDir * 2.25f + inlandDir * 5.2f, dockRot.eulerAngles.y);
        InstantiatePrefabAtWorld(bannerPrefab, root.transform, "Overview_Banner_R", pierDeckCenter + rightDir * 2.25f + inlandDir * 5.2f, dockRot.eulerAngles.y);

        // Shared 1:1 Viking village overview matching IslandScene (pads already leveled in shared TerrainData)
        PopulateSharedVikingEnvironment(root.transform, terrain, beachEntryPos, inlandDir, rightDir, longhousePrefab, watchtowerPrefab, bannerPrefab, torchPrefab, fencePrefab, bridgePrefab, true, false, out Vector3 _);
        DisableDecorativePointShadows(gameScene);
    }

    private static void BuildDetailedVikingInIslandScene(
        Scene islandScene,
        GameObject longhousePrefab,
        GameObject watchtowerPrefab,
        GameObject bannerPrefab,
        GameObject torchPrefab,
        GameObject fencePrefab,
        GameObject bridgePrefab,
        GameObject regularChestPrefab,
        GameObject specialChestPrefab)
    {
        // Ensure DiscoveryIsland has warm tropical atmosphere profile
        GameObject discoveryRoot = FindRootObject(islandScene, "DiscoveryIsland");
        if (discoveryRoot != null)
        {
            IslandAtmosphereProfile discAtmo = discoveryRoot.GetComponent<IslandAtmosphereProfile>();
            if (discAtmo == null)
                discAtmo = discoveryRoot.AddComponent<IslandAtmosphereProfile>();
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

        TerrainData vikingData = GetOrCreateRightSizedVikingTerrain();
        GameObject terrainGo = new GameObject("VikingIsland_Terrain");
        terrainGo.transform.SetParent(vikingRoot.transform, false);
        terrainGo.transform.localPosition = Vector3.zero;
        Terrain terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = vikingData;
        terrain.materialTemplate = IslandPlacementUtility.GetOrCreateUrpTerrainMaterial();
        TerrainCollider tc = terrainGo.AddComponent<TerrainCollider>();
        tc.terrainData = vikingData;

        IslandPlacementUtility.FindClearWaterShorelineDock(terrain, out Vector3 shoreEdge, out Vector3 inlandDir);
        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 rightDir = Vector3.Cross(Vector3.up, inlandDir).normalized;
        float dockYaw = dockRot.eulerAngles.y;

        // Realistic 15m Viking Harbor Pier + Spawn + Return Boat + Repair Workbench
        GameObject dockArea = new GameObject("VikingDockArea");
        dockArea.transform.SetParent(vikingRoot.transform, false);

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingDarkTimber.mat");
        Material ropeMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardRope.mat");
        Material brassMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardBrass.mat");
        Material glowMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatLanternGlow.mat");
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingStoneWall.mat");
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");

        IslandPlacementUtility.BuildRealisticHarborPier(
            dockArea.transform,
            "VikingHarborPier",
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

        InstantiatePrefabAtWorld(torchPrefab, dockArea.transform, "DockTorch_L", pierOuterTip - rightDir * 2.25f + inlandDir * 0.6f + Vector3.up * 0.58f, dockYaw);
        InstantiatePrefabAtWorld(torchPrefab, dockArea.transform, "DockTorch_R", pierOuterTip + rightDir * 2.25f + inlandDir * 0.6f + Vector3.up * 0.58f, dockYaw);
        InstantiatePrefabAtWorld(bannerPrefab, dockArea.transform, "DockBanner_L", pierDeckCenter - rightDir * 2.25f + inlandDir * 5.2f, dockYaw);
        InstantiatePrefabAtWorld(bannerPrefab, dockArea.transform, "DockBanner_R", pierDeckCenter + rightDir * 2.25f + inlandDir * 5.2f, dockYaw);

        Vector3 retPos = pierOuterTip - inlandDir * 3.0f;
        retPos.y = 0.08f;
        GameObject returnBoat = InstantiateAssetAtWorld(BoatPrefabPath, dockArea.transform, "VikingDockedBoat_Return", retPos, dockYaw + 180f);
        BoatMovement bm = returnBoat.GetComponent<BoatMovement>(); if (bm != null) Object.DestroyImmediate(bm);
        BoatSink bs = returnBoat.GetComponent<BoatSink>(); if (bs != null) Object.DestroyImmediate(bs);
        GentleFloat gf = returnBoat.AddComponent<GentleFloat>();
        gf.bobAmplitude = 0.05f;
        gf.tiltAngle = 2.0f;
        IslandBoatReturn boatRet = returnBoat.AddComponent<IslandBoatReturn>();
        boatRet.islandIndex = 1;
        boatRet.oceanSceneName = "GameScene";
        boatRet.interactRange = 8.5f;

        BuildDockRepairBench(dockArea.transform, pierDeckCenter + rightDir * 1.85f, dockRot, woodMat, brassMat, sparkleMat);

        GameObject spawnGo = new GameObject("VikingSpawnPoint");
        spawnGo.transform.SetParent(dockArea.transform, false);
        spawnGo.transform.position = pierDeckCenter + Vector3.up * 0.06f;
        spawnGo.transform.rotation = dockRot;

        // Shared + Detailed Viking Environment (populated before chests so OverlapCapsule + footprints guarantee zero overlap!)
        PopulateSharedVikingEnvironment(vikingRoot.transform, terrain, beachEntryPos, inlandDir, rightDir, longhousePrefab, watchtowerPrefab, bannerPrefab, torchPrefab, fencePrefab, bridgePrefab, false, true, out Vector3 jarlShrinePos);
        Physics.SyncTransforms();

        Vector3 astridSpot = IslandPlacementUtility.FindUnobstructedSpot(terrain, beachEntryPos.x + rightDir.x * 4.2f + inlandDir.x * 8.0f, beachEntryPos.z + rightDir.z * 4.2f + inlandDir.z * 8.0f, 8f, 1.0f);
        IslandPlacementUtility.BuildIslandGuideNpc(
            vikingRoot.transform,
            terrain,
            astridSpot,
            dockYaw + 180f,
            1,
            "Scout Astrid",
            "Skal, Captain! The Viking clans hid a Golden Key inside one of our longhouse valley chests.",
            "Tip: Once you hold the Golden Key, follow the golden sky-beam up the cobblestone road to Jarl's Runestone Shrine!",
            20,
            new Color(0.62f, 0.18f, 0.16f, 1f),
            new Color(0.42f, 0.34f, 0.28f, 1f)
        );

        // 6 Well-Spaced, Accessible Viking Chests + 8-Spot Respawn Pool
        GameObject chestsRoot = new GameObject("VikingChests");
        chestsRoot.transform.SetParent(vikingRoot.transform, false);

        List<IslandPlacementUtility.PlacedFootprint> chestSpots = new List<IslandPlacementUtility.PlacedFootprint>();
        chestSpots.Add(new IslandPlacementUtility.PlacedFootprint(jarlShrinePos, 9.5f));
        Vector2[] respawnCoordPool = new Vector2[]
        {
            new Vector2(-20f, 12f), new Vector2(12f, 38f), new Vector2(-18f, 32f), new Vector2(-26f, 62f),
            new Vector2(28f, 58f), new Vector2(-10f, 50f), new Vector2(22f, 22f), new Vector2(6f, 68f)
        };
        Vector3[] respawnPool = new Vector3[respawnCoordPool.Length];
        for (int r = 0; r < respawnCoordPool.Length; r++)
        {
            Vector3 rp = beachEntryPos + rightDir * respawnCoordPool[r].x + inlandDir * respawnCoordPool[r].y;
            respawnPool[r] = FindSpacedChestSpot(terrain, rp.x, rp.z, 20f, 8.5f, chestSpots);
        }

        Vector3 villageCenter = beachEntryPos + inlandDir * 44f;
        for (int i = 0; i < 5; i++)
        {
            Vector3 p = respawnPool[i];
            Vector3 look = villageCenter - p;
            look.y = 0f;
            Quaternion cRot = look.sqrMagnitude > 0.1f ? Quaternion.LookRotation(look.normalized, Vector3.up) : dockRot;

            GameObject cObj = (GameObject)PrefabUtility.InstantiatePrefab(regularChestPrefab, chestsRoot.transform);
            cObj.name = "VikingChest_Regular_" + (i + 1);
            cObj.transform.position = p;
            cObj.transform.rotation = cRot;
            IslandPlacementUtility.GroundObjectExactToTerrain(cObj, terrain, 0.04f);
            TreasureChest tcScript = cObj.GetComponent<TreasureChest>();
            tcScript.chestId = "viking_regular_" + (i + 1);
            tcScript.islandIndex = 1;
            tcScript.isSpecialChest = false;
            tcScript.respawnCandidateSpots = respawnPool;
            EditorUtility.SetDirty(tcScript);
        }

        // Special Chest at Jarl's Runestone Shrine on its pre-leveled hilltop pad
        Vector3 shrinePos = jarlShrinePos;
        shrinePos.y = terrain.SampleHeight(shrinePos) + terrain.transform.position.y;

        CreatePart(chestsRoot.transform, "JarlShrineBaseSkirt", PrimitiveType.Cylinder, shrinePos - Vector3.up * 1.18f, Vector3.zero, new Vector3(5.6f, 1.2f, 5.6f), stoneMat, true);
        CreatePart(chestsRoot.transform, "JarlShrineDais", PrimitiveType.Cylinder, shrinePos + Vector3.up * 0.16f, Vector3.zero, new Vector3(5.2f, 0.16f, 5.2f), stoneMat, true);
        CreatePart(chestsRoot.transform, "JarlShrineStep", PrimitiveType.Cube, shrinePos - inlandDir * 2.7f + Vector3.up * 0.08f, new Vector3(0f, dockYaw, 0f), new Vector3(2.5f, 0.22f, 1.3f), stoneMat, true);

        Vector3[] shrineCorners = new Vector3[]
        {
            new Vector3(-2.6f, 0f, -2.6f), new Vector3(2.6f, 0f, -2.6f),
            new Vector3(-2.6f, 0f, 2.6f), new Vector3(2.6f, 0f, 2.6f)
        };
        for (int i = 0; i < shrineCorners.Length; i++)
        {
            Vector3 lp = shrinePos + shrineCorners[i];
            lp.y = terrain.SampleHeight(lp) + terrain.transform.position.y;
            GameObject marker = InstantiatePrefabAtWorld(i % 2 == 0 ? torchPrefab : bannerPrefab, chestsRoot.transform, "ShrineMarker_" + (i + 1), lp, dockYaw);
            IslandPlacementUtility.GroundObjectExactToTerrain(marker, terrain, 0.15f, true);
        }

        GameObject specialObj = (GameObject)PrefabUtility.InstantiatePrefab(specialChestPrefab, chestsRoot.transform);
        specialObj.name = "VikingChest_Special";
        specialObj.transform.position = shrinePos + Vector3.up * 0.32f;
        specialObj.transform.rotation = Quaternion.Euler(0f, dockYaw + 180f, 0f);
        TreasureChest spTc = specialObj.GetComponent<TreasureChest>();
        spTc.chestId = "viking_special_1";
        spTc.islandIndex = 1;
        spTc.isSpecialChest = true;
        spTc.respawnCandidateSpots = respawnPool;
        EditorUtility.SetDirty(spTc);

        // Wire VikingIsland & VikingSpawnPoint into IslandSceneController; keep DiscoveryIsland active by default
        IslandSceneController sceneCtrl = Object.FindAnyObjectByType<IslandSceneController>();
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
        Vector3 beachEntryPos,
        Vector3 inlandDir,
        Vector3 rightDir,
        GameObject longhousePrefab,
        GameObject watchtowerPrefab,
        GameObject bannerPrefab,
        GameObject torchPrefab,
        GameObject fencePrefab,
        GameObject bridgePrefab,
        bool overviewOnly,
        bool flattenPads,
        out Vector3 jarlShrinePos)
    {
        GameObject env = new GameObject(overviewOnly ? "VikingOverviewEnv" : "VikingEnvironment");
        env.transform.SetParent(parent, false);

        float dockYaw = Quaternion.LookRotation(inlandDir, Vector3.up).eulerAngles.y;
        Material stoneMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/VikingStoneWall.mat");

        List<IslandPlacementUtility.PlacedFootprint> placed = new List<IslandPlacementUtility.PlacedFootprint>();

        // 1. Reserve Harbor Plaza & Curved Cobblestone Main Road from Beach to Hilltop Shrine
        placed.Add(new IslandPlacementUtility.PlacedFootprint(beachEntryPos, 11.5f));
        Vector3[] roadNodes = new Vector3[14];
        for (int i = 0; i < roadNodes.Length; i++)
        {
            float dist = 6f + i * 6.4f;
            float curve = Mathf.Sin(i * 0.42f) * 3.0f;
            Vector3 rt = beachEntryPos + rightDir * curve + inlandDir * dist;
            roadNodes[i] = IslandPlacementUtility.FindFlatWalkablePoint(terrain, rt.x, rt.z, 8f, 22f, 0.75f);
            placed.Add(new IslandPlacementUtility.PlacedFootprint(roadNodes[i], 4.8f));
        }

        // Level Jarl Shrine hilltop pad BEFORE mountains, rocks, trees, and chests so nothing shifts afterwards
        Vector3 shrineReserved = beachEntryPos + rightDir * 2f + inlandDir * 88f;
        if (IslandPlacementUtility.FindFlatBuildingPad(terrain, shrineReserved.x, shrineReserved.z, 24f, 3.6f, 3.6f, 1.8f, 1.2f, placed, out Vector3 foundShrinePos, out float shrinePadY))
        {
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, foundShrinePos, 3.6f, 3.6f, dockYaw, 4.0f, shrinePadY);
            jarlShrinePos = foundShrinePos;
        }
        else
        {
            jarlShrinePos = IslandPlacementUtility.FindFlatWalkablePoint(terrain, shrineReserved.x, shrineReserved.z, 20f, 22f, 1.0f);
            placed.Add(new IslandPlacementUtility.PlacedFootprint(jarlShrinePos, 7.5f));
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, jarlShrinePos, 3.6f, 3.6f, dockYaw, 4.0f, jarlShrinePos.y);
        }

        // 2. Central Mead-Hall Firepit Plaza (around 48m inland)
        Vector3 campTarget = beachEntryPos + inlandDir * 48f;
        Vector3 campPos = campTarget;
        if (IslandPlacementUtility.FindFlatBuildingPad(terrain, campTarget.x, campTarget.z, 14f, 4.2f, 4.2f, 1.4f, 1.1f, placed, out Vector3 foundCampPos, out float campPadY))
        {
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, foundCampPos, 4.2f, 4.2f, dockYaw, 4.0f, campPadY);
            campPos = foundCampPos;
        }
        else if (IslandPlacementUtility.ReserveFlatSpot(terrain, campTarget.x, campTarget.z, 14f, 7.5f, 22f, 0.9f, placed, out Vector3 fallbackCamp))
        {
            campPos = fallbackCamp;
        }

        // 3. 9 Nordic Longhouses on verified flat terrain pads facing Main Road & Square
        Vector2[] houseOffsets = new Vector2[]
        {
            new Vector2(-14f, 24f),
            new Vector2(14f, 24f),
            new Vector2(-16f, 44f),
            new Vector2(16f, 44f),
            new Vector2(-15f, 64f),
            new Vector2(15f, 64f),
            new Vector2(-26f, 34f),
            new Vector2(26f, 34f),
            new Vector2(-17f, 82f)
        };
        int houseLimit = overviewOnly ? 6 : houseOffsets.Length;
        for (int i = 0; i < houseLimit; i++)
        {
            Vector3 t = beachEntryPos + rightDir * houseOffsets[i].x + inlandDir * houseOffsets[i].y;
            if (!IslandPlacementUtility.FindFlatBuildingPad(terrain, t.x, t.z, 22f, 4.0f, 6.0f, 1.8f, 1.1f, placed, out Vector3 p, out float padY))
                continue;

            float distAlongRoad = Vector3.Dot(p - beachEntryPos, inlandDir);
            Vector3 roadRef = beachEntryPos + inlandDir * Mathf.Clamp(distAlongRoad, 12f, 86f);
            Vector3 faceDir = roadRef - p;
            faceDir.y = 0f;
            float yaw = faceDir.sqrMagnitude > 0.1f ? Quaternion.LookRotation(faceDir.normalized, Vector3.up).eulerAngles.y : dockYaw;

            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, p, 4.0f, 6.0f, yaw, 4.2f, padY);

            GameObject lh = InstantiatePrefabAtWorld(longhousePrefab, env.transform, "VikingLonghouse_" + (i + 1), p, yaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(lh, terrain, 3.6f, 5.6f, 0.04f);
            IslandPlacementUtility.EnsureExactSolidColliders(lh, false, false);
        }

        // 4. 3 Coastal & Ridge Watchtowers (moved away from shoreline dock scanner)
        Vector2[] towerOffsets = new Vector2[]
        {
            new Vector2(15f, 14f),
            new Vector2(-28f, 54f),
            new Vector2(24f, 84f)
        };
        int towerLimit = overviewOnly ? 2 : towerOffsets.Length;
        for (int i = 0; i < towerLimit; i++)
        {
            Vector3 t = beachEntryPos + rightDir * towerOffsets[i].x + inlandDir * towerOffsets[i].y;
            if (!IslandPlacementUtility.FindFlatBuildingPad(terrain, t.x, t.z, 18f, 2.4f, 2.4f, 1.6f, 1.1f, placed, out Vector3 p, out float padY))
                continue;

            float yaw = dockYaw + (i == 0 ? 180f : (i == 1 ? 110f : -130f));
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, p, 2.4f, 2.4f, yaw, 3.5f, padY);

            GameObject tw = InstantiatePrefabAtWorld(watchtowerPrefab, env.transform, "VikingWatchtower_" + (i + 1), p, yaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(tw, terrain, 2.0f, 2.0f, 0.04f);
            IslandPlacementUtility.EnsureExactSolidColliders(tw, false, false);
        }

        // 5. 2 Grounded VikingTimberBridge Spans (1 overview, 2 detailed)
        Vector3 b1Target = beachEntryPos - rightDir * 16f + inlandDir * 20f;
        if (IslandPlacementUtility.FindFlatBuildingPad(terrain, b1Target.x, b1Target.z, 16f, 2.2f, 6.2f, 1.2f, 1.0f, placed, out Vector3 bridgePos, out float b1PadY))
        {
            float b1Yaw = dockYaw + 20f;
            if (flattenPads)
                IslandPlacementUtility.FlattenTerrainPad(terrain, bridgePos, 2.2f, 6.2f, b1Yaw, 3.6f, b1PadY);
            GameObject bridge = InstantiatePrefabAtWorld(bridgePrefab, env.transform, "VikingWoodenBridge", bridgePos, b1Yaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(bridge, terrain, 1.8f, 5.8f, 0.04f);
            IslandPlacementUtility.EnsureExactSolidColliders(bridge, false, true);
        }

        if (!overviewOnly)
        {
            Vector3 b2Target = beachEntryPos + rightDir * 16f + inlandDir * 74f;
            if (IslandPlacementUtility.FindFlatBuildingPad(terrain, b2Target.x, b2Target.z, 16f, 2.2f, 6.2f, 1.2f, 1.0f, placed, out Vector3 bridge2Pos, out float b2PadY))
            {
                float b2Yaw = dockYaw - 15f;
                if (flattenPads)
                    IslandPlacementUtility.FlattenTerrainPad(terrain, bridge2Pos, 2.2f, 6.2f, b2Yaw, 3.6f, b2PadY);
                GameObject bridge2 = InstantiatePrefabAtWorld(bridgePrefab, env.transform, "VikingWoodenBridge_North", bridge2Pos, b2Yaw);
                IslandPlacementUtility.GroundBuildingWithFoundation(bridge2, terrain, 1.8f, 5.8f, 0.04f);
                IslandPlacementUtility.EnsureExactSolidColliders(bridge2, false, true);
            }
        }

        Physics.SyncTransforms();

        // 6. Outer Scenic Mountains Strictly Outside Village & Walkable Corridors
        int mountainLimit = overviewOnly ? 4 : 6;
        Vector3 hubCenter = beachEntryPos + inlandDir * 55f;
        for (int i = 0; i < mountainLimit; i++)
        {
            float angle = (-55f + i * (290f / mountainLimit)) * Mathf.Deg2Rad;
            float radius = 108f + (i % 2) * 28f;
            Vector3 mt = hubCenter + rightDir * (Mathf.Cos(angle) * radius) + inlandDir * (Mathf.Sin(angle) * radius);
            if (IslandPlacementUtility.ReserveFlatSpot(terrain, mt.x, mt.z, 18f, 24.0f, 35f, 0.8f, placed, out Vector3 mp))
            {
                GameObject mtn = InstantiateAssetAtWorld(MountainPaths[i % MountainPaths.Length], env.transform, "VikingMountain_" + (i + 1), mp, i * 67f);
                IslandPlacementUtility.GroundObjectExactToTerrain(mtn, terrain, 1.2f);
                IslandPlacementUtility.EnsureExactSolidColliders(mtn, false, false);
            }
        }

        Physics.SyncTransforms();

        // 7. Coastal & Woodland Boulders (checking Return value & IsSpotClearOfObstacles so zero rocks bury houses)
        int rockLimit = overviewOnly ? 18 : 34;
        Vector3 origin = terrain.transform.position;
        Vector3 tSize = terrain.terrainData.size;
        for (int i = 0; i < rockLimit; i++)
        {
            float angle = i * 31f * Mathf.Deg2Rad;
            float radius = 34f + (i % 5) * 16f;
            Vector3 rt = (i < 22)
                ? beachEntryPos + rightDir * (Mathf.Cos(angle) * radius) + inlandDir * (44f + Mathf.Sin(angle) * radius)
                : new Vector3(origin.x + (0.14f + ((i * 19) % 10) * 0.078f) * tSize.x, 0f, origin.z + (0.18f + ((i * 23) % 10) * 0.072f) * tSize.z);

            if (IslandPlacementUtility.ReserveFlatSpot(terrain, rt.x, rt.z, 16f, 4.2f, 26f, 0.65f, placed, out Vector3 p)
                && IslandPlacementUtility.IsSpotClearOfObstacles(p, 2.0f))
            {
                GameObject rock = InstantiateAssetAtWorld(RockPaths[i % RockPaths.Length], env.transform, "VikingRock_" + (i + 1), p, i * 63f);
                IslandPlacementUtility.GroundObjectExactToTerrain(rock, terrain, 0.25f);
                IslandPlacementUtility.EnsureExactSolidColliders(rock, false, false);
            }
        }

        Physics.SyncTransforms();

        // 8. Nordic Pine Trees in natural woodland groves (strictly clear of buildings & mountains!)
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
                GameObject tree = InstantiateAssetAtWorld(TreePaths[i % TreePaths.Length], env.transform, "VikingTree_" + (i + 1), p, i * 47f);
                IslandPlacementUtility.GroundObjectExactToTerrain(tree, terrain, 0.18f, true);
                IslandPlacementUtility.EnsureExactSolidColliders(tree, true, false);
                if (tree != null && tree.GetComponent<WindSway>() == null)
                {
                    WindSway ws = tree.AddComponent<WindSway>();
                    ws.swayAngle = 2.0f;
                }
            }
        }

        if (overviewOnly)
            return;

        Physics.SyncTransforms();

        // 9. Detailed Mead-Hall Courtyard Firepit + Cargo + Palisade Fences + Cobblestone Road on leveled ground
        campPos.y = terrain.SampleHeight(campPos) + terrain.transform.position.y;
        GameObject camp = InstantiateAssetAtWorld(CampfirePath, env.transform, "VikingFirepit", campPos, dockYaw);
        camp.transform.localScale = Vector3.one * 1.35f;
        IslandPlacementUtility.GroundObjectExactToTerrain(camp, terrain, 0.10f);
        IslandPlacementUtility.EnsureExactSolidColliders(camp, false, false);
        InstantiateAssetAtWorld(EmbersPath, env.transform, "VikingFirepitEmbers", camp.transform.position + Vector3.up * 0.25f, 0f);
        AddFlickeringPointLight(env.transform, "VikingFirepitLight", camp.transform.position + Vector3.up * 1.2f, new Color(1f, 0.56f, 0.18f), 1.8f, 15f);

        Vector3 bSpot = IslandPlacementUtility.FindUnobstructedSpot(terrain, campPos.x - 3.5f, campPos.z - 2f, 6f, 0.9f);
        GameObject b1 = InstantiateAssetAtWorld(BarrelPath, env.transform, "VikingBarrel_1", bSpot, 15f);
        IslandPlacementUtility.GroundObjectExactToTerrain(b1, terrain, 0.05f);
        IslandPlacementUtility.EnsureExactSolidColliders(b1, false, false);

        Vector3 cSpot = IslandPlacementUtility.FindUnobstructedSpot(terrain, campPos.x + 3.5f, campPos.z - 2f, 6f, 0.9f);
        GameObject c1 = InstantiateAssetAtWorld(CratePath, env.transform, "VikingCrate_1", cSpot, -25f);
        IslandPlacementUtility.GroundObjectExactToTerrain(c1, terrain, 0.05f);
        IslandPlacementUtility.EnsureExactSolidColliders(c1, false, false);

        Vector2[] fenceOffsets = new Vector2[]
        {
            new Vector2(-6.0f, 14f), new Vector2(-10.0f, 14f),
            new Vector2(6.0f, 14f), new Vector2(10.0f, 14f),
            new Vector2(-21f, 38f), new Vector2(-21f, 44f),
            new Vector2(21f, 38f), new Vector2(21f, 44f),
            new Vector2(-21f, 58f), new Vector2(21f, 58f),
            new Vector2(-8f, 76f), new Vector2(8f, 76f),
            new Vector2(-26f, 24f), new Vector2(26f, 24f)
        };
        for (int i = 0; i < fenceOffsets.Length; i++)
        {
            Vector3 ft = beachEntryPos + rightDir * fenceOffsets[i].x + inlandDir * fenceOffsets[i].y;
            Vector3 fp = IslandPlacementUtility.FindFlatWalkablePoint(terrain, ft.x, ft.z, 8f, 20f, 0.8f);
            if (!IslandPlacementUtility.IsSpotClearOfObstacles(fp, 1.4f))
                fp = IslandPlacementUtility.FindUnobstructedSpot(terrain, fp.x, fp.z, 6f, 1.4f);
            float extraYaw = (i >= 4 && i <= 9) ? 90f : 0f;
            GameObject fence = InstantiatePrefabAtWorld(fencePrefab, env.transform, "VikingFence_" + (i + 1), fp, dockYaw + extraYaw);
            IslandPlacementUtility.GroundBuildingWithFoundation(fence, terrain, 1.8f, 0.3f, 0.04f);
        }

        for (int i = 0; i < roadNodes.Length; i++)
        {
            Vector3 sp = roadNodes[i];
            if (!IslandPlacementUtility.IsSpotClearOfObstacles(sp, 1.0f))
                sp = IslandPlacementUtility.FindUnobstructedSpot(terrain, sp.x, sp.z, 5f, 1.0f);
            sp.y = terrain.SampleHeight(sp) + terrain.transform.position.y;
            CreatePart(env.transform, "VikingPathStone_" + (i + 1), PrimitiveType.Cylinder, sp + Vector3.up * 0.01f, new Vector3(0f, i * 31f, 0f), new Vector3(1.8f, 0.04f, 1.8f), stoneMat);

            if (i < 8)
            {
                Vector3 tp = IslandPlacementUtility.FindFlatWalkablePoint(terrain, sp.x + rightDir.x * 2.8f, sp.z + rightDir.z * 2.8f, 5f, 20f, 0.8f);
                if (!IslandPlacementUtility.IsSpotClearOfObstacles(tp, 0.8f))
                    tp = IslandPlacementUtility.FindUnobstructedSpot(terrain, tp.x, tp.z, 6f, 0.8f);
                GameObject torch = InstantiatePrefabAtWorld(torchPrefab, env.transform, "TrailTorch_" + (i + 1), tp, dockYaw);
                IslandPlacementUtility.GroundObjectExactToTerrain(torch, terrain, 0.14f, true);
            }
            if (i < 10)
            {
                Vector3 bp = IslandPlacementUtility.FindFlatWalkablePoint(terrain, sp.x - rightDir.x * 3.0f, sp.z - rightDir.z * 3.0f, 5f, 20f, 0.8f);
                if (!IslandPlacementUtility.IsSpotClearOfObstacles(bp, 0.8f))
                    bp = IslandPlacementUtility.FindUnobstructedSpot(terrain, bp.x, bp.z, 6f, 0.8f);
                GameObject banner = InstantiatePrefabAtWorld(bannerPrefab, env.transform, "TrailBanner_" + (i + 1), bp, dockYaw + 15f * i);
                IslandPlacementUtility.GroundObjectExactToTerrain(banner, terrain, 0.14f, true);
            }
        }
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

    private static TerrainData GetOrCreateRightSizedVikingTerrain()
    {
        TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(VikingTerrainPath);
        if (existing == null)
        {
            AssetDatabase.CopyAsset(SourceVikingTerrainPath, VikingTerrainPath);
            AssetDatabase.ImportAsset(VikingTerrainPath, ImportAssetOptions.ForceUpdate);
            existing = AssetDatabase.LoadAssetAtPath<TerrainData>(VikingTerrainPath);
        }

        if (existing != null && !Mathf.Approximately(existing.size.x, 480f))
        {
            existing.size = new Vector3(480f, 68f, 480f);
            EditorUtility.SetDirty(existing);
        }

        return existing;
    }

    private static void BuildDockRepairBench(Transform parent, Vector3 worldPos, Quaternion dockRot, Material woodMat, Material brassMat, Material sparkleMat)
    {
        GameObject bench = new GameObject("VikingDockRepairWorkbench");
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
        CreatePart(bench.transform, "HammerHead", PrimitiveType.Cube, new Vector3(0.52f, 0.98f, 0.18f), new Vector3(0f, 25f, 0f), new Vector3(0.22f, 0.10f, 0.11f), brassMat);

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
