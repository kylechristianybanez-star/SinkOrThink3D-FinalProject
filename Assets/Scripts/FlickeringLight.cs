using UnityEngine;

/// <summary>
/// Adds gentle warm campfire/lantern flicker while ensuring decorative lights
/// do not cast heavy realtime shadows on the GPU.
/// </summary>
[RequireComponent(typeof(Light))]
public class FlickeringLight : MonoBehaviour
{
    public float baseIntensity = 2.2f;
    public float flickerRange = 0.45f;
    public float flickerSpeed = 7.5f;

    private Light targetLight;
    private float seed;

    void Awake()
    {
        targetLight = GetComponent<Light>();
        if (targetLight != null)
        {
            targetLight.shadows = LightShadows.None;
            if (baseIntensity <= 0f)
                baseIntensity = targetLight.intensity;
        }

        seed = (transform.position.x * 0.7f) + (transform.position.z * 1.3f);
    }

    void Update()
    {
        if (targetLight == null || Time.timeScale == 0f)
            return;

        float noise = Mathf.PerlinNoise(seed, Time.time * flickerSpeed);
        targetLight.intensity = baseIntensity + (noise - 0.5f) * 2f * flickerRange;
    }
}
