
using UnityEngine;

public class BoatSink : MonoBehaviour
{
    public float sinkSpeed = 0.5f;
    public bool isSinking = false;

    void Update()
    {
        if (isSinking == true)
        {
            transform.position += Vector3.down * sinkSpeed * Time.deltaTime;
        }
    }

    public void SinkBoat()
    {
        isSinking = true;
    }
}
