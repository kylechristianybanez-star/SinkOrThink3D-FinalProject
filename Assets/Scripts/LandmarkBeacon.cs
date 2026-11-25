using UnityEngine;

/// <summary>
/// Translucent vertical sky-beam marker for key island landmarks (Harbor Dock and Key-unlocked Special Chest).
/// Automatically hides itself if the linked chest requires a Golden Key not yet found or is already opened.
/// </summary>
public class LandmarkBeacon : MonoBehaviour
{
    public bool requireIslandKey = false;
    public int islandIndex = 0;
    public string linkedChestId = "";
    public float pulseSpeed = 2.4f;

    private Renderer beamRenderer;
    private Material beamMat;
    private Color baseColor = Color.white;
    private Vector3 baseScale;

    void Awake()
    {
        beamRenderer = GetComponent<Renderer>();
        if (beamRenderer != null)
        {
            beamMat = beamRenderer.material;
            if (beamMat != null)
            {
                if (beamMat.HasProperty("_BaseColor"))
                    baseColor = beamMat.GetColor("_BaseColor");
                else if (beamMat.HasProperty("_Color"))
                    baseColor = beamMat.GetColor("_Color");
            }
        }
        baseScale = transform.localScale;
    }

    void Update()
    {
        if (beamRenderer == null)
            return;

        bool visible = true;
        if (requireIslandKey)
        {
            GameStateManager state = GameStateManager.EnsureInstance();
            bool hasKey = islandIndex >= 0 && islandIndex < 3 && state.hasIslandKey[islandIndex];
            bool opened = !string.IsNullOrEmpty(linkedChestId) && state.IsChestOpened(linkedChestId);
            visible = hasKey && !opened;
        }

        if (beamRenderer.enabled != visible)
            beamRenderer.enabled = visible;

        if (!visible)
            return;

        float pulse = 0.78f + 0.22f * Mathf.Sin(Time.time * pulseSpeed);
        transform.localScale = new Vector3(baseScale.x * (0.9f + 0.1f * pulse), baseScale.y, baseScale.z * (0.9f + 0.1f * pulse));

        if (beamMat != null)
        {
            Color c = baseColor;
            c.a = baseColor.a * pulse;
            if (beamMat.HasProperty("_BaseColor")) beamMat.SetColor("_BaseColor", c);
            if (beamMat.HasProperty("_Color")) beamMat.SetColor("_Color", c);
        }
    }
}
