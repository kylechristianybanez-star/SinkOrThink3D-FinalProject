using UnityEngine;

/// <summary>
/// Optional debug logger for 3D answer clicks. Submission is handled exclusively by QuizManager.
/// </summary>
public class MouseClickDebugger : MonoBehaviour
{
    public bool enableDebugLogs = false;

    void Update()
    {
        if (!enableDebugLogs || Camera.main == null)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
            for (int i = 0; i < hits.Length; i++)
            {
                AnswerButton3D button = hits[i].collider.GetComponentInParent<AnswerButton3D>();
                if (button != null)
                {
                    Debug.Log("Debug Raycast hit AnswerButton3D: " + button.answerNumber);
                    break;
                }
            }
        }
    }
}
