using UnityEngine;

/// <summary>
/// Represents a clickable 3D answer button on the quiz board with hover and press feedback.
/// Clicks are routed centrally by QuizManager so answers cannot submit twice.
/// </summary>
public class AnswerButton3D : MonoBehaviour
{
    public int answerNumber;
    public QuizManager quizManager;

    private Vector3 originalScale;
    private bool scaleInitialized = false;
    private bool isHovered = false;
    private float pressTimer = 0f;

    void Awake()
    {
        originalScale = transform.localScale;
        scaleInitialized = true;
    }

    void OnEnable()
    {
        isHovered = false;
        pressTimer = 0f;
        if (scaleInitialized)
            transform.localScale = originalScale;
    }

    void Update()
    {
        if (!scaleInitialized)
            return;

        if (pressTimer > 0f)
            pressTimer -= Time.deltaTime;

        float targetMultiplier = 1f;
        if (pressTimer > 0f)
            targetMultiplier = 0.94f;
        else if (isHovered)
            targetMultiplier = 1.05f;

        transform.localScale = Vector3.Lerp(transform.localScale, originalScale * targetMultiplier, Time.deltaTime * 14f);
    }

    public void SetHovered(bool hovered)
    {
        isHovered = hovered;
    }

    public void TriggerClick()
    {
        pressTimer = 0.14f;

        if (quizManager != null)
            quizManager.CheckAnswer(answerNumber);
    }
}
