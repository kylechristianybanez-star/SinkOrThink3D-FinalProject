
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using System.Collections.Generic;

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

    private float timeRemaining;
    private bool canAnswer = false;
    private bool isTraveling = false;
    private bool gameFinished = false;

    private int questionsInRound = 0;
    private int roundNumber = 1;
    private int activeQuestionIndex = 0;

    private Color originalColor;

    private List<int> questionOrder = new List<int>();
    private int questionPosition = 0;

    // EASY ELEMENTARY QUESTIONS
    private string[] questions =
    {
        // MATHEMATICS
        "What is 5 + 3?",
        "What is 10 - 4?",
        "What is 2 x 3?",
        "What is 12 / 3?",
        "What is 7 + 2?",
        "What is 5 x 2?",

        // SCIENCE
        "What do plants need to grow?",
        "Which animal can fly?",
        "What do we use to see?",
        "What gives us light during the day?",
        "What do fish use to breathe?",
        "What do we drink to stay healthy?",

        // ENGLISH
        "What is the opposite of hot?",
        "Which word is a color?",
        "What is the plural of cat?",
        "Which animal says meow?",
        "What is the opposite of big?",
        "Which word is a fruit?",

        // FILIPINO
        "Ano ang kulay ng dugo?",
        "Ano ang kabaligtaran ng taas?",
        "Ilang araw mayroon sa isang linggo?",
        "Ano ang tawag sa ina ng iyong ina?",
        "Ano ang pambansang wika ng Pilipinas?",
        "Ano ang kulay ng dahon?",

        // GENERAL KNOWLEDGE
        "How many days are in a week?",
        "How many months are in a year?",
        "What shape has three sides?",
        "What is the first month of the year?",
        "How many legs does a dog have?",
        "What do we use to write?"
    };

    private string[] answersA =
    {
        "6", "4", "5", "2", "8", "10",
        "Water", "Dog", "Ears", "Moon", "Lungs", "Soda",
        "Cold", "Apple", "Cats", "Dog", "Small", "Carrot",
        "Pula", "Baba", "5", "Lola", "English", "Asul",
        "5", "10", "Circle", "January", "2", "Spoon"
    };

    private string[] answersB =
    {
        "8", "5", "6", "3", "9", "12",
        "Plastic", "Cat", "Eyes", "Sun", "Gills", "Coffee",
        "Warm", "Blue", "Cates", "Cat", "Tall", "Mango",
        "Asul", "Laki", "6", "Ate", "Filipino", "Pula",
        "6", "11", "Square", "March", "3", "Pencil"
    };

    private string[] answersC =
    {
        "9", "6", "7", "4", "10", "15",
        "Stone", "Bird", "Nose", "Star", "Wings", "Water",
        "Big", "Table", "Cat's", "Cow", "Long", "Chair",
        "Dilaw", "Liit", "7", "Tita", "Spanish", "Berde",
        "7", "12", "Triangle", "June", "4", "Plate"
    };

    private string[] answersD =
    {
        "10", "7", "8", "5", "11", "20",
        "Glass", "Fish", "Hands", "Cloud", "Feet", "Juice",
        "Fast", "Chair", "Caties", "Duck", "Heavy", "Book",
        "Berde", "Init", "8", "Pinsan", "French", "Itim",
        "8", "13", "Rectangle", "August", "6", "Fork"
    };

    // A=0, B=1, C=2, D=3
    private int[] correctAnswers =
    {
        1, 2, 1, 2, 1, 0,
        0, 2, 1, 1, 1, 2,
        0, 1, 0, 1, 0, 1,
        0, 0, 2, 0, 1, 2,
        2, 2, 2, 0, 2, 1
    };

    void Start()
    {
        if (firstPersonMouseLook != null)
            startingCameraRotation = firstPersonMouseLook.transform.localRotation;

        currentQuestion = 0;
        questionsInRound = 0;
        roundNumber = 1;

        originalColor = buttonA.material.color;

        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (celebrationEffect != null)
            celebrationEffect.Stop();

        ShuffleQuestions();
        StartTravel();
    }

    void Update()
    {
        if (gameFinished)
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

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            canAnswer = false;

            timerText.text = "Question Time: 0s";
            timerText.color = Color.red;

            boatSink.SinkBoat();

            if (boatSink.currentHealth <= 0)
            {
                GameOver();
                return;
            }

            questionText.text = "Time's Up! Boat damaged!";
            Invoke(nameof(NextQuestion), 2f);
            return;
        }

        UpdateTimerDisplay();
    }

    void ShuffleQuestions()
    {
        questionOrder.Clear();

        for (int i = 0; i < questions.Length; i++)
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
        if (firstPersonMouseLook != null)
            firstPersonMouseLook.enabled = true;

        canAnswer = false;
        isTraveling = true;
        timeRemaining = travelTime;

        if (boatMovement != null)
            boatMovement.canMove = true;

        if (quizBoard != null)
            quizBoard.SetActive(false);

        timerText.text = "";
        timerText.color = Color.white;

        UpdateOceanTravelTimer();
    }

    void UpdateOceanTravelTimer()
    {
        if (oceanTravelTimerText != null)
        {
            oceanTravelTimerText.text =
                "Round " + roundNumber +
                " - Ocean Travel: " +
                Mathf.CeilToInt(timeRemaining) + "s";
        }
    }

    void ShowQuestion()
    {
        if (firstPersonMouseLook != null)
        {
            firstPersonMouseLook.enabled = false;
            firstPersonMouseLook.transform.localRotation = startingCameraRotation;
        }

        if (boatMovement != null)
            boatMovement.canMove = false;

        ResetButtonColors();

        if (quizBoard != null)
            quizBoard.SetActive(true);

        if (questionPosition >= questionOrder.Count)
            ShuffleQuestions();

        activeQuestionIndex = questionOrder[questionPosition];
        questionPosition++;

        questionText.gameObject.SetActive(true);
        questionText.text = questions[activeQuestionIndex];

        answerTextA.text = "A. " + answersA[activeQuestionIndex];
        answerTextB.text = "B. " + answersB[activeQuestionIndex];
        answerTextC.text = "C. " + answersC[activeQuestionIndex];
        answerTextD.text = "D. " + answersD[activeQuestionIndex];

        timeRemaining = answerTime;
        canAnswer = true;

        UpdateTimerDisplay();
    }

    void UpdateTimerDisplay()
    {
        timerText.text =
            "Question Time: " +
            Mathf.CeilToInt(timeRemaining) + "s";

        timerText.color =
            timeRemaining <= 5f ? Color.red : Color.white;
    }

    public void CheckAnswer(int answer)
    {
        if (!canAnswer || gameFinished)
            return;

        if (boatSink.currentHealth <= 0)
            return;

        Renderer selectedButton = null;

        if (answer == 0) selectedButton = buttonA;
        else if (answer == 1) selectedButton = buttonB;
        else if (answer == 2) selectedButton = buttonC;
        else if (answer == 3) selectedButton = buttonD;

        if (selectedButton == null)
            return;

        canAnswer = false;

        if (answer == correctAnswers[activeQuestionIndex])
        {
            selectedButton.material.color = Color.green;
            questionText.text = "Correct! The boat is safe!";
        }
        else
        {
            selectedButton.material.color = Color.red;
            boatSink.SinkBoat();

            if (boatSink.currentHealth <= 0)
            {
                GameOver();
                return;
            }

            questionText.text = "Wrong answer! Boat damaged!";
        }

        Invoke(nameof(NextQuestion), 2f);
    }

    void NextQuestion()
    {
        currentQuestion++;
        questionsInRound++;

        if (questionsInRound >= 5)
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

        if (celebrationEffect != null)
            celebrationEffect.Play();

        StartTravel();
    }

    void GameOver()
    {
        if (firstPersonMouseLook != null)
            firstPersonMouseLook.enabled = false;

        gameFinished = true;
        canAnswer = false;
        isTraveling = false;

        if (boatMovement != null)
            boatMovement.canMove = false;

        CancelInvoke(nameof(NextQuestion));

        if (quizBoard != null)
            quizBoard.SetActive(false);

        timerText.text = "";

        if (oceanTravelTimerText != null)
            oceanTravelTimerText.text = "";

        if (gameOverPanel != null)
            gameOverPanel.SetActive(true);
    }

    public void PlayAgain()
    {
        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    void ResetButtonColors()
    {   
        buttonA.material.color = originalColor;
        buttonB.material.color = originalColor;
        buttonC.material.color = originalColor;
        buttonD.material.color = originalColor;
    }
}
