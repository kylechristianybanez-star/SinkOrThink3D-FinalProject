using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Spawns interactive floating pickups (Coin Barrels, Driftwood Planks, Speed-Boost Currents)
/// and dodgeable sea hazards (Sea Reefs & Storm Buoys) in open ocean channels during sailing.
/// Uses proximity checks rather than blocking colliders so the boat never gets wedged.
/// </summary>
public class OceanGameplaySpawner : MonoBehaviour
{
    public static OceanGameplaySpawner Instance { get; private set; }

    private enum OceanItemKind
    {
        CoinBarrel,
        DriftwoodPlank,
        SpeedCurrent,
        ReefHazard
    }

    private class OceanNode
    {
        public GameObject root;
        public OceanItemKind kind;
        public float baseY;
        public float phase;
        public float cooldownUntil;
        public bool collected;
    }

    private readonly List<OceanNode> nodes = new List<OceanNode>();
    private BoatMovement boatMovement;
    private BoatSink boatSink;
    private QuizManager quizManager;
    private Material goldMat;
    private Material woodMat;
    private Material boostMat;
    private Material hazardMat;
    private Material rockMat;

    public static OceanGameplaySpawner EnsureInScene()
    {
        if (Instance != null)
            return Instance;

        OceanGameplaySpawner existing = FindAnyObjectByType<OceanGameplaySpawner>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("OceanGameplaySpawner");
        Instance = go.AddComponent<OceanGameplaySpawner>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        quizManager = FindAnyObjectByType<QuizManager>();
        GameObject boatObj = GameObject.Find("OldWoodenRowboat_Complete");
        if (boatObj != null)
        {
            boatMovement = boatObj.GetComponent<BoatMovement>();
            boatSink = boatObj.GetComponent<BoatSink>();
        }

        CreateMaterials();
        SpawnOceanItems();
    }

    void Update()
    {
        if (Time.timeScale == 0f || boatMovement == null)
            return;

        if (boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0))
            return;

        bool quizPaused = quizManager != null && quizManager.IsQuizActive;
        Vector3 boatPos = boatMovement.transform.position;

        for (int i = 0; i < nodes.Count; i++)
        {
            OceanNode n = nodes[i];
            if (n.collected || n.root == null)
                continue;

            float yBob = Mathf.Sin(Time.time * 1.9f + n.phase) * 0.14f;
            Vector3 p = n.root.transform.position;
            n.root.transform.position = new Vector3(p.x, n.baseY + yBob, p.z);

            if (n.kind == OceanItemKind.SpeedCurrent)
                n.root.transform.Rotate(0f, 55f * Time.deltaTime, 0f);
            else if (n.kind == OceanItemKind.CoinBarrel || n.kind == OceanItemKind.DriftwoodPlank)
                n.root.transform.Rotate(0f, 22f * Time.deltaTime, 0f);

            if (quizPaused)
                continue;

            float dx = boatPos.x - p.x;
            float dz = boatPos.z - p.z;
            float distSq = dx * dx + dz * dz;

            if (n.kind == OceanItemKind.ReefHazard)
            {
                if (distSq <= 14.5f && Time.time >= n.cooldownUntil)
                {
                    n.cooldownUntil = Time.time + 3.0f;
                    GameAudioManager.EnsureInstance().PlaySplash();
                    if (boatSink != null)
                    {
                        boatSink.ApplyDamage(10);
                        if (boatSink.currentHealth <= 0 && quizManager != null)
                        {
                            quizManager.TriggerGameOverExternal("Your boat struck too many ocean hazards!");
                            return;
                        }
                    }
                    ScreenFader.EnsureInstance().ShowArrivalBanner("Struck a Sea Hazard! -10 Boat HP", 2.0f);
                }
            }
            else if (distSq <= 21.0f)
            {
                CollectPickup(n);
            }
        }
    }

    private void CollectPickup(OceanNode node)
    {
        node.collected = true;
        GameStateManager state = GameStateManager.EnsureInstance();

        if (node.kind == OceanItemKind.CoinBarrel)
        {
            state.coins += 8;
            state.NotifyStateChanged();
            GameAudioManager.EnsureInstance().PlayTreasureCollect();
            ScreenFader.EnsureInstance().ShowArrivalBanner("Floating Barrel Collected: +8 Coins!", 2.0f);
        }
        else if (node.kind == OceanItemKind.DriftwoodPlank)
        {
            state.planks += 1;
            state.NotifyStateChanged();
            GameAudioManager.EnsureInstance().PlayTreasureCollect();
            ScreenFader.EnsureInstance().ShowArrivalBanner("Salvaged Driftwood: +1 Wooden Plank!", 2.0f);
        }
        else if (node.kind == OceanItemKind.SpeedCurrent)
        {
            if (boatMovement != null)
                boatMovement.ApplySpeedBoost(1.42f, 6.0f);
            GameAudioManager.EnsureInstance().PlaySpeedBoost();
            ScreenFader.EnsureInstance().ShowArrivalBanner("Caught Tailwind Current! Speed Boost (6s)", 2.2f);
        }

        if (node.root != null)
            node.root.SetActive(false);
    }

    private void SpawnOceanItems()
    {
        Terrain[] terrains = FindObjectsByType<Terrain>();
        BoatDocking[] docks = FindObjectsByType<BoatDocking>();

        Vector3 boatStart = boatMovement != null ? boatMovement.transform.position : Vector3.zero;
        List<Vector2> reserved = new List<Vector2>();
        reserved.Add(new Vector2(boatStart.x, boatStart.z));

        for (int i = 0; i < docks.Length; i++)
        {
            if (docks[i] != null)
                reserved.Add(new Vector2(docks[i].transform.position.x, docks[i].transform.position.z));
        }

        // Compute ocean bounding corridor across all islands + boat
        float minX = boatStart.x - 160f;
        float maxX = boatStart.x + 160f;
        float minZ = boatStart.z - 60f;
        float maxZ = boatStart.z + 180f;

        for (int i = 0; i < docks.Length; i++)
        {
            if (docks[i] == null) continue;
            Vector3 dp = docks[i].transform.position;
            minX = Mathf.Min(minX, dp.x - 120f);
            maxX = Mathf.Max(maxX, dp.x + 120f);
            minZ = Mathf.Min(minZ, dp.z - 140f);
            maxZ = Mathf.Max(maxZ, dp.z + 80f);
        }

        System.Random rng = new System.Random(2026);
        OceanItemKind[] pickupCycle = new OceanItemKind[]
        {
            OceanItemKind.CoinBarrel,
            OceanItemKind.SpeedCurrent,
            OceanItemKind.DriftwoodPlank,
            OceanItemKind.CoinBarrel
        };

        int pickupsSpawned = 0;
        int hazardsSpawned = 0;

        for (int attempt = 0; attempt < 260 && (pickupsSpawned < 14 || hazardsSpawned < 12); attempt++)
        {
            float wx = Mathf.Lerp(minX, maxX, (float)rng.NextDouble());
            float wz = Mathf.Lerp(minZ, maxZ, (float)rng.NextDouble());
            Vector3 cand = new Vector3(wx, 0f, wz);

            if (!IsOpenDeepWater(cand, terrains, reserved, 22f))
                continue;

            reserved.Add(new Vector2(wx, wz));

            if (pickupsSpawned < 14)
            {
                OceanItemKind kind = pickupCycle[pickupsSpawned % pickupCycle.Length];
                SpawnNodeObject(kind, cand, pickupsSpawned + hazardsSpawned);
                pickupsSpawned++;
            }
            else
            {
                SpawnNodeObject(OceanItemKind.ReefHazard, cand, pickupsSpawned + hazardsSpawned);
                hazardsSpawned++;
            }
        }
    }

    private bool IsOpenDeepWater(Vector3 pos, Terrain[] terrains, List<Vector2> reserved, float minClearance)
    {
        for (int i = 0; i < reserved.Count; i++)
        {
            float dx = pos.x - reserved[i].x;
            float dz = pos.z - reserved[i].y;
            if (dx * dx + dz * dz < minClearance * minClearance)
                return false;
        }

        Vector2[] checks = new Vector2[]
        {
            Vector2.zero,
            new Vector2(-18f, 0f), new Vector2(18f, 0f),
            new Vector2(0f, -18f), new Vector2(0f, 18f)
        };

        for (int t = 0; t < terrains.Length; t++)
        {
            Terrain ter = terrains[t];
            if (ter == null || !ter.gameObject.activeInHierarchy || ter.terrainData == null)
                continue;

            Vector3 origin = ter.transform.position;
            Vector3 size = ter.terrainData.size;
            for (int c = 0; c < checks.Length; c++)
            {
                float sx = pos.x + checks[c].x;
                float sz = pos.z + checks[c].y;
                if (sx >= origin.x && sx <= origin.x + size.x && sz >= origin.z && sz <= origin.z + size.z)
                {
                    float h = ter.SampleHeight(new Vector3(sx, 0f, sz)) + origin.y;
                    if (h > -0.8f)
                        return false;
                }
            }
        }

        return true;
    }

    private void SpawnNodeObject(OceanItemKind kind, Vector3 pos, int idx)
    {
        GameObject root = new GameObject("Ocean_" + kind + "_" + idx);
        root.transform.SetParent(transform, false);
        root.transform.position = pos;

        float baseY = 0.08f;
        if (kind == OceanItemKind.CoinBarrel)
        {
            CreateSimplePart(root.transform, "BarrelBody", PrimitiveType.Cylinder, Vector3.up * 0.45f, Vector3.zero, new Vector3(1.1f, 0.55f, 1.1f), woodMat);
            CreateSimplePart(root.transform, "GoldBand", PrimitiveType.Cylinder, Vector3.up * 0.45f, Vector3.zero, new Vector3(1.18f, 0.14f, 1.18f), goldMat);
        }
        else if (kind == OceanItemKind.DriftwoodPlank)
        {
            baseY = 0.04f;
            CreateSimplePart(root.transform, "PlankA", PrimitiveType.Cube, new Vector3(0f, 0.14f, 0f), new Vector3(0f, 18f, 0f), new Vector3(1.9f, 0.16f, 0.52f), woodMat);
            CreateSimplePart(root.transform, "PlankB", PrimitiveType.Cube, new Vector3(0.2f, 0.24f, 0.12f), new Vector3(0f, -24f, 6f), new Vector3(1.6f, 0.14f, 0.44f), woodMat);
        }
        else if (kind == OceanItemKind.SpeedCurrent)
        {
            baseY = 0.25f;
            CreateSimplePart(root.transform, "WindRing", PrimitiveType.Cylinder, Vector3.up * 0.2f, Vector3.zero, new Vector3(2.8f, 0.06f, 2.8f), boostMat);
            CreateSimplePart(root.transform, "WindPillar", PrimitiveType.Cylinder, Vector3.up * 1.4f, Vector3.zero, new Vector3(0.45f, 1.4f, 0.45f), boostMat);
        }
        else
        {
            baseY = 0.05f;
            CreateSimplePart(root.transform, "ReefRock", PrimitiveType.Sphere, new Vector3(0f, 0.28f, 0f), new Vector3(10f, 25f, 0f), new Vector3(2.4f, 1.3f, 2.1f), rockMat);
            CreateSimplePart(root.transform, "WarningBuoy", PrimitiveType.Cylinder, new Vector3(0f, 1.1f, 0f), Vector3.zero, new Vector3(0.55f, 0.75f, 0.55f), hazardMat);
            CreateSimplePart(root.transform, "WarningTip", PrimitiveType.Sphere, new Vector3(0f, 1.95f, 0f), Vector3.zero, new Vector3(0.42f, 0.42f, 0.42f), hazardMat);
        }

        nodes.Add(new OceanNode
        {
            root = root,
            kind = kind,
            baseY = baseY,
            phase = idx * 0.85f,
            cooldownUntil = 0f,
            collected = false
        });
    }

    private static void CreateSimplePart(Transform parent, string name, PrimitiveType type, Vector3 pos, Vector3 euler, Vector3 scale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.name = name;
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = Quaternion.Euler(euler);
        go.transform.localScale = scale;

        Collider c = go.GetComponent<Collider>();
        if (c != null)
            Destroy(c);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            if (mat != null)
                mr.sharedMaterial = mat;
        }
    }

    private void CreateMaterials()
    {
        Shader lit = Shader.Find("Universal Render Pipeline/Lit");
        if (lit == null) lit = Shader.Find("Standard");

        woodMat = new Material(lit);
        if (woodMat.HasProperty("_BaseColor")) woodMat.SetColor("_BaseColor", new Color(0.58f, 0.38f, 0.2f, 1f));

        goldMat = new Material(lit);
        if (goldMat.HasProperty("_BaseColor")) goldMat.SetColor("_BaseColor", new Color(0.95f, 0.76f, 0.22f, 1f));
        if (goldMat.HasProperty("_Metallic")) goldMat.SetFloat("_Metallic", 0.8f);

        rockMat = new Material(lit);
        if (rockMat.HasProperty("_BaseColor")) rockMat.SetColor("_BaseColor", new Color(0.32f, 0.34f, 0.38f, 1f));

        Shader unlit = Shader.Find("Universal Render Pipeline/Unlit");
        if (unlit == null) unlit = lit;

        boostMat = new Material(unlit);
        if (boostMat.HasProperty("_BaseColor")) boostMat.SetColor("_BaseColor", new Color(0.24f, 0.92f, 0.88f, 1f));

        hazardMat = new Material(lit);
        if (hazardMat.HasProperty("_BaseColor")) hazardMat.SetColor("_BaseColor", new Color(0.86f, 0.18f, 0.14f, 1f));
        hazardMat.EnableKeyword("_EMISSION");
        if (hazardMat.HasProperty("_EmissionColor")) hazardMat.SetColor("_EmissionColor", new Color(0.8f, 0.15f, 0.1f, 1f) * 1.1f);
    }
}
