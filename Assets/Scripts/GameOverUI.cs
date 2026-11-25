using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Polished persistent Game Over overlay shown when boat health reaches 0 HP.
/// Displays pulsing danger vignette, animated rising underwater bubbles, voyage stats,
/// somber Game Over background music, and Play Again / Main Menu buttons.
/// </summary>
public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    private Canvas rootCanvas;
    private GameObject overlayRoot;
    private Image dangerVignette;
    private RectTransform cardRect;
    private TMP_Text subtitleText;
    private TMP_Text statsText;

    private readonly RectTransform[] bubbleRects = new RectTransform[16];
    private readonly float[] bubbleSpeeds = new float[16];
    private readonly float[] bubblePhase = new float[16];
    private GameObject activeBubbleFx;

    private float popAnimTimer = 0f;
    private bool isActionPending = false;

    public bool IsShowing => overlayRoot != null && overlayRoot.activeSelf;

    public static GameOverUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameOverUI existing = FindAnyObjectByType<GameOverUI>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("GameOverUI");
        Instance = go.AddComponent<GameOverUI>();
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
        BuildUI();
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
            cardRect.localScale = Vector3.one * Mathf.Lerp(0.84f, 1f, easeOutCubic);
        }
        else if (cardRect != null)
        {
            cardRect.localScale = Vector3.one;
        }

        if (dangerVignette != null)
        {
            float pulse = 0.22f + 0.14f * Mathf.Sin(Time.unscaledTime * 2.1f);
            dangerVignette.color = new Color(0.55f, 0.08f, 0.08f, pulse);
        }

        AnimateUiBubbles();
    }

    private void AnimateUiBubbles()
    {
        float dt = Time.unscaledDeltaTime;
        for (int i = 0; i < bubbleRects.Length; i++)
        {
            RectTransform br = bubbleRects[i];
            if (br == null) continue;

            Vector2 pos = br.anchoredPosition;
            pos.y += bubbleSpeeds[i] * dt;
            pos.x += Mathf.Sin(Time.unscaledTime * 1.4f + bubblePhase[i]) * 18f * dt;

            if (pos.y > 590f)
                pos.y = -590f;

            br.anchoredPosition = pos;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        isActionPending = false;
        HideOverlay();
    }

    public void HideOverlay()
    {
        if (overlayRoot != null)
            overlayRoot.SetActive(false);

        if (activeBubbleFx != null)
        {
            Destroy(activeBubbleFx);
            activeBubbleFx = null;
        }
    }

    public void ShowGameOver(string customReason = null)
    {
        if (overlayRoot == null)
            BuildUI();

        if (IsShowing || (VictoryScreenUI.Instance != null && VictoryScreenUI.Instance.IsShowing))
            return;

        isActionPending = false;
        GameStateManager.ClearSavedProgress();

        if (InventoryHotbarUI.Instance != null && InventoryHotbarUI.Instance.IsInventoryOpen)
            InventoryHotbarUI.Instance.SetInventoryOpen(false);

        if (PauseAndSettingsUI.Instance != null && PauseAndSettingsUI.Instance.IsOpen)
            PauseAndSettingsUI.Instance.CloseAll();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (subtitleText != null)
        {
            string reason = string.IsNullOrEmpty(customReason)
                ? "Your boat sank beneath the ocean waves!"
                : customReason;
            subtitleText.text = reason + "\nTip: Use Planks [1], Potions [2], or Dock Repair Benches to keep your hull afloat.";
        }

        PopulateStats();
        overlayRoot.SetActive(true);
        popAnimTimer = 0.35f;

        if (cardRect != null)
            cardRect.localScale = Vector3.one * 0.84f;

        SpawnUnderwaterBubbleParticles();
        GameAudioManager.EnsureInstance().SetGameOverMusic();
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

        int totalChests = state.openedChestIds != null ? state.openedChestIds.Count : 0;
        int acc = Mathf.RoundToInt(state.GetAccuracyPercent());
        statsText.text =
            "Rank:  " + state.GetNavigatorRank() + "  |  Time: " + state.GetFormattedPlayTime() + "\n" +
            "Quiz Accuracy:  " + acc + "% (" + state.totalQuestionsCorrect + "/" + state.totalQuestionsAnswered + ")  |  Best Streak: " + state.bestStreak + "\n" +
            "Voyage:  " + state.GetVoyageLabel(state.currentIslandIndex) + "  |  Islands: " + conqueredCount + "/3\n" +
            "Chests Opened:  " + totalChests + "/18  |  Gold Coins: " + state.coins;
    }

    private void OnPlayAgainClicked()
    {
        if (isActionPending)
            return;

        isActionPending = true;
        GameAudioManager.EnsureInstance().PlayClick();
        HideOverlay();
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        GameAudioManager.EnsureInstance().SetAdventureMusic();
        ScreenFader.EnsureInstance().FadeAndLoadScene("GameScene", 0.65f);
    }

    private void OnMainMenuClicked()
    {
        if (isActionPending)
            return;

        isActionPending = true;
        GameAudioManager.EnsureInstance().PlayClick();
        HideOverlay();
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        GameAudioManager.EnsureInstance().SetAdventureMusic();
        ScreenFader.EnsureInstance().FadeAndLoadScene("MainMenu", 0.65f);
    }

    private void SpawnUnderwaterBubbleParticles()
    {
        Camera cam = Camera.main;
        if (cam == null)
            return;

        if (activeBubbleFx != null)
            Destroy(activeBubbleFx);

        activeBubbleFx = new GameObject("GameOverSeaBubblesFX");
        activeBubbleFx.transform.position = cam.transform.position + cam.transform.forward * 2.6f - Vector3.up * 1.1f;
        activeBubbleFx.transform.rotation = Quaternion.Euler(-90f, 0f, 0f);

        ParticleSystem ps = activeBubbleFx.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = true;
        main.loop = true;
        main.startLifetime = 2.8f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 1.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.06f, 0.16f);
        main.gravityModifier = -0.12f;
        main.maxParticles = 45;
        main.startColor = new ParticleSystem.MinMaxGradient(
            new Color(0.55f, 0.85f, 1f, 0.55f),
            new Color(0.82f, 0.96f, 1f, 0.78f)
        );

        ParticleSystem.EmissionModule em = ps.emission;
        em.rateOverTime = 14f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(3.4f, 1.2f, 0.5f);

        ParticleSystemRenderer pr = activeBubbleFx.GetComponent<ParticleSystemRenderer>();
        if (pr != null)
        {
            Shader particleShader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (particleShader == null)
                particleShader = Shader.Find("Particles/Standard Unlit");
            if (particleShader != null)
            {
                Material mat = new Material(particleShader);
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.75f, 0.94f, 1f, 0.65f));
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.75f, 0.94f, 1f, 0.65f));
                pr.sharedMaterial = mat;
            }
        }
    }

    private void BuildUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 135;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        overlayRoot = CreateRect("GameOverOverlay", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backdrop = overlayRoot.AddComponent<Image>();
        backdrop.color = new Color(0.03f, 0.07f, 0.13f, 0.82f);

        // Pulsing crimson border vignette for dramatic sunk-ship atmosphere
        GameObject vigGo = CreateRect("DangerVignette", overlayRoot.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        dangerVignette = vigGo.AddComponent<Image>();
        dangerVignette.color = new Color(0.55f, 0.08f, 0.08f, 0.25f);
        dangerVignette.raycastTarget = false;

        // Rising ambient UI sea bubbles behind the Game Over card
        System.Random rng = new System.Random(404);
        for (int i = 0; i < bubbleRects.Length; i++)
        {
            float bx = Mathf.Lerp(-860f, 860f, (float)rng.NextDouble());
            float by = Mathf.Lerp(-520f, 520f, (float)rng.NextDouble());
            float size = 18f + (float)rng.NextDouble() * 34f;

            GameObject bGo = CreateRect("SeaBubble_" + i, overlayRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(bx, by), new Vector2(size, size));
            Image bImg = bGo.AddComponent<Image>();
            bImg.color = new Color(0.58f, 0.86f, 1f, 0.22f + (float)rng.NextDouble() * 0.2f);
            bImg.raycastTarget = false;

            bubbleRects[i] = bGo.GetComponent<RectTransform>();
            bubbleSpeeds[i] = 48f + (float)rng.NextDouble() * 68f;
            bubblePhase[i] = (float)rng.NextDouble() * 6.28f;
        }

        // Wooden frame & parchment panel
        GameObject frame = CreateRect("GameOverCard", overlayRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 560f));
        cardRect = frame.GetComponent<RectTransform>();
        Image frameImg = frame.AddComponent<Image>();
        frameImg.color = new Color(0.32f, 0.16f, 0.12f, 0.98f);

        GameObject paper = CreateRect("Parchment", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform pRt = paper.GetComponent<RectTransform>();
        pRt.offsetMin = new Vector2(16f, 16f);
        pRt.offsetMax = new Vector2(-16f, -16f);
        Image pImg = paper.AddComponent<Image>();
        pImg.color = new Color(0.95f, 0.89f, 0.78f, 1f);

        GameObject banner = CreateRect("BannerStrip", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 204f), new Vector2(680f, 46f));
        Image bannerImg = banner.AddComponent<Image>();
        bannerImg.color = new Color(0.46f, 0.12f, 0.12f, 1f);
        bannerImg.raycastTarget = false;

        TMP_Text bannerTxt = CreateText(banner.transform, "BannerTxt", Vector2.zero, new Vector2(640f, 38f), 20f, FontStyles.Bold, new Color(1f, 0.88f, 0.76f, 1f));
        bannerTxt.text = "* * *   SHIPWRECKED AT SEA   * * *";
        bannerTxt.raycastTarget = false;

        TMP_Text titleTxt = CreateText(paper.transform, "GameOverTitle", new Vector2(0f, 136f), new Vector2(720f, 64f), 50f, FontStyles.Bold, new Color(0.58f, 0.12f, 0.12f, 1f));
        titleTxt.text = "GAME OVER";
        titleTxt.raycastTarget = false;

        subtitleText = CreateText(paper.transform, "GameOverSubtitle", new Vector2(0f, 70f), new Vector2(700f, 58f), 19f, FontStyles.Bold, new Color(0.24f, 0.15f, 0.08f, 1f));
        subtitleText.raycastTarget = false;

        GameObject summaryBox = CreateRect("VoyageSummaryBox", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -45f), new Vector2(680f, 160f));
        Image summaryBg = summaryBox.AddComponent<Image>();
        summaryBg.color = new Color(0.89f, 0.82f, 0.68f, 1f);
        summaryBg.raycastTarget = false;

        TMP_Text sumHeader = CreateText(summaryBox.transform, "SummaryHeader", new Vector2(0f, 56f), new Vector2(640f, 30f), 20f, FontStyles.Bold, new Color(0.32f, 0.16f, 0.08f, 1f));
        sumHeader.text = "VOYAGE SUMMARY";
        sumHeader.raycastTarget = false;

        statsText = CreateText(summaryBox.transform, "SummaryStats", new Vector2(0f, -16f), new Vector2(650f, 118f), 17f, FontStyles.Bold, new Color(0.18f, 0.12f, 0.06f, 1f));
        statsText.raycastTarget = false;

        CreateButton(paper.transform, "PlayAgainBtn", new Vector2(-150f, -188f), new Color(0.18f, 0.6f, 0.28f, 1f), "Play Again", OnPlayAgainClicked);
        CreateButton(paper.transform, "MainMenuBtn", new Vector2(150f, -188f), new Color(0.45f, 0.28f, 0.14f, 1f), "Main Menu", OnMainMenuClicked);

        overlayRoot.SetActive(false);
    }

    private void CreateButton(Transform parent, string name, Vector2 pos, Color bgColor, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(250f, 56f));
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        TMP_Text txt = CreateText(btnGo.transform, "Label", Vector2.zero, new Vector2(230f, 44f), 22f, FontStyles.Bold, new Color(1f, 0.96f, 0.86f, 1f));
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
