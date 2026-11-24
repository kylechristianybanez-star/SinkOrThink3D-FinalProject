using UnityEngine;

/// <summary>
/// Controls boat steering and forward/reverse travel using a kinematic Rigidbody.
/// </summary>
public class BoatMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float turnSpeed = 60f;
    public bool canMove = true;
    public float reverseSpeedMultiplier = 0.6f;

    private Rigidbody rb;
    private BoatSink boatSink;
    private float move;
    private float turn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boatSink = GetComponent<BoatSink>();
    }

    void Update()
    {
        if (!canMove || Time.timeScale == 0f || (boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0)))
        {
            move = 0f;
            turn = 0f;
            return;
        }

        move = Input.GetAxis("Vertical");
        turn = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (!canMove || rb == null || Time.timeScale == 0f)
            return;

        if (boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0))
            return;

        // Turn the boat left or right.
        Quaternion rotation = rb.rotation *
            Quaternion.Euler(
                0f,
                turn * turnSpeed * Time.fixedDeltaTime,
                0f
            );

        rb.MoveRotation(rotation);

        // Move the boat forward or slower in reverse.
        float speedMultiplier = move < 0f ? reverseSpeedMultiplier : 1f;
        Vector3 movement = transform.forward *
            (move * speedMultiplier) * moveSpeed * Time.fixedDeltaTime;

        // Check for solid obstacles (ignore trigger volumes) before moving.
        if (movement.sqrMagnitude > 0.000001f)
        {
            if (rb.SweepTest(
                movement.normalized,
                out RaycastHit hit,
                movement.magnitude + 0.01f,
                QueryTriggerInteraction.Ignore
            ))
            {
                return;
            }
        }

        rb.MovePosition(rb.position + movement);
    }

    public void StopBoat()
    {
        canMove = false;
        move = 0f;
        turn = 0f;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
