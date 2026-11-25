using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Idempotent Editor tool to build reusable TreasureChest prefabs (regular & special)
/// and populate Discovery Island in IslandScene.unity with bamboo houses, campfire,
/// pathways, swaying palms/bushes, shoreline crabs, and 6 accessible grounded chests.
/// </summary>
public static class Phase6DiscoveryIslandSetup
{
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string PrefabsFolder = "Assets/Prefabs";
    private const string MaterialsFolder = "Assets/Materials";
    private const string BaseChestPrefabPath = "Assets/Island/Prefabs/Chest.prefab";
    private const string RegularChestPrefabPath = "Assets/Prefabs/TreasureChest_Regular.prefab";
    private const string SpecialChestPrefabPath = "Assets/Prefabs/TreasureChest_Special.prefab";

    private const string House1Path = "Assets/POLYART_Ancient Village/Prefabs/PA_BambooHouse_1.prefab";
    private const string House2Path = "Assets/POLYART_Ancient Village/Prefabs/PA_BambooHouse_2.prefab";
    private const string LampPath = "Assets/POLYART_Ancient Village/Prefabs/PA_Bamboolamp_Set1.prefab";
    private const string RockPile1Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile1.prefab";
    private const string RockPile3Path = "Assets/POLYART_Ancient Village/Prefabs/PA_RockSet1_Pile3.prefab";

    private const string PalmTallPath = "Assets/ToonTropical/Prefabs/SM_Island_PalmTree_Tall.prefab";
    private const string PalmShortPath = "Assets/ToonTropical/Prefabs/SM_Island_PalmTree_Short.prefab";
    private const string PalmPack1Path = "Assets/PalmTreePack/Prefabs/1/Palm_1_1.prefab";
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

    [MenuItem("Tools/SinkOrThink3D/Phase 6 - Build Discovery Island & 6 Chests")]
    public static void BuildDiscoveryIsland()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(PrefabsFolder);
        EnsureFolder(MaterialsFolder);

        BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab);

        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        Terrain terrain = Object.FindAnyObjectByType<Terrain>();
        if (terrain == null || terrain.terrainData == null)
        {
            EditorUtility.DisplayDialog(
                "Run Phase 5 First",
                "Could not find DiscoveryIsland_Terrain in IslandScene.unity. Please run 'Tools -> SinkOrThink3D -> Phase 5 - Setup Island Scene & Transitions' first.",
                "OK"
            );
            return;
        }

        // Rebuild DiscoveryEnvironment & DiscoveryChests cleanly without duplicates
        DestroyRootIfExists(islandScene, "DiscoveryEnvironment");
        DestroyRootIfExists(islandScene, "DiscoveryChests");

        GameObject envRoot = new GameObject("DiscoveryEnvironment");
        SceneManager.MoveGameObjectToScene(envRoot, islandScene);

        GameObject chestsRoot = new GameObject("DiscoveryChests");
        SceneManager.MoveGameObjectToScene(chestsRoot, islandScene);

        Material stonePathMat = GetOrCreateLitMaterial("IslandPathStone.mat", new Color(0.56f, 0.53f, 0.47f, 1f), 0f, 0.15f);
        Material goldMat = GetOrCreateLitMaterial("ChestGoldTrim.mat", new Color(0.95f, 0.76f, 0.22f, 1f), 0.9f, 0.7f);
        PopulateDiscoveryEnvironment(envRoot.transform, terrain, stonePathMat, goldMat);
        PlaceDiscoveryChests(chestsRoot.transform, terrain, regularChestPrefab, specialChestPrefab, stonePathMat);
        DisableDecorativePointShadows(islandScene);

        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);
        AssetDatabase.SaveAssets();

        Debug.Log("Phase 6 Complete! Built TreasureChest prefabs and populated Discovery Island with village, animations, and 6 chests.");
        EditorUtility.DisplayDialog(
            "Phase 6 Complete",
            "Built reusable Regular & Special Treasure Chest prefabs and populated Discovery Island with Bamboo Village, Campfire, Pathways, Swaying Palms/Bushes, Shoreline Crabs, and 6 Accessible Chests.\n\nIslandScene.unity has been saved!",
            "OK"
        );
    }

    public static void BuildOrLoadChestPrefabs(out GameObject regularChestPrefab, out GameObject specialChestPrefab)
    {
        EnsureFolder(PrefabsFolder);
        EnsureFolder(MaterialsFolder);

        Material goldMat = GetOrCreateLitMaterial("ChestGoldTrim.mat", new Color(0.95f, 0.76f, 0.22f, 1f), 0.9f, 0.7f);
        Material auraMat = GetOrCreateEmissiveMaterial("SpecialChestAura.mat", new Color(1f, 0.85f, 0.28f, 1f), new Color(1f, 0.78f, 0.2f, 1f) * 2.1f);
        Material sparkleMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/BoatSparkleParticle.mat");
        if (sparkleMat == null)
            sparkleMat = GetOrCreateParticleMaterial("BoatSparkleParticle.mat", new Color(1f, 0.86f, 0.32f, 0.9f));

        regularChestPrefab = BuildChestPrefab(RegularChestPrefabPath, false, goldMat, auraMat, sparkleMat);
        specialChestPrefab = BuildChestPrefab(SpecialChestPrefabPath, true, goldMat, auraMat, sparkleMat);
    }

    private static GameObject BuildChestPrefab(string outputPath, bool isSpecial, Material goldMat, Material auraMat, Material sparkleMat)
    {
        GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BaseChestPrefabPath);
        GameObject instance = basePrefab != null
            ? (GameObject)PrefabUtility.InstantiatePrefab(basePrefab)
            : new GameObject("TreasureChest");

        if (basePrefab != null)
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);

        instance.name = isSpecial ? "TreasureChest_Special" : "TreasureChest_Regular";
        instance.transform.position = Vector3.zero;
        instance.transform.rotation = Quaternion.identity;
        instance.transform.localScale = Vector3.one * (isSpecial ? 0.9f : 0.72f);

        // Ensure interaction collider exists on root (trigger for E prompt)
        BoxCollider interactionCol = instance.GetComponent<BoxCollider>();
        if (interactionCol == null)
            interactionCol = instance.AddComponent<BoxCollider>();
        interactionCol.isTrigger = true;
        interactionCol.center = new Vector3(0f, 0.75f, 0f);
        interactionCol.size = new Vector3(2.4f, 1.8f, 2.2f);

        // Add dedicated solid non-trigger body collider so the player cannot walk through chests
        Transform oldSolid = instance.transform.Find("SolidBodyCollider");
        if (oldSolid != null)
            Object.DestroyImmediate(oldSolid.gameObject);

        GameObject solidBodyGo = new GameObject("SolidBodyCollider");
        solidBodyGo.transform.SetParent(instance.transform, false);
        BoxCollider solidCol = solidBodyGo.AddComponent<BoxCollider>();
        solidCol.isTrigger = false;
        solidCol.center = new Vector3(0f, 0.52f, 0f);
        solidCol.size = new Vector3(1.55f, 1.05f, 1.05f);

        // Lock visual at front center
        GameObject lockObj = CreatePrimitiveChild(instance.transform, "LockVisual", PrimitiveType.Cube, new Vector3(0f, 0.78f, 0.88f), new Vector3(0.32f, 0.36f, 0.14f), isSpecial ? auraMat : goldMat);

        // Reward reveal point above chest
        GameObject revealPoint = new GameObject("RewardRevealPoint");
        revealPoint.transform.SetParent(instance.transform, false);
        revealPoint.transform.localPosition = new Vector3(0f, 1.35f, 0.1f);

        // Sparkle reveal particles
        GameObject fxGo = new GameObject("ChestOpenParticles");
        fxGo.transform.SetParent(instance.transform, false);
        fxGo.transform.localPosition = new Vector3(0f, 0.95f, 0f);
        fxGo.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 1f;
        main.startLifetime = 0.95f;
        main.startSpeed = 2.6f;
        main.startSize = isSpecial ? 0.18f : 0.12f;
        main.startColor = isSpecial ? new Color(1f, 0.88f, 0.25f, 1f) : new Color(1f, 0.94f, 0.55f, 1f);
        main.gravityModifier = 0.2f;
        main.maxParticles = 40;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)(isSpecial ? 32 : 18)) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.55f;

        ParticleSystemRenderer pr = fxGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null && sparkleMat != null)
            pr.sharedMaterial = sparkleMat;

        GameObject auraObj = null;
        if (isSpecial)
        {
            // Gold trim bands and radiant aura ring so special chest stands out clearly
            CreatePrimitiveChild(instance.transform, "SpecialGoldBand_L", PrimitiveType.Cube, new Vector3(-0.78f, 0.45f, 0f), new Vector3(0.08f, 0.88f, 1.76f), goldMat);
            CreatePrimitiveChild(instance.transform, "SpecialGoldBand_R", PrimitiveType.Cube, new Vector3(0.78f, 0.45f, 0f), new Vector3(0.08f, 0.88f, 1.76f), goldMat);

            auraObj = new GameObject("SpecialAura");
            auraObj.transform.SetParent(instance.transform, false);
            auraObj.transform.localPosition = new Vector3(0f, 0.12f, 0f);

            CreatePrimitiveChild(auraObj.transform, "AuraRing", PrimitiveType.Cylinder, Vector3.zero, new Vector3(2.8f, 0.03f, 2.8f), auraMat);

            GameObject lightGo = new GameObject("SpecialGlowLight");
            lightGo.transform.SetParent(auraObj.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 1.1f, 0f);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.82f, 0.32f);
            l.intensity = 2.0f;
            l.range = 8f;
            l.shadows = LightShadows.None;
            FlickeringLight fl = lightGo.AddComponent<FlickeringLight>();
            fl.baseIntensity = 2.0f;
            fl.flickerRange = 0.35f;
        }

        TreasureChest chest = instance.AddComponent<TreasureChest>();
        chest.isSpecialChest = isSpecial;
        chest.lidTransform = instance.transform.Find("Chest_top");
        chest.lockVisual = lockObj;
        chest.specialAuraObject = auraObj;
        chest.openBurstParticles = ps;
        chest.rewardRevealPoint = revealPoint.transform;

        GameObject savedPrefab = PrefabUtility.SaveAsPrefabAsset(instance, outputPath);
        Object.DestroyImmediate(instance);
        return savedPrefab;
    }

    private static void PopulateDiscoveryEnvironment(Transform parent, Terrain terrain, Material stoneMat, Material goldMat)
    {
        // 1. Bamboo Village Clearing (~38m inland from dock at 14, y, 145)
        Vector2[] houseSpots = new Vector2[]
        {
            new Vector2(2f, 184f),
            new Vector2(28f, 188f),
            new Vector2(4f, 206f),
            new Vector2(30f, 208f)
        };
        float[] houseYaws = new float[] { 65f, -70f, 115f, -120f };

        for (int i = 0; i < houseSpots.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, houseSpots[i].x, houseSpots[i].y, 12f);
            string prefabPath = (i % 2 == 0) ? House1Path : House2Path;
            InstantiateGrounded(prefabPath, parent, "VillageHouse_" + (i + 1), p, houseYaws[i], 1f);
        }

        // Village Gathering Campfire + Embers + Cargo
        Vector3 campPos = FindWalkablePoint(terrain, 16f, 194f, 10f);
        InstantiateGrounded(CampfirePath, parent, "VillageCampfire", campPos, 0f, 1.2f);
        InstantiateGrounded(EmbersPath, parent, "VillageCampfireEmbers", campPos + Vector3.up * 0.25f, 0f, 1f);
        AddFlickeringPointLight(parent, "VillageCampfireLight", campPos + Vector3.up * 1.1f, new Color(1f, 0.58f, 0.2f), 2.5f, 14f);

        InstantiateGrounded(BarrelPath, parent, "VillageBarrel_1", FindWalkablePoint(terrain, 12.5f, 192f, 6f), 20f, 1f);
        InstantiateGrounded(CratePath, parent, "VillageCrate_1", FindWalkablePoint(terrain, 19.5f, 192f, 6f), -35f, 1f);

        // 2. Stepping Stone Path & Bamboo Lanterns (Dock -> Village -> Shrine)
        Vector2[] trailWaypoints = new Vector2[]
        {
            new Vector2(14f, 160f), new Vector2(14.5f, 167f), new Vector2(15f, 174f), new Vector2(15.5f, 181f), new Vector2(16f, 188f),
            new Vector2(18f, 202f), new Vector2(21f, 211f), new Vector2(24f, 220f), new Vector2(26f, 229f)
        };

        for (int i = 0; i < trailWaypoints.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, trailWaypoints[i].x, trailWaypoints[i].y, 5f);
            GameObject stone = CreatePrimitiveChild(parent, "PathStone_" + (i + 1), PrimitiveType.Cylinder, p + Vector3.up * 0.04f, new Vector3(1.6f, 0.06f, 1.6f), stoneMat);
            stone.transform.rotation = Quaternion.Euler(0f, i * 28f, 0f);

            if (i % 2 == 0)
            {
                Vector3 lampPos = FindWalkablePoint(terrain, trailWaypoints[i].x + 2.3f, trailWaypoints[i].y, 4f);
                InstantiateGrounded(LampPath, parent, "PathLamp_" + (i + 1), lampPos, 0f, 1f);
            }
        }

        // 3. Swaying Palm Trees around beaches & ridges
        Vector2[] palmSpots = new Vector2[]
        {
            new Vector2(-6f, 166f), new Vector2(28f, 164f), new Vector2(36f, 174f), new Vector2(-12f, 182f),
            new Vector2(40f, 192f), new Vector2(-14f, 204f), new Vector2(44f, 212f), new Vector2(-6f, 222f),
            new Vector2(38f, 230f), new Vector2(10f, 236f), new Vector2(-20f, 176f), new Vector2(52f, 184f),
            new Vector2(-22f, 194f), new Vector2(48f, 224f), new Vector2(2f, 158f), new Vector2(26f, 158f),
            new Vector2(-18f, 216f), new Vector2(34f, 242f)
        };

        string[] palmPrefabs = new string[] { PalmPack1Path, PalmTallPath, PalmShortPath };
        for (int i = 0; i < palmSpots.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, palmSpots[i].x, palmSpots[i].y, 8f);
            GameObject palm = InstantiateGrounded(palmPrefabs[i % palmPrefabs.Length], parent, "SwayingPalm_" + (i + 1), p, i * 47f, 0.95f + (i % 3) * 0.12f);
            if (palm != null && palm.GetComponent<WindSway>() == null)
            {
                WindSway sway = palm.AddComponent<WindSway>();
                sway.swayAngle = 2.4f;
                sway.swaySpeed = 1.2f + (i % 4) * 0.12f;
            }
        }

        // 4. Swaying Bushes, Grass Tufts, Rocks & Shoreline Details
        for (int i = 0; i < 16; i++)
        {
            float angle = i * 22.5f * Mathf.Deg2Rad;
            float radius = 14f + (i % 3) * 12f;
            Vector3 p = FindWalkablePoint(terrain, 16f + Mathf.Cos(angle) * radius, 196f + Mathf.Sin(angle) * radius, 8f);
            string vegPath = (i % 2 == 0) ? BushPath : GrassPath;
            GameObject veg = InstantiateGrounded(vegPath, parent, "IslandFoliage_" + (i + 1), p, i * 33f, 1f + (i % 2) * 0.25f);
            if (veg != null && veg.GetComponent<WindSway>() == null)
            {
                WindSway sway = veg.AddComponent<WindSway>();
                sway.swayAngle = 4.2f;
                sway.swaySpeed = 1.8f;
            }
        }

        Vector2[] rockSpots = new Vector2[]
        {
            new Vector2(-18f, 186f), new Vector2(46f, 198f), new Vector2(-10f, 232f), new Vector2(36f, 246f),
            new Vector2(2f, 224f), new Vector2(54f, 172f)
        };
        for (int i = 0; i < rockSpots.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, rockSpots[i].x, rockSpots[i].y, 10f);
            InstantiateGrounded(i % 2 == 0 ? RockPile1Path : RockPile3Path, parent, "RockCluster_" + (i + 1), p, i * 55f, 1f);
        }

        // Shoreline driftwood, seashells, starfish, and 2 animated crabs
        Vector3 shore1 = FindWalkablePoint(terrain, 32f, 160f, 8f);
        Vector3 shore2 = FindWalkablePoint(terrain, -2f, 161f, 8f);
        InstantiateGrounded(DriftwoodPath, parent, "ShoreDriftwood_1", shore1 + Vector3.left * 2f, 35f, 1.1f);
        InstantiateGrounded(SeashellPath, parent, "ShoreShell_1", shore1 + Vector3.right * 1.5f, 15f, 1.5f);
        InstantiateGrounded(StarfishPath, parent, "ShoreStarfish_1", shore2 + Vector3.left * 1.5f, 70f, 1.5f);

        GameObject crab1 = InstantiateGrounded(CrabPath, parent, "BeachCrab_East", shore1, 0f, 1.2f);
        if (crab1 != null && crab1.GetComponent<BeachCrabWander>() == null)
            crab1.AddComponent<BeachCrabWander>();

        GameObject crab2 = InstantiateGrounded(CrabPath, parent, "BeachCrab_West", shore2, 90f, 1.2f);
        if (crab2 != null && crab2.GetComponent<BeachCrabWander>() == null)
            crab2.AddComponent<BeachCrabWander>();
    }

    private static void PlaceDiscoveryChests(Transform parent, Terrain terrain, GameObject regularPrefab, GameObject specialPrefab, Material stoneMat)
    {
        // 5 Regular Chests across distinct Discovery Island landmarks
        Vector2[] regularCoords = new Vector2[]
        {
            new Vector2(35f, 164f),   // 1: East Beach Cove
            new Vector2(21f, 196f),   // 2: Bamboo Village Square
            new Vector2(-12f, 188f),  // 3: West Palm Grove
            new Vector2(-10f, 224f),  // 4: North-West Rocky Overlook
            new Vector2(42f, 218f)    // 5: North-East Coastal Ridge
        };
        float[] regularYaws = new float[] { -145f, -160f, 115f, 145f, -125f };

        for (int i = 0; i < regularCoords.Length; i++)
        {
            Vector3 p = FindWalkablePoint(terrain, regularCoords[i].x, regularCoords[i].y, 10f);
            GameObject chestObj = (GameObject)PrefabUtility.InstantiatePrefab(regularPrefab, parent);
            chestObj.name = "DiscoveryChest_Regular_" + (i + 1);
            chestObj.transform.position = p;
            chestObj.transform.rotation = Quaternion.Euler(0f, regularYaws[i], 0f);

            TreasureChest tc = chestObj.GetComponent<TreasureChest>();
            tc.chestId = "discovery_regular_" + (i + 1);
            tc.islandIndex = 0;
            tc.isSpecialChest = false;
            EditorUtility.SetDirty(tc);
        }

        // 1 Distinct Special Chest at the Ancient Discovery Shrine (North Overlook)
        Vector3 shrinePos = FindWalkablePoint(terrain, 26f, 236f, 12f);

        GameObject plinth = CreatePrimitiveChild(parent, "SpecialShrinePlinth", PrimitiveType.Cylinder, shrinePos + Vector3.up * 0.14f, new Vector3(4.6f, 0.28f, 4.6f), stoneMat);
        plinth.AddComponent<MeshCollider>();

        // Frame the shrine with 4 bamboo lamps
        Vector3[] cornerOffsets = new Vector3[]
        {
            new Vector3(-2.4f, 0f, -2.4f), new Vector3(2.4f, 0f, -2.4f),
            new Vector3(-2.4f, 0f, 2.4f), new Vector3(2.4f, 0f, 2.4f)
        };
        for (int i = 0; i < cornerOffsets.Length; i++)
        {
            Vector3 lp = shrinePos + cornerOffsets[i];
            lp.y = terrain.SampleHeight(lp) + terrain.transform.position.y;
            InstantiateGrounded(LampPath, parent, "ShrineLamp_" + (i + 1), lp, 0f, 1.1f);
        }

        GameObject specialChestObj = (GameObject)PrefabUtility.InstantiatePrefab(specialPrefab, parent);
        specialChestObj.name = "DiscoveryChest_Special";
        specialChestObj.transform.position = shrinePos + Vector3.up * 0.28f;
        specialChestObj.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        TreasureChest specialTc = specialChestObj.GetComponent<TreasureChest>();
        specialTc.chestId = "discovery_special_1";
        specialTc.islandIndex = 0;
        specialTc.isSpecialChest = true;
        EditorUtility.SetDirty(specialTc);
    }

    private static Vector3 FindWalkablePoint(Terrain terrain, float preferredX, float preferredZ, float searchRadius)
    {
        float terrainBaseY = terrain.transform.position.y;
        TerrainData data = terrain.terrainData;

        if (IsWalkable(terrain, data, terrainBaseY, preferredX, preferredZ, out float bestY))
            return new Vector3(preferredX, bestY, preferredZ);

        // Spiral outward to guarantee grounded, gentle-slope, above-water placement
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
        return new Vector3(preferredX, Mathf.Max(0.5f, fallbackY), preferredZ);
    }

    private static bool IsWalkable(Terrain terrain, TerrainData data, float baseY, float worldX, float worldZ, out float worldY)
    {
        Vector3 terrainOrigin = terrain.transform.position;
        float normX = (worldX - terrainOrigin.x) / data.size.x;
        float normZ = (worldZ - terrainOrigin.z) / data.size.z;
        worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + baseY;

        if (normX < 0.05f || normX > 0.95f || normZ < 0.05f || normZ > 0.95f)
            return false;

        if (worldY < 0.45f)
            return false;

        float slope = data.GetSteepness(normX, normZ);
        return slope < 34f;
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

    private static GameObject CreatePrimitiveChild(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localScale = localScale;

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Object.DestroyImmediate(col);

        if (mat != null && go.GetComponent<MeshRenderer>() != null)
            go.GetComponent<MeshRenderer>().sharedMaterial = mat;

        return go;
    }

    private static Material GetOrCreateLitMaterial(string fileName, Color color, float metallic, float smoothness)
    {
        string path = MaterialsFolder + "/" + fileName;
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
        string path = MaterialsFolder + "/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        mat = GetOrCreateLitMaterial(fileName, baseColor, 0f, 0.45f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emissionColor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateParticleMaterial(string fileName, Color color)
    {
        string path = MaterialsFolder + "/" + fileName;
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
