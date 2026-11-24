using System.Collections;
using UnityEngine;
using TMPro;

/// <summary>
/// Handles dock range detection, quiz-completion gating, "Press F to Dock" UI prompt,
/// smooth boat alignment, arrival notification, and island scene transition.
/// </summary>
public class BoatDocking : MonoBehaviour
{
    public GameObject boat;
    public float dockingDistance = 9.5f;

    [Header("Docking Alignment & Scene Transition")]
    public Transform dockParkingPoint;
    public QuizManager quizManager;
    public string islandDisplayName = "Discovery Island";
    public string targetIslandSceneName = "IslandScene";
    public bool transitionToIslandScene = false;
    public float alignDuration = 1.35f;

    [Header("Docking UI")]
    public GameObject promptPanel;
    public TMP_Text promptText;
    public TMP_Text distanceGuideText;

    private bool isDocked = false;
    private bool isTransitioning = false;

    void Start()
    {
        if (quizManager == null)
            quizManager = FindFirstObjectByType<QuizManager>();

        if (promptPanel != null)
            promptPanel.SetActive(false);

        if (promptText != null)
            promptText.raycastTarget = false;

        if (distanceGuideText != null)
            distanceGuideText.raycastTarget = false;
    }

    void Update()
    {
        if (boat == null || Time.timeScale == 0f || isTransitioning)
            return;

        BoatSink boatSink = boat.GetComponent<BoatSink>();
        if (boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0))
        {
            HidePrompt();
            UpdateGuideText("");
            return;
        }

        Vector3 targetPoint = dockParkingPoint != null ? dockParkingPoint.position : transform.position;
        Vector2 dockXZ = new Vector2(targetPoint.x, targetPoint.z);
        Vector2 boatXZ = new Vector2(boat.transform.position.x, boat.transform.position.z);
        float distance = Vector2.Distance(dockXZ, boatXZ);

        bool quizActive = quizManager != null && quizManager.IsQuizActive;
        if (!isDocked && !quizActive)
            UpdateGuideText(islandDisplayName + " Dock: " + Mathf.RoundToInt(distance) + "m");
        else
            UpdateGuideText("");

        if (isDocked)
        {
            ShowPrompt("Docked at " + islandDisplayName + " — Press F to Set Sail");
            if (Input.GetKeyDown(KeyCode.F))
                UndockBoat();
            return;
        }

        if (quizActive)
        {
            HidePrompt();
            return;
        }

        if (distance <= dockingDistance)
        {
            bool eligible = quizManager == null || quizManager.CanDockNow();
            if (!eligible)
            {
                ShowPrompt("Complete 1 Ocean Quiz Round before docking!");
                return;
            }

            ShowPrompt("Press F to Dock at " + islandDisplayName);
            if (Input.GetKeyDown(KeyCode.F))
                StartCoroutine(DockRoutine());
        }
        else
        {
            HidePrompt();
        }
    }

    private IEnumerator DockRoutine()
    {
        isTransitioning = true;
        HidePrompt();
        UpdateGuideText("");

        if (quizManager != null)
            quizManager.PauseForDocking();

        BoatMovement movement = boat.GetComponent<BoatMovement>();
        if (movement != null)
            movement.StopBoat();

        BoatAnimator boatAnimator = boat.GetComponent<BoatAnimator>();
        if (boatAnimator != null)
            boatAnimator.SetDocked(true);

        FirstPersonMouseLook mouseLook = Camera.main != null ? Camera.main.GetComponent<FirstPersonMouseLook>() : null;
        if (mouseLook != null)
            mouseLook.enabled = false;

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

        // Smoothly align boat toward dock parking spot without clipping
        Vector3 startPos = boat.transform.position;
        Quaternion startRot = boat.transform.rotation;

        Vector3 destPos = dockParkingPoint != null
            ? new Vector3(dockParkingPoint.position.x, startPos.y, dockParkingPoint.position.z)
            : startPos;
        Quaternion destRot = dockParkingPoint != null
            ? Quaternion.Euler(0f, dockParkingPoint.eulerAngles.y, 0f)
            : Quaternion.Euler(0f, startRot.eulerAngles.y, 0f);

        float elapsed = 0f;
        float duration = Mathf.Max(0.25f, alignDuration);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));

            Vector3 nextPos = Vector3.Lerp(startPos, destPos, t);
            Quaternion nextRot = Quaternion.Lerp(startRot, destRot, t);

            if (rb != null && rb.isKinematic)
            {
                rb.MovePosition(nextPos);
                rb.MoveRotation(nextRot);
            }
            else
            {
                boat.transform.position = nextPos;
                boat.transform.rotation = nextRot;
            }

            yield return null;
        }

        if (movement != null)
            movement.SyncBaseTransform();

        ScreenFader fader = ScreenFader.EnsureInstance();
        fader.ShowArrivalBanner("Arrived at " + islandDisplayName + "!", 2.5f);

        Debug.Log("Docking successful at " + islandDisplayName + "!");

        if (transitionToIslandScene && !string.IsNullOrEmpty(targetIslandSceneName))
        {
            BoatSink sink = boat.GetComponent<BoatSink>();
            if (sink != null)
                GameStateManager.EnsureInstance().SetBoatHealth(sink.currentHealth);

            yield return new WaitForSeconds(0.5f);
            fader.FadeAndLoadScene(targetIslandSceneName, 0.7f);
            yield break;
        }

        // Phase 4 preview mode: stay docked at dockside and allow setting sail again
        isDocked = true;
        isTransitioning = false;
        if (mouseLook != null)
            mouseLook.enabled = true;
    }

    public void UndockBoat()
    {
        if (!isDocked || isTransitioning)
            return;

        isDocked = false;
        HidePrompt();

        BoatAnimator boatAnimator = boat != null ? boat.GetComponent<BoatAnimator>() : null;
        if (boatAnimator != null)
            boatAnimator.SetDocked(false);

        BoatMovement movement = boat != null ? boat.GetComponent<BoatMovement>() : null;
        if (movement != null)
        {
            movement.SyncBaseTransform();
            movement.canMove = true;
        }

        if (quizManager != null)
            quizManager.ResumeFromDocking();
    }

    private void ShowPrompt(string message)
    {
        if (promptPanel != null && !promptPanel.activeSelf)
            promptPanel.SetActive(true);

        if (promptText != null)
            promptText.text = message;
    }

    private void HidePrompt()
    {
        if (promptPanel != null && promptPanel.activeSelf)
            promptPanel.SetActive(false);
    }

    private void UpdateGuideText(string message)
    {
        if (distanceGuideText != null)
            distanceGuideText.text = message;
    }
}
