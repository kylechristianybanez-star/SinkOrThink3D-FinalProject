
using UnityEngine;

public class FirstPersonMouseLook : MonoBehaviour
{
    public float mouseSensitivity = 2f;

    private float verticalRotation = 0f;

    void Update()
    {
        // Hold right mouse button to look around.
        // This keeps the left mouse button free for quiz answers.
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            // Look left and right.
            transform.Rotate(0f, mouseX, 0f, Space.World);

            // Look up and down.
            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -70f, 70f);

            Vector3 angles = transform.localEulerAngles;
            angles.x = verticalRotation;
            transform.localEulerAngles = angles;
        }
    }
}
