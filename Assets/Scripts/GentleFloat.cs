using UnityEngine;

/// <summary>
/// Gently bobs and rotates decorative objects (like buoys or floating items) on the water.
/// </summary>
public class GentleFloat : MonoBehaviour
{
    public float bobAmplitude = 0.12f;
    public float bobSpeed = 1.8f;
    public float tiltAngle = 6f;
    public float spinSpeed = 0f;

    private Vector3 startLocalPosition;
    private Quaternion startLocalRotation;
    private float phaseOffset;

    void Start()
    {
        startLocalPosition = transform.localPosition;
        startLocalRotation = transform.localRotation;
        phaseOffset = (transform.position.x + transform.position.z) * 0.25f;
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        float t = Time.time * bobSpeed + phaseOffset;
        Vector3 pos = startLocalPosition;
        pos.y += Mathf.Sin(t) * bobAmplitude;
        transform.localPosition = pos;

        float tiltX = Mathf.Sin(t * 0.8f) * tiltAngle;
        float tiltZ = Mathf.Cos(t * 0.7f) * tiltAngle;
        Quaternion tilt = Quaternion.Euler(tiltX, spinSpeed * Time.time, tiltZ);
        transform.localRotation = startLocalRotation * tilt;
    }
}
