using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Persistent Victory Screen overlay shown after recovering the Legendary Golden Crown
/// from Final Treasure Island's Special Chest. Displays adventure stats and Play Again / Main Menu actions.
/// </summary>
public class VictoryScreenUI : MonoBehaviour
{
    public static VictoryScreenUI Instance { get; private set; }

    private Canvas rootCanvas;
    private GameObject overlayRoot;
    private RectTransform cardRect;
    private TMP_Text statsText;
    private float popAnimTimer = 0f;
    private bool isActionPending = false;

    public bool IsShowing => overlayRoot != null && overlayRoot.activeSelf;

    public static VictoryScreenUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        VictoryScreenUI existing = FindFirstObjectByType<VictoryScreenUI>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("VictoryScreenUI");
        Instance = go.AddComponent<VictoryScreenUI>();
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
        BuildVictoryUI();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Update()
    {
        if (overlayRoot == null || !overlayRoot.activeSelf)
            return;

        if (popAnimTimer > 0f && cardRect != null)
        {
            popAnimTimer = Mathf.Max(0f, popAnimTimer - Time.unscaledDeltaTime);
            float t = 1f - (popAnimTimer / 0.35f);
            float easeOutCubic = 1f - Mathf.Pow(1f - t, 3f);
            float scale = Mathf.Lerp(0.82f, 1f, easeOutCubic);
            cardRect.localScale = Vector3.one * scale;
        }
        else if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isActionPending = false;
        if (overlayRoot != null && (string.Equals(scene.name, "MainMenu") || string.Equals(scene.name, "GameScene")))
            overlayRoot.SetActive(false);
    }

    public void ShowVictory()
    {
        if (overlayRoot == null)
            BuildVictoryUI();

        if (IsShowing)
            return;

        isActionPending = false;

        if (InventoryHotbarUI.Instance != null && InventoryHotbarUI.Instance.IsInventoryOpen)
            InventoryHotbarUI.Instance.SetInventoryOpen(false);

        IslandPlayerController player = FindFirstObjectByType<IslandPlayerController>();
        if (player != null)
            player.SetControlsEnabled(false);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        PopulateStats();
        overlayRoot.SetActive(true);
        popAnimTimer = 0.35f;

        if (cardRect != null)
            cardRect.localScale = Vector3.one * 0.82f;

        SpawnVictoryConfetti();
    }

    private void PopulateStats()
    {
        if (statsText == null)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        int conqueredCount = 0;
        if (state.completedIslandSpecialChest != null)
        {
            for (int i = 0; i < state.completedIslandSpecialChest.Length; i++)
            {
                if (state.completedIslandSpecialChest[i])
                    conqueredCount++;
            }
        }

        int totalChestsOpened = state.openedChestIds != null ? state.openedChestIds.Count : 0;
        string compassStatus = state.hasMagicCompass ? "Unlocked" : "Not Found";

        statsText.text =
            "Islands Conquered:  " + conqueredCount + " / 3\n" +
            "Total Treasure Chests Opened:  " + totalChestsOpened + " / 18\n" +
            "Final Gold Coins:  " + state.coins + "\n" +
            "Remaining Supplies:  " + state.planks + " Planks  |  " + state.potions + " Potions\n" +
            "Final Boat Health:  " + state.boatHealth + " / " + state.maxBoatHealth + "\n" +
            "Magic Compass:  " + compassStatus;
    }

    private void OnPlayAgainClicked()
    {
        if (isActionPending)
            return;

        isActionPending = true;
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        ScreenFader.EnsureInstance().FadeAndLoadScene("GameScene", 0.7f);
    }

    private void OnMainMenuClicked()
    {
        if (isActionPending)
            return;

        isActionPending = true;
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        ScreenFader.EnsureInstance().FadeAndLoadScene("MainMenu", 0.7f);
    }

    private void SpawnVictoryConfetti()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        GameObject confettiGo = new GameObject("VictoryCrownConfetti");
        confettiGo.transform.position = cam.transform.position + cam.transform.forward * 3.2f + Vector3.up * 1.1f;
        confettiGo.transform.rotation = Quaternion.Euler(-75f, cam.transform.eulerAngles.y, 0f);

        ParticleSystem ps = confettiGo.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = true;
        main.loop = false;
        main.duration = 2.5f;
        main.startLifetime = 2.6f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 5.4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
        main.gravityModifier = 0.45f;
        main.maxParticles = 60;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(1f, 0.84f, 0.22f, 1f),
            new Color(0.28f, 0.92f, 0.48f, 1f)
        );

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 0f;
        em.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 55) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 36f;
        shape.radius = 0.7f;

        ParticleSystemRenderer pr = confettiGo.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? Shader.Find("Particles/Standard Unlit");
            if (particleShader != null)
            {
                Material mat = new Material(particleShader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(1f, 0.88f, 0.35f, 1f));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(1f, 0.88f, 0.35f, 1f));
                pr.sharedMaterial = mat;
            }
        }

        Destroy(confettiGo, 4f);
    }

    private void BuildVictoryUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 140;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        overlayRoot = CreateRect("VictoryOverlay", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = overlayRoot.AddComponent<Image>();
        dim.color = new Color(0.05f, 0.04f, 0.08f, 0.78f);

        GameObject outerFrame = CreateRect("VictoryFrame", overlayRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(860f, 620f));
        cardRect = outerFrame.GetComponent<RectTransform>();
        Image frameImg = outerFrame.AddComponent<Image>();
        frameImg.color = new Color(0.68f, 0.48f, 0.14f, 0.98f);

        GameObject parchment = CreateRect("ParchmentBody", outerFrame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform pRt = parchment.GetComponent<RectTransform>();
        pRt.offsetMin = new Vector2(16f, 16f);
        pRt.offsetMax = new Vector2(-16f, -16f);
        Image pImg = parchment.AddComponent<Image>();
        pImg.color = new Color(0.97f, 0.92f, 0.8f, 1f);

        // Crown emblem header strip
        GameObject crownBanner = CreateRect("CrownBanner", parchment.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 232f), new Vector2(740f, 54f));
        Image bannerImg = crownBanner.AddComponent<Image>();
        bannerImg.color = new Color(0.32f, 0.2f, 0.08f, 1f);
        bannerImg.raycastTarget = false;

        TMP_Text emblemText = CreateText(crownBanner.transform, "CrownEmblem", Vector2.zero, new Vector2(700f, 44f), 22f, FontStyles.Bold, new Color(1f, 0.85f, 0.28f, 1f));
        emblemText.text = "* * *  LEGENDARY GOLDEN CROWN RECOVERED  * * *";
        emblemText.raycastTarget = false;

        TMP_Text titleText = CreateText(parchment.transform, "VictoryTitle", new Vector2(0f, 158f), new Vector2(740f, 64f), 48f, FontStyles.Bold, new Color(0.58f, 0.36f, 0.06f, 1f));
        titleText.text = "VICTORY!";
        titleText.raycastTarget = false;

        TMP_Text subtitleText = CreateText(parchment.transform, "VictorySubtitle", new Vector2(0f, 108f), new Vector2(740f, 40f), 24f, FontStyles.Bold, new Color(0.24f, 0.14f, 0.06f, 1f));
        subtitleText.text = "You Recovered the Legendary Golden Crown!";
        subtitleText.raycastTarget = false;

        // Adventure Summary box
        GameObject summaryBox = CreateRect("AdventureSummaryBox", parchment.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -35f), new Vector2(720f, 224f));
        Image summaryBg = summaryBox.AddComponent<Image>();
        summaryBg.color = new Color(0.91f, 0.84f, 0.7f, 1f);
        summaryBg.raycastTarget = false;

        TMP_Text statsHeader = CreateText(summaryBox.transform, "StatsHeader", new Vector2(0f, 84f), new Vector2(680f, 34f), 22f, FontStyles.Bold, new Color(0.32f, 0.18f, 0.06f, 1f));
        statsHeader.text = "ADVENTURE SUMMARY";
        statsHeader.raycastTarget = false;

        statsText = CreateText(summaryBox.transform, "StatsDetails", new Vector2(0f, -18f), new Vector2(660f, 165f), 20f, FontStyles.Bold, new Color(0.18f, 0.12f, 0.06f, 1f));
        statsText.raycastTarget = false;

        // Play Again & Main Menu buttons
        CreateActionButton(parchment.transform, "PlayAgainButton", new Vector2(-155f, -210f), new Color(0.18f, 0.6f, 0.28f, 1f), "Play Again", OnPlayAgainClicked);
        CreateActionButton(parchment.transform, "MainMenuButton", new Vector2(155f, -210f), new Color(0.45f, 0.28f, 0.14f, 1f), "Main Menu", OnMainMenuClicked);

        overlayRoot.SetActive(false);
    }

    private void CreateActionButton(Transform parent, string name, Vector2 pos, Color bgColor, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(260f, 58f));
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        TMP_Text txt = CreateText(btnGo.transform, "Label", Vector2.zero, new Vector2(240f, 44f), 22f, FontStyles.Bold, new Color(1f, 0.96f, 0.86f, 1f));
        txt.text = label;
        txt.raycastTarget = false;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
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
