using UnityEngine;

/// <summary>
/// Spawns an animated 3D treasure prop above an opened chest that rises, spins,
/// and flies smoothly toward the player camera to show what was collected.
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
        Shader litShader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        Material goldMat = CreateMat(litShader, new Color(1f, 0.82f, 0.2f), 0.9f, 0.72f, new Color(0.55f, 0.4f, 0.05f));
        Material woodMat = CreateMat(litShader, new Color(0.52f, 0.34f, 0.18f), 0f, 0.2f, Color.black);
        Material potionMat = CreateMat(litShader, new Color(0.22f, 0.92f, 0.48f), 0.1f, 0.85f, new Color(0.15f, 0.65f, 0.3f));
        Material redMat = CreateMat(litShader, new Color(0.88f, 0.18f, 0.18f), 0.2f, 0.5f, new Color(0.35f, 0.05f, 0.05f));

        switch (type)
        {
            case ChestRewardType.Coins:
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.12f, 0f, 0f), new Vector3(90f, 0f, 15f), new Vector3(0.26f, 0.04f, 0.26f), goldMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0.12f, 0.08f, 0.04f), new Vector3(90f, 25f, -10f), new Vector3(0.26f, 0.04f, 0.26f), goldMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.16f, -0.05f), new Vector3(90f, -15f, 5f), new Vector3(0.26f, 0.04f, 0.26f), goldMat);
                break;

            case ChestRewardType.Planks:
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0f, 0f), new Vector3(0f, 15f, 12f), new Vector3(0.7f, 0.09f, 0.22f), woodMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.11f, 0f), new Vector3(0f, -20f, -8f), new Vector3(0.7f, 0.09f, 0.22f), woodMat);
                break;

            case ChestRewardType.Potions:
                CreatePart(parent, PrimitiveType.Sphere, Vector3.zero, Vector3.zero, new Vector3(0.34f, 0.38f, 0.34f), potionMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.24f, 0f), Vector3.zero, new Vector3(0.13f, 0.12f, 0.13f), potionMat);
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(0f, 0.35f, 0f), Vector3.zero, new Vector3(0.11f, 0.06f, 0.11f), woodMat);
                break;

            case ChestRewardType.GoldenKey:
                CreatePart(parent, PrimitiveType.Cylinder, new Vector3(-0.22f, 0f, 0f), new Vector3(90f, 0f, 0f), new Vector3(0.26f, 0.05f, 0.26f), goldMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0.08f, 0f, 0f), Vector3.zero, new Vector3(0.46f, 0.07f, 0.07f), goldMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0.22f, -0.09f, 0f), Vector3.zero, new Vector3(0.06f, 0.14f, 0.06f), goldMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0.30f, -0.07f, 0f), Vector3.zero, new Vector3(0.06f, 0.10f, 0.06f), goldMat);
                break;

            case ChestRewardType.MagicCompass:
                CreatePart(parent, PrimitiveType.Cylinder, Vector3.zero, new Vector3(70f, 0f, 0f), new Vector3(0.42f, 0.07f, 0.42f), goldMat);
                CreatePart(parent, PrimitiveType.Cube, new Vector3(0f, 0.02f, -0.05f), new Vector3(-20f, 0f, 0f), new Vector3(0.06f, 0.32f, 0.04f), redMat);
                break;

            case ChestRewardType.GoldenCrown:
                CreatePart(parent, PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.48f, 0.14f, 0.48f), goldMat);
                for (int i = 0; i < 4; i++)
                {
                    float angle = i * 90f * Mathf.Deg2Rad;
                    Vector3 spirePos = new Vector3(Mathf.Cos(angle) * 0.2f, 0.22f, Mathf.Sin(angle) * 0.2f);
                    CreatePart(parent, PrimitiveType.Cube, spirePos, Vector3.zero, new Vector3(0.08f, 0.22f, 0.08f), goldMat);
                }
                CreatePart(parent, PrimitiveType.Sphere, new Vector3(0f, 0.08f, 0.24f), Vector3.zero, Vector3.one * 0.10f, redMat);
                break;
        }
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
