using UnityEngine;

/// <summary>
/// Gently patrols shoreline crabs back and forth along the beach.
/// </summary>
public class BeachCrabWander : MonoBehaviour
{
    public float wanderRadius = 2.5f;
    public float moveSpeed = 0.85f;
    public float pauseDuration = 1.4f;

    private Vector3 homePosition;
    private Vector3 targetPosition;
    private float pauseTimer = 0f;

    void Start()
    {
        homePosition = transform.position;
        PickNextTarget();
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (pauseTimer > 0f)
        {
            pauseTimer -= Time.deltaTime;
            return;
        }

        Vector3 offset = targetPosition - transform.position;
        offset.y = 0f;

        if (offset.magnitude < 0.15f)
        {
            pauseTimer = pauseDuration;
            PickNextTarget();
            return;
        }

        Vector3 dir = offset.normalized;
        transform.position += dir * moveSpeed * Time.deltaTime;
        Quaternion targetRot = Quaternion.LookRotation(dir, Vector3.up);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 6f);
    }

    private void PickNextTarget()
    {
        Vector2 randomCircle = Random.insideUnitCircle * wanderRadius;
        targetPosition = homePosition + new Vector3(randomCircle.x, 0f, randomCircle.y);
    }
}
