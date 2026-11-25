using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// Interactive animated treasure chest supporting regular and special chests,
/// hinged lid animation, locked rattle, vanish-and-respawn relocation on wrong answers,
/// particle reveals, and persistent opened state.
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

    [Header("Wrong-Answer Relocation")]
    public Vector3[] respawnCandidateSpots;

    private Quaternion lidClosedRotation;
    private Quaternion lidOpenRotation;
    private float rattleTimer = 0f;
    private bool isRelocating = false;

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
        if (lidTransform == null || Time.timeScale == 0f || isRelocating)
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
        return !IsOpened && !isRelocating;
    }

    public override string GetPromptText()
    {
        if (IsOpened || isRelocating)
            return "";

        if (isSpecialChest)
            return "Press E to Unlock Special Chest (Requires Golden Key)";

        return "Press E to Open Treasure Chest";
    }

    public override void Interact(IslandPlayerController player)
    {
        if (IsOpened || isRelocating)
            return;

        if (OnChestInteractRequested != null)
        {
            OnChestInteractRequested.Invoke(this, player);
            return;
        }

        PlayLockedRattle();
        string msg = isSpecialChest
            ? "Special Chest — Requires Golden Key"
            : "Treasure Chest Ready!";
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

    public void RelocateOnWrongAnswer()
    {
        if (IsOpened || isRelocating)
            return;

        StartCoroutine(RelocateRoutine());
    }

    private IEnumerator RelocateRoutine()
    {
        isRelocating = true;

        Vector3 oldPos = transform.position;
        SpawnPoofEffect(oldPos + Vector3.up * 0.6f, new Color(0.95f, 0.32f, 0.22f, 1f));
        SetChestActiveVisuals(false);

        yield return new WaitForSeconds(0.45f);

        Vector3 newPos = PickNewRespawnPosition(oldPos);
        transform.position = newPos;
        transform.rotation = Quaternion.Euler(0f, UnityEngine.Random.Range(0f, 360f), 0f);
        rattleTimer = 0f;

        SetChestActiveVisuals(true);
        SpawnPoofEffect(newPos + Vector3.up * 0.7f, new Color(1f, 0.86f, 0.28f, 1f));

        isRelocating = false;

        if (MagicCompassUI.Instance != null)
            MagicCompassUI.Instance.RefreshTargetsNow();

        IslandPlayerController playerCtrl = FindAnyObjectByType<IslandPlayerController>();
        if (playerCtrl != null)
            playerCtrl.RefreshInteractables();
    }

    private void SetChestActiveVisuals(bool visible)
    {
        Renderer[] rends = GetComponentsInChildren<Renderer>(true);
        for (int i = 0; i < rends.Length; i++)
        {
            if (rends[i] != null)
                rends[i].enabled = visible;
        }

        Collider[] cols = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < cols.Length; i++)
        {
            if (cols[i] != null)
                cols[i].enabled = visible;
        }
    }

    private Vector3 PickNewRespawnPosition(Vector3 oldPos)
    {
        TreasureChest[] allChests = FindObjectsByType<TreasureChest>();

        // 1. Prefer verified walkable curated spots on this island
        if (respawnCandidateSpots != null && respawnCandidateSpots.Length > 0)
        {
            int[] order = new int[respawnCandidateSpots.Length];
            for (int i = 0; i < order.Length; i++) order[i] = i;
            for (int i = order.Length - 1; i > 0; i--)
            {
                int swap = UnityEngine.Random.Range(0, i + 1);
                int tmp = order[i];
                order[i] = order[swap];
                order[swap] = tmp;
            }

            for (int i = 0; i < order.Length; i++)
            {
                Vector3 cand = respawnCandidateSpots[order[i]];
                if (Vector3.Distance(cand, oldPos) < 14f)
                    continue;
                if (IsClearOfChestsAndObstacles(cand, allChests))
                    return cand;
            }
        }

        // 2. Fallback: sample active Terrain in surrounding rings
        Terrain activeTerrain = FindActiveTerrain();
        if (activeTerrain != null && activeTerrain.terrainData != null)
        {
            TerrainData data = activeTerrain.terrainData;
            Vector3 origin = activeTerrain.transform.position;
            float baseY = origin.y;

            for (int attempt = 0; attempt < 36; attempt++)
            {
                float radius = UnityEngine.Random.Range(18f, 60f);
                float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
                float tx = oldPos.x + Mathf.Cos(angle) * radius;
                float tz = oldPos.z + Mathf.Sin(angle) * radius;

                float nx = (tx - origin.x) / data.size.x;
                float nz = (tz - origin.z) / data.size.z;
                if (nx < 0.08f || nx > 0.92f || nz < 0.08f || nz > 0.92f)
                    continue;

                float wy = activeTerrain.SampleHeight(new Vector3(tx, 0f, tz)) + baseY;
                if (wy < 0.8f || data.GetSteepness(nx, nz) > 28f)
                    continue;

                Vector3 cand = new Vector3(tx, wy, tz);
                if (Vector3.Distance(cand, oldPos) >= 14f && IsClearOfChestsAndObstacles(cand, allChests))
                    return cand;
            }
        }

        return oldPos;
    }

    private bool IsClearOfChestsAndObstacles(Vector3 candidate, TreasureChest[] allChests)
    {
        for (int i = 0; i < allChests.Length; i++)
        {
            TreasureChest other = allChests[i];
            if (other == null || other == this || !other.gameObject.activeInHierarchy)
                continue;
            if (Vector3.Distance(candidate, other.transform.position) < 8f)
                return false;
        }

        return !Physics.CheckSphere(candidate + Vector3.up * 1.15f, 1.0f, ~0, QueryTriggerInteraction.Ignore);
    }

    private Terrain FindActiveTerrain()
    {
        Terrain[] terrains = FindObjectsByType<Terrain>();
        for (int i = 0; i < terrains.Length; i++)
        {
            if (terrains[i] != null && terrains[i].gameObject.activeInHierarchy)
                return terrains[i];
        }
        return null;
    }

    private static void SpawnPoofEffect(Vector3 pos, Color color)
    {
        GameObject fxGo = new GameObject("ChestRelocatePoof");
        fxGo.transform.position = pos;

        ParticleSystem ps = fxGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.8f;
        main.startLifetime = 0.65f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f, 3.2f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.14f, 0.28f);
        main.startColor = color;
        main.maxParticles = 28;

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 24) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.55f;

        ParticleSystemRenderer pr = fxGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader pShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (pShader == null)
                pShader = Shader.Find("Particles/Standard Unlit");
            if (pShader != null)
            {
                Material mat = new Material(pShader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                pr.sharedMaterial = mat;
            }
        }

        ps.Play();
        Destroy(fxGo, 1.5f);
    }
}
