using UnityEngine;

/// <summary>
/// Spawns a multi-part animated 3D treasure prop above an opened chest that rises,
/// spins, and flies smoothly toward the player camera to show what was collected.
/// </summary>
public class RewardRevealEffect : MonoBehaviour
{
    public Transform collectTarget;
    public float riseDuration = 1.15f;
    public float flyDuration = 0.65f;
    public float riseHeight = 0.95f;
    public float spinSpeed = 200f;

    private Vector3 startPos;
    private Vector3 peakPos;
    private Vector3 targetScale = Vector3.one;
    private float elapsed = 0f;

    public static void SpawnReveal(Vector3 spawnPosition, Transform collectTarget, ChestRewardType rewardType)
    {
        GameObject root = new GameObject("RewardReveal_" + rewardType);
        root.transform.position = spawnPosition;

        BuildVisualProp(root.transform, rewardType);

        RewardRevealEffect effect = root.AddComponent<RewardRevealEffect>();
        effect.collectTarget = collectTarget;

        if (rewardType == ChestRewardType.GoldenCrown)
        {
            effect.riseDuration = 1.65f;
            effect.flyDuration = 0.75f;
            effect.riseHeight = 1.25f;
            effect.spinSpeed = 160f;
            root.transform.localScale = Vector3.one * 1.45f;
            AttachSparkles(root.transform, new Color(1f, 0.88f, 0.25f, 1f), 28);
        }
        else if (rewardType == ChestRewardType.GoldenKey || rewardType == ChestRewardType.MagicCompass)
        {
            AttachSparkles(root.transform, new Color(1f, 0.92f, 0.45f, 1f), 16);
        }
    }

    private static void AttachSparkles(Transform parent, Color sparkleColor, int maxCount)
    {
        GameObject fxGo = new GameObject("RewardSparkleBurst");
        fxGo.transform.SetParent(parent, false);
        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = 0.75f;
        main.startSpeed = 1.4f;
        main.startSize = 0.08f;
        main.startColor = sparkleColor;
        main.maxParticles = maxCount;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 16f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.28f;

        ParticleSystemRenderer pr = fxGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader pShader = FindSafeShader("Universal Render Pipeline/Particles/Unlit", "Particles/Standard Unlit");
            if (pShader != null)
            {
                Material mat = new Material(pShader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", sparkleColor);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", sparkleColor);
                pr.sharedMaterial = mat;
            }
        }
    }

    void Start()
    {
        startPos = transform.position;
        peakPos = startPos + Vector3.up * riseHeight;
        targetScale = transform.localScale;
        transform.localScale = targetScale * 0.2f;
    }

    void Update()
    {
        elapsed += Time.deltaTime;
        transform.Rotate(0f, spinSpeed * Time.deltaTime, 0f, Space.World);

        if (elapsed <= riseDuration)
        {
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, riseDuration));
            float smooth = 1f - Mathf.Pow(1f - t, 3f);
            transform.position = Vector3.Lerp(startPos, peakPos, smooth);
            transform.localScale = Vector3.Lerp(targetScale * 0.2f, targetScale, smooth);
            return;
        }

        float flyElapsed = elapsed - riseDuration;
        if (flyElapsed <= flyDuration)
        {
            float t = Mathf.Clamp01(flyElapsed / Mathf.Max(0.01f, flyDuration));
            Vector3 dest = collectTarget != null
                ? collectTarget.position - Vector3.up * 0.35f
                : peakPos + Vector3.up * 0.8f;

            transform.position = Vector3.Lerp(peakPos, dest, t * t);
            transform.localScale = Vector3.Lerp(targetScale, targetScale * 0.12f, t);
            return;
        }

        Destroy(gameObject);
    }

    private static void BuildVisualProp(Transform parent, ChestRewardType type)
    {
        Shader litShader = FindSafeShader("Universal Render Pipeline/Lit", "Standard");
        Material goldMat = CreateMat(litShader, new Color(1f, 0.82f, 0.2f), 0.92f, 0.78f, new Color(0.45f, 0.32f, 0.04f));
        Material bronzeMat = CreateMat(litShader, new Color(0.84f, 0.62f, 0.18f), 0.85f, 0.62f, new Color(0.22f, 0.14f, 0.02f));
        Material woodMat = CreateMat(litShader, new Color(0.54f, 0.34f, 0.17f), 0f, 0.22f, Color.black);
        Material ironMat = CreateMat(litShader, new Color(0.28f, 0.3f, 0.34f), 0.85f, 0.55f, Color.black);
        Material glassMat = CreateMat(litShader, new Color(0.76f, 0.95f, 0.96f), 0.1f, 0.92f, new Color(0.08f, 0.22f, 0.24f));
        Material potionMat = CreateMat(litShader, new Color(0.18f, 0.92f, 0.45f), 0.1f, 0.85f, new Color(0.18f, 0.72f, 0.32f));
        Material parchmentMat = CreateMat(litShader, new Color(0.95f, 0.9f, 0.75f), 0f, 0.2f, Color.black);
        Material rubyMat = CreateMat(litShader, new Color(0.9f, 0.16f, 0.2f), 0.25f, 0.82f, new Color(0.45f, 0.05f, 0.06f));
        Material sapphireMat = CreateMat(litShader, new Color(0.16f, 0.48f, 0.95f), 0.25f, 0.82f, new Color(0.05f, 0.18f, 0.48f));

        switch (type)
        {
            case ChestRewardType.Coins:
                BuildCoinMesh(parent, new Vector3(-0.15f, 0f, 0f), new Vector3(90f, 0f, 14f), goldMat, bronzeMat);
                BuildCoinMesh(parent, new Vector3(0.15f, 0.09f, 0.05f), new Vector3(90f, 24f, -12f), goldMat, bronzeMat);
                BuildCoinMesh(parent, new Vector3(0f, 0.18f, -0.06f), new Vector3(90f, -18f, 6f), goldMat, bronzeMat);
                BuildCoinMesh(parent, new Vector3(0.05f, 0.28f, 0.03f), new Vector3(84f, 10f, -5f), goldMat, bronzeMat);
                break;

            case ChestRewardType.Planks:
                BuildPlankWithBands(parent, new Vector3(0f, -0.04f, 0f), new Vector3(0f, 16f, 8f), woodMat, ironMat);
                BuildPlankWithBands(parent, new Vector3(0f, 0.08f, 0.03f), new Vector3(0f, -18f, -6f), woodMat, ironMat);
                BuildPlankWithBands(parent, new Vector3(0f, 0.20f, -0.02f), new Vector3(4f, 4f, 12f), woodMat, ironMat);
                break;

            case ChestRewardType.Potions:
                // Ornate multi-tier glass potion flask with emerald elixir and cork
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, -0.14f, 0f), Vector3.zero, new Vector3(0.32f, 0.06f, 0.32f), bronzeMat);
                CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.04f, 0f), Vector3.zero, new Vector3(0.38f, 0.34f, 0.38f), potionMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.21f, 0f), Vector3.zero, new Vector3(0.18f, 0.07f, 0.18f), glassMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.30f, 0f), Vector3.zero, new Vector3(0.12f, 0.08f, 0.12f), glassMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.37f, 0f), Vector3.zero, new Vector3(0.16f, 0.025f, 0.16f), glassMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.42f, 0f), Vector3.zero, new Vector3(0.10f, 0.05f, 0.10f), woodMat);
                break;

            case ChestRewardType.GoldenKey:
                // Open ring bow built from 6 gold segments + center ruby + collar + fluted blade + teeth
                for (int i = 0; i < 6; i++)
                {
                    float a = i * 60f * Mathf.Deg2Rad;
                    Vector3 segPos = new Vector3(-0.24f + Mathf.Cos(a) * 0.11f, Mathf.Sin(a) * 0.11f, 0f);
                    CreatePart(parent, PrimitiveType.Cube, segPos, new Vector3(0f, 0f, i * 60f + 90f), new Vector3(0.05f, 0.12f, 0.05f), goldMat);
                }
                CreatePart(parent, PrimitiveType.Sphere, new Vector3(-0.24f, 0f, 0f), Vector3.zero, new Vector3(0.09f, 0.09f, 0.07f), rubyMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.08f, 0f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.09f, 0.03f, 0.09f), bronzeMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0.14f, 0f, 0f), new Vector3(0f, 0f, 90f), new Vector3(0.06f, 0.24f, 0.06f), goldMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0.24f, -0.09f, 0f), Vector3.zero, new Vector3(0.05f, 0.15f, 0.045f), goldMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0.32f, -0.07f, 0f), Vector3.zero, new Vector3(0.05f, 0.11f, 0.045f), goldMat);
                break;

            case ChestRewardType.MagicCompass:
                // Beveled brass case, dark bezel, parchment dial, two-tone needle, top loop
                CreatePart(parent, PrimitiveType.Cylinder, Vector3.zero, new Vector3(72f, 0f, 0f), new Vector3(0.46f, 0.055f, 0.46f), bronzeMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.01f, -0.015f), new Vector3(72f, 0f, 0f), new Vector3(0.40f, 0.058f, 0.40f), ironMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.014f, -0.022f), new Vector3(72f, 0f, 0f), new Vector3(0.35f, 0.06f, 0.35f), parchmentMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.06f, -0.07f), new Vector3(-18f, 0f, 0f), new Vector3(0.05f, 0.16f, 0.03f), rubyMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, -0.04f, -0.035f), new Vector3(-18f, 0f, 0f), new Vector3(0.04f, 0.14f, 0.03f), sapphireMat);
                CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.015f, -0.05f), Vector3.zero, Vector3.one * 0.07f, goldMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.25f, 0.07f), new Vector3(72f, 0f, 0f), new Vector3(0.11f, 0.03f, 0.11f), goldMat);
                break;

            case ChestRewardType.GoldenCrown:
                // Ornate gold band, crimson royal velvet cap, 6 jeweled spires
                CreatePart(parent, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.52f, 0.13f, 0.52f), goldMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.12f, 0f), Vector3.zero, new Vector3(0.46f, 0.03f, 0.46f), bronzeMat);
                CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.13f, 0f), Vector3.zero, new Vector3(0.40f, 0.24f, 0.40f), rubyMat);
                for (int i = 0; i < 6; i++)
                {
                    float angle = i * 60f * Mathf.Deg2Rad;
                    float r = 0.22f;
                    Vector3 spirePos = new Vector3(Mathf.Cos(angle) * r, 0.24f, Mathf.Sin(angle) * r);
                    CreatePart(parent, PrimitiveType.Cube, spirePos, new Vector3(0f, -i * 60f, 0f), new Vector3(0.075f, 0.24f, 0.06f), goldMat);
                    CreatePart(parent, PrimitiveType.Sphere, spirePos + Vector3.up * 0.14f, Vector3.zero, Vector3.one * 0.075f, goldMat);

                    Vector3 gemPos = new Vector3(Mathf.Cos(angle) * 0.265f, 0.02f, Mathf.Sin(angle) * 0.265f);
                    Material gemMat = (i % 3 == 0) ? rubyMat : (i % 3 == 1 ? sapphireMat : potionMat);
                    CreatePart(parent, PrimitiveType.Sphere, gemPos, Vector3.zero, Vector3.one * 0.08f, gemMat);
                }
                break;
        }
    }

    private static void BuildCoinMesh(Transform parent, Vector3 pos, Vector3 euler, Material goldMat, Material rimMat)
    {
        GameObject coin = new GameObject("Coin");
        coin.transform.SetParent(parent, false);
        coin.transform.localPosition = pos;
        coin.transform.localRotation = Quaternion.Euler(euler);
        CreatePart(coin.transform, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.28f, 0.028f, 0.28f), rimMat);
        CreatePart(coin.transform, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.22f, 0.036f, 0.22f), goldMat);
    }

    private static void BuildPlankWithBands(Transform parent, Vector3 pos, Vector3 euler, Material woodMat, Material ironMat)
    {
        GameObject plank = new GameObject("Plank");
        plank.transform.SetParent(parent, false);
        plank.transform.localPosition = pos;
        plank.transform.localRotation = Quaternion.Euler(euler);
        CreatePart(plank.transform, PrimitiveType.Cube, Vector3.zero, Vector3.zero, new Vector3(0.76f, 0.075f, 0.22f), woodMat);
        CreatePart(plank.transform, PrimitiveType.Cube, new Vector3(-0.26f, 0f, 0f), Vector3.zero, new Vector3(0.06f, 0.085f, 0.235f), ironMat);
        CreatePart(plank.transform, PrimitiveType.Cube, new Vector3(0.26f, 0f, 0f), Vector3.zero, new Vector3(0.06f, 0.085f, 0.235f), ironMat);
    }

    private static Shader FindSafeShader(string primary, string fallback)
    {
        Shader shader = Shader.Find(primary);
        if (shader == null)
            shader = Shader.Find(fallback);
        return shader;
    }

    private static void CreatePart(Transform parent, PrimitiveType type, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material mat)
    {
        GameObject go = GameObject.CreatePrimitive(type);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Destroy(col);

        MeshRenderer mr = go.GetComponent<MeshRenderer>();
        if (mr != null && mat != null)
            mr.sharedMaterial = mat;
    }

    private static Material CreateMat(Shader shader, Color baseColor, float metallic, float smoothness, Color emission)
    {
        Material mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", baseColor);
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", baseColor);
        if (mat.HasProperty("_Metallic")) mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", smoothness);
        if (emission != Color.black)
        {
            mat.EnableKeyword("_EMISSION");
            if (mat.HasProperty("_EmissionColor")) mat.SetColor("_EmissionColor", emission);
        }
        return mat;
    }
}
