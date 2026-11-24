using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Persistent Magic Compass HUD widget unlocked by Discovery Island's Special Chest.
/// Smoothly rotates its needle toward the nearest unopened treasure chest (or Special Chest
/// when holding the island's Golden Key) and updates immediately as chests are opened.
/// </summary>
public class MagicCompassUI : MonoBehaviour
{
    public static MagicCompassUI Instance { get; private set; }

    private Canvas rootCanvas;
    private GameObject compassRoot;
    private RectTransform compassRect;
    private RectTransform needlePivot;
    private TMP_Text targetLabel;

    private IslandPlayerController cachedPlayer;
    private TreasureChest[] cachedChests;
    private IslandBoatReturn cachedBoatReturn;

    private float currentNeedleAngle = 0f;
    private float revealTimer = 0f;
    private bool wasUnlocked = false;

    public static MagicCompassUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        MagicCompassUI existing = FindFirstObjectByType<MagicCompassUI>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("MagicCompassUI");
        Instance = go.AddComponent<MagicCompassUI>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildCompassUI();
    }

    void OnEnable()
    {
        GameStateManager.OnStateChanged += OnGameStateChanged;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameStateManager.OnStateChanged -= OnGameStateChanged;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        wasUnlocked = GameStateManager.EnsureInstance().hasMagicCompass;
        RefreshSceneTargets();
        RefreshVisibility();
    }

    void Update()
    {
        if (rootCanvas == null || !rootCanvas.enabled || compassRoot == null || !compassRoot.activeSelf)
            return;

        if (revealTimer > 0f && compassRect != null)
        {
            revealTimer = Mathf.Max(0f, revealTimer - Time.unscaledDeltaTime);
            float t = 1f - (revealTimer / 0.55f);
            float scale = Mathf.Lerp(0.4f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            compassRect.localScale = Vector3.one * scale;
        }
        else if (compassRect != null)
        {
            compassRect.localScale = Vector3.one;
        }

        UpdateNeedleAndLabel();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RefreshSceneTargets();
        RefreshVisibility();
    }

    private void OnGameStateChanged()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.hasMagicCompass && !wasUnlocked)
        {
            wasUnlocked = true;
            revealTimer = 0.55f;
        }
        else if (!state.hasMagicCompass)
        {
            wasUnlocked = false;
        }

        RefreshVisibility();
    }

    public void RefreshTargetsNow()
    {
        RefreshSceneTargets();
        RefreshVisibility();
    }

    private void RefreshSceneTargets()
    {
        cachedPlayer = FindFirstObjectByType<IslandPlayerController>();
        cachedChests = FindObjectsByType<TreasureChest>(FindObjectsSortMode.None);
        cachedBoatReturn = FindFirstObjectByType<IslandBoatReturn>();
    }

    private void RefreshVisibility()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        bool show = state.hasMagicCompass && cachedPlayer != null;

        if (rootCanvas != null)
            rootCanvas.enabled = show;
        if (compassRoot != null)
            compassRoot.SetActive(show);
    }

    private void UpdateNeedleAndLabel()
    {
        if (cachedPlayer == null || needlePivot == null || targetLabel == null)
            return;

        Transform referenceTransform = cachedPlayer.cameraTransform != null
            ? cachedPlayer.cameraTransform
            : cachedPlayer.transform;

        Vector3 targetPos;
        string labelText;
        Color labelColor;

        if (!TryFindNextTarget(out targetPos, out labelText, out labelColor))
        {
            targetLabel.text = "Explore Island";
            targetLabel.color = new Color(1f, 0.92f, 0.65f, 1f);
            return;
        }

        Vector3 flatForward = referenceTransform.forward;
        flatForward.y = 0f;
        if (flatForward.sqrMagnitude < 0.0001f)
            flatForward = cachedPlayer.transform.forward;
        flatForward.y = 0f;
        flatForward.Normalize();

        Vector3 flatToTarget = targetPos - cachedPlayer.transform.position;
        flatToTarget.y = 0f;
        float distanceMeters = flatToTarget.magnitude;

        if (flatToTarget.sqrMagnitude > 0.001f)
        {
            float signedAngle = Vector3.SignedAngle(flatForward, flatToTarget.normalized, Vector3.up);
            currentNeedleAngle = Mathf.LerpAngle(currentNeedleAngle, -signedAngle, Time.deltaTime * 8.5f);
            needlePivot.localRotation = Quaternion.Euler(0f, 0f, currentNeedleAngle);
        }

        targetLabel.text = labelText + " (" + Mathf.RoundToInt(distanceMeters) + "m)";
        targetLabel.color = labelColor;
    }

    private bool TryFindNextTarget(out Vector3 targetPos, out string label, out Color labelColor)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        int idx = Mathf.Clamp(state.currentIslandIndex, 0, 2);
        bool hasKey = state.hasIslandKey[idx];

        TreasureChest specialChest = null;
        TreasureChest nearestRegular = null;
        float nearestRegularDistSq = float.MaxValue;
        TreasureChest nearestAny = null;
        float nearestAnyDistSq = float.MaxValue;

        if (cachedChests != null)
        {
            for (int i = 0; i < cachedChests.Length; i++)
            {
                TreasureChest chest = cachedChests[i];
                if (chest == null || chest.IsOpened)
                    continue;

                float distSq = (chest.transform.position - cachedPlayer.transform.position).sqrMagnitude;

                if (chest.isSpecialChest)
                    specialChest = chest;
                else if (distSq < nearestRegularDistSq)
                {
                    nearestRegularDistSq = distSq;
                    nearestRegular = chest;
                }

                if (distSq < nearestAnyDistSq)
                {
                    nearestAnyDistSq = distSq;
                    nearestAny = chest;
                }
            }
        }

        // Prioritize Special Chest when player already holds the Golden Key
        if (hasKey && specialChest != null)
        {
            targetPos = specialChest.transform.position;
            label = "Special Chest";
            labelColor = new Color(1f, 0.86f, 0.28f, 1f);
            return true;
        }

        if (nearestRegular != null)
        {
            targetPos = nearestRegular.transform.position;
            label = hasKey ? "Treasure Chest" : "Next Chest";
            labelColor = new Color(0.95f, 0.96f, 0.82f, 1f);
            return true;
        }

        if (nearestAny != null)
        {
            targetPos = nearestAny.transform.position;
            label = nearestAny.isSpecialChest ? "Special Chest (Need Key)" : "Next Chest";
            labelColor = new Color(1f, 0.88f, 0.5f, 1f);
            return true;
        }

        if (cachedBoatReturn != null)
        {
            targetPos = cachedBoatReturn.transform.position;
            label = "All Chests Open! Boat";
            labelColor = new Color(0.55f, 0.96f, 0.62f, 1f);
            return true;
        }

        targetPos = Vector3.zero;
        label = "";
        labelColor = Color.white;
        return false;
    }

    private void BuildCompassUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 45;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        compassRoot = CreateRect("MagicCompassWidget", transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-110f, -142f), new Vector2(160f, 185f));
        compassRect = compassRoot.GetComponent<RectTransform>();

        // Brass housing, bezel, and parchment dial
        GameObject brass = CreateRect("BrassRing", compassRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 22f), new Vector2(126f, 126f));
        Image brassImg = brass.AddComponent<Image>();
        brassImg.color = new Color(0.76f, 0.58f, 0.22f, 0.96f);
        brassImg.raycastTarget = false;

        GameObject bezel = CreateRect("InnerBezel", brass.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(110f, 110f));
        Image bezelImg = bezel.AddComponent<Image>();
        bezelImg.color = new Color(0.22f, 0.14f, 0.07f, 1f);
        bezelImg.raycastTarget = false;

        GameObject dial = CreateRect("DialFace", bezel.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(98f, 98f));
        Image dialImg = dial.AddComponent<Image>();
        dialImg.color = new Color(0.95f, 0.9f, 0.78f, 1f);
        dialImg.raycastTarget = false;

        CreateDialMark(dial.transform, new Vector2(0f, 38f), "T");
        CreateDialMark(dial.transform, new Vector2(38f, 0f), "E");
        CreateDialMark(dial.transform, new Vector2(0f, -38f), "S");
        CreateDialMark(dial.transform, new Vector2(-38f, 0f), "W");

        // Rotating Needle Pivot
        GameObject pivotGo = CreateRect("NeedlePivot", dial.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 74f));
        needlePivot = pivotGo.GetComponent<RectTransform>();

        GameObject northTip = CreateRect("NeedleNorth", pivotGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f), new Vector2(10f, 36f));
        Image northImg = northTip.AddComponent<Image>();
        northImg.color = new Color(0.86f, 0.18f, 0.16f, 1f);
        northImg.raycastTarget = false;

        GameObject southTip = CreateRect("NeedleSouth", pivotGo.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -18f), new Vector2(8f, 32f));
        Image southImg = southTip.AddComponent<Image>();
        southImg.color = new Color(0.24f, 0.24f, 0.26f, 1f);
        southImg.raycastTarget = false;

        GameObject cap = CreateRect("CenterCap", dial.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(14f, 14f));
        Image capImg = cap.AddComponent<Image>();
        capImg.color = new Color(0.85f, 0.66f, 0.22f, 1f);
        capImg.raycastTarget = false;

        // Readout banner under dial
        GameObject labelBg = CreateRect("LabelBg", compassRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -64f), new Vector2(210f, 40f));
        Image bgImg = labelBg.AddComponent<Image>();
        bgImg.color = new Color(0.16f, 0.1f, 0.05f, 0.9f);
        bgImg.raycastTarget = false;

        GameObject labelGo = CreateRect("TargetLabel", labelBg.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = labelGo.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = 15f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.65f, 1f);
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        targetLabel = tmp;

        compassRoot.SetActive(false);
    }

    private static void CreateDialMark(Transform parent, Vector2 pos, string letter)
    {
        GameObject go = CreateRect("Mark_" + letter, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(20f, 20f));
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = letter;
        tmp.fontSize = 12f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(0.28f, 0.16f, 0.06f, 1f);
        tmp.raycastTarget = false;
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }
}
