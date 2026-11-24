
using UnityEngine;

public class BoatDocking : MonoBehaviour
{
    public GameObject boat;
    public float dockingDistance = 8f;

    private bool isDocked = false;

    void Update()
    {
        if (boat == null || isDocked)
            return;

        float distance = Vector3.Distance(
            transform.position,
            boat.transform.position
        );

        if (distance <= dockingDistance)
        {
            if (Input.GetKeyDown(KeyCode.F))
            {
                isDocked = true;

                // Stop the boat movement.
                BoatMovement movement = boat.GetComponent<BoatMovement>();

                if (movement != null)
                {
                    movement.enabled = false;
                }

                // Stop the Rigidbody safely.
                Rigidbody rb = boat.GetComponent<Rigidbody>();

                if (rb != null)
                {
                    if (!rb.isKinematic)
                    {
                        rb.linearVelocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }

                    rb.isKinematic = true;
                }

                Debug.Log("Docking successful! Boat stopped.");
            }
        }
    }
}
