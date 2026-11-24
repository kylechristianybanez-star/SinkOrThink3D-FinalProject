using UnityEngine;

/// <summary>
/// Animates Viking banners and coastal flags gently rippling in the wind.
/// </summary>
public class FlagWaveAnimator : MonoBehaviour
{
    public float waveAngle = 14f;
    public float waveSpeed = 3.2f;
    public float flutterAngle = 5f;

    private Quaternion baseLocalRotation;
    private float phaseOffset;

    void Start()
    {
        baseLocalRotation = transform.localRotation;
        phaseOffset = (transform.position.x * 0.41f) + (transform.position.z * 0.29f);
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        float t = Time.time * waveSpeed + phaseOffset;
        float yaw = Mathf.Sin(t) * waveAngle;
        float pitch = Mathf.Cos(t * 1.7f) * flutterAngle;
        transform.localRotation = baseLocalRotation * Quaternion.Euler(pitch, yaw, 0f);
    }
}
