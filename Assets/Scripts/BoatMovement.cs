
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float turnSpeed = 60f;
    public bool canMove = true;

    private Rigidbody rb;
    private float move;
    private float turn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void Update()
    {
        if (!canMove)
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
        if (!canMove || rb == null)
            return;

        // Turn the boat left or right.
        Quaternion rotation = rb.rotation *
            Quaternion.Euler(
                0f,
                turn * turnSpeed * Time.fixedDeltaTime,
                0f
            );

        rb.MoveRotation(rotation);

        // Move the boat forward or backward.
        Vector3 movement = transform.forward *
            move * moveSpeed * Time.fixedDeltaTime;

        // Check for obstacles before moving.
        if (movement.sqrMagnitude > 0f)
        {
            if (rb.SweepTest(
                movement.normalized,
                out RaycastHit hit,
                movement.magnitude + 0.01f
            ))
            {
                return;
            }
        }

        rb.MovePosition(rb.position + movement);
    }
}
