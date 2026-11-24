using UnityEngine;

/// <summary>
/// Animates OceanSurface water texture scrolling and subtle sea swell
/// using lightweight URP-compatible material properties.
/// </summary>
public class OceanWaterAnimator : MonoBehaviour
{
    public Vector2 primaryWaveScrollSpeed = new Vector2(0.025f, 0.018f);
    public Vector2 secondaryWaveScrollSpeed = new Vector2(-0.014f, 0.022f);
    public float subtleVerticalSwell = 0.03f;
    public float swellSpeed = 0.9f;
    public bool animateTextureOffset = true;

    private Material waterMaterial;
    private float baseY;
    private Vector2 primaryOffset;
    private Vector2 secondaryOffset;
    private bool hasBaseMap = false;
    private bool hasMainTex = false;
    private bool hasBumpMap = false;

    void Start()
    {
        baseY = transform.position.y;

        MeshRenderer renderer = GetComponent<MeshRenderer>();
        if (renderer != null && renderer.material != null)
        {
            waterMaterial = renderer.material;
            hasBaseMap = waterMaterial.HasProperty("_BaseMap");
            hasMainTex = waterMaterial.HasProperty("_MainTex");
            hasBumpMap = waterMaterial.HasProperty("_BumpMap");

            if (hasBaseMap)
                primaryOffset = waterMaterial.GetTextureOffset("_BaseMap");
            else if (hasMainTex)
                primaryOffset = waterMaterial.GetTextureOffset("_MainTex");

            if (hasBumpMap)
                secondaryOffset = waterMaterial.GetTextureOffset("_BumpMap");
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (subtleVerticalSwell > 0f)
        {
            Vector3 pos = transform.position;
            pos.y = baseY + Mathf.Sin(Time.time * swellSpeed) * subtleVerticalSwell;
            transform.position = pos;
        }

        if (!animateTextureOffset || waterMaterial == null)
            return;

        primaryOffset += primaryWaveScrollSpeed * Time.deltaTime;
        primaryOffset.x %= 1f;
        primaryOffset.y %= 1f;

        if (hasBaseMap)
            waterMaterial.SetTextureOffset("_BaseMap", primaryOffset);
        else if (hasMainTex)
            waterMaterial.SetTextureOffset("_MainTex", primaryOffset);

        if (hasBumpMap)
        {
            secondaryOffset += secondaryWaveScrollSpeed * Time.deltaTime;
            secondaryOffset.x %= 1f;
            secondaryOffset.y %= 1f;
            waterMaterial.SetTextureOffset("_BumpMap", secondaryOffset);
        }
    }
}
