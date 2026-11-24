using System;
using UnityEngine;

/// <summary>
/// Interactive animated treasure chest supporting regular and special chests,
/// hinged lid animation, locked rattle feedback, particle reveals, and persistent opened state.
/// </summary>
public class TreasureChest : IslandInteractable
{
    public static event Action<TreasureChest, IslandPlayerController> OnChestInteractRequested;

    [Header("Chest Identity")]
    public string chestId = "discovery_regular_1";
    public int islandIndex = 0;
    public bool isSpecialChest = false;

    [Header("Lid Animation & Visuals")]
    public Transform lidTransform;
    public float closedAngle = 0f;
    public float openAngle = -105f;
    public float lidOpenSpeed = 4.5f;
    public GameObject lockVisual;
    public GameObject specialAuraObject;
    public ParticleSystem openBurstParticles;
    public Transform rewardRevealPoint;

    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation;
    private float rattleTimer = 0f;

    public bool IsOpened { get; private set; }

    void Awake()
    {
        if (lidTransform == null)
            lidTransform = transform.Find("Chest_top");

        if (lidTransform != null)
        {
            Vector3 baseEuler = lidTransform.localEulerAngles;
            lidClosedRotation = Quaternion.Euler(closedAngle, baseEuler.y, baseEuler.z);
            lidOpenRotation = Quaternion.Euler(openAngle, baseEuler.y, baseEuler.z);
        }
    }

    void Start()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.IsChestOpened(chestId))
        {
            IsOpened = true;
            if (lidTransform != null)
                lidTransform.localRotation = lidOpenRotation;
            if (lockVisual != null)
                lockVisual.SetActive(false);
            if (specialAuraObject != null)
                specialAuraObject.SetActive(false);
        }
    }

    void Update()
    {
        if (lidTransform == null || Time.timeScale == 0f)
            return;

        if (IsOpened)
        {
            lidTransform.localRotation = Quaternion.Lerp(lidTransform.localRotation, lidOpenRotation, Time.deltaTime * lidOpenSpeed);
            return;
        }

        if (rattleTimer > 0f)
        {
            rattleTimer = Mathf.Max(0f, rattleTimer - Time.deltaTime);
            float rattle = Mathf.Sin(Time.time * 45f) * 6f * (rattleTimer / 0.35f);
            lidTransform.localRotation = lidClosedRotation * Quaternion.Euler(rattle, 0f, 0f);
        }
        else
        {
            lidTransform.localRotation = Quaternion.Lerp(lidTransform.localRotation, lidClosedRotation, Time.deltaTime * 8f);
        }
    }

    public override KeyCode GetInteractKey()
    {
        return KeyCode.E;
    }

    public override float GetInteractRange()
    {
        return 4.0f;
    }

    public override bool CanInteract()
    {
        return !IsOpened;
    }

    public override string GetPromptText()
    {
        if (IsOpened)
            return "";

        if (isSpecialChest)
            return "Press E to Unlock Special Chest (Requires Golden Key)";

        return "Press E to Open Treasure Chest";
    }

    public override void Interact(IslandPlayerController player)
    {
        if (IsOpened)
            return;

        if (OnChestInteractRequested != null)
        {
            OnChestInteractRequested.Invoke(this, player);
            return;
        }

        // Phase 6 preview before Phase 7 ChestQuizManager is attached
        PlayLockedRattle();
        string msg = isSpecialChest
            ? "Special Chest — Requires Golden Key (Quiz unlocks in Phase 7)"
            : "Treasure Chest Ready! (Quiz & random rewards unlock in Phase 7)";
        ScreenFader.EnsureInstance().ShowArrivalBanner(msg, 2.2f);
    }

    public void OpenChestAnimated()
    {
        if (IsOpened)
            return;

        IsOpened = true;
        GameStateManager.EnsureInstance().MarkChestOpened(chestId);
        GameAudioManager.EnsureInstance().PlayChestOpen();

        if (lockVisual != null)
            lockVisual.SetActive(false);

        if (specialAuraObject != null)
            specialAuraObject.SetActive(false);

        if (openBurstParticles != null)
            openBurstParticles.Play();
    }

    public void PlayLockedRattle()
    {
        rattleTimer = 0.35f;
        GameAudioManager.EnsureInstance().PlayChestLocked();
    }
}
