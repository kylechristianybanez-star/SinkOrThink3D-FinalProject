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
    public int islandIndex = 0;
    public Transform dockParkingPoint;
    public Transform boatReturnSpawnPoint;
    public QuizManager quizManager;
    public bool autoMatchCurrentIsland = false;
    public string islandDisplayName = "Discovery Island";
    public string targetIslandSceneName = "IslandScene";
    public bool transitionToIslandScene = true;
    public float alignDuration = 1.35f;

    [Header("Docking UI")]
    public GameObject promptPanel;
    public TMP_Text promptText;
    public TMP_Text distanceGuideText;

    private bool isDocked = false;
    private bool isTransitioning = false;
    private BoatSink cachedBoatSink;

    void Start()
    {
        if (boat != null)
            cachedBoatSink = boat.GetComponent<BoatSink>();
        if (autoMatchCurrentIsland)
        {
            GameStateManager state = GameStateManager.EnsureInstance();
            islandDisplayName = state.GetIslandDisplayName(state.currentIslandIndex);
            string sceneForIsland = state.GetIslandSceneName(state.currentIslandIndex);
            if (!string.IsNullOrEmpty(sceneForIsland))
                targetIslandSceneName = sceneForIsland;
        }

        if (quizManager == null)
            quizManager = FindAnyObjectByType<QuizManager>();

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

        if (cachedBoatSink == null && boat != null)
            cachedBoatSink = boat.GetComponent<BoatSink>();

        if (cachedBoatSink != null && (cachedBoatSink.isSinking || cachedBoatSink.currentHealth <= 0))
        {
            HidePrompt();
            UpdateGuideText("");
            return;
        }

        Vector3 targetPoint = dockParkingPoint != null ? dockParkingPoint.position : transform.position;
        Vector2 dockXZ = new Vector2(targetPoint.x, targetPoint.z);
        Vector2 boatXZ = new Vector2(boat.transform.position.x, boat.transform.position.z);
        float distance = Vector2.Distance(dockXZ, boatXZ);

        GameStateManager state = GameStateManager.EnsureInstance();
        bool isActiveTargetIsland = (islandIndex == state.currentIslandIndex);

        bool quizActive = quizManager != null && quizManager.IsQuizActive;
        if (isActiveTargetIsland && !isDocked && !quizActive)
        {
            Vector3 boatFwd = new Vector3(boat.transform.forward.x, 0f, boat.transform.forward.z);
            Vector3 toDock = new Vector3(targetPoint.x - boat.transform.position.x, 0f, targetPoint.z - boat.transform.position.z);
            string dirLabel = "";
            if (boatFwd.sqrMagnitude > 0.001f && toDock.sqrMagnitude > 0.01f)
            {
                float signedAngle = Vector3.SignedAngle(boatFwd.normalized, toDock.normalized, Vector3.up);
                if (Mathf.Abs(signedAngle) <= 22f)
                    dirLabel = "Ahead ^  ";
                else if (signedAngle > 22f && signedAngle < 135f)
                    dirLabel = "Turn Right ->  ";
                else if (signedAngle < -22f && signedAngle > -135f)
                    dirLabel = "<- Turn Left  ";
                else
                    dirLabel = "Behind v  ";
            }

            UpdateGuideText("Next Dock: " + islandDisplayName + " — " + dirLabel + "(" + Mathf.RoundToInt(distance) + "m)");
        }
        else if (isActiveTargetIsland)
        {
            UpdateGuideText("");
        }

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
            if (!state.IsIslandUnlocked(islandIndex))
            {
                string prevName = state.GetIslandDisplayName(Mathf.Max(0, islandIndex - 1));
                ShowPrompt("Locked! Complete " + prevName + " first!");
                return;
            }

            bool eligible = !isActiveTargetIsland || quizManager == null || quizManager.CanDockNow();
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
        GameAudioManager.EnsureInstance().PlayDocking();

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
            GameStateManager state = GameStateManager.EnsureInstance();
            state.selectedIslandIndex = islandIndex;
            state.lastDockedIslandIndex = islandIndex;

            BoatSink sink = boat.GetComponent<BoatSink>();
            if (sink != null)
                state.SetBoatHealth(sink.currentHealth);

            state.SaveProgress();
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
