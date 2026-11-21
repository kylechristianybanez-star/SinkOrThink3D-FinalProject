
using UnityEngine;

public class BoatMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float turnSpeed = 60f;

    public bool canMove = true;

    void Update()
    {
        if (!canMove)
        {
            return;
        }

        float move = Input.GetAxis("Vertical");
        float turn = Input.GetAxis("Horizontal");

        transform.Translate(
            Vector3.forward * move * moveSpeed * Time.deltaTime
        );

        transform.Rotate(
            Vector3.up * turn * turnSpeed * Time.deltaTime
        );
    }
}
