using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Runs the ocean travel and 5-question quiz rounds, handles single-click 3D answer input,
/// randomizes questions and A-D answer choices, triggers board animations, and manages Game Over.
/// </summary>
public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public TMP_Text timerText;
    public TMP_Text oceanTravelTimerText;

    public TMP_Text answerTextA;
    public TMP_Text answerTextB;
    public TMP_Text answerTextC;
    public TMP_Text answerTextD;

    public Renderer buttonA;
    public Renderer buttonB;
    public Renderer buttonC;
    public Renderer buttonD;

    public BoatSink boatSink;
    public BoatMovement boatMovement;

    public MonoBehaviour firstPersonMouseLook;
    private Quaternion startingCameraRotation;

    public ParticleSystem celebrationEffect;
    public GameObject gameOverPanel;
    public GameObject quizBoard;

    public int currentQuestion = 0;
    public float travelTime = 20f;
    public float answerTime = 15f;

    [Header("Quiz Rules & Feedback")]
    public int questionsPerRound = 5;
    public int requiredRoundsBeforeDock = 1;
    public TMP_Text questionCounterText;
    public QuizBoardAnimator quizBoardAnimator;
    public Color correctColor = new Color(0.2f, 0.78f, 0.3f);
    public Color wrongColor = new Color(0.85f, 0.22f, 0.22f);
    public Color hoverTintColor = new Color(1f, 0.92f, 0.65f);

    private float timeRemaining;
    private bool canAnswer = false;
    private bool isTraveling = false;
    private bool gameFinished = false;
    private bool pausedForDocking = false;

    private int questionsInRound = 0;
    private int roundNumber = 1;
    private int currentIslandIndex = 0;
    private int displayedCorrectAnswer = 0;

    private Color originalColor = Color.white;
    private Vector3 originalTimerScale = Vector3.one;

    private QuizQuestion[] activeQuestionPool;
    private readonly List<int> questionOrder = new List<int>();
    private int questionPosition = 0;
    private readonly int[] choiceShuffle = new int[4] { 0, 1, 2, 3 };

    private AnswerButton3D[] answerButtons = new AnswerButton3D[4];
    private AnswerButton3D hoveredButton;
    private Coroutine nextQuestionCoroutine;
    private readonly List<RaycastResult> uiRaycastResults = new List<RaycastResult>();

    public bool IsQuizActive => canAnswer || (quizBoard != null && quizBoard.activeInHierarchy);
    public bool IsGameFinished => gameFinished;

    void Start()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        currentIslandIndex = Mathf.Clamp(state.currentIslandIndex, 0, 2);

        if (firstPersonMouseLook != null)
            startingCameraRotation = firstPersonMouseLook.transform.localRotation;

        if (timerText != null)
        {
            originalTimerScale = timerText.transform.localScale;
            timerText.raycastTarget = false;
        }

        if (oceanTravelTimerText != null)
            oceanTravelTimerText.raycastTarget = false;

        if (questionText != null)
            questionText.raycastTarget = false;

        DisableAnswerTextRaycasts();

        if (buttonA != null && buttonA.material != null)
            originalColor = buttonA.material.color;

        if (quizBoard != null && quizBoardAnimator == null)
        {
            quizBoardAnimator = quizBoard.GetComponent<QuizBoardAnimator>();
            if (quizBoardAnimator == null)
                quizBoardAnimator = quizBoard.AddComponent<QuizBoardAnimator>();
        }

        CacheAnswerButtons();

        currentQuestion = 0;
        questionsInRound = 0;
        roundNumber = 1;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (celebrationEffect != null)
            celebrationEffect.Stop();

        QuizDifficulty difficulty = (QuizDifficulty)currentIslandIndex;
        activeQuestionPool = QuestionBank.GetQuestions(difficulty);

        if (state.boatHealth <= 0)
        {
            GameOver();
            return;
        }

        ShuffleQuestions();
        StartTravel();
    }

    void Update()
    {
        if (gameFinished || pausedForDocking || Time.timeScale == 0f)
            return;

        if (isTraveling)
        {
            timeRemaining -= Time.deltaTime;

            if (timeRemaining <= 0f)
            {
                isTraveling = false;

                if (oceanTravelTimerText != null)
                    oceanTravelTimerText.text = "";

                ShowQuestion();
                return;
            }

            UpdateOceanTravelTimer();
            return;
        }

        if (!canAnswer)
            return;

        HandleBoardPointerInput();

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            HandleTimeout();
            return;
        }

        UpdateTimerDisplay();
    }

    private void CacheAnswerButtons()
    {
        Renderer[] renderers = new Renderer[] { buttonA, buttonB, buttonC, buttonD };
        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
                answerButtons[i] = renderers[i].GetComponent<AnswerButton3D>();
        }
    }

    private void DisableAnswerTextRaycasts()
    {
        TMP_Text[] texts = new TMP_Text[] { answerTextA, answerTextB, answerTextC, answerTextD };
        for (int i = 0; i < texts.Length; i++)
        {
            if (texts[i] != null)
                texts[i].raycastTarget = false;
        }
    }

    private void HandleBoardPointerInput()
    {
        if (Camera.main == null)
            return;

        if (gameOverPanel != null && gameOverPanel.activeInHierarchy)
        {
            ClearHover();
            return;
        }

        if (IsPointerBlockedByInteractiveUI())
        {
            ClearHover();
            return;
        }

        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        AnswerButton3D closestButton = null;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < hits.Length; i++)
        {
            AnswerButton3D button = hits[i].collider.GetComponentInParent<AnswerButton3D>();
            if (button != null && hits[i].distance < closestDistance)
            {
                closestDistance = hits[i].distance;
                closestButton = button;
            }
        }

        UpdateHoveredButton(closestButton);

        if (closestButton != null && Input.GetMouseButtonDown(0))
        {
            closestButton.TriggerClick();
        }
    }

    private bool IsPointerBlockedByInteractiveUI()
    {
        if (EventSystem.current == null)
            return false;

        PointerEventData pointerData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        uiRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointerData, uiRaycastResults);

        for (int i = 0; i < uiRaycastResults.Count; i++)
        {
            GameObject hitObject = uiRaycastResults[i].gameObject;
            if (hitObject.GetComponentInParent<Selectable>() != null)
                return true;
        }

        return false;
    }

    private void UpdateHoveredButton(AnswerButton3D targetButton)
    {
        if (hoveredButton == targetButton)
            return;

        if (hoveredButton != null)
        {
            hoveredButton.SetHovered(false);
            SetButtonColor(hoveredButton.answerNumber, originalColor);
        }

        hoveredButton = targetButton;

        if (hoveredButton != null)
        {
            hoveredButton.SetHovered(true);
            SetButtonColor(hoveredButton.answerNumber, originalColor * hoverTintColor);
        }
    }

    private void ClearHover()
    {
        if (hoveredButton != null)
        {
            hoveredButton.SetHovered(false);
            hoveredButton = null;
        }
    }

    void ShuffleQuestions()
    {
        questionOrder.Clear();

        if (activeQuestionPool == null || activeQuestionPool.Length == 0)
            return;

        for (int i = 0; i < activeQuestionPool.Length; i++)
            questionOrder.Add(i);

        for (int i = questionOrder.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = questionOrder[i];
            questionOrder[i] = questionOrder[randomIndex];
            questionOrder[randomIndex] = temp;
        }

        questionPosition = 0;
    }

    void StartTravel()
    {
        ClearHover();

        if (firstPersonMouseLook != null)
            firstPersonMouseLook.enabled = true;

        canAnswer = false;
        isTraveling = true;
        timeRemaining = travelTime;

        if (boatMovement != null)
            boatMovement.canMove = true;

        if (quizBoardAnimator != null)
            quizBoardAnimator.ResetVisualState();

        if (quizBoard != null)
            quizBoard.SetActive(false);

        if (timerText != null)
        {
            timerText.text = "";
            timerText.color = Color.white;
            timerText.transform.localScale = originalTimerScale;
        }

        if (questionCounterText != null)
            questionCounterText.text = "";

        UpdateOceanTravelTimer();
    }

    void UpdateOceanTravelTimer()
    {
        if (oceanTravelTimerText != null)
        {
            string dockHint = CanDockNow() ? "  |  Dock Ready (Press F near dock)" : "";
            oceanTravelTimerText.text =
                "Round " + roundNumber +
                " - Ocean Travel: " +
                Mathf.CeilToInt(timeRemaining) + "s" + dockHint;
        }
    }

    void ShowQuestion()
    {
        if (activeQuestionPool == null || activeQuestionPool.Length == 0)
            return;

        if (firstPersonMouseLook != null)
        {
            firstPersonMouseLook.enabled = false;
            if (firstPersonMouseLook is FirstPersonMouseLook look)
                look.ResetLook(startingCameraRotation);
            else
                firstPersonMouseLook.transform.localRotation = startingCameraRotation;
        }

        if (boatMovement != null)
            boatMovement.StopBoat();

        ClearHover();
        ResetButtonColors();

        bool wasHidden = quizBoard != null && !quizBoard.activeInHierarchy;
        if (quizBoard != null)
            quizBoard.SetActive(true);

        if (quizBoardAnimator != null)
        {
            if (wasHidden)
                quizBoardAnimator.PlayAppear();
            else
                quizBoardAnimator.PlayNextQuestionPulse();
        }

        if (questionPosition >= questionOrder.Count)
            ShuffleQuestions();

        int activeQuestionIndex = questionOrder[questionPosition];
        questionPosition++;

        QuizQuestion q = activeQuestionPool[activeQuestionIndex];

        for (int i = 0; i < 4; i++)
            choiceShuffle[i] = i;

        for (int i = 3; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            int temp = choiceShuffle[i];
            choiceShuffle[i] = choiceShuffle[swapIndex];
            choiceShuffle[swapIndex] = temp;
        }

        displayedCorrectAnswer = 0;
        for (int i = 0; i < 4; i++)
        {
            if (choiceShuffle[i] == q.correctIndex)
            {
                displayedCorrectAnswer = i;
                break;
            }
        }

        if (questionText != null)
        {
            questionText.gameObject.SetActive(true);
            questionText.text = q.questionText;
        }

        if (answerTextA != null) answerTextA.text = "A. " + q.choices[choiceShuffle[0]];
        if (answerTextB != null) answerTextB.text = "B. " + q.choices[choiceShuffle[1]];
        if (answerTextC != null) answerTextC.text = "C. " + q.choices[choiceShuffle[2]];
        if (answerTextD != null) answerTextD.text = "D. " + q.choices[choiceShuffle[3]];

        timeRemaining = answerTime;
        canAnswer = true;

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        int questionNum = questionsInRound + 1;
        int seconds = Mathf.CeilToInt(timeRemaining);

        if (questionCounterText != null)
            questionCounterText.text = "Question " + questionNum + "/" + questionsPerRound;

        if (timerText != null)
        {
            if (questionCounterText == null)
                timerText.text = "Question " + questionNum + "/" + questionsPerRound + "  |  Time: " + seconds + "s";
            else
                timerText.text = "Question Time: " + seconds + "s";

            bool lowTime = timeRemaining <= 5f;
            timerText.color = lowTime ? Color.red : Color.white;

            if (lowTime)
            {
                float pulse = 1f + Mathf.Abs(Mathf.Sin(Time.time * 6f)) * 0.08f;
                timerText.transform.localScale = originalTimerScale * pulse;
            }
            else
            {
                timerText.transform.localScale = originalTimerScale;
            }
        }
    }

    private void HandleTimeout()
    {
        timeRemaining = 0f;
        canAnswer = false;
        ClearHover();

        if (timerText != null)
        {
            timerText.text = "Question Time: 0s";
            timerText.color = Color.red;
            timerText.transform.localScale = originalTimerScale;
        }

        SetButtonColor(displayedCorrectAnswer, correctColor);
        if (quizBoardAnimator != null)
            quizBoardAnimator.PlayWrongFeedback();

        if (boatSink != null)
        {
            boatSink.SinkBoat();
            if (boatSink.currentHealth <= 0)
            {
                GameOver();
                return;
            }
        }

        if (questionText != null)
            questionText.text = "Time's Up! Boat damaged!";

        ScheduleNextQuestion(2f);
    }

    public void CheckAnswer(int answer)
    {
        if (!canAnswer || gameFinished || pausedForDocking)
            return;

        if (boatSink != null && boatSink.currentHealth <= 0)
            return;

        Renderer selectedButton = GetButtonRenderer(answer);
        if (selectedButton == null)
            return;

        canAnswer = false;
        ClearHover();

        if (timerText != null)
            timerText.transform.localScale = originalTimerScale;

        if (answer == displayedCorrectAnswer)
        {
            SetButtonColor(answer, correctColor);
            if (quizBoardAnimator != null)
                quizBoardAnimator.PlayCorrectFeedback();
            if (questionText != null)
                questionText.text = "Correct! The boat is safe!";
        }
        else
        {
            SetButtonColor(answer, wrongColor);
            SetButtonColor(displayedCorrectAnswer, correctColor);
            if (quizBoardAnimator != null)
                quizBoardAnimator.PlayWrongFeedback();

            if (boatSink != null)
            {
                boatSink.SinkBoat();
                if (boatSink.currentHealth <= 0)
                {
                    GameOver();
                    return;
                }
            }

            if (questionText != null)
                questionText.text = "Wrong answer! Boat damaged!";
        }

        ScheduleNextQuestion(2f);
    }

    private void ScheduleNextQuestion(float delaySeconds)
    {
        if (nextQuestionCoroutine != null)
            StopCoroutine(nextQuestionCoroutine);

        nextQuestionCoroutine = StartCoroutine(NextQuestionDelayed(delaySeconds));
    }

    private IEnumerator NextQuestionDelayed(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);
        nextQuestionCoroutine = null;
        NextQuestion();
    }

    void NextQuestion()
    {
        if (gameFinished || pausedForDocking)
            return;

        currentQuestion++;
        questionsInRound++;

        if (questionsInRound >= questionsPerRound)
        {
            RoundCompleted();
            return;
        }

        ShowQuestion();
    }

    void RoundCompleted()
    {
        canAnswer = false;
        roundNumber++;
        questionsInRound = 0;

        GameStateManager.EnsureInstance().MarkOceanRoundCompleted(currentIslandIndex);

        if (celebrationEffect != null)
            celebrationEffect.Play();

        StartTravel();
    }

    public bool CanDockNow()
    {
        if (gameFinished || IsQuizActive)
            return false;

        return GameStateManager.EnsureInstance().HasCompletedOceanRound(currentIslandIndex, requiredRoundsBeforeDock);
    }

    public void PauseForDocking()
    {
        pausedForDocking = true;
        canAnswer = false;
        isTraveling = false;
        ClearHover();

        if (nextQuestionCoroutine != null)
        {
            StopCoroutine(nextQuestionCoroutine);
            nextQuestionCoroutine = null;
        }

        if (quizBoardAnimator != null)
            quizBoardAnimator.ResetVisualState();

        if (quizBoard != null)
            quizBoard.SetActive(false);

        if (oceanTravelTimerText != null)
            oceanTravelTimerText.text = "";

        if (timerText != null)
            timerText.text = "";
    }

    void GameOver()
    {
        if (firstPersonMouseLook != null)
            firstPersonMouseLook.enabled = false;

        gameFinished = true;
        canAnswer = false;
        isTraveling = false;
        ClearHover();

        if (nextQuestionCoroutine != null)
        {
            StopCoroutine(nextQuestionCoroutine);
            nextQuestionCoroutine = null;
        }

        if (boatMovement != null)
            boatMovement.StopBoat();

        if (quizBoardAnimator != null)
            quizBoardAnimator.ResetVisualState();

        if (quizBoard != null)
            quizBoard.SetActive(false);

        if (timerText != null)
        {
            timerText.text = "";
            timerText.transform.localScale = originalTimerScale;
        }

        if (oceanTravelTimerText != null)
            oceanTravelTimerText.text = "";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void PlayAgain()
    {
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private Renderer GetButtonRenderer(int index)
    {
        if (index == 0) return buttonA;
        if (index == 1) return buttonB;
        if (index == 2) return buttonC;
        if (index == 3) return buttonD;
        return null;
    }

    private void SetButtonColor(int index, Color color)
    {
        Renderer renderer = GetButtonRenderer(index);
        if (renderer != null && renderer.material != null)
            renderer.material.color = color;
    }

    void ResetButtonColors()
    {
        SetButtonColor(0, originalColor);
        SetButtonColor(1, originalColor);
        SetButtonColor(2, originalColor);
        SetButtonColor(3, originalColor);
    }
}
