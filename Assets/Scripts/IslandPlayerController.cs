using UnityEngine;
using TMPro;

/// <summary>
/// First-person island exploration controller using Unity's CharacterController.
/// Supports WASD movement, Shift sprint, Space jump, mouse look, subtle head bob,
/// landing dip, water safety respawn, and E/F interactable prompts.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class IslandPlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 6f;
    public float sprintSpeed = 8.5f;
    public float jumpHeight = 1.35f;
    public float gravity = -18f;

    [Header("Mouse Look")]
    public Transform cameraTransform;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 75f;

    [Header("Head Bob & Landing")]
    public bool enableHeadBob = true;
    public float bobFrequency = 9.5f;
    public float bobVerticalAmplitude = 0.05f;
    public float bobHorizontalAmplitude = 0.03f;
    public float landingDipAmount = 0.08f;

    [Header("Audio Hook")]
    public AudioSource footstepAudioSource;
    public AudioClip[] footstepClips;
    public float stepInterval = 0.48f;

    [Header("Interaction & HUD")]
    public float defaultInteractRange = 4.5f;
    public GameObject interactionPromptPanel;
    public TMP_Text interactionPromptText;
    public GameObject crosshairObject;

    [Header("Water Safety")]
    public float minSafeY = -1.2f;

    private CharacterController controller;
    private float verticalVelocity = 0f;
    private float pitch = 0f;
    private bool controlsEnabled = true;

    private Vector3 baseCameraLocalPos;
    private float bobTimer = 0f;
    private float currentLandingDip = 0f;
    private bool wasGrounded = true;
    private float stepTimer = 0f;

    private Vector3 safeSpawnPosition;
    private Quaternion safeSpawnRotation;
    private IslandInteractable[] cachedInteractables;
    private IslandInteractable currentTarget;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        mouseSensitivity = PauseAndSettingsUI.GetSavedMouseSensitivity();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null)
                cameraTransform = cam.transform;
        }

        if (cameraTransform != null)
            baseCameraLocalPos = cameraTransform.localPosition;

        safeSpawnPosition = transform.position;
        safeSpawnRotation = transform.rotation;
    }

    void Start()
    {
        RefreshInteractables();

        if (interactionPromptText != null)
            interactionPromptText.raycastTarget = false;

        if (interactionPromptPanel != null)
            interactionPromptPanel.SetActive(false);

        SetControlsEnabled(true);
    }

    void Update()
    {
        if (!controlsEnabled || Time.timeScale == 0f)
        {
            HideInteractionPrompt();
            return;
        }

        // Allow clicking back into the game if Escape unlocked the cursor in Editor
        if (Cursor.lockState != CursorLockMode.Locked && Input.GetMouseButtonDown(0))
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        HandleMouseLook();
        HandleMovement();
        HandleWaterSafety();
        HandleInteractions();
    }

    public void RefreshInteractables()
    {
        cachedInteractables = FindObjectsByType<IslandInteractable>();
    }

    public void SetControlsEnabled(bool enabled)
    {
        controlsEnabled = enabled;

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            HideInteractionPrompt();
        }

        if (crosshairObject != null)
            crosshairObject.SetActive(enabled);
    }

    public void TeleportToSpawn(Vector3 position, Quaternion rotation)
    {
        safeSpawnPosition = position;
        safeSpawnRotation = rotation;

        if (controller != null)
            controller.enabled = false;

        transform.position = position;
        transform.rotation = Quaternion.Euler(0f, rotation.eulerAngles.y, 0f);
        verticalVelocity = 0f;
        pitch = 0f;

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.identity;

        if (controller != null)
            controller.enabled = true;
    }

    private void HandleMouseLook()
    {
        if (Cursor.lockState != CursorLockMode.Locked)
            return;

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

        transform.Rotate(0f, mouseX, 0f);

        pitch -= mouseY;
        pitch = Mathf.Clamp(pitch, -maxLookAngle, maxLookAngle);

        if (cameraTransform != null)
            cameraTransform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
    }

    private void HandleMovement()
    {
        if (controller == null || !controller.enabled)
            return;

        bool isGrounded = controller.isGrounded;
        if (isGrounded && !wasGrounded && verticalVelocity < -4f)
            currentLandingDip = landingDipAmount;

        if (isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;

        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        Vector3 moveDir = (transform.right * moveX + transform.forward * moveZ);
        if (moveDir.sqrMagnitude > 1f)
            moveDir.Normalize();

        bool sprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        float speed = sprinting ? sprintSpeed : walkSpeed;

        if (isGrounded && Input.GetButtonDown("Jump"))
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

        verticalVelocity += gravity * Time.deltaTime;

        Vector3 velocity = moveDir * speed;
        velocity.y = verticalVelocity;
        controller.Move(velocity * Time.deltaTime);

        UpdateHeadBobAndSteps(isGrounded, moveDir.magnitude, sprinting);
        wasGrounded = isGrounded;
    }

    private void UpdateHeadBobAndSteps(bool isGrounded, float inputMag, bool sprinting)
    {
        if (cameraTransform == null)
            return;

        currentLandingDip = Mathf.Lerp(currentLandingDip, 0f, Time.deltaTime * 10f);

        Vector3 targetLocalPos = baseCameraLocalPos;
        targetLocalPos.y -= currentLandingDip;

        if (enableHeadBob && isGrounded && inputMag > 0.1f)
        {
            float freq = sprinting ? bobFrequency * 1.25f : bobFrequency;
            bobTimer += Time.deltaTime * freq;

            targetLocalPos.y += Mathf.Sin(bobTimer) * bobVerticalAmplitude;
            targetLocalPos.x += Mathf.Cos(bobTimer * 0.5f) * bobHorizontalAmplitude;

            stepTimer -= Time.deltaTime * (sprinting ? 1.3f : 1f);
            if (stepTimer <= 0f)
            {
                stepTimer = stepInterval;
                PlayFootstep();
            }
        }
        else
        {
            bobTimer = 0f;
            stepTimer = 0.12f;
        }

        cameraTransform.localPosition = Vector3.Lerp(cameraTransform.localPosition, targetLocalPos, Time.deltaTime * 12f);
    }

    private void PlayFootstep()
    {
        if (footstepAudioSource != null && footstepClips != null && footstepClips.Length > 0)
        {
            int index = Random.Range(0, footstepClips.Length);
            if (footstepClips[index] != null)
            {
                footstepAudioSource.PlayOneShot(footstepClips[index]);
                return;
            }
        }

        bool isWoodDock = false;
        if (Physics.Raycast(transform.position + Vector3.up * 0.25f, Vector3.down, out RaycastHit hit, 1.8f))
        {
            string hitName = hit.collider.gameObject.name;
            isWoodDock = hitName.IndexOf("Dock", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Pier", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Gangplank", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Deck", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Plank", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Ramp", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Bridge", System.StringComparison.OrdinalIgnoreCase) >= 0 ||
                         hitName.IndexOf("Wood", System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        GameAudioManager.EnsureInstance().PlayFootstep(isWoodDock);
    }

    private void HandleWaterSafety()
    {
        if (transform.position.y < minSafeY)
        {
            TeleportToSpawn(safeSpawnPosition, safeSpawnRotation);
            ScreenFader.EnsureInstance().ShowArrivalBanner("Deep Water! Returned to the Dock.", 2f);
        }
    }

    private void HandleInteractions()
    {
        currentTarget = FindBestInteractable();

        if (currentTarget != null)
        {
            ShowInteractionPrompt(currentTarget.GetPromptText());
            if (Input.GetKeyDown(currentTarget.GetInteractKey()))
                currentTarget.Interact(this);
        }
        else
        {
            HideInteractionPrompt();
        }
    }

    private IslandInteractable FindBestInteractable()
    {
        // Prefer direct camera look target first for crisp selection
        if (cameraTransform != null)
        {
            Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);
            if (Physics.Raycast(ray, out RaycastHit hit, defaultInteractRange + 3f))
            {
                IslandInteractable looked = hit.collider.GetComponentInParent<IslandInteractable>();
                if (looked != null && looked.CanInteract() && hit.distance <= looked.GetInteractRange())
                    return looked;
            }
        }

        if (cachedInteractables == null)
            return null;

        IslandInteractable best = null;
        float bestDistance = float.MaxValue;

        for (int i = 0; i < cachedInteractables.Length; i++)
        {
            IslandInteractable item = cachedInteractables[i];
            if (item == null || !item.gameObject.activeInHierarchy || !item.CanInteract())
                continue;

            float range = item.GetInteractRange();
            float dist = Vector3.Distance(transform.position, item.transform.position);
            if (dist <= range && dist < bestDistance)
            {
                bestDistance = dist;
                best = item;
            }
        }

        return best;
    }

    private void ShowInteractionPrompt(string text)
    {
        if (interactionPromptPanel != null && !interactionPromptPanel.activeSelf)
            interactionPromptPanel.SetActive(true);

        if (interactionPromptText != null)
            interactionPromptText.text = text;
    }

    private void HideInteractionPrompt()
    {
        if (interactionPromptPanel != null && interactionPromptPanel.activeSelf)
            interactionPromptPanel.SetActive(false);
    }
}
