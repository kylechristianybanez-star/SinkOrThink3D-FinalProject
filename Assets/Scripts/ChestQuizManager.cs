using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages educational questions when opening island treasure chests, enforces Golden Key
/// rules on special chests, persists randomized rewards, and triggers animated reward reveals.
/// </summary>
public class ChestQuizManager : MonoBehaviour
{
    public int islandIndex = 0;

    private GameObject quizCanvasGo;
    private RectTransform panelRect;
    private TMP_Text titleText;
    private TMP_Text subtitleText;
    private TMP_Text questionText;
    private TMP_Text feedbackText;
    private readonly Button[] choiceButtons = new Button[4];
    private readonly Image[] choiceImages = new Image[4];
    private readonly TMP_Text[] choiceTexts = new TMP_Text[4];

    private TreasureChest activeChest;
    private IslandPlayerController activePlayer;
    private QuizQuestion activeQuestion;
    private int displayedCorrectIndex = 0;
    private bool awaitingResponse = false;
    private bool isOpen = false;
    private bool hintUsedThisQuestion = false;
    private Button hintButton;
    private TMP_Text hintButtonText;
    private readonly List<int> recentQuestionIndices = new List<int>();
    private float panelAnimTimer = 0f;

    private readonly Color normalBtnColor = new Color(0.95f, 0.88f, 0.72f, 1f);
    private readonly Color correctBtnColor = new Color(0.24f, 0.78f, 0.34f, 1f);
    private readonly Color wrongBtnColor = new Color(0.86f, 0.24f, 0.24f, 1f);

    public bool IsQuizOpen => isOpen;

    public static ChestQuizManager EnsureInScene(int targetIslandIndex)
    {
        ChestQuizManager existing = FindAnyObjectByType<ChestQuizManager>();
        if (existing != null)
        {
            existing.islandIndex = targetIslandIndex;
            return existing;
        }

        GameObject go = new GameObject("ChestQuizManager");
        ChestQuizManager mgr = go.AddComponent<ChestQuizManager>();
        mgr.islandIndex = targetIslandIndex;
        return mgr;
    }

    void OnEnable()
    {
        TreasureChest.OnChestInteractRequested += HandleChestInteract;
    }

    void OnDisable()
    {
        TreasureChest.OnChestInteractRequested -= HandleChestInteract;
    }

    void Start()
    {
        AssignIslandChestRewards();
        BuildQuizUI();
        if (quizCanvasGo != null)
            quizCanvasGo.SetActive(false);
    }

    void Update()
    {
        if (!isOpen)
            return;

        if (panelAnimTimer > 0f && panelRect != null)
        {
            panelAnimTimer = Mathf.Max(0f, panelAnimTimer - Time.unscaledDeltaTime);
            float t = 1f - (panelAnimTimer / 0.22f);
            float scale = Mathf.Lerp(0.86f, 1f, 1f - Mathf.Pow(1f - t, 3f));
            panelRect.localScale = Vector3.one * scale;
        }

        if (awaitingResponse && Input.GetKeyDown(KeyCode.Escape))
            CloseQuizPanel(true);
    }

    private void AssignIslandChestRewards()
    {
        TreasureChest[] allChests = FindObjectsByType<TreasureChest>();
        List<string> regularIds = new List<string>();

        for (int i = 0; i < allChests.Length; i++)
        {
            TreasureChest chest = allChests[i];
            if (chest != null && chest.islandIndex == islandIndex && !chest.isSpecialChest)
                regularIds.Add(chest.chestId);
        }

        regularIds.Sort();
        GameStateManager.EnsureInstance().EnsureIslandRewardsAssigned(islandIndex, regularIds.ToArray());
    }

    public void RefreshForIsland(int targetIslandIndex)
    {
        islandIndex = targetIslandIndex;
        AssignIslandChestRewards();
    }

    private void HandleChestInteract(TreasureChest chest, IslandPlayerController player)
    {
        if (chest == null || chest.IsOpened || isOpen)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();

        if (chest.isSpecialChest && !state.hasIslandKey[chest.islandIndex])
        {
            chest.PlayLockedRattle();
            ScreenFader.EnsureInstance().ShowArrivalBanner("You need a Golden Key to unlock this chest.", 2.6f);
            return;
        }

        activeChest = chest;
        activePlayer = player;
        OpenQuizForChest(chest);
    }

    private void OpenQuizForChest(TreasureChest chest)
    {
        if (quizCanvasGo == null)
            BuildQuizUI();

        QuizDifficulty difficulty = chest.isSpecialChest
            ? (QuizDifficulty)Mathf.Min(2, chest.islandIndex + 1)
            : (QuizDifficulty)Mathf.Clamp(chest.islandIndex, 0, 2);

        QuizQuestion[] pool = QuestionBank.GetQuestions(difficulty);
        if (pool == null || pool.Length == 0)
            return;

        int chosenIndex = PickFreshQuestionIndex(pool.Length);
        QuizQuestion q = pool[chosenIndex];
        activeQuestion = q;
        hintUsedThisQuestion = false;

        int[] shuffle = new int[] { 0, 1, 2, 3 };
        for (int i = 3; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            int tmp = shuffle[i];
            shuffle[i] = shuffle[swap];
            shuffle[swap] = tmp;
        }

        displayedCorrectIndex = 0;
        for (int i = 0; i < 4; i++)
        {
            if (shuffle[i] == q.correctIndex)
            {
                displayedCorrectIndex = i;
                break;
            }
        }

        titleText.text = chest.isSpecialChest ? "SPECIAL CHEST CHALLENGE" : "TREASURE CHEST CHALLENGE";
        subtitleText.text = q.category + "  |  " + difficulty + " Difficulty";
        questionText.text = q.questionText;
        feedbackText.text = chest.isSpecialChest
            ? "Golden Key ready! Answer correctly to unlock the Special Chest."
            : "Answer correctly to open the chest and claim your treasure!";
        feedbackText.color = new Color(0.28f, 0.18f, 0.08f, 1f);

        string[] letters = new string[] { "A. ", "B. ", "C. ", "D. " };
        for (int i = 0; i < 4; i++)
        {
            choiceTexts[i].text = letters[i] + q.choices[shuffle[i]];
            choiceImages[i].color = normalBtnColor;
            choiceButtons[i].interactable = true;
        }

        RefreshHintButtonState();

        if (activePlayer != null)
            activePlayer.SetControlsEnabled(false);

        isOpen = true;
        awaitingResponse = true;
        panelAnimTimer = 0.22f;
        quizCanvasGo.SetActive(true);
    }

    private int PickFreshQuestionIndex(int poolLength)
    {
        if (recentQuestionIndices.Count >= poolLength)
            recentQuestionIndices.Clear();

        for (int attempt = 0; attempt < 25; attempt++)
        {
            int candidate = Random.Range(0, poolLength);
            if (!recentQuestionIndices.Contains(candidate))
            {
                recentQuestionIndices.Add(candidate);
                return candidate;
            }
        }

        int fallback = Random.Range(0, poolLength);
        recentQuestionIndices.Add(fallback);
        return fallback;
    }

    private void OnChoiceSelected(int index)
    {
        if (!awaitingResponse || activeChest == null)
            return;

        awaitingResponse = false;
        for (int i = 0; i < 4; i++)
            choiceButtons[i].interactable = false;
        if (hintButton != null)
            hintButton.interactable = false;

        StartCoroutine(ResolveAnswerRoutine(index == displayedCorrectIndex, index));
    }

    private void RefreshHintButtonState()
    {
        if (hintButton == null || hintButtonText == null)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        bool canAfford = !hintUsedThisQuestion && state.coins >= GameStateManager.HintLifelineCost;
        hintButton.interactable = canAfford;
        hintButtonText.text = hintUsedThisQuestion
            ? "Hint Used"
            : "50/50 Hint (" + GameStateManager.HintLifelineCost + "c)";
    }

    private void OnHintButtonClicked()
    {
        if (!awaitingResponse || hintUsedThisQuestion)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        if (!state.TrySpendHintCoins(out string msg))
        {
            feedbackText.text = msg;
            feedbackText.color = new Color(0.68f, 0.12f, 0.12f, 1f);
            return;
        }

        hintUsedThisQuestion = true;
        List<int> wrongIndices = new List<int>();
        for (int i = 0; i < 4; i++)
        {
            if (i != displayedCorrectIndex && choiceButtons[i].interactable)
                wrongIndices.Add(i);
        }

        for (int r = 0; r < 2 && wrongIndices.Count > 0; r++)
        {
            int pick = Random.Range(0, wrongIndices.Count);
            int targetIdx = wrongIndices[pick];
            wrongIndices.RemoveAt(pick);
            choiceButtons[targetIdx].interactable = false;
            choiceImages[targetIdx].color = new Color(0.55f, 0.52f, 0.46f, 0.65f);
            choiceTexts[targetIdx].text = "---";
        }

        GameAudioManager.EnsureInstance().PlayHintChime();
        feedbackText.text = "50/50 Hint Used! 2 wrong answers eliminated.";
        feedbackText.color = new Color(0.14f, 0.38f, 0.58f, 1f);
        RefreshHintButtonState();
    }

    private IEnumerator ResolveAnswerRoutine(bool isCorrect, int selectedIndex)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        state.RecordQuizResult(isCorrect);
        string explanationLine = (activeQuestion != null && !string.IsNullOrEmpty(activeQuestion.explanation))
            ? "\n" + activeQuestion.explanation
            : "";

        if (isCorrect)
        {
            choiceImages[selectedIndex].color = correctBtnColor;
            feedbackText.text = "Correct! Opening chest..." + explanationLine;
            feedbackText.color = new Color(0.1f, 0.52f, 0.15f, 1f);
            GameAudioManager.EnsureInstance().PlayCorrect();

            yield return new WaitForSecondsRealtime(1.65f);

            GameAudioManager.EnsureInstance().PlayTreasureCollect();

            ChestRewardType revealType = ChestRewardType.Coins;
            string rewardMessage;

            if (activeChest.isSpecialChest)
            {
                rewardMessage = state.GrantSpecialChestReward(activeChest.islandIndex);
                if (activeChest.islandIndex == 0)
                    revealType = ChestRewardType.MagicCompass;
                else if (activeChest.islandIndex == 1)
                    revealType = ChestRewardType.Potions;
                else
                    revealType = ChestRewardType.GoldenCrown;
            }
            else
            {
                ChestRewardAssignment assignment = state.GetChestReward(activeChest.chestId);
                rewardMessage = state.GrantRegularReward(assignment);
                if (assignment != null)
                    revealType = assignment.rewardType;
            }

            bool isFinalCrownVictory = activeChest.isSpecialChest && activeChest.islandIndex == 2;

            activeChest.OpenChestAnimated();
            if (!isFinalCrownVictory)
                state.SaveProgress();

            Vector3 spawnPos = activeChest.rewardRevealPoint != null
                ? activeChest.rewardRevealPoint.position
                : activeChest.transform.position + Vector3.up * 1.2f;
            Transform target = activePlayer != null && activePlayer.cameraTransform != null
                ? activePlayer.cameraTransform
                : null;
            RewardRevealEffect.SpawnReveal(spawnPos, target, revealType);

            CloseQuizPanel(!isFinalCrownVictory);
            ScreenFader.EnsureInstance().ShowArrivalBanner(rewardMessage, 2.8f);

            if (isFinalCrownVictory)
            {
                yield return new WaitForSecondsRealtime(2.35f);
                VictoryScreenUI.EnsureInstance().ShowVictory();
            }
        }
        else
        {
            choiceImages[selectedIndex].color = wrongBtnColor;
            choiceImages[displayedCorrectIndex].color = correctBtnColor;
            activeChest.PlayLockedRattle();

            feedbackText.text = "Wrong! Chest respawned elsewhere!" + explanationLine;
            feedbackText.color = new Color(0.68f, 0.12f, 0.12f, 1f);
            GameAudioManager.EnsureInstance().PlayWrong();

            TreasureChest wrongChest = activeChest;
            yield return new WaitForSecondsRealtime(1.85f);

            CloseQuizPanel(true);
            if (wrongChest != null)
            {
                wrongChest.RelocateOnWrongAnswer();
                ScreenFader.EnsureInstance().ShowArrivalBanner("Chest vanished! Search the island to find where it respawned.", 2.8f);
            }
        }
    }

    public void CloseQuizPanel(bool restorePlayerImmediately)
    {
        isOpen = false;
        awaitingResponse = false;

        if (quizCanvasGo != null)
            quizCanvasGo.SetActive(false);

        if (restorePlayerImmediately && activePlayer != null)
            activePlayer.SetControlsEnabled(true);

        activeChest = null;
        activePlayer = null;
    }

    private void BuildQuizUI()
    {
        if (quizCanvasGo != null)
            return;

        quizCanvasGo = new GameObject("ChestQuizCanvas");
        Canvas canvas = quizCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = quizCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        quizCanvasGo.AddComponent<GraphicRaycaster>();

        // Dim backdrop
        GameObject backdrop = CreateUIObject("Backdrop", quizCanvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image backdropImg = backdrop.AddComponent<Image>();
        backdropImg.color = new Color(0f, 0f, 0f, 0.58f);

        // Wooden frame
        GameObject frame = CreateUIObject("WoodFrame", backdrop.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 560f));
        panelRect = frame.GetComponent<RectTransform>();
        Image frameImg = frame.AddComponent<Image>();
        frameImg.color = new Color(0.32f, 0.2f, 0.1f, 0.98f);

        // Parchment inner panel
        GameObject parchment = CreateUIObject("ParchmentPanel", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform pRect = parchment.GetComponent<RectTransform>();
        pRect.offsetMin = new Vector2(16f, 16f);
        pRect.offsetMax = new Vector2(-16f, -16f);
        Image pImg = parchment.AddComponent<Image>();
        pImg.color = new Color(0.96f, 0.91f, 0.8f, 1f);

        titleText = CreateText(parchment.transform, "Title", new Vector2(0f, 222f), new Vector2(680f, 44f), 28f, FontStyles.Bold, new Color(0.26f, 0.14f, 0.05f, 1f));
        subtitleText = CreateText(parchment.transform, "Subtitle", new Vector2(0f, 184f), new Vector2(680f, 30f), 18f, FontStyles.Bold, new Color(0.52f, 0.34f, 0.14f, 1f));
        questionText = CreateText(parchment.transform, "Question", new Vector2(0f, 110f), new Vector2(670f, 105f), 24f, FontStyles.Bold, new Color(0.14f, 0.1f, 0.06f, 1f));

        Vector2[] btnPos = new Vector2[]
        {
            new Vector2(-168f, -12f), new Vector2(168f, -12f),
            new Vector2(-168f, -94f), new Vector2(168f, -94f)
        };

        for (int i = 0; i < 4; i++)
        {
            int capturedIndex = i;
            GameObject btnGo = CreateUIObject("ChoiceButton_" + i, parchment.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), btnPos[i], new Vector2(316f, 68f));
            Image btnImg = btnGo.AddComponent<Image>();
            btnImg.color = normalBtnColor;

            Button btn = btnGo.AddComponent<Button>();
            ColorBlock colors = btn.colors;
            colors.highlightedColor = new Color(1f, 0.94f, 0.72f, 1f);
            colors.pressedColor = new Color(0.85f, 0.75f, 0.52f, 1f);
            btn.colors = colors;
            btn.onClick.AddListener(() => OnChoiceSelected(capturedIndex));

            TMP_Text btnTxt = CreateText(btnGo.transform, "Label", Vector2.zero, new Vector2(296f, 58f), 20f, FontStyles.Bold, new Color(0.15f, 0.1f, 0.06f, 1f));
            btnTxt.raycastTarget = false;

            choiceButtons[i] = btn;
            choiceImages[i] = btnImg;
            choiceTexts[i] = btnTxt;
        }

        feedbackText = CreateText(parchment.transform, "Feedback", new Vector2(0f, -162f), new Vector2(670f, 62f), 17f, FontStyles.Bold, new Color(0.28f, 0.18f, 0.08f, 1f));

        // 50/50 Hint button (spend 10 Coins)
        GameObject hintGo = CreateUIObject("HintButton", parchment.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-115f, -222f), new Vector2(210f, 42f));
        Image hintImg = hintGo.AddComponent<Image>();
        hintImg.color = new Color(0.74f, 0.54f, 0.14f, 1f);
        hintButton = hintGo.AddComponent<Button>();
        hintButton.onClick.AddListener(OnHintButtonClicked);
        hintButtonText = CreateText(hintGo.transform, "Label", Vector2.zero, new Vector2(196f, 36f), 17f, FontStyles.Bold, new Color(1f, 0.96f, 0.86f, 1f));
        hintButtonText.text = "50/50 Hint (10c)";
        hintButtonText.raycastTarget = false;

        // Close button
        GameObject closeGo = CreateUIObject("CloseButton", parchment.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(115f, -222f), new Vector2(180f, 42f));
        Image closeImg = closeGo.AddComponent<Image>();
        closeImg.color = new Color(0.45f, 0.25f, 0.14f, 1f);
        Button closeBtn = closeGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(() => { if (awaitingResponse) CloseQuizPanel(true); });

        TMP_Text closeTxt = CreateText(closeGo.transform, "Label", Vector2.zero, new Vector2(160f, 36f), 18f, FontStyles.Bold, new Color(1f, 0.94f, 0.82f, 1f));
        closeTxt.text = "Close (Esc)";
        closeTxt.raycastTarget = false;
    }

    private static GameObject CreateUIObject(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pos, Vector2 size)
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

    private static TMP_Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        GameObject go = CreateUIObject(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
