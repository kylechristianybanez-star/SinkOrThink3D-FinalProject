
using UnityEngine;
using TMPro;

public class QuizManager : MonoBehaviour
{
    public TMP_Text questionText;
    public BoatSink boatSink;

    public void CheckAnswer(int answer)
    {
        if (answer == 1)
        {
            questionText.text = "Correct! The boat is safe!";
        }
        else
        {
            questionText.text = "Wrong answer! The boat sinks!";
            boatSink.SinkBoat();
        }
    }
}
