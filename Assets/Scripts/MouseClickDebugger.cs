
using UnityEngine;

public class MouseClickDebugger : MonoBehaviour
{
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, 100f))
            {
                AnswerButton3D button =
                    hit.collider.GetComponent<AnswerButton3D>();

                if (button != null)
                {
                    Debug.Log("You clicked Answer " + button.answerNumber);

                    button.quizManager.CheckAnswer(button.answerNumber);
                }
            }
        }
    }
}
