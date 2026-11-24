using UnityEngine;

/// <summary>
/// Gently sways palm trees, bushes, and tall grass in a tropical breeze.
/// </summary>
public class WindSway : MonoBehaviour
{
    public float swayAngle = 2.8f;
    public float swaySpeed = 1.35f;
    public float secondaryAxisScale = 0.6f;

    private Quaternion baseLocalRotation;
    private float phaseOffset;

    void Start()
    {
        baseLocalRotation = transform.localRotation;
        phaseOffset = (transform.position.x * 0.37f) + (transform.position.z * 0.23f);
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        float t = Time.time * swaySpeed + phaseOffset;
        float swayX = Mathf.Sin(t) * swayAngle;
        float swayZ = Mathf.Cos(t * 0.85f) * swayAngle * secondaryAxisScale;

        transform.localRotation = baseLocalRotation * Quaternion.Euler(swayX, 0f, swayZ);
    }
}
