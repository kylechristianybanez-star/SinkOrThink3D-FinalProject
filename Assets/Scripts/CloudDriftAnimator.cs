using UnityEngine;

/// <summary>
/// Gently drifts child sky cloud clusters across the horizon and wraps them smoothly
/// so the ocean and island skies feel alive at minimal GPU/CPU cost.
/// </summary>
public class CloudDriftAnimator : MonoBehaviour
{
    public Vector3 driftVelocity = new Vector3(1.8f, 0f, 0.4f);
    public float wrapHalfSize = 650f;
    public Vector3 centerAnchor = Vector3.zero;

    void Update()
    {
        if (Time.timeScale == 0f || wrapHalfSize <= 0f)
            return;

        Vector3 step = driftVelocity * Time.deltaTime;
        float span = wrapHalfSize * 2f;

        for (int i = 0; i < transform.childCount; i++)
        {
            Transform cloud = transform.GetChild(i);
            Vector3 pos = cloud.position + step;

            if (pos.x > centerAnchor.x + wrapHalfSize)
                pos.x -= span;
            else if (pos.x < centerAnchor.x - wrapHalfSize)
                pos.x += span;

            if (pos.z > centerAnchor.z + wrapHalfSize)
                pos.z -= span;
            else if (pos.z < centerAnchor.z - wrapHalfSize)
                pos.z += span;

            cloud.position = pos;
        }
    }
}
