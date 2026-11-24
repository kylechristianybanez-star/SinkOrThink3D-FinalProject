using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Persistent wooden/parchment Bottom Hotbar and [I]-toggled Inventory UI.
/// Handles Plank (1) and Potion (2) boat repairs, dims empty slots, animates updates,
/// and prevents item usage during quizzes, menus, or Game Over.
/// </summary>
public class InventoryHotbarUI : MonoBehaviour
{
    public static InventoryHotbarUI Instance { get; private set; }

    private Canvas rootCanvas;
    private GameObject hotbarRoot;
    private GameObject inventoryOverlay;
    private RectTransform inventoryPanelRect;

    // Hotbar elements
    private RectTransform plankSlotRect;
    private RectTransform potionSlotRect;
    private CanvasGroup plankSlotGroup;
    private CanvasGroup potionSlotGroup;
    private TMP_Text plankCountText;
    private TMP_Text potionCountText;
    private TMP_Text hotbarCoinsText;
    private TMP_Text hotbarKeyText;
    private TMP_Text hotbarCompassText;

    // Inventory modal elements
    private TMP_Text invHealthText;
    private TMP_Text invCoinsQty;
    private TMP_Text invPlanksQty;
    private TMP_Text invPotionsQty;
    private TMP_Text invKeysQty;
    private TMP_Text invCompassQty;
    private TMP_Text invCrownQty;
    private Button invUsePlankBtn;
    private Button invUsePotionBtn;

    private int lastCoins = -1;
    private int lastPlanks = -1;
    private int lastPotions = -1;
    private float plankPulseTimer = 0f;
    private float potionPulseTimer = 0f;
    private float modalAnimTimer = 0f;

    public bool IsInventoryOpen => inventoryOverlay != null && inventoryOverlay.activeSelf;

    public static InventoryHotbarUI EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        InventoryHotbarUI existing = FindFirstObjectByType<InventoryHotbarUI>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("InventoryHotbarUI");
        Instance = go.AddComponent<InventoryHotbarUI>();
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
        GameStateManager.OnStateChanged += RefreshUI;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        GameStateManager.OnStateChanged -= RefreshUI;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        RefreshVisibilityForScene(SceneManager.GetActiveScene().name);
        RefreshUI();
    }

    void Update()
    {
        if (rootCanvas == null || !rootCanvas.enabled)
            return;

        AnimatePulses();

        if (Time.timeScale == 0f)
            return;

        if (IsGameplayBlockedByQuizOrGameOver())
        {
            if (IsInventoryOpen)
                SetInventoryOpen(false);
            return;
        }

        if (Input.GetKeyDown(KeyCode.I))
        {
            SetInventoryOpen(!IsInventoryOpen);
            return;
        }

        if (IsInventoryOpen && Input.GetKeyDown(KeyCode.Escape))
        {
            SetInventoryOpen(false);
            return;
        }

        if (!IsInventoryOpen)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1) || Input.GetKeyDown(KeyCode.Keypad1))
                UsePlankFromUI();
            else if (Input.GetKeyDown(KeyCode.Alpha2) || Input.GetKeyDown(KeyCode.Keypad2))
                UsePotionFromUI();
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsInventoryOpen)
            SetInventoryOpen(false);

        RefreshVisibilityForScene(scene.name);
        RefreshUI();
    }

    private void RefreshVisibilityForScene(string sceneName)
    {
        bool show = !string.Equals(sceneName, "MainMenu");
        if (rootCanvas != null)
            rootCanvas.enabled = show;
        if (hotbarRoot != null)
            hotbarRoot.SetActive(show);
    }

    private bool IsGameplayBlockedByQuizOrGameOver()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        if (state.boatHealth <= 0)
            return true;

        if (VictoryScreenUI.Instance != null && VictoryScreenUI.Instance.IsShowing)
            return true;

        QuizManager oceanQuiz = FindFirstObjectByType<QuizManager>();
        if (oceanQuiz != null && (oceanQuiz.IsQuizActive || oceanQuiz.IsGameFinished))
            return true;

        ChestQuizManager chestQuiz = FindFirstObjectByType<ChestQuizManager>();
        if (chestQuiz != null && chestQuiz.IsQuizOpen)
            return true;

        return false;
    }

    public void SetInventoryOpen(bool open)
    {
        if (inventoryOverlay == null)
            return;

        if (open && IsGameplayBlockedByQuizOrGameOver())
            return;

        inventoryOverlay.SetActive(open);
        if (open)
        {
            modalAnimTimer = 0.22f;
            RefreshUI();
        }

        IslandPlayerController islandPlayer = FindFirstObjectByType<IslandPlayerController>();
        if (islandPlayer != null)
            islandPlayer.SetControlsEnabled(!open);

        BoatMovement boatMove = FindFirstObjectByType<BoatMovement>();
        if (boatMove != null)
            boatMove.canMove = !open;

        FirstPersonMouseLook mouseLook = FindFirstObjectByType<FirstPersonMouseLook>();
        if (mouseLook != null)
            mouseLook.enabled = !open;

        if (open)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    public void UsePlankFromUI()
    {
        if (IsGameplayBlockedByQuizOrGameOver())
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        bool ok = state.TryUsePlank(out int restored, out string feedback);
        plankPulseTimer = 0.28f;

        if (ok)
        {
            BoatSink sink = FindFirstObjectByType<BoatSink>();
            if (sink != null)
                sink.SyncFromGameState(true);
        }

        ScreenFader.EnsureInstance().ShowArrivalBanner(feedback, 2.2f);
        RefreshUI();
    }

    public void UsePotionFromUI()
    {
        if (IsGameplayBlockedByQuizOrGameOver())
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        bool ok = state.TryUsePotion(out int restored, out string feedback);
        potionPulseTimer = 0.28f;

        if (ok)
        {
            BoatSink sink = FindFirstObjectByType<BoatSink>();
            if (sink != null)
                sink.SyncFromGameState(true);
        }

        ScreenFader.EnsureInstance().ShowArrivalBanner(feedback, 2.2f);
        RefreshUI();
    }

    public void RefreshUI()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        int idx = Mathf.Clamp(state.currentIslandIndex, 0, 2);

        if (lastPlanks >= 0 && state.planks != lastPlanks)
            plankPulseTimer = 0.28f;
        if (lastPotions >= 0 && state.potions != lastPotions)
            potionPulseTimer = 0.28f;

        lastCoins = state.coins;
        lastPlanks = state.planks;
        lastPotions = state.potions;

        if (plankCountText != null)
            plankCountText.text = "x" + state.planks;
        if (potionCountText != null)
            potionCountText.text = "x" + state.potions;

        if (plankSlotGroup != null)
            plankSlotGroup.alpha = state.planks > 0 ? 1f : 0.45f;
        if (potionSlotGroup != null)
            potionSlotGroup.alpha = state.potions > 0 ? 1f : 0.45f;

        if (hotbarCoinsText != null)
            hotbarCoinsText.text = "Coins: " + state.coins;
        if (hotbarKeyText != null)
            hotbarKeyText.text = state.hasIslandKey[idx] ? "Key: READY" : "Key: None";
        if (hotbarCompassText != null)
            hotbarCompassText.text = state.hasMagicCompass ? "Compass: ON" : "Compass: Locked";

        if (invHealthText != null)
            invHealthText.text = "Boat Health: " + state.boatHealth + " / " + state.maxBoatHealth;
        if (invCoinsQty != null)
            invCoinsQty.text = state.coins.ToString();
        if (invPlanksQty != null)
            invPlanksQty.text = "x" + state.planks;
        if (invPotionsQty != null)
            invPotionsQty.text = "x" + state.potions;

        if (invKeysQty != null)
        {
            string k0 = state.hasIslandKey[0] ? "Discovery" : (state.completedIslandSpecialChest[0] ? "Used" : "-");
            string k1 = state.hasIslandKey[1] ? "Viking" : (state.completedIslandSpecialChest[1] ? "Used" : "-");
            string k2 = state.hasIslandKey[2] ? "Final" : (state.completedIslandSpecialChest[2] ? "Used" : "-");
            invKeysQty.text = k0 + " | " + k1 + " | " + k2;
        }

        if (invCompassQty != null)
            invCompassQty.text = state.hasMagicCompass ? "Unlocked" : "Locked";
        if (invCrownQty != null)
            invCrownQty.text = state.hasGoldenCrown ? "Unlocked" : "Locked";

        bool canHeal = state.boatHealth > 0 && state.boatHealth < state.maxBoatHealth;
        if (invUsePlankBtn != null)
            invUsePlankBtn.interactable = state.planks > 0 && canHeal;
        if (invUsePotionBtn != null)
            invUsePotionBtn.interactable = state.potions > 0 && canHeal;
    }

    private void AnimatePulses()
    {
        if (plankPulseTimer > 0f && plankSlotRect != null)
        {
            plankPulseTimer = Mathf.Max(0f, plankPulseTimer - Time.unscaledDeltaTime);
            float scale = 1f + Mathf.Sin((plankPulseTimer / 0.28f) * Mathf.PI) * 0.14f;
            plankSlotRect.localScale = Vector3.one * scale;
        }
        else if (plankSlotRect != null)
        {
            plankSlotRect.localScale = Vector3.one;
        }

        if (potionPulseTimer > 0f && potionSlotRect != null)
        {
            potionPulseTimer = Mathf.Max(0f, potionPulseTimer - Time.unscaledDeltaTime);
            float scale = 1f + Mathf.Sin((potionPulseTimer / 0.28f) * Mathf.PI) * 0.14f;
            potionSlotRect.localScale = Vector3.one * scale;
        }
        else if (potionSlotRect != null)
        {
            potionSlotRect.localScale = Vector3.one;
        }

        if (modalAnimTimer > 0f && inventoryPanelRect != null)
        {
            modalAnimTimer = Mathf.Max(0f, modalAnimTimer - Time.unscaledDeltaTime);
            float t = 1f - (modalAnimTimer / 0.22f);
            float scale = Mathf.Lerp(0.88f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            inventoryPanelRect.localScale = Vector3.one * scale;
        }
    }

    private void BuildUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 50;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        gameObject.AddComponent<GraphicRaycaster>();

        BuildHotbar();
        BuildInventoryOverlay();
    }

    private void BuildHotbar()
    {
        hotbarRoot = CreateRect("BottomHotbar", transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 38f), new Vector2(820f, 76f));
        Image bg = hotbarRoot.AddComponent<Image>();
        bg.color = new Color(0.24f, 0.15f, 0.08f, 0.92f);
        bg.raycastTarget = false;

        // Slot 1: Plank
        GameObject slot1 = CreateRect("Slot1_Plank", hotbarRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-315f, 0f), new Vector2(160f, 58f));
        plankSlotRect = slot1.GetComponent<RectTransform>();
        plankSlotGroup = slot1.AddComponent<CanvasGroup>();
        Image s1Bg = slot1.AddComponent<Image>();
        s1Bg.color = new Color(0.42f, 0.28f, 0.15f, 1f);
        s1Bg.raycastTarget = false;
        CreateBadge(slot1.transform, new Vector2(-52f, 0f), new Color(0.68f, 0.45f, 0.22f, 1f), "P");
        CreateHotbarLabel(slot1.transform, new Vector2(16f, 11f), 16f, "[1] Plank");
        plankCountText = CreateHotbarLabel(slot1.transform, new Vector2(16f, -12f), 18f, "x0");

        // Slot 2: Potion
        GameObject slot2 = CreateRect("Slot2_Potion", hotbarRoot.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-142f, 0f), new Vector2(160f, 58f));
        potionSlotRect = slot2.GetComponent<RectTransform>();
        potionSlotGroup = slot2.AddComponent<CanvasGroup>();
        Image s2Bg = slot2.AddComponent<Image>();
        s2Bg.color = new Color(0.42f, 0.28f, 0.15f, 1f);
        s2Bg.raycastTarget = false;
        CreateBadge(slot2.transform, new Vector2(-52f, 0f), new Color(0.18f, 0.72f, 0.38f, 1f), "+");
        CreateHotbarLabel(slot2.transform, new Vector2(16f, 11f), 16f, "[2] Potion");
        potionCountText = CreateHotbarLabel(slot2.transform, new Vector2(16f, -12f), 18f, "x0");

        // Coins, Key, Compass, Bag Hint
        hotbarCoinsText = CreateStatusBox(hotbarRoot.transform, "HotbarCoins", new Vector2(18f, 0f), new Vector2(138f, 52f), new Color(0.98f, 0.85f, 0.35f, 1f), "Coins: 0");
        hotbarKeyText = CreateStatusBox(hotbarRoot.transform, "HotbarKey", new Vector2(162f, 0f), new Vector2(132f, 52f), new Color(1f, 0.92f, 0.65f, 1f), "Key: None");
        hotbarCompassText = CreateStatusBox(hotbarRoot.transform, "HotbarCompass", new Vector2(314f, 0f), new Vector2(158f, 52f), new Color(0.75f, 0.94f, 1f, 1f), "Compass: Locked");

        TMP_Text bagHint = CreateHotbarLabel(hotbarRoot.transform, new Vector2(314f, -38f), 14f, "Press [I] for Inventory");
        bagHint.rectTransform.sizeDelta = new Vector2(220f, 22f);
    }

    private void BuildInventoryOverlay()
    {
        inventoryOverlay = CreateRect("InventoryOverlay", transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image dim = inventoryOverlay.AddComponent<Image>();
        dim.color = new Color(0f, 0f, 0f, 0.62f);

        GameObject frame = CreateRect("InventoryWoodFrame", inventoryOverlay.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(820f, 590f));
        inventoryPanelRect = frame.GetComponent<RectTransform>();
        Image frameImg = frame.AddComponent<Image>();
        frameImg.color = new Color(0.32f, 0.2f, 0.1f, 0.98f);

        GameObject paper = CreateRect("Parchment", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform pRt = paper.GetComponent<RectTransform>();
        pRt.offsetMin = new Vector2(16f, 16f);
        pRt.offsetMax = new Vector2(-16f, -16f);
        Image pImg = paper.AddComponent<Image>();
        pImg.color = new Color(0.96f, 0.91f, 0.8f, 1f);

        TMP_Text header = CreatePanelText(paper.transform, "Header", new Vector2(0f, 240f), new Vector2(720f, 44f), 30f, new Color(0.24f, 0.14f, 0.05f, 1f), TextAlignmentOptions.Center);
        header.text = "ADVENTURER'S INVENTORY";

        invHealthText = CreatePanelText(paper.transform, "BoatHealth", new Vector2(0f, 200f), new Vector2(720f, 32f), 20f, new Color(0.18f, 0.48f, 0.2f, 1f), TextAlignmentOptions.Center);

        invCoinsQty = CreateInventoryRow(paper.transform, 140f, new Color(0.95f, 0.78f, 0.2f, 1f), "Gold Coins", "Spend 10 Coins at the Dock Repair Bench (+20 HP).", false, null, out _);
        invPlanksQty = CreateInventoryRow(paper.transform, 70f, new Color(0.68f, 0.45f, 0.22f, 1f), "Wooden Planks [Key 1]", "Patches boat leaks immediately (+10 Boat HP).", true, UsePlankFromUI, out invUsePlankBtn);
        invPotionsQty = CreateInventoryRow(paper.transform, 0f, new Color(0.18f, 0.75f, 0.38f, 1f), "Repair Potions [Key 2]", "Restores major hull strength immediately (+25 Boat HP).", true, UsePotionFromUI, out invUsePotionBtn);
        invKeysQty = CreateInventoryRow(paper.transform, -70f, new Color(0.96f, 0.8f, 0.24f, 1f), "Island Golden Keys", "Unlocks each island's Special Treasure Chest.", false, null, out _);
        invCompassQty = CreateInventoryRow(paper.transform, -140f, new Color(0.26f, 0.66f, 0.86f, 1f), "Magic Compass", "Guides you toward unopened island treasure chests.", false, null, out _);
        invCrownQty = CreateInventoryRow(paper.transform, -205f, new Color(0.9f, 0.28f, 0.24f, 1f), "Legendary Golden Crown", "Grand treasure of Final Treasure Island.", false, null, out _);

        GameObject closeBtnGo = CreateRect("CloseInventoryBtn", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -254f), new Vector2(210f, 40f));
        Image closeBg = closeBtnGo.AddComponent<Image>();
        closeBg.color = new Color(0.45f, 0.25f, 0.14f, 1f);
        Button closeBtn = closeBtnGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => SetInventoryOpen(false));
        TMP_Text closeLbl = CreatePanelText(closeBtnGo.transform, "Lbl", Vector2.zero, new Vector2(190f, 34f), 18f, new Color(1f, 0.94f, 0.82f, 1f), TextAlignmentOptions.Center);
        closeLbl.text = "Close (I / Esc)";
        closeLbl.raycastTarget = false;

        inventoryOverlay.SetActive(false);
    }

    private TMP_Text CreateInventoryRow(Transform parent, float y, Color badgeColor, string title, string desc, bool hasUseButton, UnityEngine.Events.UnityAction onUse, out Button useBtn)
    {
        GameObject row = CreateRect("Row_" + title, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, y), new Vector2(730f, 58f));
        Image bg = row.AddComponent<Image>();
        bg.color = new Color(0.9f, 0.84f, 0.7f, 1f);
        bg.raycastTarget = false;

        GameObject badge = CreateRect("Icon", row.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(30f, 0f), new Vector2(38f, 38f));
        Image bImg = badge.AddComponent<Image>();
        bImg.color = badgeColor;
        bImg.raycastTarget = false;

        TMP_Text titleTxt = CreatePanelText(row.transform, "Title", new Vector2(-105f, 11f), new Vector2(370f, 24f), 18f, new Color(0.2f, 0.12f, 0.05f, 1f), TextAlignmentOptions.Left);
        titleTxt.text = title;
        titleTxt.raycastTarget = false;

        TMP_Text descTxt = CreatePanelText(row.transform, "Desc", new Vector2(-105f, -12f), new Vector2(370f, 22f), 14f, new Color(0.38f, 0.28f, 0.16f, 1f), TextAlignmentOptions.Left);
        descTxt.text = desc;
        descTxt.raycastTarget = false;

        TMP_Text qtyTxt = CreatePanelText(row.transform, "Qty", new Vector2(hasUseButton ? 155f : 230f, 0f), new Vector2(200f, 34f), 18f, new Color(0.22f, 0.12f, 0.04f, 1f), TextAlignmentOptions.Right);
        qtyTxt.raycastTarget = false;

        useBtn = null;
        if (hasUseButton)
        {
            GameObject btnGo = CreateRect("UseBtn", row.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(308f, 0f), new Vector2(92f, 38f));
            Image uImg = btnGo.AddComponent<Image>();
            uImg.color = new Color(0.22f, 0.58f, 0.25f, 1f);
            useBtn = btnGo.AddComponent<Button>();
            if (onUse != null)
                useBtn.onClick.AddListener(onUse);

            TMP_Text uLbl = CreatePanelText(btnGo.transform, "Lbl", Vector2.zero, new Vector2(84f, 32f), 16f, Color.white, TextAlignmentOptions.Center);
            uLbl.text = "Use";
            uLbl.raycastTarget = false;
        }

        return qtyTxt;
    }

    private void CreateBadge(Transform parent, Vector2 pos, Color color, string letter)
    {
        GameObject badge = CreateRect("Badge", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(36f, 36f));
        Image img = badge.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        TMP_Text txt = CreateHotbarLabel(badge.transform, Vector2.zero, 20f, letter);
        txt.rectTransform.sizeDelta = new Vector2(36f, 36f);
    }

    private TMP_Text CreateHotbarLabel(Transform parent, Vector2 pos, float fontSize, string text)
    {
        GameObject go = CreateRect("Label", parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, new Vector2(104f, 24f));
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.94f, 0.82f, 1f);
        tmp.raycastTarget = false;
        return tmp;
    }

    private TMP_Text CreateStatusBox(Transform parent, string name, Vector2 pos, Vector2 size, Color textColor, string text)
    {
        GameObject box = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        Image bg = box.AddComponent<Image>();
        bg.color = new Color(0.16f, 0.1f, 0.05f, 0.9f);
        bg.raycastTarget = false;

        GameObject txtGo = CreateRect("Text", box.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = txtGo.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = 16f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = textColor;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static TMP_Text CreatePanelText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, Color color, TextAlignmentOptions align)
    {
        GameObject go = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = align;
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
