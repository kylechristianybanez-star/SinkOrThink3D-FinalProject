using UnityEngine;

/// <summary>
/// Dynamically blends ocean fog, ambient sky color, directional sunlight, and light sea mist
/// as the player sails between Discovery (Tropical), Viking (Nordic Mist), and Final (Twilight Dusk).
/// </summary>
public class OceanWeatherController : MonoBehaviour
{
    public static OceanWeatherController Instance { get; private set; }

    private Transform boatTransform;
    private Transform discoveryAnchor;
    private Transform vikingAnchor;
    private Transform finalAnchor;
    private Light mainSun;
    private ParticleSystem rainMistPs;

    public static OceanWeatherController EnsureInScene()
    {
        if (Instance != null)
            return Instance;

        OceanWeatherController existing = FindAnyObjectByType<OceanWeatherController>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("OceanWeatherController");
        Instance = go.AddComponent<OceanWeatherController>();
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
        GameObject boat = GameObject.Find("OldWoodenRowboat_Complete");
        if (boat != null)
            boatTransform = boat.transform;

        GameObject disc = GameObject.Find("DiscoveryIsland_MainTerrain");
        if (disc != null)
            discoveryAnchor = disc.transform;

        GameObject vik = GameObject.Find("VikingIsland_OverviewTerrain");
        if (vik != null)
            vikingAnchor = vik.transform;

        GameObject fin = GameObject.Find("FinalIsland_OverviewTerrain");
        if (fin != null)
            finalAnchor = fin.transform;

        Light[] lights = FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                mainSun = lights[i];
                break;
            }
        }

        if (boatTransform != null)
            CreateSeaMistParticles();
    }

    void Update()
    {
        if (boatTransform == null)
            return;

        Vector3 bp = boatTransform.position;
        float wDisc = ComputeProximityWeight(bp, discoveryAnchor, 1f);
        float wVik = ComputeProximityWeight(bp, vikingAnchor, 0f);
        float wFin = ComputeProximityWeight(bp, finalAnchor, 0f);

        float sum = Mathf.Max(0.0001f, wDisc + wVik + wFin);
        wDisc /= sum;
        wVik /= sum;
        wFin /= sum;

        Color fogDisc = new Color(0.67f, 0.83f, 0.93f, 1f);
        Color fogVik = new Color(0.54f, 0.65f, 0.76f, 1f);
        Color fogFin = new Color(0.42f, 0.40f, 0.56f, 1f);
        RenderSettings.fogColor = fogDisc * wDisc + fogVik * wVik + fogFin * wFin;
        RenderSettings.fogDensity = 0.0016f * wDisc + 0.0027f * wVik + 0.0034f * wFin;

        Color skyDisc = new Color(0.68f, 0.80f, 0.94f, 1f);
        Color skyVik = new Color(0.55f, 0.65f, 0.78f, 1f);
        Color skyFin = new Color(0.54f, 0.50f, 0.68f, 1f);
        RenderSettings.ambientSkyColor = skyDisc * wDisc + skyVik * wVik + skyFin * wFin;
        RenderSettings.ambientEquatorColor = (skyDisc * 0.85f) * wDisc + (skyVik * 0.85f) * wVik + (skyFin * 0.85f) * wFin;

        if (mainSun != null)
        {
            Color sunDisc = new Color(1f, 0.95f, 0.86f);
            Color sunVik = new Color(0.88f, 0.93f, 1f);
            Color sunFin = new Color(0.95f, 0.78f, 0.64f);
            mainSun.color = sunDisc * wDisc + sunVik * wVik + sunFin * wFin;
            mainSun.intensity = 1.32f * wDisc + 1.18f * wVik + 1.08f * wFin;
        }

        if (rainMistPs != null)
        {
            ParticleSystem.EmissionModule em = rainMistPs.emission;
            em.rateOverTime = Mathf.Lerp(0f, 38f, Mathf.Clamp01(wVik * 0.45f + wFin));
        }
    }

    private static float ComputeProximityWeight(Vector3 boatPos, Transform anchor, float fallbackWeight)
    {
        if (anchor == null)
            return fallbackWeight;

        float dist = Vector2.Distance(new Vector2(boatPos.x, boatPos.z), new Vector2(anchor.position.x + 180f, anchor.position.z + 180f));
        return 1f / (1f + Mathf.Pow(Mathf.Max(1f, dist) / 280f, 2f));
    }

    private void CreateSeaMistParticles()
    {
        GameObject go = new GameObject("OceanWeatherMistFX");
        go.transform.SetParent(boatTransform, false);
        go.transform.localPosition = new Vector3(0f, 11f, 12f);
        go.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);

        rainMistPs = go.AddComponent<ParticleSystem>();
        rainMistPs.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = rainMistPs.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = 1.2f;
        main.startSpeed = 12f;
        main.startSize = 0.08f;
        main.startColor = new Color(0.84f, 0.92f, 1f, 0.45f);
        main.maxParticles = 90;

        ParticleSystem.EmissionModule em = rainMistPs.emission;
        em.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = rainMistPs.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(24f, 24f, 1f);

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s != null)
            {
                Material mat = new Material(s);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.86f, 0.94f, 1f, 0.5f));
                pr.sharedMaterial = mat;
            }
        }

        rainMistPs.Play();
    }
}
