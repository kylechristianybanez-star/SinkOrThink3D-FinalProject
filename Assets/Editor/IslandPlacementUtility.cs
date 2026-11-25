using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

/// <summary>
/// Shared Phase 9.5 - 13 Editor placement, grounding, lighting-sanitization,
/// collider-enforcement, and POLYART_Ancient Village import utility.
/// </summary>
public static class IslandPlacementUtility
{
    public const string PolyartScenePath = "Assets/POLYART_Ancient Village/Scenes/PA_AncientVillageDay.unity";
    private const string VolumeProfilePath = "Assets/Settings/SampleSceneProfile.asset";
    private const string FireMat1Path = "Assets/POLYART_Ancient Village/Materials/FX/PA_Fireset1_mtl.mat";
    private const string FireMat2Path = "Assets/POLYART_Ancient Village/Materials/FX/PA_Fireset2_mtl.mat";
    private const string PolyartWaterMatPath = "Assets/POLYART_Ancient Village/Materials/Water/Water.mat";
    private const int MaxMeshColliderVerts = 12000;

    public struct PlacedFootprint
    {
        public Vector3 position;
        public float radius;

        public PlacedFootprint(Vector3 pos, float r)
        {
            position = pos;
            radius = r;
        }
    }

    /// <summary>
    /// Eliminates blown-out HDR Bloom, excessive fire emissions, shadow-casting point lights,
    /// and over-bright directional suns across the target scene and shared assets.
    /// </summary>
    public static void SanitizeGlobalLightingAndMaterials(Scene scene)
    {
        ClampMaterialEmission(FireMat1Path, new Color(2.0f, 0.65f, 0.14f, 1f));
        ClampMaterialEmission(FireMat2Path, new Color(1.6f, 0.52f, 0.12f, 1f));
        ClampMaterialEmission(PolyartWaterMatPath, Color.black);

        BrightenBuildingMaterial("Assets/Materials/FinalMossyStone.mat", new Color(0.88f, 0.94f, 0.86f, 1f));
        BrightenBuildingMaterial("Assets/Materials/FinalDarkCliff.mat", new Color(0.78f, 0.78f, 0.82f, 1f));
        BrightenBuildingMaterial("Assets/Materials/FinalAncientTimber.mat", new Color(0.82f, 0.68f, 0.52f, 1f));
        BrightenBuildingMaterial("Assets/Materials/VikingDarkTimber.mat", new Color(0.82f, 0.66f, 0.48f, 1f));
        BrightenBuildingMaterial("Assets/Materials/VikingRoofShingle.mat", new Color(0.64f, 0.50f, 0.38f, 1f));
        BrightenBuildingMaterial("Assets/Materials/VikingStoneWall.mat", new Color(0.90f, 0.92f, 0.95f, 1f));
        BrightenBuildingMaterial("Assets/Materials/VikingRunestone.mat", new Color(0.82f, 0.88f, 0.92f, 1f));
        BrightenBuildingMaterial("Assets/Materials/QuizBoardFrameWood.mat", new Color(0.62f, 0.44f, 0.28f, 1f));

        // Ensure bright, balanced outdoor Trilight ambient fill so shadowed faces never turn pitch black
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(0.68f, 0.80f, 0.94f, 1f);
        RenderSettings.ambientEquatorColor = new Color(0.58f, 0.64f, 0.70f, 1f);
        RenderSettings.ambientGroundColor = new Color(0.38f, 0.35f, 0.29f, 1f);
        RenderSettings.ambientIntensity = 1.15f;

        VolumeProfile profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        SanitizeVolumeProfile(profile);

        Volume[] volumes = Object.FindObjectsByType<Volume>(FindObjectsInactive.Include);
        for (int i = 0; i < volumes.Length; i++)
        {
            if (volumes[i] != null && volumes[i].gameObject.scene == scene && volumes[i].sharedProfile != null)
                SanitizeVolumeProfile(volumes[i].sharedProfile);
        }

        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include);
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l == null || l.gameObject.scene != scene)
                continue;

            if (l.type == LightType.Directional)
            {
                l.intensity = Mathf.Clamp(l.intensity, 1.15f, 1.45f);
                RenderSettings.sun = l;
            }
            else
            {
                l.shadows = LightShadows.None;
                l.intensity = Mathf.Min(l.intensity, 2.2f);
                l.range = Mathf.Min(l.range, 16f);
            }
            EditorUtility.SetDirty(l);
        }
    }

    private static void BrightenBuildingMaterial(string assetPath, Color targetColor)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
            return;

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", targetColor);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", targetColor);
        EditorUtility.SetDirty(mat);
    }

    private static void SanitizeVolumeProfile(VolumeProfile profile)
    {
        if (profile == null)
            return;

        if (profile.TryGet<Bloom>(out Bloom bloom))
        {
            bloom.threshold.Override(1.25f);
            bloom.intensity.Override(0.22f);
            bloom.scatter.Override(0.45f);
            bloom.clamp.Override(3.5f);
            EditorUtility.SetDirty(bloom);
            EditorUtility.SetDirty(profile);
        }
    }

    private static void ClampMaterialEmission(string assetPath, Color targetEmission)
    {
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(assetPath);
        if (mat == null)
            return;

        if (mat.HasProperty("_EmissionColor"))
        {
            mat.SetColor("_EmissionColor", targetEmission);
            EditorUtility.SetDirty(mat);
        }
    }

    /// <summary>
    /// Returns a URP-compatible terrain material so Terrains never render pink.
    /// </summary>
    public static Material GetOrCreateUrpTerrainMaterial()
    {
        string defaultUrpPath = AssetDatabase.GUIDToAssetPath("594ea882c5a793440b60ff72d896021e");
        if (!string.IsNullOrEmpty(defaultUrpPath))
        {
            Material m = AssetDatabase.LoadAssetAtPath<Material>(defaultUrpPath);
            if (m != null)
                return m;
        }

        Material demoUrp = AssetDatabase.LoadAssetAtPath<Material>("Assets/Phantom Studios/Demo/Shared/Terrain Material URP.mat");
        if (demoUrp != null)
            return demoUrp;

        const string customPath = "Assets/Materials/URPTerrainLit.mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(customPath);
        if (existing != null)
            return existing;

        Shader terrainShader = Shader.Find("Universal Render Pipeline/Terrain/Lit");
        if (terrainShader == null)
            terrainShader = Shader.Find("Nature/Terrain/Standard");

        Material created = new Material(terrainShader);
        AssetDatabase.CreateAsset(created, customPath);
        return created;
    }

    /// <summary>
    /// Returns true if the terrain has any TreePrototype that lacks Unity's Nature/Soft Occlusion
    /// or SpeedTree shader (which causes shader/billboard console warnings under URP).
    /// </summary>
    public static bool HasInvalidTerrainTreePrototypes(Terrain terrain)
    {
        if (terrain == null || terrain.terrainData == null)
            return false;

        TreePrototype[] protos = terrain.terrainData.treePrototypes;
        if (protos == null || protos.Length == 0)
            return false;

        for (int i = 0; i < protos.Length; i++)
        {
            TreePrototype tp = protos[i];
            if (tp == null || tp.prefab == null)
                return true;

            Renderer[] renderers = tp.prefab.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
                return true;

            for (int r = 0; r < renderers.Length; r++)
            {
                if (renderers[r] == null)
                    continue;
                Material[] mats = renderers[r].sharedMaterials;
                for (int m = 0; m < mats.Length; m++)
                {
                    Material mat = mats[m];
                    if (mat == null || mat.shader == null)
                        return true;
                    string sn = mat.shader.name;
                    if (!sn.Contains("Nature/Soft Occlusion") && !sn.Contains("Nature/SpeedTree"))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Converts any painted URP/Polyart terrain TreeInstances into regular GameObjects with trunk colliders
    /// and clears incompatible TreePrototypes so Unity never logs shader/billboard warnings.
    /// </summary>
    public static int SanitizeTerrainTreePrototypes(Terrain terrain, Transform convertedTreesParent = null)
    {
        if (!HasInvalidTerrainTreePrototypes(terrain))
            return 0;

        TerrainData td = terrain.terrainData;
        TreePrototype[] protos = td.treePrototypes;
        int clearedCount = protos != null ? protos.Length : 0;

        if (convertedTreesParent != null && td.treeInstances != null && td.treeInstances.Length > 0)
        {
            Transform holder = convertedTreesParent.Find("TerrainTrees");
            if (holder == null)
            {
                GameObject holderGo = new GameObject("TerrainTrees");
                holderGo.transform.SetParent(convertedTreesParent, false);
                holder = holderGo.transform;
            }

            if (holder.childCount == 0)
            {
                TreeInstance[] instances = td.treeInstances;
                Vector3 origin = terrain.transform.position;
                Vector3 tSize = td.size;

                for (int i = 0; i < instances.Length; i++)
                {
                    TreeInstance inst = instances[i];
                    if (inst.prototypeIndex < 0 || inst.prototypeIndex >= protos.Length)
                        continue;

                    TreePrototype tp = protos[inst.prototypeIndex];
                    if (tp == null || tp.prefab == null)
                        continue;

                    GameObject treeGo = (GameObject)PrefabUtility.InstantiatePrefab(tp.prefab, holder);
                    treeGo.name = tp.prefab.name + "_Converted_" + (i + 1);
                    treeGo.transform.position = origin + new Vector3(
                        inst.position.x * tSize.x,
                        inst.position.y * tSize.y,
                        inst.position.z * tSize.z
                    );
                    treeGo.transform.rotation = Quaternion.Euler(0f, inst.rotation * Mathf.Rad2Deg, 0f);
                    treeGo.transform.localScale = new Vector3(inst.widthScale, inst.heightScale, inst.widthScale);

                    GroundObjectExactToTerrain(treeGo, terrain, 0.12f, true);
                    EnsureExactSolidColliders(treeGo, true, false);
                    if (treeGo.GetComponent<WindSway>() == null)
                    {
                        WindSway ws = treeGo.AddComponent<WindSway>();
                        ws.swayAngle = 2.0f;
                    }
                }
            }
        }

        td.treeInstances = new TreeInstance[0];
        td.treePrototypes = new TreePrototype[0];
        td.RefreshPrototypes();
        terrain.Flush();
        EditorUtility.SetDirty(td);
        EditorUtility.SetDirty(terrain);
        return clearedCount;
    }

    /// <summary>
    /// Grounds an object flush to terrain using either its trunk root point (for wide canopies/poles)
    /// or a clamped 5-point footprint so no tree sinks too deep and no rock or chest floats.
    /// </summary>
    public static void GroundObjectExactToTerrain(GameObject go, Terrain terrain, float sinkEmbed = 0.14f, bool useRootCenterOnly = false)
    {
        if (go == null || terrain == null || terrain.terrainData == null)
            return;

        Renderer[] renderers = go.GetComponentsInChildren<Renderer>(true);
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

        if (!hasBounds)
        {
            float fallbackY = terrain.SampleHeight(go.transform.position) + terrain.transform.position.y;
            go.transform.position = new Vector3(go.transform.position.x, fallbackY - sinkEmbed, go.transform.position.z);
            return;
        }

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float minTx = origin.x + 1f;
        float maxTx = origin.x + size.x - 1f;
        float minTz = origin.z + 1f;
        float maxTz = origin.z + size.z - 1f;

        float targetGroundY;
        if (useRootCenterOnly)
        {
            float cx = Mathf.Clamp(go.transform.position.x, minTx, maxTx);
            float cz = Mathf.Clamp(go.transform.position.z, minTz, maxTz);
            targetGroundY = terrain.SampleHeight(new Vector3(cx, 0f, cz)) + origin.y;
        }
        else
        {
            float ex = Mathf.Min(bounds.extents.x, 1.5f);
            float ez = Mathf.Min(bounds.extents.z, 1.5f);
            Vector2[] samplePts = new Vector2[]
            {
                new Vector2(bounds.center.x, bounds.center.z),
                new Vector2(bounds.center.x - ex, bounds.center.z - ez),
                new Vector2(bounds.center.x - ex, bounds.center.z + ez),
                new Vector2(bounds.center.x + ex, bounds.center.z - ez),
                new Vector2(bounds.center.x + ex, bounds.center.z + ez)
            };

            targetGroundY = float.MaxValue;
            for (int i = 0; i < samplePts.Length; i++)
            {
                float sx = Mathf.Clamp(samplePts[i].x, minTx, maxTx);
                float sz = Mathf.Clamp(samplePts[i].y, minTz, maxTz);
                float h = terrain.SampleHeight(new Vector3(sx, 0f, sz)) + origin.y;
                if (h < targetGroundY)
                    targetGroundY = h;
            }
        }

        float deltaY = targetGroundY - bounds.min.y - sinkEmbed;
        go.transform.position = go.transform.position + Vector3.up * deltaY;
    }

    /// <summary>
    /// Resets destination TerrainData from a source terrain asset while preserving the asset GUID
    /// and the source terrain's original world size.
    /// </summary>
    public static TerrainData ResetTerrainFromSourceKeepSize(string sourceAssetPath, string destAssetPath)
    {
        TerrainData src = AssetDatabase.LoadAssetAtPath<TerrainData>(sourceAssetPath);
        Vector3 keepSize = src != null ? src.size : new Vector3(400f, 100f, 400f);
        return ResetTerrainFromSource(sourceAssetPath, destAssetPath, keepSize);
    }

    /// <summary>
    /// Resets destination TerrainData from a source terrain asset while preserving the asset GUID,
    /// ensuring terrain heightmaps start clean before flattening building pads.
    /// </summary>
    public static TerrainData ResetTerrainFromSource(string sourceAssetPath, string destAssetPath, Vector3 targetSize)
    {
        TerrainData existing = AssetDatabase.LoadAssetAtPath<TerrainData>(destAssetPath);
        if (existing == null)
        {
            AssetDatabase.CopyAsset(sourceAssetPath, destAssetPath);
            AssetDatabase.ImportAsset(destAssetPath, ImportAssetOptions.ForceUpdate);
            existing = AssetDatabase.LoadAssetAtPath<TerrainData>(destAssetPath);
        }

        TerrainData src = AssetDatabase.LoadAssetAtPath<TerrainData>(sourceAssetPath);
        if (existing != null && src != null)
        {
            int res = src.heightmapResolution;
            if (existing.heightmapResolution != res)
                existing.heightmapResolution = res;
            float[,] heights = src.GetHeights(0, 0, res, res);
            existing.SetHeights(0, 0, heights);
            existing.size = targetSize;
            EditorUtility.SetDirty(existing);
        }
        else if (existing != null)
        {
            existing.size = targetSize;
            EditorUtility.SetDirty(existing);
        }

        return existing;
    }

    /// <summary>
    /// Smoothly levels a rectangular terrain pad under a building or shrine so doorways and walls
    /// sit flush at the surface without getting buried uphill or hovering downhill.
    /// </summary>
    public static void FlattenTerrainPad(
        Terrain terrain,
        Vector3 centerWorld,
        float halfWidth,
        float halfDepth,
        float yawDegrees,
        float blendMargin,
        float targetWorldY)
    {
        if (terrain == null || terrain.terrainData == null)
            return;

        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        int res = data.heightmapResolution;
        if (res < 16 || data.size.x <= 0.1f || data.size.z <= 0.1f || data.size.y <= 0.1f)
            return;

        float safeTargetY = Mathf.Max(targetWorldY, 1.15f);
        float targetNormH = Mathf.Clamp01((safeTargetY - origin.y) / data.size.y);

        float outerExtent = Mathf.Sqrt(halfWidth * halfWidth + halfDepth * halfDepth) + blendMargin + 1.5f;
        float normPerMeterX = (res - 1) / data.size.x;
        float normPerMeterZ = (res - 1) / data.size.z;

        int minX = Mathf.Clamp(Mathf.FloorToInt((centerWorld.x - outerExtent - origin.x) * normPerMeterX), 4, res - 5);
        int maxX = Mathf.Clamp(Mathf.CeilToInt((centerWorld.x + outerExtent - origin.x) * normPerMeterX), 4, res - 5);
        int minZ = Mathf.Clamp(Mathf.FloorToInt((centerWorld.z - outerExtent - origin.z) * normPerMeterZ), 4, res - 5);
        int maxZ = Mathf.Clamp(Mathf.CeilToInt((centerWorld.z + outerExtent - origin.z) * normPerMeterZ), 4, res - 5);

        int width = maxX - minX + 1;
        int height = maxZ - minZ + 1;
        if (width <= 0 || height <= 0)
            return;

        float[,] heights = data.GetHeights(minX, minZ, width, height);
        float rad = -yawDegrees * Mathf.Deg2Rad;
        float cosA = Mathf.Cos(rad);
        float sinA = Mathf.Sin(rad);
        float flatHalfW = halfWidth + 1.2f;
        float flatHalfD = halfDepth + 1.2f;
        float safeBlend = Mathf.Max(0.5f, blendMargin);

        for (int z = 0; z < height; z++)
        {
            float wz = origin.z + ((minZ + z) / (float)(res - 1)) * data.size.z;
            for (int x = 0; x < width; x++)
            {
                float wx = origin.x + ((minX + x) / (float)(res - 1)) * data.size.x;
                float origWorldY = origin.y + heights[z, x] * data.size.y;
                if (origWorldY < 0.75f)
                    continue;

                float dWorldX = wx - centerWorld.x;
                float dWorldZ = wz - centerWorld.z;
                float lx = dWorldX * cosA - dWorldZ * sinA;
                float lz = dWorldX * sinA + dWorldZ * cosA;

                float dx = Mathf.Max(0f, Mathf.Abs(lx) - flatHalfW);
                float dz = Mathf.Max(0f, Mathf.Abs(lz) - flatHalfD);
                float dist = Mathf.Sqrt(dx * dx + dz * dz);
                if (dist >= safeBlend)
                    continue;

                float t = dist <= 0f ? 1f : 0.5f * (1f + Mathf.Cos(Mathf.Clamp01(dist / safeBlend) * Mathf.PI));
                heights[z, x] = Mathf.Lerp(heights[z, x], targetNormH, t);
            }
        }

        data.SetHeights(minX, minZ, heights);
        EditorUtility.SetDirty(data);
    }

    /// <summary>
    /// Evaluates a 9-point full rectangular building pad across halfWidth and halfDepth,
    /// avoiding steep cliffs, shoreline water, and previously reserved footprints.
    /// </summary>
    public static bool FindFlatBuildingPad(
        Terrain terrain,
        float preferredX,
        float preferredZ,
        float searchRadius,
        float halfWidth,
        float halfDepth,
        float maxHeightSpread,
        float minHeight,
        List<PlacedFootprint> placed,
        out Vector3 centerPos,
        out float padWorldY)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            padWorldY = minHeight;
            centerPos = new Vector3(preferredX, padWorldY, preferredZ);
            if (placed != null)
                placed.Add(new PlacedFootprint(centerPos, Mathf.Sqrt(halfWidth * halfWidth + halfDepth * halfDepth) + 1.6f));
            return true;
        }

        float baseY = terrain.transform.position.y;
        TerrainData data = terrain.terrainData;
        float footprintRadius = Mathf.Sqrt(halfWidth * halfWidth + halfDepth * halfDepth) + 1.6f;

        if (EvaluateBuildingPad(terrain, data, baseY, preferredX, preferredZ, halfWidth, halfDepth, 22f, maxHeightSpread, minHeight, out _, out float max0, out float avg0))
        {
            float targetY = Mathf.Max(minHeight + 0.15f, Mathf.Lerp(avg0, max0, 0.35f));
            Vector3 c0 = new Vector3(preferredX, targetY, preferredZ);
            if (IsClearOfFootprints(c0, footprintRadius, placed))
            {
                padWorldY = targetY;
                centerPos = c0;
                if (placed != null) placed.Add(new PlacedFootprint(centerPos, footprintRadius));
                return true;
            }
        }

        for (float r = 2.5f; r <= searchRadius; r += 2.5f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (EvaluateBuildingPad(terrain, data, baseY, tx, tz, halfWidth, halfDepth, 22f, maxHeightSpread, minHeight, out _, out float maxS, out float avgS))
                {
                    float targetY = Mathf.Max(minHeight + 0.15f, Mathf.Lerp(avgS, maxS, 0.35f));
                    Vector3 cand = new Vector3(tx, targetY, tz);
                    if (IsClearOfFootprints(cand, footprintRadius, placed))
                    {
                        padWorldY = targetY;
                        centerPos = cand;
                        if (placed != null) placed.Add(new PlacedFootprint(centerPos, footprintRadius));
                        return true;
                    }
                }
            }
        }

        float relaxedMinH = Mathf.Max(0.85f, minHeight - 0.1f);
        for (float r = 2.5f; r <= searchRadius + 14f; r += 3.0f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (EvaluateBuildingPad(terrain, data, baseY, tx, tz, halfWidth, halfDepth, 28f, maxHeightSpread + 1.1f, relaxedMinH, out _, out float maxR, out float avgR))
                {
                    float targetY = Mathf.Max(relaxedMinH + 0.15f, Mathf.Lerp(avgR, maxR, 0.35f));
                    Vector3 cand = new Vector3(tx, targetY, tz);
                    if (IsClearOfFootprints(cand, footprintRadius, placed))
                    {
                        padWorldY = targetY;
                        centerPos = cand;
                        if (placed != null) placed.Add(new PlacedFootprint(centerPos, footprintRadius));
                        return true;
                    }
                }
            }
        }

        centerPos = Vector3.zero;
        padWorldY = minHeight;
        return false;
    }

    private static bool EvaluateBuildingPad(
        Terrain terrain,
        TerrainData data,
        float baseY,
        float cx,
        float cz,
        float halfW,
        float halfD,
        float maxSlope,
        float maxSpread,
        float minH,
        out float minY,
        out float maxY,
        out float avgY)
    {
        minY = float.MaxValue;
        maxY = float.MinValue;
        float sum = 0f;
        Vector3 origin = terrain.transform.position;

        Vector2[] offsets = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(-halfW, -halfD),
            new Vector2(halfW, -halfD),
            new Vector2(-halfW, halfD),
            new Vector2(halfW, halfD),
            new Vector2(-halfW, 0f),
            new Vector2(halfW, 0f),
            new Vector2(0f, -halfD),
            new Vector2(0f, halfD)
        };

        for (int i = 0; i < offsets.Length; i++)
        {
            float sx = cx + offsets[i].x;
            float sz = cz + offsets[i].y;
            float nx = (sx - origin.x) / data.size.x;
            float nz = (sz - origin.z) / data.size.z;
            if (nx < 0.08f || nx > 0.92f || nz < 0.08f || nz > 0.92f)
            {
                avgY = 0f;
                return false;
            }

            float h = terrain.SampleHeight(new Vector3(sx, 0f, sz)) + baseY;
            if (h < minH)
            {
                avgY = 0f;
                return false;
            }

            if (i < 5 && data.GetSteepness(nx, nz) >= maxSlope)
            {
                avgY = 0f;
                return false;
            }

            if (h < minY) minY = h;
            if (h > maxY) maxY = h;
            sum += h;
        }

        avgY = sum / offsets.Length;
        return (maxY - minY) <= maxSpread;
    }

    /// <summary>
    /// Grounds a multi-part building or shrine by its center terrain height so doors stay level
    /// while deep foundation skirts cover downward terrain slopes without hovering.
    /// </summary>
    public static void GroundBuildingWithFoundation(GameObject go, Terrain terrain, float sinkEmbed = 0.04f)
    {
        GroundBuildingWithFoundation(go, terrain, 2.2f, 2.2f, sinkEmbed);
    }

    /// <summary>
    /// Grounds a multi-part building across a 9-point rotated footprint using the highest terrain corner
    /// so doorways and walls never bury underground while deep foundation skirts seal any lower corner.
    /// </summary>
    public static void GroundBuildingWithFoundation(GameObject go, Terrain terrain, float halfWidth, float halfDepth, float sinkEmbed = 0.04f)
    {
        if (go == null || terrain == null || terrain.terrainData == null)
            return;

        Vector3 pos = go.transform.position;
        Vector3 right = go.transform.right;
        Vector3 forward = go.transform.forward;
        float baseY = terrain.transform.position.y;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;

        Vector2[] offsets = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(-halfWidth, -halfDepth),
            new Vector2(halfWidth, -halfDepth),
            new Vector2(-halfWidth, halfDepth),
            new Vector2(halfWidth, halfDepth),
            new Vector2(-halfWidth, 0f),
            new Vector2(halfWidth, 0f),
            new Vector2(0f, -halfDepth),
            new Vector2(0f, halfDepth)
        };

        float maxCornerY = float.MinValue;
        for (int i = 0; i < offsets.Length; i++)
        {
            Vector3 wp = pos + right * offsets[i].x + forward * offsets[i].y;
            float sx = Mathf.Clamp(wp.x, origin.x + 1f, origin.x + size.x - 1f);
            float sz = Mathf.Clamp(wp.z, origin.z + 1f, origin.z + size.z - 1f);
            float h = terrain.SampleHeight(new Vector3(sx, 0f, sz)) + baseY;
            if (h > maxCornerY)
                maxCornerY = h;
        }

        go.transform.position = new Vector3(pos.x, maxCornerY - sinkEmbed, pos.z);
    }

    /// <summary>
    /// Checks horizontal clearance against all previously reserved object footprints.
    /// </summary>
    public static bool IsClearOfFootprints(Vector3 pos, float radius, List<PlacedFootprint> placed)
    {
        if (placed == null)
            return true;

        for (int i = 0; i < placed.Count; i++)
        {
            float dx = pos.x - placed[i].position.x;
            float dz = pos.z - placed[i].position.z;
            float minDist = radius + placed[i].radius;
            if (dx * dx + dz * dz < minDist * minDist)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Finds a flat dry spot around preferredX/Z that also respects all placed footprints,
    /// then registers the chosen footprint into placed.
    /// </summary>
    public static bool ReserveFlatSpot(
        Terrain terrain,
        float preferredX,
        float preferredZ,
        float searchRadius,
        float footprintRadius,
        float maxSlope,
        float minHeight,
        List<PlacedFootprint> placed,
        out Vector3 result)
    {
        if (terrain == null || terrain.terrainData == null)
        {
            result = new Vector3(preferredX, minHeight, preferredZ);
            if (placed != null) placed.Add(new PlacedFootprint(result, footprintRadius));
            return true;
        }

        float baseY = terrain.transform.position.y;
        TerrainData data = terrain.terrainData;

        if (IsFlatAndDry(terrain, data, baseY, preferredX, preferredZ, maxSlope, minHeight, out float y0))
        {
            Vector3 c0 = new Vector3(preferredX, y0, preferredZ);
            if (IsClearOfFootprints(c0, footprintRadius, placed))
            {
                result = c0;
                if (placed != null) placed.Add(new PlacedFootprint(result, footprintRadius));
                return true;
            }
        }

        for (float r = 2.5f; r <= searchRadius; r += 2.5f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (IsFlatAndDry(terrain, data, baseY, tx, tz, maxSlope, minHeight, out float sy))
                {
                    Vector3 cand = new Vector3(tx, sy, tz);
                    if (IsClearOfFootprints(cand, footprintRadius, placed))
                    {
                        result = cand;
                        if (placed != null) placed.Add(new PlacedFootprint(result, footprintRadius));
                        return true;
                    }
                }
            }
        }

        // Relax slope slightly while strictly respecting footprints so objects never overlap
        for (float r = 2.5f; r <= searchRadius + 8f; r += 3.0f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (IsFlatAndDry(terrain, data, baseY, tx, tz, maxSlope + 8f, Mathf.Min(0.55f, minHeight), out float ry))
                {
                    Vector3 cand = new Vector3(tx, ry, tz);
                    if (IsClearOfFootprints(cand, footprintRadius, placed))
                    {
                        result = cand;
                        if (placed != null) placed.Add(new PlacedFootprint(result, footprintRadius));
                        return true;
                    }
                }
            }
        }

        float fbY = terrain.SampleHeight(new Vector3(preferredX, 0f, preferredZ)) + baseY;
        result = new Vector3(preferredX, Mathf.Max(0.55f, fbY), preferredZ);
        return false;
    }

    /// <summary>
    /// Finds a dry, reasonably flat point around preferredX/preferredZ using 5-point slope checks.
    /// </summary>
    public static Vector3 FindFlatWalkablePoint(
        Terrain terrain,
        float preferredX,
        float preferredZ,
        float searchRadius,
        float maxSlope = 18f,
        float minHeight = 0.85f)
    {
        if (terrain == null || terrain.terrainData == null)
            return new Vector3(preferredX, minHeight, preferredZ);

        float baseY = terrain.transform.position.y;
        TerrainData data = terrain.terrainData;

        if (IsFlatAndDry(terrain, data, baseY, preferredX, preferredZ, maxSlope, minHeight, out float y0))
            return new Vector3(preferredX, y0, preferredZ);

        for (float r = 1.5f; r <= searchRadius; r += 2.0f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (IsFlatAndDry(terrain, data, baseY, tx, tz, maxSlope, minHeight, out float sampleY))
                    return new Vector3(tx, sampleY, tz);
            }
        }

        // Relax slope slightly if terrain near target is rugged
        for (float r = 1.5f; r <= searchRadius; r += 2.5f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                if (IsFlatAndDry(terrain, data, baseY, tx, tz, 28f, 0.55f, out float relaxedY))
                    return new Vector3(tx, relaxedY, tz);
            }
        }

        float fbY = terrain.SampleHeight(new Vector3(preferredX, 0f, preferredZ)) + baseY;
        return new Vector3(preferredX, Mathf.Max(0.55f, fbY), preferredZ);
    }

    private static bool IsFlatAndDry(
        Terrain terrain,
        TerrainData data,
        float baseY,
        float worldX,
        float worldZ,
        float maxSlope,
        float minHeight,
        out float worldY)
    {
        Vector3 origin = terrain.transform.position;
        float nx = (worldX - origin.x) / data.size.x;
        float nz = (worldZ - origin.z) / data.size.z;
        worldY = terrain.SampleHeight(new Vector3(worldX, 0f, worldZ)) + baseY;

        if (nx < 0.06f || nx > 0.94f || nz < 0.06f || nz > 0.94f)
            return false;
        if (worldY < minHeight || data.GetSteepness(nx, nz) >= maxSlope)
            return false;

        Vector2[] checks = new Vector2[]
        {
            new Vector2(-2.2f, 0f),
            new Vector2(2.2f, 0f),
            new Vector2(0f, -2.2f),
            new Vector2(0f, 2.2f)
        };
        for (int i = 0; i < checks.Length; i++)
        {
            float cx = worldX + checks[i].x;
            float cz = worldZ + checks[i].y;
            float cnx = Mathf.Clamp01((cx - origin.x) / data.size.x);
            float cnz = Mathf.Clamp01((cz - origin.z) / data.size.z);
            float cy = terrain.SampleHeight(new Vector3(cx, 0f, cz)) + baseY;
            if (cy < minHeight - 0.3f || data.GetSteepness(cnx, cnz) >= maxSlope + 6f)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Finds a flat walkable terrain spot that is clear of solid non-terrain colliders
    /// and mountain overhangs so treasure chests and props never clip inside houses or rocks.
    /// </summary>
    public static Vector3 FindUnobstructedSpot(
        Terrain terrain,
        float preferredX,
        float preferredZ,
        float searchRadius,
        float clearanceRadius = 1.6f)
    {
        Physics.SyncTransforms();

        Vector3 first = FindFlatWalkablePoint(terrain, preferredX, preferredZ, searchRadius, 22f, 0.7f);
        if (IsLocalGroundLevel(terrain, first.x, first.z, 1.2f, 0.75f) && IsSpotClearOfObstacles(first, clearanceRadius))
            return first;

        for (float r = 2.5f; r <= searchRadius + 14f; r += 2.5f)
        {
            for (int deg = 0; deg < 360; deg += 30)
            {
                float rad = deg * Mathf.Deg2Rad;
                float tx = preferredX + Mathf.Cos(rad) * r;
                float tz = preferredZ + Mathf.Sin(rad) * r;
                Vector3 candidate = FindFlatWalkablePoint(terrain, tx, tz, 4f, 22f, 0.7f);
                if (IsLocalGroundLevel(terrain, candidate.x, candidate.z, 1.2f, 0.75f) && IsSpotClearOfObstacles(candidate, clearanceRadius))
                    return candidate;
            }
        }

        return first;
    }

    private static bool IsLocalGroundLevel(Terrain terrain, float worldX, float worldZ, float sampleRadius, float maxSpread)
    {
        if (terrain == null || terrain.terrainData == null)
            return true;

        float baseY = terrain.transform.position.y;
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float minY = float.MaxValue;
        float maxY = float.MinValue;
        Vector2[] pts = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(-sampleRadius, 0f),
            new Vector2(sampleRadius, 0f),
            new Vector2(0f, -sampleRadius),
            new Vector2(0f, sampleRadius)
        };
        for (int i = 0; i < pts.Length; i++)
        {
            float sx = Mathf.Clamp(worldX + pts[i].x, origin.x + 1f, origin.x + size.x - 1f);
            float sz = Mathf.Clamp(worldZ + pts[i].y, origin.z + 1f, origin.z + size.z - 1f);
            float h = terrain.SampleHeight(new Vector3(sx, 0f, sz)) + baseY;
            if (h < minY) minY = h;
            if (h > maxY) maxY = h;
        }
        return (maxY - minY) <= maxSpread;
    }

    /// <summary>
    /// Verifies that no solid collider occupies the target capsule or forms a cliff/mountain roof overhead.
    /// </summary>
    public static bool IsSpotClearOfObstacles(Vector3 worldPos, float clearanceRadius)
    {
        Collider[] hits = Physics.OverlapCapsule(
            worldPos + Vector3.up * 0.45f,
            worldPos + Vector3.up * 2.6f,
            clearanceRadius
        );
        for (int i = 0; i < hits.Length; i++)
        {
            Collider c = hits[i];
            if (c == null || c.isTrigger || c is TerrainCollider || c.gameObject.name == "OceanSurface")
                continue;
            return false;
        }

        RaycastHit[] overheadHits = Physics.RaycastAll(
            new Vector3(worldPos.x, worldPos.y + 120f, worldPos.z),
            Vector3.down,
            125f
        );
        for (int i = 0; i < overheadHits.Length; i++)
        {
            Collider c = overheadHits[i].collider;
            if (c == null || c.isTrigger || c is TerrainCollider || c.gameObject.name == "OceanSurface")
                continue;
            if (overheadHits[i].point.y > worldPos.y + 0.35f)
                return false;
        }

        return true;
    }

    /// <summary>
    /// Scans shoreline edges for a gentle beach spot backed by dry land and fronted by open water.
    /// Starts with the southern edge and falls back to East, West, and North if irregular.
    /// </summary>
    public static void FindClearWaterShorelineDock(Terrain terrain, out Vector3 dockPos, out Vector3 inlandDir)
    {
        inlandDir = Vector3.forward;
        if (terrain == null || terrain.terrainData == null)
        {
            dockPos = new Vector3(0f, 0.55f, 145f);
            return;
        }

        Vector3[] candidateInlandDirs = new Vector3[]
        {
            Vector3.forward,
            Vector3.left,
            Vector3.right,
            Vector3.back
        };

        float bestOverallScore = float.MaxValue;
        Vector3 bestPos = Vector3.zero;
        Vector3 bestDir = Vector3.forward;
        bool found = false;

        for (int d = 0; d < candidateInlandDirs.Length; d++)
        {
            Vector3 dir = candidateInlandDirs[d];
            if (TryFindEdgeDock(terrain, dir, out Vector3 candidatePos, out float score))
            {
                // Prefer South edge slightly so all 3 docks face the ocean corridor cleanly
                float edgeBias = d == 0 ? 0f : 15f;
                if (score + edgeBias < bestOverallScore)
                {
                    bestOverallScore = score + edgeBias;
                    bestPos = candidatePos;
                    bestDir = dir;
                    found = true;
                }
            }
        }

        if (found)
        {
            dockPos = bestPos;
            inlandDir = bestDir;
            return;
        }

        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        dockPos = new Vector3(origin.x + data.size.x * 0.5f, 0.55f, origin.z + data.size.z * 0.22f);
        inlandDir = Vector3.forward;
    }

    private static bool TryFindEdgeDock(Terrain terrain, Vector3 inlandDir, out Vector3 bestPos, out float bestScore)
    {
        TerrainData data = terrain.terrainData;
        Vector3 origin = terrain.transform.position;
        bestScore = float.MaxValue;
        bestPos = Vector3.zero;
        bool found = false;

        bool scanAlongX = Mathf.Abs(inlandDir.z) > 0.5f;
        float marchSign = scanAlongX ? Mathf.Sign(inlandDir.z) : Mathf.Sign(inlandDir.x);

        for (float along = 0.18f; along <= 0.82f; along += 0.025f)
        {
            for (float depth = 0.03f; depth <= 0.60f; depth += 0.015f)
            {
                float nx = scanAlongX ? along : (marchSign > 0f ? depth : 1f - depth);
                float nz = scanAlongX ? (marchSign > 0f ? depth : 1f - depth) : along;

                float wx = origin.x + nx * data.size.x;
                float wz = origin.z + nz * data.size.z;
                float wy = terrain.SampleHeight(new Vector3(wx, 0f, wz)) + origin.y;

                if (wy < -0.10f || wy > 0.55f)
                    continue;

                float slope = data.GetSteepness(nx, nz);
                if (slope >= 22f)
                    continue;

                Vector3 shorePt = new Vector3(wx, 0f, wz);
                float sea1 = terrain.SampleHeight(shorePt - inlandDir * 12f) + origin.y;
                float sea2 = terrain.SampleHeight(shorePt - inlandDir * 24f) + origin.y;
                float sea3 = terrain.SampleHeight(shorePt - inlandDir * 38f) + origin.y;
                float land1 = terrain.SampleHeight(shorePt + inlandDir * 10f) + origin.y;
                float land2 = terrain.SampleHeight(shorePt + inlandDir * 18f) + origin.y;

                if (sea1 < -0.45f && sea2 < -0.45f && sea3 < -0.45f && land1 >= 0.70f && land2 >= 0.85f)
                {
                    float centerDist = Mathf.Abs(along - 0.5f);
                    float score = centerDist * 22f + slope * 0.2f + Mathf.Abs(wy - 0.20f) * 4f;
                    if (score < bestScore)
                    {
                        bestScore = score;
                        bestPos = new Vector3(wx, 0f, wz);
                        found = true;
                    }
                    break;
                }
            }
        }

        return found;
    }

    /// <summary>
    /// Constructs a realistic 15m timber harbor pier jutting cleanly out over deep ocean water (deck Y = 1.05m),
    /// complete with seabed pilings, rope bollards, pierhead lanterns, railed beach gangplank, and cargo props.
    /// </summary>
    public static GameObject BuildRealisticHarborPier(
        Transform parent,
        string pierName,
        Vector3 shoreEdgePos,
        Vector3 inlandDir,
        Vector3 rightDir,
        Terrain terrain,
        Material deckTimberMat,
        Material trimMat,
        Material ropeMat,
        Material lanternGlowMat,
        string barrelPath,
        string cratePath,
        bool includeLanternLight,
        out Vector3 pierOuterTip,
        out Vector3 pierDeckCenter,
        out Vector3 beachEntryPos)
    {
        Quaternion dockRot = Quaternion.LookRotation(inlandDir, Vector3.up);
        Vector3 pierOrigin = shoreEdgePos - inlandDir * 4.5f;
        pierOrigin.y = 0f;

        GameObject pier = new GameObject(pierName);
        pier.transform.SetParent(parent, false);
        pier.transform.position = pierOrigin;
        pier.transform.rotation = dockRot;

        // Main walkable planked deck well above wave height
        CreatePierPart(pier.transform, "PierMainDeck", PrimitiveType.Cube, new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(5.4f, 0.24f, 15.0f), deckTimberMat, true);

        // Sturdy cross-beam ribs across deck (reduced draw-call footprint for smoother GPU performance)
        float[] ribZs = new float[] { -6.0f, -3.0f, 0f, 3.0f, 6.0f };
        for (int i = 0; i < ribZs.Length; i++)
            CreatePierPart(pier.transform, "DeckRib_" + (i + 1), PrimitiveType.Cube, new Vector3(0f, 1.18f, ribZs[i]), Vector3.zero, new Vector3(5.24f, 0.04f, 0.65f), deckTimberMat, false);

        // Timber side curbs & heavy under-deck stringers
        CreatePierPart(pier.transform, "PierCurb_L", PrimitiveType.Cube, new Vector3(-2.56f, 1.24f, 0f), Vector3.zero, new Vector3(0.28f, 0.16f, 15.0f), trimMat, false);
        CreatePierPart(pier.transform, "PierCurb_R", PrimitiveType.Cube, new Vector3(2.56f, 1.24f, 0f), Vector3.zero, new Vector3(0.28f, 0.16f, 15.0f), trimMat, false);
        CreatePierPart(pier.transform, "PierStringer_L", PrimitiveType.Cube, new Vector3(-1.8f, 0.78f, 0f), Vector3.zero, new Vector3(0.36f, 0.34f, 14.8f), trimMat, false);
        CreatePierPart(pier.transform, "PierStringer_R", PrimitiveType.Cube, new Vector3(1.8f, 0.78f, 0f), Vector3.zero, new Vector3(0.36f, 0.34f, 14.8f), trimMat, false);

        // 10 Seabed pilings with bollards, rope wraps & caps
        float[] zRows = new float[] { -7.0f, -3.5f, 0f, 3.5f, 7.0f };
        for (int r = 0; r < zRows.Length; r++)
        {
            for (int side = -1; side <= 1; side += 2)
            {
                float px = side * 2.45f;
                float pz = zRows[r];
                string tag = (side < 0 ? "L_" : "R_") + (r + 1);
                CreatePierPart(pier.transform, "PierPiling_" + tag, PrimitiveType.Cylinder, new Vector3(px, -0.52f, pz), Vector3.zero, new Vector3(0.34f, 2.48f, 0.34f), deckTimberMat, true);
                if (ropeMat != null)
                    CreatePierPart(pier.transform, "PierBollardRope_" + tag, PrimitiveType.Cylinder, new Vector3(px, 1.48f, pz), Vector3.zero, new Vector3(0.42f, 0.14f, 0.42f), ropeMat, false);
                if (trimMat != null)
                    CreatePierPart(pier.transform, "PierBollardCap_" + tag, PrimitiveType.Cylinder, new Vector3(px, 1.99f, pz), Vector3.zero, new Vector3(0.38f, 0.06f, 0.38f), trimMat, false);
            }
        }

        // Stretched mooring rope guard lines along both sides
        if (ropeMat != null)
        {
            CreatePierPart(pier.transform, "PierRopeLine_L", PrimitiveType.Cylinder, new Vector3(-2.45f, 1.56f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 7.0f, 0.08f), ropeMat, false);
            CreatePierPart(pier.transform, "PierRopeLine_R", PrimitiveType.Cylinder, new Vector3(2.45f, 1.56f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.08f, 7.0f, 0.08f), ropeMat, false);
        }

        // Twin Pierhead Harbor Lantern Posts on outer sea corners
        for (int side = -1; side <= 1; side += 2)
        {
            float lx = side * 2.45f;
            string sTag = side < 0 ? "L" : "R";
            CreatePierPart(pier.transform, "LanternGallows_" + sTag, PrimitiveType.Cube, new Vector3(lx, 2.65f, -7.0f), Vector3.zero, new Vector3(0.18f, 1.45f, 0.18f), deckTimberMat, false);
            CreatePierPart(pier.transform, "LanternArm_" + sTag, PrimitiveType.Cube, new Vector3(lx * 0.88f, 3.28f, -7.0f), Vector3.zero, new Vector3(0.75f, 0.12f, 0.12f), trimMat, false);
            CreatePierPart(pier.transform, "PierLantern_" + sTag, PrimitiveType.Cube, new Vector3(lx * 0.76f, 2.98f, -7.0f), Vector3.zero, new Vector3(0.28f, 0.36f, 0.28f), lanternGlowMat, false);
        }

        if (includeLanternLight)
        {
            GameObject lightGo = new GameObject("PierHeadWarmLight");
            lightGo.transform.SetParent(pier.transform, false);
            lightGo.transform.localPosition = new Vector3(0f, 3.1f, -6.5f);
            Light l = lightGo.AddComponent<Light>();
            l.type = LightType.Point;
            l.color = new Color(1f, 0.78f, 0.45f);
            l.intensity = 1.8f;
            l.range = 15f;
            l.shadows = LightShadows.None;
            FlickeringLight fl = lightGo.AddComponent<FlickeringLight>();
            fl.baseIntensity = 1.8f;
        }

        // Smooth walkable Beach Gangplank / Ramp onto dry beach plaza
        beachEntryPos = FindFlatWalkablePoint(
            terrain,
            shoreEdgePos.x + inlandDir.x * 11.5f,
            shoreEdgePos.z + inlandDir.z * 11.5f,
            8f,
            22f,
            0.55f
        );
        Vector3 rampStart = pier.transform.TransformPoint(new Vector3(0f, 1.05f, 7.2f));
        Vector3 rampEnd = beachEntryPos + Vector3.up * 0.04f;
        Vector3 rampMid = (rampStart + rampEnd) * 0.5f - Vector3.up * 0.04f;
        float rampLen = Vector3.Distance(rampStart, rampEnd) + 0.9f;

        GameObject rampGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        rampGo.name = "PierBeachGangplank";
        rampGo.transform.SetParent(pier.transform, false);
        rampGo.transform.position = rampMid;
        rampGo.transform.localScale = new Vector3(4.4f, 0.24f, rampLen);
        rampGo.transform.LookAt(rampEnd);
        if (deckTimberMat != null && rampGo.GetComponent<MeshRenderer>() != null)
            rampGo.GetComponent<MeshRenderer>().sharedMaterial = deckTimberMat;

        CreatePierPart(rampGo.transform, "GangplankRail_L", PrimitiveType.Cube, new Vector3(-0.48f, 1.35f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), trimMat, true);
        CreatePierPart(rampGo.transform, "GangplankRail_R", PrimitiveType.Cube, new Vector3(0.48f, 1.35f, 0f), Vector3.zero, new Vector3(0.05f, 1.8f, 1f), trimMat, true);

        // Cargo barrels & crates neatly tucked along left curb so central 3.4m walkway stays wide open
        float[] barrelZs = new float[] { 1.4f, -3.2f };
        for (int i = 0; i < barrelZs.Length; i++)
        {
            if (string.IsNullOrEmpty(barrelPath)) break;
            GameObject bp = AssetDatabase.LoadAssetAtPath<GameObject>(barrelPath);
            if (bp == null) break;
            GameObject b = (GameObject)PrefabUtility.InstantiatePrefab(bp, pier.transform);
            b.name = "PierBarrel_" + (i + 1);
            b.transform.localPosition = new Vector3(-1.82f, 1.17f, barrelZs[i]);
            b.transform.localRotation = Quaternion.Euler(0f, 22f * (i + 1), 0f);
            EnsureExactSolidColliders(b, false, false);
        }

        float[] crateZs = new float[] { 2.8f, -1.6f };
        for (int i = 0; i < crateZs.Length; i++)
        {
            if (string.IsNullOrEmpty(cratePath)) break;
            GameObject cp = AssetDatabase.LoadAssetAtPath<GameObject>(cratePath);
            if (cp == null) break;
            GameObject c = (GameObject)PrefabUtility.InstantiatePrefab(cp, pier.transform);
            c.name = "PierCrate_" + (i + 1);
            c.transform.localPosition = new Vector3(-1.82f, 1.17f, crateZs[i]);
            c.transform.localRotation = Quaternion.Euler(0f, -18f * (i + 1), 0f);
            EnsureExactSolidColliders(c, false, false);
        }

        Vector3 tipWorld = pier.transform.TransformPoint(new Vector3(0f, 0f, -7.5f));
        pierOuterTip = new Vector3(tipWorld.x, 0.6f, tipWorld.z);
        pierDeckCenter = pier.transform.TransformPoint(new Vector3(0f, 1.18f, 0f));

        return pier;
    }

    private static GameObject CreatePierPart(
        Transform parent,
        string name,
        PrimitiveType type,
        Vector3 localPos,
        Vector3 localEuler,
        Vector3 scale,
        Material mat,
        bool keepCollider)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
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

    /// <summary>
    /// Enforces exact solid colliders on houses, bridges, rocks, cliffs, and tree trunks
    /// while removing any oversized (>12k vert) MeshColliders that trigger PhysX warnings.
    /// </summary>
    public static void EnsureExactSolidColliders(GameObject root, bool isTree = false, bool isWalkableStructure = false)
    {
        if (root == null)
            return;

        // Clean any invalid or oversized MeshColliders first
        MeshCollider[] existingMeshCols = root.GetComponentsInChildren<MeshCollider>(true);
        for (int i = 0; i < existingMeshCols.Length; i++)
        {
            MeshCollider mc = existingMeshCols[i];
            if (mc == null)
                continue;
            if (mc.sharedMesh == null || mc.sharedMesh.vertexCount > MaxMeshColliderVerts || mc.sharedMesh.triangles.Length > 300000)
                Object.DestroyImmediate(mc);
        }

        if (isTree)
        {
            if (HasNonTriggerCollider(root))
                return;

            Bounds? localBounds = ComputeLocalRendererBounds(root);
            CapsuleCollider trunkCol = root.AddComponent<CapsuleCollider>();
            if (localBounds.HasValue)
            {
                Bounds b = localBounds.Value;
                float radius = Mathf.Clamp(Mathf.Min(b.size.x, b.size.z) * 0.14f, 0.34f, 0.75f);
                float height = Mathf.Clamp(b.size.y * 0.62f, 3.6f, 8.0f);
                trunkCol.radius = radius;
                trunkCol.height = height;
                trunkCol.center = new Vector3(b.center.x, b.min.y + height * 0.48f, b.center.z);
            }
            else
            {
                trunkCol.radius = 0.45f;
                trunkCol.height = 5.2f;
                trunkCol.center = new Vector3(0f, 2.4f, 0f);
            }
            return;
        }

        if (isWalkableStructure)
        {
            AddPerMeshColliders(root);
            if (!HasNonTriggerCollider(root))
                AddCombinedBoxCollider(root);
            return;
        }

        if (HasNonTriggerCollider(root))
            return;

        MeshFilter[] childMeshes = root.GetComponentsInChildren<MeshFilter>(true);
        if (childMeshes.Length > 12)
        {
            AddCombinedBoxCollider(root);
            return;
        }

        AddPerMeshColliders(root);
        if (!HasNonTriggerCollider(root))
            AddCombinedBoxCollider(root);
    }

    private static void AddPerMeshColliders(GameObject root)
    {
        MeshFilter[] filters = root.GetComponentsInChildren<MeshFilter>(true);
        for (int i = 0; i < filters.Length; i++)
        {
            MeshFilter mf = filters[i];
            if (mf == null || mf.sharedMesh == null)
                continue;

            MeshRenderer mr = mf.GetComponent<MeshRenderer>();
            if (mr == null)
                continue;

            if (HasNonTriggerColliderOnSelf(mf.gameObject))
                continue;

            if (mf.sharedMesh.vertexCount <= MaxMeshColliderVerts)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.sharedMesh = mf.sharedMesh;
            }
            else
            {
                BoxCollider bc = mf.gameObject.AddComponent<BoxCollider>();
                Bounds mb = mf.sharedMesh.bounds;
                bc.center = mb.center;
                bc.size = mb.size;
            }
        }
    }

    private static void AddCombinedBoxCollider(GameObject root)
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

        if (!hasBounds)
            return;

        Vector3 ls = root.transform.lossyScale;
        float sx = Mathf.Max(0.01f, Mathf.Abs(ls.x));
        float sy = Mathf.Max(0.01f, Mathf.Abs(ls.y));
        float sz = Mathf.Max(0.01f, Mathf.Abs(ls.z));

        BoxCollider box = root.AddComponent<BoxCollider>();
        box.center = root.transform.InverseTransformPoint(bounds.center);
        box.size = new Vector3(bounds.size.x / sx, bounds.size.y / sy, bounds.size.z / sz);
    }

    private static Bounds? ComputeLocalRendererBounds(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        Bounds? combined = null;
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer r = renderers[i];
            if (r == null || r is ParticleSystemRenderer || r is TrailRenderer)
                continue;

            Bounds wb = r.bounds;
            Vector3 c = root.transform.InverseTransformPoint(wb.center);
            Vector3 ls = root.transform.lossyScale;
            Vector3 size = new Vector3(
                wb.size.x / Mathf.Max(0.01f, Mathf.Abs(ls.x)),
                wb.size.y / Mathf.Max(0.01f, Mathf.Abs(ls.y)),
                wb.size.z / Mathf.Max(0.01f, Mathf.Abs(ls.z))
            );
            Bounds lb = new Bounds(c, size);
            if (!combined.HasValue)
                combined = lb;
            else
            {
                Bounds acc = combined.Value;
                acc.Encapsulate(lb);
                combined = acc;
            }
        }
        return combined;
    }

    private static bool HasNonTriggerCollider(GameObject root)
    {
        Collider[] cols = root.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && !cols[i].isTrigger)
                return true;
        }
        return false;
    }

    private static bool HasNonTriggerColliderOnSelf(GameObject go)
    {
        Collider[] cols = go.GetComponents<Collider>();
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null && !cols[i].isTrigger)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Clears stale baked lightmap references from imported scene objects so dynamic lighting renders cleanly.
    /// </summary>
    public static void ClearBakedLightmapData(GameObject root)
    {
        if (root == null)
            return;

        Renderer[] renderers = root.GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] == null)
                continue;
            renderers[i].lightmapIndex = -1;
            renderers[i].realtimeLightmapIndex = -1;
            renderers[i].lightmapScaleOffset = new Vector4(1f, 1f, 0f, 0f);
        }

        Transform[] transforms = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
                GameObjectUtility.SetStaticEditorFlags(transforms[i].gameObject, 0);
        }
    }

    /// <summary>
    /// Imports the handcrafted island Terrain + SceneAssets from PA_AncientVillageDay.unity into
    /// the target scene, shifts it vertically so PA_Water rests at OceanSurface Y = 0, strips stray FX/lights,
    /// clears baked lightmaps, levels house terrain pads when requested, grounds all buildings and props
    /// without floating or burying, assigns URP Terrain material, and enforces colliders on every prop.
    /// </summary>
    public static GameObject CopyPolyartAncientVillage(
        Scene targetScene,
        Transform targetParent,
        Vector3 horizontalCenterOffset,
        string terrainRename,
        out Terrain copiedTerrain,
        TerrainData sharedDiscoveryTerrainData = null,
        bool flattenHousePads = false)
    {
        copiedTerrain = null;

        GameObject villageRoot = new GameObject("PolyartAncientVillage");
        if (targetParent != null)
            villageRoot.transform.SetParent(targetParent, false);
        else
            SceneManager.MoveGameObjectToScene(villageRoot, targetScene);
        villageRoot.transform.localPosition = Vector3.zero;
        villageRoot.transform.localRotation = Quaternion.identity;

        Scene sourceScene = EditorSceneManager.OpenScene(PolyartScenePath, OpenSceneMode.Additive);
        try
        {
            float waterY = FindPolyartWaterY(sourceScene);
            float yShift = -waterY;
            Vector3 worldShift = new Vector3(horizontalCenterOffset.x, yShift, horizontalCenterOffset.z);

            GameObject[] srcRoots = sourceScene.GetRootGameObjects();
            GameObject terrainSrc = FindNamedGameObject(srcRoots, "Terrain");
            GameObject sceneAssetsSrc = FindNamedGameObject(srcRoots, "SceneAssets");

            if (terrainSrc != null)
            {
                GameObject terrainCopy = Object.Instantiate(terrainSrc, villageRoot.transform);
                terrainCopy.name = "PolyartTerrainRoot";
                terrainCopy.transform.position = terrainSrc.transform.position + worldShift;
                Terrain t = terrainCopy.GetComponentInChildren<Terrain>(true);
                if (t != null)
                {
                    t.gameObject.name = terrainRename;
                    if (sharedDiscoveryTerrainData != null)
                        t.terrainData = sharedDiscoveryTerrainData;
                    t.materialTemplate = GetOrCreateUrpTerrainMaterial();
                    TerrainCollider tc = t.GetComponent<TerrainCollider>();
                    if (tc == null)
                        tc = t.gameObject.AddComponent<TerrainCollider>();
                    tc.terrainData = t.terrainData;
                    SanitizeTerrainTreePrototypes(t, villageRoot.transform);
                    copiedTerrain = t;
                }
            }

            if (sceneAssetsSrc != null)
            {
                GameObject assetsGroup = new GameObject("SceneAssets");
                assetsGroup.transform.SetParent(villageRoot.transform, false);
                assetsGroup.transform.position = sceneAssetsSrc.transform.position + worldShift;

                for (int i = 0; i < sceneAssetsSrc.transform.childCount; i++)
                {
                    Transform child = sceneAssetsSrc.transform.GetChild(i);
                    if (IsExcludedPolyartGroup(child.name))
                        continue;

                    GameObject groupCopy = Object.Instantiate(child.gameObject, assetsGroup.transform);
                    groupCopy.name = child.name;
                    groupCopy.transform.position = child.position + worldShift;
                    groupCopy.transform.rotation = child.rotation;
                    groupCopy.transform.localScale = child.localScale;
                }
            }

            StripStrayComponentsAndLights(villageRoot);
            ClearBakedLightmapData(villageRoot);
            FixPolyartFloatingAndBuriedObjects(villageRoot, copiedTerrain, flattenHousePads);
            ApplyCollidersToPolyartHierarchy(villageRoot);
            Physics.SyncTransforms();
        }
        finally
        {
            if (sourceScene.IsValid() && sourceScene.isLoaded)
                EditorSceneManager.CloseScene(sourceScene, true);
        }

        return villageRoot;
    }

    private static float FindPolyartWaterY(Scene sourceScene)
    {
        GameObject[] roots = sourceScene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            Transform[] all = roots[i].GetComponentsInChildren<Transform>(true);
            for (int j = 0; j < all.Length; j++)
            {
                string n = all[j].name;
                if (n == "PA_Water" || n.StartsWith("PA_Water"))
                {
                    Renderer r = all[j].GetComponentInChildren<Renderer>(true);
                    float y = r != null ? r.bounds.center.y : all[j].position.y;
                    if (y >= 10f && y <= 40f)
                        return y;
                }
            }
        }
        return 23.5f;
    }

    private static GameObject FindNamedGameObject(GameObject[] roots, string targetName)
    {
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == targetName)
                return roots[i];
        }
        return null;
    }

    private static bool IsExcludedPolyartGroup(string name)
    {
        string lower = name.ToLowerInvariant();
        return lower == "sky" || lower == "water" || lower == "camera" ||
               lower.StartsWith("directional light") || lower.StartsWith("postprocessing") ||
               lower == "lights" || lower == "fx" || lower.Contains("firefx") ||
               lower == "main" || lower == "pa_mountains";
    }

    private static void StripStrayComponentsAndLights(GameObject root)
    {
        List<GameObject> toDestroy = new List<GameObject>();
        Transform[] all = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < all.Length; i++)
        {
            if (all[i] == null)
                continue;
            string n = all[i].name;
            if (n.Contains("PA_FireFX") || n.Contains("PostProcessing") || n == "Main" || n == "Camera")
                toDestroy.Add(all[i].gameObject);
        }
        for (int i = 0; i < toDestroy.Count; i++)
        {
            if (toDestroy[i] != null)
                Object.DestroyImmediate(toDestroy[i]);
        }

        Camera[] cams = root.GetComponentsInChildren<Camera>(true);
        for (int i = 0; i < cams.Length; i++)
            Object.DestroyImmediate(cams[i]);

        AudioListener[] listeners = root.GetComponentsInChildren<AudioListener>(true);
        for (int i = 0; i < listeners.Length; i++)
            Object.DestroyImmediate(listeners[i]);

        Light[] lights = root.GetComponentsInChildren<Light>(true);
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                Object.DestroyImmediate(lights[i]);
                continue;
            }
            lights[i].shadows = LightShadows.None;
            lights[i].intensity = Mathf.Min(lights[i].intensity, 1.8f);
            lights[i].range = Mathf.Min(lights[i].range, 14f);
        }

        Transform[] remaining = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < remaining.Length; i++)
        {
            if (remaining[i] != null)
                GameObjectUtility.RemoveMonoBehavioursWithMissingScript(remaining[i].gameObject);
        }
    }

    private static void ApplyCollidersToPolyartHierarchy(GameObject root)
    {
        Transform sceneAssets = root.transform.Find("SceneAssets");
        if (sceneAssets == null)
            return;

        Transform housesGroup = sceneAssets.Find("Houses");
        if (housesGroup != null)
        {
            for (int i = 0; i < housesGroup.childCount; i++)
            {
                Transform house = housesGroup.GetChild(i);
                if (house != null && !HasNonTriggerCollider(house.gameObject))
                    AddCombinedBoxCollider(house.gameObject);
            }
        }

        Transform treesGroup = sceneAssets.Find("Trees");
        if (treesGroup != null)
        {
            for (int i = 0; i < treesGroup.childCount; i++)
            {
                Transform child = treesGroup.GetChild(i);
                if (child == null)
                    continue;

                if (child.name == "Bamboo" || child.name == "Banana" || child.name == "Coconut")
                {
                    for (int j = 0; j < child.childCount; j++)
                        ApplyTreeColliderAndSway(child.GetChild(j));
                }
                else
                {
                    ApplyTreeColliderAndSway(child);
                }
            }
        }

        string[] staticGroups = new string[] { "Rocks", "Mountains", "Extra" };
        for (int g = 0; g < staticGroups.Length; g++)
        {
            Transform grp = sceneAssets.Find(staticGroups[g]);
            if (grp == null)
                continue;

            for (int i = 0; i < grp.childCount; i++)
            {
                Transform item = grp.GetChild(i);
                if (item == null)
                    continue;

                bool isBridge = item.name.IndexOf("Bridge", System.StringComparison.OrdinalIgnoreCase) >= 0;
                EnsureExactSolidColliders(item.gameObject, false, isBridge);
            }
        }
    }

    private static void ApplyTreeColliderAndSway(Transform item)
    {
        if (item == null)
            return;

        EnsureExactSolidColliders(item.gameObject, true, false);
        if (item.GetComponent<WindSway>() == null)
        {
            WindSway ws = item.gameObject.AddComponent<WindSway>();
            ws.swayAngle = 2.0f;
        }
    }

    private static void FixPolyartFloatingAndBuriedObjects(GameObject villageRoot, Terrain terrain, bool flattenHousePads)
    {
        if (villageRoot == null || terrain == null || terrain.terrainData == null)
            return;

        Transform sceneAssets = villageRoot.transform.Find("SceneAssets");
        if (sceneAssets == null)
            return;

        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float minTx = origin.x + 1f;
        float maxTx = origin.x + size.x - 1f;
        float minTz = origin.z + 1f;
        float maxTz = origin.z + size.z - 1f;

        // 1. Level terrain pads under every bamboo house and place its true bottom flush at the pad
        Transform housesGroup = sceneAssets.Find("Houses");
        if (housesGroup != null)
        {
            for (int i = 0; i < housesGroup.childCount; i++)
            {
                Transform house = housesGroup.GetChild(i);
                if (house == null)
                    continue;

                Transform oldPlinth = house.Find("HouseSupportPlinth");
                if (oldPlinth != null)
                    Object.DestroyImmediate(oldPlinth.gameObject);

                Bounds? wb = ComputeWorldRendererBounds(house.gameObject);
                if (!wb.HasValue)
                    continue;

                Bounds b = wb.Value;
                float padHalfW = Mathf.Clamp(b.extents.x, 2.5f, 6.5f);
                float padHalfD = Mathf.Clamp(b.extents.z, 2.5f, 6.5f);

                SampleFootprintStatsTerrain(terrain, b.center.x, b.center.z, padHalfW, padHalfD, out float minTerrainY, out float maxTerrainY, out float avgTerrainY);
                float padY = Mathf.Max(1.15f, avgTerrainY);

                if (flattenHousePads)
                    FlattenTerrainPad(terrain, new Vector3(b.center.x, padY, b.center.z), padHalfW, padHalfD, house.eulerAngles.y, 4.5f, padY);

                float exactCenterX = Mathf.Clamp(b.center.x, minTx, maxTx);
                float exactCenterZ = Mathf.Clamp(b.center.z, minTz, maxTz);
                float exactGroundY = terrain.SampleHeight(new Vector3(exactCenterX, 0f, exactCenterZ)) + origin.y;
                house.position += Vector3.up * (exactGroundY - b.min.y - 0.04f);
            }
        }

        // 2. Re-ground all direct Trees, Rocks, and Extra props AFTER house pad flattening
        Transform treesGroup = sceneAssets.Find("Trees");
        if (treesGroup != null)
        {
            for (int i = 0; i < treesGroup.childCount; i++)
            {
                Transform child = treesGroup.GetChild(i);
                if (child == null)
                    continue;

                if (child.name == "Bamboo" || child.name == "Banana" || child.name == "Coconut")
                {
                    for (int j = 0; j < child.childCount; j++)
                        GroundPolyartDirectObject(child.GetChild(j), terrain, origin, minTx, maxTx, minTz, maxTz, 0.14f, true);
                }
                else
                {
                    GroundPolyartDirectObject(child, terrain, origin, minTx, maxTx, minTz, maxTz, 0.14f, true);
                }
            }
        }

        Transform rocksGroup = sceneAssets.Find("Rocks");
        if (rocksGroup != null)
        {
            for (int i = 0; i < rocksGroup.childCount; i++)
                GroundPolyartDirectObject(rocksGroup.GetChild(i), terrain, origin, minTx, maxTx, minTz, maxTz, 0.22f, false);
        }

        Transform extraGroup = sceneAssets.Find("Extra");
        if (extraGroup != null)
        {
            for (int i = 0; i < extraGroup.childCount; i++)
                GroundPolyartDirectObject(extraGroup.GetChild(i), terrain, origin, minTx, maxTx, minTz, maxTz, 0.04f, false);
        }
    }

    private static void GroundPolyartDirectObject(
        Transform node,
        Terrain terrain,
        Vector3 origin,
        float minTx,
        float maxTx,
        float minTz,
        float maxTz,
        float sinkEmbed,
        bool useRootCenterOnly)
    {
        if (node == null)
            return;

        Bounds? wb = ComputeWorldRendererBounds(node.gameObject);
        if (!wb.HasValue)
            return;

        Bounds b = wb.Value;
        float sx = Mathf.Clamp(useRootCenterOnly ? node.position.x : b.center.x, minTx, maxTx);
        float sz = Mathf.Clamp(useRootCenterOnly ? node.position.z : b.center.z, minTz, maxTz);
        float centerTerrainY = terrain.SampleHeight(new Vector3(sx, 0f, sz)) + origin.y;

        if (centerTerrainY < -0.25f)
            return;

        float targetY;
        if (useRootCenterOnly)
        {
            targetY = centerTerrainY;
        }
        else
        {
            float ex = Mathf.Min(b.extents.x, 1.5f);
            float ez = Mathf.Min(b.extents.z, 1.5f);
            float minCornerY = centerTerrainY;
            Vector2[] offsets = new Vector2[]
            {
                new Vector2(-ex, -ez),
                new Vector2(-ex, ez),
                new Vector2(ex, -ez),
                new Vector2(ex, ez)
            };
            for (int i = 0; i < offsets.Length; i++)
            {
                float px = Mathf.Clamp(b.center.x + offsets[i].x, minTx, maxTx);
                float pz = Mathf.Clamp(b.center.z + offsets[i].y, minTz, maxTz);
                float py = terrain.SampleHeight(new Vector3(px, 0f, pz)) + origin.y;
                if (py < minCornerY)
                    minCornerY = py;
            }
            targetY = minCornerY;
        }

        node.position += Vector3.up * (targetY - b.min.y - sinkEmbed);
    }

    private static void SampleFootprintStatsTerrain(
        Terrain terrain,
        float cx,
        float cz,
        float halfW,
        float halfD,
        out float minTerrainY,
        out float maxTerrainY,
        out float avgTerrainY)
    {
        Vector3 origin = terrain.transform.position;
        Vector3 size = terrain.terrainData.size;
        float minTx = origin.x + 1f;
        float maxTx = origin.x + size.x - 1f;
        float minTz = origin.z + 1f;
        float maxTz = origin.z + size.z - 1f;

        minTerrainY = float.MaxValue;
        maxTerrainY = float.MinValue;
        float sum = 0f;
        Vector2[] offsets = new Vector2[]
        {
            new Vector2(0f, 0f),
            new Vector2(-halfW, -halfD),
            new Vector2(halfW, -halfD),
            new Vector2(-halfW, halfD),
            new Vector2(halfW, halfD),
            new Vector2(-halfW, 0f),
            new Vector2(halfW, 0f),
            new Vector2(0f, -halfD),
            new Vector2(0f, halfD)
        };
        for (int i = 0; i < offsets.Length; i++)
        {
            float sx = Mathf.Clamp(cx + offsets[i].x, minTx, maxTx);
            float sz = Mathf.Clamp(cz + offsets[i].y, minTz, maxTz);
            float h = terrain.SampleHeight(new Vector3(sx, 0f, sz)) + origin.y;
            if (h < minTerrainY) minTerrainY = h;
            if (h > maxTerrainY) maxTerrainY = h;
            sum += h;
        }
        avgTerrainY = sum / offsets.Length;
    }

    private static Bounds? ComputeWorldRendererBounds(GameObject root)
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

    /// <summary>
    /// Builds a stylized animated Island Guide NPC with solid CapsuleCollider, waving arm,
    /// floating quest indicator, and grounded placement on the target terrain.
    /// </summary>
    public static GameObject BuildIslandGuideNpc(
        Transform parent,
        Terrain terrain,
        Vector3 worldPos,
        float yawDegrees,
        int islandIndex,
        string npcName,
        string loreGreeting,
        string keyHint,
        int bonusCoins,
        Color tunicColor,
        Color hatColor)
    {
        GameObject npc = new GameObject("GuideNPC_" + npcName.Replace(" ", "_"));
        npc.transform.SetParent(parent, false);
        npc.transform.position = worldPos;
        npc.transform.rotation = Quaternion.Euler(0f, yawDegrees, 0f);

        CapsuleCollider col = npc.AddComponent<CapsuleCollider>();
        col.center = new Vector3(0f, 0.95f, 0f);
        col.radius = 0.42f;
        col.height = 1.9f;

        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");

        Material tunicMat = new Material(lit);
        if (tunicMat.HasProperty("_BaseColor")) tunicMat.SetColor("_BaseColor", tunicColor);
        if (tunicMat.HasProperty("_Color")) tunicMat.SetColor("_Color", tunicColor);

        Material hatMat = new Material(lit);
        if (hatMat.HasProperty("_BaseColor")) hatMat.SetColor("_BaseColor", hatColor);
        if (hatMat.HasProperty("_Color")) hatMat.SetColor("_Color", hatColor);

        Material skinMat = new Material(lit);
        Color skinCol = new Color(0.92f, 0.76f, 0.62f, 1f);
        if (skinMat.HasProperty("_BaseColor")) skinMat.SetColor("_BaseColor", skinCol);
        if (skinMat.HasProperty("_Color")) skinMat.SetColor("_Color", skinCol);

        Material goldMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/ChestGoldTrim.mat");

        CreateNpcPart(npc.transform, "Body", PrimitiveType.Capsule, new Vector3(0f, 0.85f, 0f), Vector3.zero, new Vector3(0.58f, 0.68f, 0.58f), tunicMat);
        CreateNpcPart(npc.transform, "Head", PrimitiveType.Sphere, new Vector3(0f, 1.66f, 0f), Vector3.zero, new Vector3(0.44f, 0.46f, 0.44f), skinMat);
        CreateNpcPart(npc.transform, "HatBrim", PrimitiveType.Cylinder, new Vector3(0f, 1.88f, 0f), Vector3.zero, new Vector3(0.68f, 0.04f, 0.68f), hatMat);
        CreateNpcPart(npc.transform, "HatCrown", PrimitiveType.Cylinder, new Vector3(0f, 2.02f, 0f), Vector3.zero, new Vector3(0.42f, 0.14f, 0.42f), hatMat);
        CreateNpcPart(npc.transform, "LeftArm", PrimitiveType.Capsule, new Vector3(-0.38f, 0.96f, 0f), new Vector3(0f, 0f, 14f), new Vector3(0.18f, 0.34f, 0.18f), tunicMat);

        GameObject armPivot = new GameObject("RightArmPivot");
        armPivot.transform.SetParent(npc.transform, false);
        armPivot.transform.localPosition = new Vector3(0.34f, 1.24f, 0f);
        CreateNpcPart(armPivot.transform, "RightArm", PrimitiveType.Capsule, new Vector3(0.12f, -0.22f, 0f), new Vector3(0f, 0f, 22f), new Vector3(0.18f, 0.34f, 0.18f), tunicMat);

        GameObject marker = CreateNpcPart(npc.transform, "QuestMarker", PrimitiveType.Cube, new Vector3(0f, 2.52f, 0f), new Vector3(45f, 0f, 45f), new Vector3(0.24f, 0.24f, 0.24f), goldMat != null ? goldMat : hatMat);

        IslandGuideNPC npcScript = npc.AddComponent<IslandGuideNPC>();
        npcScript.islandIndex = islandIndex;
        npcScript.npcName = npcName;
        npcScript.loreGreeting = loreGreeting;
        npcScript.keyHint = keyHint;
        npcScript.bonusCoins = bonusCoins;
        npcScript.wavingArm = armPivot.transform;
        npcScript.questMarker = marker.transform;

        if (terrain != null)
            GroundObjectExactToTerrain(npc, terrain, 0.04f, true);

        return npc;
    }

    private static GameObject CreateNpcPart(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;

        Collider c = go.GetComponent<Collider>();
        if (c != null)
            Object.DestroyImmediate(c);

        if (mat != null && go.GetComponent<Renderer>() != null)
            go.GetComponent<Renderer>().sharedMaterial = mat;

        return go;
    }
}
