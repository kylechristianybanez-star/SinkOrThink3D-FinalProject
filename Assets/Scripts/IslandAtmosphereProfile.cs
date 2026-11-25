using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Applies distinct lighting and coastal fog settings when an island parent is activated
/// inside the reusable IslandScene (Discovery tropical, Viking fjord, Final ancient dusk).
/// </summary>
public class IslandAtmosphereProfile : MonoBehaviour
{
    public Color sunColor = new Color(1f, 0.95f, 0.86f);
    public float sunIntensity = 1.3f;
    public Vector3 sunEulerAngles = new Vector3(46f, -35f, 0f);

    public Color fogColor = new Color(0.67f, 0.83f, 0.93f, 1f);
    public float fogDensity = 0.0018f;
    public Color ambientSkyColor = new Color(0.66f, 0.78f, 0.92f, 1f);

    void OnEnable()
    {
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = fogColor;
        RenderSettings.fogDensity = fogDensity;

        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = ambientSkyColor;
        RenderSettings.ambientEquatorColor = Color.Lerp(ambientSkyColor, Color.white, 0.18f) * 0.85f;
        RenderSettings.ambientGroundColor = new Color(0.36f, 0.34f, 0.28f, 1f);

        Light[] lights = FindObjectsByType<Light>();
        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i].type == LightType.Directional)
            {
                RenderSettings.sun = lights[i];
                lights[i].color = sunColor;
                lights[i].intensity = sunIntensity;
                lights[i].transform.rotation = Quaternion.Euler(sunEulerAngles);
                break;
            }
        }
    }
}
