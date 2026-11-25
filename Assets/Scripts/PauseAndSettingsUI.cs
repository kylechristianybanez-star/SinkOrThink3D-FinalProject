using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Persistent Pause Menu (Escape during gameplay) and Settings Screen (accessible from
/// both Pause Menu and Main Menu) controlling Master, Ambience, SFX volume and Mouse Sensitivity.
/// </summary>
public class PauseAndSettingsUI : MonoBehaviour
{
    public static PauseAndSettingsUI Instance { get; private set; }

    public const string MouseSensitivityKey = "SOT_MouseSensitivity";

    private Canvas rootCanvas;
    private GameObject backdropRoot;
    private GameObject pausePanelRoot;
    private GameObject settingsPanelRoot;

    private Slider masterSlider;
    private Slider musicSlider;
    private Slider ambientSlider;
    private Slider sfxSlider;
    private Slider sensitivitySlider;

    private TMP_Text masterValueText;
    private TMP_Text musicValueText;
    private TMP_Text ambientValueText;
    private TMP_Text sfxValueText;
    private TMP_Text sensitivityValueText;

    private bool openedSettingsFromPause = false;
    private bool otherModalWasOpenLastFrame = false;
    private bool suppressSliderEvents = false;
    private QuizManager cachedOceanQuiz;
    private ChestQuizManager cachedChestQuiz;
    private bool sceneRefsCached = false;

    public bool IsOpen =>
        (pausePanelRoot != null && pausePanelRoot.activeSelf) ||
        (settingsPanelRoot != null && settingsPanelRoot.activeSelf);

    public static float GetSavedMouseSensitivity()
    {
        return PlayerPrefs.GetFloat(MouseSensitivityKey, 2.0f);
    }

    public static PauseAndSettingsUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        PauseAndSettingsUI existing = FindAnyObjectByType<PauseAndSettingsUI>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("PauseAndSettingsUI");
        Instance = go.AddComponent<PauseAndSettingsUI>();
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
        ApplySavedSensitivityToScene();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        RefreshSceneReferences();
    }

    private void RefreshSceneReferences()
    {
        cachedOceanQuiz = FindAnyObjectByType<QuizManager>();
        cachedChestQuiz = FindAnyObjectByType<ChestQuizManager>();
        sceneRefsCached = true;
    }

    void Update()
    {
        bool otherModalOpenNow = IsBlockedByOtherModal();

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            string sceneName = SceneManager.GetActiveScene().name;
            if (string.Equals(sceneName, "MainMenu"))
            {
                if (IsOpen)
                    CloseAll();
            }
            else if (!otherModalOpenNow && !otherModalWasOpenLastFrame)
            {
                if (settingsPanelRoot != null && settingsPanelRoot.activeSelf)
                {
                    BackFromSettings();
                }
                else if (pausePanelRoot != null && pausePanelRoot.activeSelf)
                {
                    ResumeGame();
                }
                else
                {
                    OpenPause();
                }
            }
        }

        otherModalWasOpenLastFrame = otherModalOpenNow;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        sceneRefsCached = false;
        CloseAll();
        RefreshSceneReferences();
        ApplySavedSensitivityToScene();
    }

    private bool IsBlockedByOtherModal()
    {
        if (VictoryScreenUI.Instance != null && VictoryScreenUI.Instance.IsShowing)
            return true;

        if (GameOverUI.Instance != null && GameOverUI.Instance.IsShowing)
            return true;

        if (InventoryHotbarUI.Instance != null && InventoryHotbarUI.Instance.IsInventoryOpen)
            return true;

        if (!sceneRefsCached)
            RefreshSceneReferences();

        if (cachedChestQuiz != null && cachedChestQuiz.IsQuizOpen)
            return true;

        if (IslandGuideNPC.AnyDialogueOpen)
            return true;

        if (cachedOceanQuiz != null && cachedOceanQuiz.IsGameFinished)
            return true;

        return false;
    }

    public void OpenPause()
    {
        if (backdropRoot == null)
            BuildUI();

        if (IsBlockedByOtherModal())
            return;

        GameAudioManager.EnsureInstance().PlayClick();
        Time.timeScale = 0f;

        backdropRoot.SetActive(true);
        pausePanelRoot.SetActive(true);
        settingsPanelRoot.SetActive(false);

        SetSceneControlsEnabled(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ResumeGame()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        CloseAll();

        if (!string.Equals(SceneManager.GetActiveScene().name, "MainMenu"))
            SetSceneControlsEnabled(true);
    }

    public void OpenSettingsFromPause()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        openedSettingsFromPause = true;
        RefreshSlidersFromSaved();

        backdropRoot.SetActive(true);
        pausePanelRoot.SetActive(false);
        settingsPanelRoot.SetActive(true);
    }

    public void OpenSettingsStandalone()
    {
        if (backdropRoot == null)
            BuildUI();

        GameAudioManager.EnsureInstance().PlayClick();
        openedSettingsFromPause = false;
        RefreshSlidersFromSaved();

        backdropRoot.SetActive(true);
        pausePanelRoot.SetActive(false);
        settingsPanelRoot.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void BackFromSettings()
    {
        GameAudioManager.EnsureInstance().PlayClick();

        if (openedSettingsFromPause)
        {
            pausePanelRoot.SetActive(true);
            settingsPanelRoot.SetActive(false);
        }
        else
        {
            CloseAll();
        }
    }

    public void CloseAll()
    {
        if (backdropRoot != null) backdropRoot.SetActive(false);
        if (pausePanelRoot != null) pausePanelRoot.SetActive(false);
        if (settingsPanelRoot != null) settingsPanelRoot.SetActive(false);

        if (VictoryScreenUI.Instance == null || !VictoryScreenUI.Instance.IsShowing)
            Time.timeScale = 1f;
    }

    private void OnReturnToMainMenu()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        CloseAll();
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        ScreenFader.EnsureInstance().FadeAndLoadScene("MainMenu", 0.65f);
    }

    private void OnQuitClicked()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        Debug.Log("Quit Game from Pause Menu");
        Application.Quit();
    }

    private void OnResetDefaultsClicked()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        GameAudioManager.EnsureInstance().ApplyVolumes(0.85f, 0.42f, 0.35f, 0.65f, true);
        PlayerPrefs.SetFloat(MouseSensitivityKey, 2.0f);
        PlayerPrefs.Save();
        ApplySavedSensitivityToScene();
        RefreshSlidersFromSaved();
    }

    private void OnSliderChanged()
    {
        if (suppressSliderEvents)
            return;

        float m = masterSlider != null ? masterSlider.value : 0.85f;
        float mu = musicSlider != null ? musicSlider.value : 0.42f;
        float a = ambientSlider != null ? ambientSlider.value : 0.35f;
        float s = sfxSlider != null ? sfxSlider.value : 0.65f;
        float sens = sensitivitySlider != null ? sensitivitySlider.value : 2.0f;

        GameAudioManager.EnsureInstance().ApplyVolumes(m, mu, a, s, true);
        PlayerPrefs.SetFloat(MouseSensitivityKey, sens);
        PlayerPrefs.Save();

        ApplySavedSensitivityToScene();
        UpdateSliderValueLabels(m, mu, a, s, sens);
    }

    private void RefreshSlidersFromSaved()
    {
        suppressSliderEvents = true;

        GameAudioManager audioMgr = GameAudioManager.EnsureInstance();
        float m = audioMgr.MasterVolume;
        float mu = audioMgr.MusicVolume;
        float a = audioMgr.AmbientVolume;
        float s = audioMgr.SfxVolume;
        float sens = GetSavedMouseSensitivity();

        if (masterSlider != null) masterSlider.value = m;
        if (musicSlider != null) musicSlider.value = mu;
        if (ambientSlider != null) ambientSlider.value = a;
        if (sfxSlider != null) sfxSlider.value = s;
        if (sensitivitySlider != null) sensitivitySlider.value = sens;

        UpdateSliderValueLabels(m, mu, a, s, sens);
        suppressSliderEvents = false;
    }

    private void UpdateSliderValueLabels(float m, float mu, float a, float s, float sens)
    {
        if (masterValueText != null) masterValueText.text = Mathf.RoundToInt(m * 100f) + "%";
        if (musicValueText != null) musicValueText.text = Mathf.RoundToInt(mu * 100f) + "%";
        if (ambientValueText != null) ambientValueText.text = Mathf.RoundToInt(a * 100f) + "%";
        if (sfxValueText != null) sfxValueText.text = Mathf.RoundToInt(s * 100f) + "%";
        if (sensitivityValueText != null) sensitivityValueText.text = sens.ToString("0.0");
    }

    private void ApplySavedSensitivityToScene()
    {
        float sens = GetSavedMouseSensitivity();

        FirstPersonMouseLook boatLook = FindAnyObjectByType<FirstPersonMouseLook>();
        if (boatLook != null)
            boatLook.mouseSensitivity = sens;

        IslandPlayerController islandPlayer = FindAnyObjectByType<IslandPlayerController>();
        if (islandPlayer != null)
            islandPlayer.mouseSensitivity = sens;
    }

    private void SetSceneControlsEnabled(bool enabled)
    {
        IslandPlayerController islandPlayer = FindAnyObjectByType<IslandPlayerController>();
        if (islandPlayer != null)
        {
            ChestQuizManager chestQuiz = FindAnyObjectByType<ChestQuizManager>();
            bool keepLockedForQuiz = chestQuiz != null && chestQuiz.IsQuizOpen;
            if (!enabled || !keepLockedForQuiz)
                islandPlayer.SetControlsEnabled(enabled);
        }

        BoatMovement boatMove = FindAnyObjectByType<BoatMovement>();
        QuizManager oceanQuiz = FindAnyObjectByType<QuizManager>();
        bool oceanQuizOpen = oceanQuiz != null && oceanQuiz.IsQuizActive;

        if (boatMove != null)
            boatMove.canMove = enabled && !oceanQuizOpen;

        FirstPersonMouseLook mouseLook = FindAnyObjectByType<FirstPersonMouseLook>();
        if (mouseLook != null)
            mouseLook.enabled = enabled && !oceanQuizOpen;
    }

    private void BuildUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 125;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        backdropRoot = CreateRect("PauseSettingsBackdrop", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = backdropRoot.AddComponent<Image>();
        dim.color = new Color(0.04f, 0.05f, 0.08f, 0.72f);

        BuildPausePanel(backdropRoot.transform);
        BuildSettingsPanel(backdropRoot.transform);

        backdropRoot.SetActive(false);
        pausePanelRoot.SetActive(false);
        settingsPanelRoot.SetActive(false);
    }

    private void BuildPausePanel(Transform parent)
    {
        pausePanelRoot = CreateWoodenCard(parent, "PauseMenuCard", new Vector2(520f, 470f), out GameObject paper);

        TMP_Text title = CreateText(paper.transform, "Title", new Vector2(0f, 170f), new Vector2(440f, 52f), 34f, FontStyles.Bold, new Color(0.24f, 0.14f, 0.05f, 1f));
        title.text = "GAME PAUSED";
        title.raycastTarget = false;

        CreateButton(paper.transform, "ResumeBtn", new Vector2(0f, 82f), new Vector2(300f, 54f), new Color(0.18f, 0.58f, 0.28f, 1f), "Resume", ResumeGame);
        CreateButton(paper.transform, "SettingsBtn", new Vector2(0f, 14f), new Vector2(300f, 54f), new Color(0.24f, 0.45f, 0.68f, 1f), "Settings", OpenSettingsFromPause);
        CreateButton(paper.transform, "MainMenuBtn", new Vector2(0f, -54f), new Vector2(300f, 54f), new Color(0.52f, 0.34f, 0.16f, 1f), "Main Menu", OnReturnToMainMenu);
        CreateButton(paper.transform, "QuitBtn", new Vector2(0f, -122f), new Vector2(300f, 54f), new Color(0.68f, 0.2f, 0.2f, 1f), "Quit Game", OnQuitClicked);
    }

    private void BuildSettingsPanel(Transform parent)
    {
        settingsPanelRoot = CreateWoodenCard(parent, "SettingsCard", new Vector2(680f, 580f), out GameObject paper);

        TMP_Text title = CreateText(paper.transform, "Title", new Vector2(0f, 228f), new Vector2(580f, 48f), 32f, FontStyles.Bold, new Color(0.24f, 0.14f, 0.05f, 1f));
        title.text = "SETTINGS";
        title.raycastTarget = false;

        masterSlider = CreateSliderRow(paper.transform, "Master Volume", 148f, 0f, 1f, out masterValueText);
        musicSlider = CreateSliderRow(paper.transform, "Music Volume", 84f, 0f, 1f, out musicValueText);
        ambientSlider = CreateSliderRow(paper.transform, "Ambience Volume", 20f, 0f, 1f, out ambientValueText);
        sfxSlider = CreateSliderRow(paper.transform, "SFX Volume", -44f, 0f, 1f, out sfxValueText);
        sensitivitySlider = CreateSliderRow(paper.transform, "Mouse Sensitivity", -108f, 0.5f, 5.0f, out sensitivityValueText);

        CreateButton(paper.transform, "ResetBtn", new Vector2(-135f, -206f), new Vector2(220f, 50f), new Color(0.52f, 0.38f, 0.2f, 1f), "Reset Defaults", OnResetDefaultsClicked);
        CreateButton(paper.transform, "BackBtn", new Vector2(135f, -206f), new Vector2(220f, 50f), new Color(0.2f, 0.56f, 0.28f, 1f), "Back", BackFromSettings);

        RefreshSlidersFromSaved();
    }

    private Slider CreateSliderRow(Transform parent, string label, float y, float minVal, float maxVal, out TMP_Text valueText)
    {
        GameObject row = CreateRect("Row_" + label, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(590f, 52f));
        Image rowBg = row.AddComponent<Image>();
        rowBg.color = new Color(0.91f, 0.84f, 0.7f, 1f);
        rowBg.raycastTarget = false;

        TMP_Text lbl = CreateText(row.transform, "Label", new Vector2(-175f, 0f), new Vector2(210f, 36f), 20f, FontStyles.Bold, new Color(0.2f, 0.12f, 0.05f, 1f));
        lbl.text = label;
        lbl.alignment = TextAlignmentOptions.Left;
        lbl.raycastTarget = false;

        // Slider root + Fill + Handle
        GameObject sliderGo = CreateRect("Slider", row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(55f, 0f), new Vector2(240f, 28f));
        Slider slider = sliderGo.AddComponent<Slider>();
        slider.minValue = minVal;
        slider.maxValue = maxVal;

        GameObject bgGo = CreateRect("Background", sliderGo.transform, new Vector2(0f, 0.25f), new Vector2(1f, 0.75f), Vector2.zero, Vector2.zero);
        Image bgImg = bgGo.AddComponent<Image>();
        bgImg.color = new Color(0.45f, 0.34f, 0.22f, 1f);

        GameObject fillArea = CreateRect("FillArea", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, 0f));
        GameObject fillGo = CreateRect("Fill", fillArea.transform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
        Image fillImg = fillGo.AddComponent<Image>();
        fillImg.color = new Color(0.24f, 0.68f, 0.34f, 1f);

        GameObject handleArea = CreateRect("HandleSlideArea", sliderGo.transform, Vector2.zero, Vector2.one, Vector2.zero, new Vector2(-16f, 0f));
        GameObject handleGo = CreateRect("Handle", handleArea.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(22f, 26f));
        Image handleImg = handleGo.AddComponent<Image>();
        handleImg.color = new Color(0.95f, 0.78f, 0.25f, 1f);

        slider.fillRect = fillGo.GetComponent<RectTransform>();
        slider.handleRect = handleGo.GetComponent<RectTransform>();
        slider.targetGraphic = handleImg;
        slider.direction = Slider.Direction.LeftToRight;
        slider.onValueChanged.AddListener(_ => OnSliderChanged());

        valueText = CreateText(row.transform, "Value", new Vector2(232f, 0f), new Vector2(88f, 36f), 19f, FontStyles.Bold, new Color(0.22f, 0.14f, 0.05f, 1f));
        valueText.alignment = TextAlignmentOptions.Right;
        valueText.raycastTarget = false;

        return slider;
    }

    private GameObject CreateWoodenCard(Transform parent, string name, Vector2 size, out GameObject paper)
    {
        GameObject frame = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Image frameImg = frame.AddComponent<Image>();
        frameImg.color = new Color(0.34f, 0.21f, 0.1f, 0.98f);

        paper = CreateRect("Parchment", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform pRt = paper.GetComponent<RectTransform>();
        pRt.offsetMin = new Vector2(16f, 16f);
        pRt.offsetMax = new Vector2(-16f, -16f);
        Image pImg = paper.AddComponent<Image>();
        pImg.color = new Color(0.96f, 0.91f, 0.8f, 1f);

        return frame;
    }

    private void CreateButton(Transform parent, string name, Vector2 pos, Vector2 size, Color bgColor, string label, UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnGo = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image btnImg = btnGo.AddComponent<Image>();
        btnImg.color = bgColor;

        Button btn = btnGo.AddComponent<Button>();
        ColorBlock colors = btn.colors;
        colors.highlightedColor = new Color(1.12f, 1.12f, 1.12f, 1f);
        colors.pressedColor = new Color(0.84f, 0.84f, 0.84f, 1f);
        btn.colors = colors;
        btn.onClick.AddListener(onClick);

        TMP_Text txt = CreateText(btnGo.transform, "Label", Vector2.zero, size - new Vector2(16f, 8f), 22f, FontStyles.Bold, new Color(1f, 0.96f, 0.86f, 1f));
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
