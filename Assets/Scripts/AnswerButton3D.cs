
using UnityEngine;

public class AnswerButton3D : MonoBehaviour
{
    public int answerNumber;
    public QuizManager quizManager;

    private void OnMouseDown()
    {
        Debug.Log("You clicked Answer " + answerNumber);
        quizManager.CheckAnswer(answerNumber);
    }
}
