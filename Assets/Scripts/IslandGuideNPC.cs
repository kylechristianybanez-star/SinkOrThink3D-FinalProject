using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Interactive stylized Island Guide NPC who offers local island lore, a clue toward the
/// Special Chest / Golden Key, and a one-time Bonus Trivia Quiz for extra Gold Coins.
/// </summary>
public class IslandGuideNPC : IslandInteractable
{
    public static bool AnyDialogueOpen { get; private set; }

    public int islandIndex = 0;
    public string npcName = "Elder Kael";
    [TextArea(2, 4)]
    public string loreGreeting = "Welcome, traveler! Search the island's chests to recover our hidden Golden Key.";
    [TextArea(2, 3)]
    public string keyHint = "Clue: Check both the village huts and hilltop shrine. Press M to view your island map.";
    public int bonusCoins = 15;

    public Transform wavingArm;
    public Transform questMarker;

    private float animPhase = 0f;
    private Vector3 armBaseEuler;
    private Vector3 markerBaseLocalPos;
    private bool baseCached = false;

    private static GameObject dialogueCanvasGo;
    private static TMP_Text npcTitleText;
    private static TMP_Text npcBodyText;
    private static TMP_Text npcFeedbackText;
    private static Button bonusQuizBtn;
    private static TMP_Text bonusQuizBtnText;
    private static Button closeBtn;
    private static GameObject quizChoicesGroup;
    private static readonly Button[] choiceButtons = new Button[4];
    private static readonly Image[] choiceImages = new Image[4];
    private static readonly TMP_Text[] choiceTexts = new TMP_Text[4];

    private static IslandGuideNPC activeNpc;
    private static IslandPlayerController activePlayer;
    private static QuizQuestion activeBonusQuestion;
    private static int displayedCorrectIdx = 0;
    private static bool awaitingAnswer = false;

    void Start()
    {
        animPhase = Random.Range(0f, 6.28f);
        if (wavingArm != null)
            armBaseEuler = wavingArm.localEulerAngles;
        if (questMarker != null)
            markerBaseLocalPos = questMarker.localPosition;
        baseCached = true;
    }

    void Update()
    {
        if (!baseCached)
            return;

        if (wavingArm != null)
        {
            float wave = Mathf.Sin(Time.time * 2.4f + animPhase) * 14f;
            wavingArm.localRotation = Quaternion.Euler(armBaseEuler.x + wave, armBaseEuler.y, armBaseEuler.z + 18f + wave * 0.5f);
        }

        if (questMarker != null)
        {
            GameStateManager state = GameStateManager.EnsureInstance();
            bool done = state.completedNpcGuides != null && islandIndex >= 0 && islandIndex < state.completedNpcGuides.Length && state.completedNpcGuides[islandIndex];
            if (questMarker.gameObject.activeSelf == done)
                questMarker.gameObject.SetActive(!done);

            if (!done)
            {
                float bob = Mathf.Sin(Time.time * 2.8f + animPhase) * 0.12f;
                questMarker.localPosition = markerBaseLocalPos + Vector3.up * bob;
                questMarker.Rotate(0f, 65f * Time.deltaTime, 0f);
            }
        }

        if (AnyDialogueOpen && activeNpc == this && Input.GetKeyDown(KeyCode.Escape))
            CloseDialogue();
    }

    public override KeyCode GetInteractKey()
    {
        return KeyCode.E;
    }

    public override float GetInteractRange()
    {
        return 4.5f;
    }

    public override bool CanInteract()
    {
        return !AnyDialogueOpen;
    }

    public override string GetPromptText()
    {
        return "Press E to Talk to " + npcName;
    }

    public override void Interact(IslandPlayerController player)
    {
        if (AnyDialogueOpen)
            return;

        activeNpc = this;
        activePlayer = player;
        awaitingAnswer = false;

        if (dialogueCanvasGo == null)
            BuildDialogueUI();

        GameStateManager state = GameStateManager.EnsureInstance();
        bool alreadyDone = state.completedNpcGuides != null &&
                           islandIndex >= 0 && islandIndex < state.completedNpcGuides.Length &&
                           state.completedNpcGuides[islandIndex];

        npcTitleText.text = npcName;
        npcBodyText.text = loreGreeting + "\n\n" + keyHint;
        npcFeedbackText.text = alreadyDone
            ? "Bonus trivia already completed on this island. Safe sailing!"
            : "Answer a bonus island question to earn +" + bonusCoins + " Gold Coins!";
        npcFeedbackText.color = new Color(0.28f, 0.18f, 0.08f, 1f);

        quizChoicesGroup.SetActive(false);
        bonusQuizBtn.gameObject.SetActive(!alreadyDone);
        bonusQuizBtn.interactable = !alreadyDone;
        bonusQuizBtnText.text = "Bonus Quiz (+" + bonusCoins + " Coins)";
        closeBtn.gameObject.SetActive(true);

        if (activePlayer != null)
            activePlayer.SetControlsEnabled(false);

        AnyDialogueOpen = true;
        dialogueCanvasGo.SetActive(true);
        GameAudioManager.EnsureInstance().PlayClick();
    }

    private static void StartBonusQuiz()
    {
        if (activeNpc == null)
            return;

        QuizDifficulty diff = (QuizDifficulty)Mathf.Clamp(activeNpc.islandIndex, 0, 2);
        QuizQuestion[] pool = QuestionBank.GetQuestions(diff);
        if (pool == null || pool.Length == 0)
            return;

        activeBonusQuestion = pool[Random.Range(0, pool.Length)];
        int[] shuffle = new int[] { 0, 1, 2, 3 };
        for (int i = 3; i > 0; i--)
        {
            int swap = Random.Range(0, i + 1);
            int tmp = shuffle[i];
            shuffle[i] = shuffle[swap];
            shuffle[swap] = tmp;
        }

        displayedCorrectIdx = 0;
        for (int i = 0; i < 4; i++)
        {
            if (shuffle[i] == activeBonusQuestion.correctIndex)
            {
                displayedCorrectIdx = i;
                break;
            }
        }

        npcBodyText.text = activeBonusQuestion.questionText;
        npcFeedbackText.text = "Select the correct answer for +" + activeNpc.bonusCoins + " Gold Coins!";
        npcFeedbackText.color = new Color(0.28f, 0.18f, 0.08f, 1f);

        string[] letters = new string[] { "A. ", "B. ", "C. ", "D. " };
        Color normColor = new Color(0.95f, 0.88f, 0.72f, 1f);
        for (int i = 0; i < 4; i++)
        {
            choiceTexts[i].text = letters[i] + activeBonusQuestion.choices[shuffle[i]];
            choiceImages[i].color = normColor;
            choiceButtons[i].interactable = true;
        }

        bonusQuizBtn.gameObject.SetActive(false);
        quizChoicesGroup.SetActive(true);
        awaitingAnswer = true;
        GameAudioManager.EnsureInstance().PlayClick();
    }

    private static void OnChoiceClicked(int idx)
    {
        if (!awaitingAnswer || activeNpc == null)
            return;

        awaitingAnswer = false;
        for (int i = 0; i < 4; i++)
            choiceButtons[i].interactable = false;

        activeNpc.StartCoroutine(ResolveBonusQuizRoutine(idx == displayedCorrectIdx, idx));
    }

    private static IEnumerator ResolveBonusQuizRoutine(bool isCorrect, int selectedIdx)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        state.RecordQuizResult(isCorrect);
        string fact = (activeBonusQuestion != null && !string.IsNullOrEmpty(activeBonusQuestion.explanation))
            ? "\n" + activeBonusQuestion.explanation
            : "";

        if (isCorrect)
        {
            choiceImages[selectedIdx].color = new Color(0.24f, 0.78f, 0.34f, 1f);
            int reward = activeNpc != null ? activeNpc.bonusCoins : 15;
            if (activeNpc != null && state.completedNpcGuides != null && activeNpc.islandIndex >= 0 && activeNpc.islandIndex < state.completedNpcGuides.Length)
                state.completedNpcGuides[activeNpc.islandIndex] = true;
            state.coins += reward;
            state.NotifyStateChanged();
            state.SaveProgress();

            npcFeedbackText.text = "Correct! Earned +" + reward + " Gold Coins!" + fact;
            npcFeedbackText.color = new Color(0.1f, 0.52f, 0.15f, 1f);
            GameAudioManager.EnsureInstance().PlayTreasureCollect();
        }
        else
        {
            choiceImages[selectedIdx].color = new Color(0.86f, 0.24f, 0.24f, 1f);
            choiceImages[displayedCorrectIdx].color = new Color(0.24f, 0.78f, 0.34f, 1f);
            npcFeedbackText.text = "Not quite! Talk again anytime to retry." + fact;
            npcFeedbackText.color = new Color(0.68f, 0.12f, 0.12f, 1f);
            GameAudioManager.EnsureInstance().PlayWrong();
        }

        yield return new WaitForSecondsRealtime(2.0f);
        CloseDialogue();
    }

    public static void CloseDialogue()
    {
        AnyDialogueOpen = false;
        awaitingAnswer = false;

        if (dialogueCanvasGo != null)
            dialogueCanvasGo.SetActive(false);

        if (activePlayer != null)
            activePlayer.SetControlsEnabled(true);

        activeNpc = null;
        activePlayer = null;
    }

    private static void BuildDialogueUI()
    {
        dialogueCanvasGo = new GameObject("IslandNpcDialogueCanvas");
        Canvas canvas = dialogueCanvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 102;

        CanvasScaler scaler = dialogueCanvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        dialogueCanvasGo.AddComponent<GraphicRaycaster>();

        GameObject backdrop = CreateUI("Backdrop", dialogueCanvasGo.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        Image bImg = backdrop.AddComponent<Image>();
        bImg.color = new Color(0f, 0f, 0f, 0.55f);

        GameObject frame = CreateUI("DialogueFrame", backdrop.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 540f));
        Image fImg = frame.AddComponent<Image>();
        fImg.color = new Color(0.28f, 0.18f, 0.1f, 0.98f);

        GameObject paper = CreateUI("Parchment", frame.transform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        RectTransform pr = paper.GetComponent<RectTransform>();
        pr.offsetMin = new Vector2(16f, 16f);
        pr.offsetMax = new Vector2(-16f, -16f);
        Image pImg = paper.AddComponent<Image>();
        pImg.color = new Color(0.96f, 0.91f, 0.8f, 1f);

        npcTitleText = CreateText(paper.transform, "NpcName", new Vector2(0f, 212f), new Vector2(680f, 44f), 30f, FontStyles.Bold, new Color(0.26f, 0.14f, 0.05f, 1f));
        npcBodyText = CreateText(paper.transform, "NpcBody", new Vector2(0f, 104f), new Vector2(660f, 145f), 21f, FontStyles.Bold, new Color(0.16f, 0.1f, 0.06f, 1f));

        quizChoicesGroup = CreateUI("QuizChoicesGroup", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(680f, 200f));
        Vector2[] pos = new Vector2[]
        {
            new Vector2(-165f, -12f), new Vector2(165f, -12f),
            new Vector2(-165f, -88f), new Vector2(165f, -88f)
        };
        for (int i = 0; i < 4; i++)
        {
            int cap = i;
            GameObject cGo = CreateUI("NpcChoice_" + i, quizChoicesGroup.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos[i], new Vector2(310f, 62f));
            Image cImg = cGo.AddComponent<Image>();
            cImg.color = new Color(0.95f, 0.88f, 0.72f, 1f);
            Button btn = cGo.AddComponent<Button>();
            btn.onClick.AddListener(() => OnChoiceClicked(cap));
            TMP_Text cTxt = CreateText(cGo.transform, "Label", Vector2.zero, new Vector2(290f, 54f), 19f, FontStyles.Bold, new Color(0.15f, 0.1f, 0.06f, 1f));
            cTxt.raycastTarget = false;
            choiceButtons[i] = btn;
            choiceImages[i] = cImg;
            choiceTexts[i] = cTxt;
        }
        quizChoicesGroup.SetActive(false);

        npcFeedbackText = CreateText(paper.transform, "NpcFeedback", new Vector2(0f, -156f), new Vector2(670f, 60f), 17f, FontStyles.Bold, new Color(0.28f, 0.18f, 0.08f, 1f));

        GameObject qGo = CreateUI("BonusQuizButton", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-120f, -216f), new Vector2(250f, 44f));
        Image qImg = qGo.AddComponent<Image>();
        qImg.color = new Color(0.2f, 0.58f, 0.28f, 1f);
        bonusQuizBtn = qGo.AddComponent<Button>();
        bonusQuizBtn.onClick.AddListener(StartBonusQuiz);
        bonusQuizBtnText = CreateText(qGo.transform, "Label", Vector2.zero, new Vector2(236f, 38f), 18f, FontStyles.Bold, new Color(1f, 0.96f, 0.86f, 1f));
        bonusQuizBtnText.raycastTarget = false;

        GameObject clGo = CreateUI("CloseNpcButton", paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(130f, -216f), new Vector2(180f, 44f));
        Image clImg = clGo.AddComponent<Image>();
        clImg.color = new Color(0.45f, 0.25f, 0.14f, 1f);
        closeBtn = clGo.AddComponent<Button>();
        closeBtn.onClick.AddListener(CloseDialogue);
        TMP_Text clTxt = CreateText(clGo.transform, "Label", Vector2.zero, new Vector2(160f, 38f), 18f, FontStyles.Bold, new Color(1f, 0.94f, 0.82f, 1f));
        clTxt.text = "Close (Esc)";
        clTxt.raycastTarget = false;

        dialogueCanvasGo.SetActive(false);
    }

    private static GameObject CreateUI(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }

    private static TMP_Text CreateText(Transform parent, string name, Vector2 pos, Vector2 size, float fontSize, FontStyles style, Color color)
    {
        GameObject go = CreateUI(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), pos, size);
        TextMeshProUGUI tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = color;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }
}
