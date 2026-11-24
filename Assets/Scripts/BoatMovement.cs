using UnityEngine;

/// <summary>
/// Controls boat steering and forward/reverse travel using a kinematic Rigidbody,
/// while applying smooth ocean floating/rocking offsets from BoatAnimator.
/// </summary>
public class BoatMovement : MonoBehaviour
{
    public float moveSpeed = 10f;
    public float turnSpeed = 60f;
    public bool canMove = true;
    public float reverseSpeedMultiplier = 0.6f;

    private Rigidbody rb;
    private BoatSink boatSink;
    private BoatAnimator boatAnimator;
    private QuizManager quizManager;

    private float move;
    private float turn;
    private float baseWaterY;
    private float currentYaw;

    public float CurrentMoveInput => move;
    public float CurrentTurnInput => turn;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boatSink = GetComponent<BoatSink>();
        boatAnimator = GetComponent<BoatAnimator>();
        SyncBaseTransform();
    }

    void Start()
    {
        quizManager = FindFirstObjectByType<QuizManager>();
    }

    void Update()
    {
        if (!canMove || Time.timeScale == 0f || (boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0)))
        {
            move = 0f;
            turn = 0f;
            return;
        }

        move = Input.GetAxis("Vertical");
        turn = Input.GetAxis("Horizontal");
    }

    void FixedUpdate()
    {
        if (rb == null || Time.timeScale == 0f)
            return;

        if (boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0))
            return;

        Vector3 targetHorizontalPos = new Vector3(rb.position.x, baseWaterY, rb.position.z);

        if (canMove)
        {
            currentYaw += turn * turnSpeed * Time.fixedDeltaTime;

            float speedMultiplier = move < 0f ? reverseSpeedMultiplier : 1f;
            Vector3 horizontalForward = new Vector3(transform.forward.x, 0f, transform.forward.z).normalized;
            Vector3 movement = horizontalForward * (move * speedMultiplier) * moveSpeed * Time.fixedDeltaTime;

            if (movement.sqrMagnitude > 0.000001f)
            {
                if (!rb.SweepTest(
                    movement.normalized,
                    out RaycastHit hit,
                    movement.magnitude + 0.01f,
                    QueryTriggerInteraction.Ignore
                ))
                {
                    targetHorizontalPos += movement;
                }
            }
        }

        bool isQuizActive = quizManager != null && quizManager.IsQuizActive;
        float waveBob = boatAnimator != null ? boatAnimator.GetWaveBobOffset(isQuizActive) : 0f;
        Quaternion waveTilt = boatAnimator != null
            ? boatAnimator.GetWaveTiltRotation(canMove ? move : 0f, canMove ? turn : 0f, isQuizActive)
            : Quaternion.identity;

        Quaternion targetRotation = Quaternion.Euler(0f, currentYaw, 0f) * waveTilt;
        Vector3 finalPosition = new Vector3(targetHorizontalPos.x, baseWaterY + waveBob, targetHorizontalPos.z);

        rb.MoveRotation(targetRotation);
        rb.MovePosition(finalPosition);
    }

    public void SyncBaseTransform()
    {
        baseWaterY = transform.position.y;
        currentYaw = transform.eulerAngles.y;
    }

    public void StopBoat()
    {
        canMove = false;
        move = 0f;
        turn = 0f;

        if (rb != null && !rb.isKinematic)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
}
