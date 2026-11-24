using UnityEngine;

/// <summary>
/// Allows holding right mouse button to look around from the boat without blocking quiz clicks.
/// </summary>
public class FirstPersonMouseLook : MonoBehaviour
{
    public float mouseSensitivity = 2f;
    public bool clampHorizontal = false;
    public float maxHorizontalAngle = 110f;

    private float verticalRotation = 0f;
    private float horizontalRotation = 0f;

    void Awake()
    {
        SyncFromLocalRotation(transform.localRotation);
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        // Hold right mouse button to look around so left mouse button stays free for quiz answers.
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            horizontalRotation += mouseX;
            if (clampHorizontal)
                horizontalRotation = Mathf.Clamp(horizontalRotation, -maxHorizontalAngle, maxHorizontalAngle);

            verticalRotation -= mouseY;
            verticalRotation = Mathf.Clamp(verticalRotation, -70f, 70f);

            transform.localRotation = Quaternion.Euler(verticalRotation, horizontalRotation, 0f);
        }
    }

    public void ResetLook(Quaternion targetLocalRotation)
    {
        transform.localRotation = targetLocalRotation;
        SyncFromLocalRotation(targetLocalRotation);
    }

    private void SyncFromLocalRotation(Quaternion localRotation)
    {
        Vector3 angles = localRotation.eulerAngles;
        verticalRotation = NormalizeAngle(angles.x);
        horizontalRotation = NormalizeAngle(angles.y);
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;
        return angle;
    }
}
