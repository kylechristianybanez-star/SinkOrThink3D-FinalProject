using UnityEngine;

/// <summary>
/// Animates cascading waterfall surfaces by scrolling texture UVs and subtly pulsing water foam brightness.
/// Uses a per-instance material so shared material assets on disk are not modified.
/// </summary>
public class WaterfallAnimator : MonoBehaviour
{
    public float scrollSpeedY = -0.85f;
    public float foamPulseSpeed = 2.4f;
    public float foamPulseAmount = 0.08f;
    public string texturePropertyName = "_BaseMap";

    private Renderer targetRenderer;
    private Material instanceMat;
    private Vector2 uvOffset = Vector2.zero;
    private Color baseColor = new Color(0.32f, 0.78f, 0.88f, 0.82f);
    private string activeTexProp = "_BaseMap";
    private string activeColorProp = "_BaseColor";

    void Start()
    {
        targetRenderer = GetComponent<Renderer>();
        if (targetRenderer == null)
            return;

        instanceMat = targetRenderer.material;
        if (instanceMat == null)
            return;

        if (instanceMat.HasProperty(texturePropertyName))
            activeTexProp = texturePropertyName;
        else if (instanceMat.HasProperty("_BaseMap"))
            activeTexProp = "_BaseMap";
        else if (instanceMat.HasProperty("_MainTex"))
            activeTexProp = "_MainTex";

        uvOffset = instanceMat.HasProperty(activeTexProp) ? instanceMat.GetTextureOffset(activeTexProp) : Vector2.zero;

        if (instanceMat.HasProperty("_BaseColor"))
        {
            activeColorProp = "_BaseColor";
            baseColor = instanceMat.GetColor("_BaseColor");
        }
        else if (instanceMat.HasProperty("_Color"))
        {
            activeColorProp = "_Color";
            baseColor = instanceMat.GetColor("_Color");
        }
    }

    void Update()
    {
        if (instanceMat == null)
            return;

        uvOffset.y = Mathf.Repeat(uvOffset.y + scrollSpeedY * Time.deltaTime, 1f);
        if (instanceMat.HasProperty(activeTexProp))
            instanceMat.SetTextureOffset(activeTexProp, uvOffset);

        float pulse = Mathf.Sin(Time.time * foamPulseSpeed) * foamPulseAmount;
        Color pulsed = new Color(
            Mathf.Clamp01(baseColor.r + pulse * 0.5f),
            Mathf.Clamp01(baseColor.g + pulse * 0.8f),
            Mathf.Clamp01(baseColor.b + pulse),
            Mathf.Clamp(baseColor.a + pulse * 0.4f, 0.45f, 0.98f)
        );

        if (instanceMat.HasProperty(activeColorProp))
            instanceMat.SetColor(activeColorProp, pulsed);
    }

    void OnDestroy()
    {
        if (instanceMat != null)
            Destroy(instanceMat);
    }
}
