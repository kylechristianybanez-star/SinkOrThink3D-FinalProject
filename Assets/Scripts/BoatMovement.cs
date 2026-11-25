using UnityEngine;

/// <summary>
/// Controls boat steering and forward/reverse travel using a kinematic Rigidbody,
/// enforcing solid hull collision checks (physics obstacles + shallow terrain) during
/// both movement and rotation while applying visual wave motion from BoatAnimator.
/// </summary>
public class BoatMovement : MonoBehaviour
{
    public float moveSpeed = 15f;
    public float turnSpeed = 68f;
    public bool canMove = true;
    public float reverseSpeedMultiplier = 0.6f;

    [Header("Collision & Grounding Safety")]
    public Vector3 hullHalfExtents = new Vector3(1.15f, 0.72f, 2.35f);
    public Vector3 hullCenterOffset = new Vector3(0f, 0.45f, 0f);
    public float maxAllowedTerrainHeight = -0.18f;

    private Rigidbody rb;
    private BoatSink boatSink;
    private BoatAnimator boatAnimator;
    private QuizManager quizManager;
    private Collider[] ownColliders;
    private Terrain[] sceneTerrains;
    private readonly Collider[] overlapResults = new Collider[32];

    private float move;
    private float turn;
    private float baseWaterY;
    private float currentYaw;
    private float speedBoostMultiplier = 1f;
    private float speedBoostTimer = 0f;

    public float CurrentMoveInput => move;
    public float CurrentTurnInput => turn;
    public bool IsSpeedBoostActive => speedBoostTimer > 0f;

    public void ApplySpeedBoost(float multiplier, float duration)
    {
        speedBoostMultiplier = Mathf.Max(speedBoostMultiplier, multiplier);
        speedBoostTimer = Mathf.Max(speedBoostTimer, duration);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        boatSink = GetComponent<BoatSink>();
        boatAnimator = GetComponent<BoatAnimator>();
        ownColliders = GetComponentsInChildren<Collider>(true);
        SyncBaseTransform();
    }

    void Start()
    {
        quizManager = FindAnyObjectByType<QuizManager>();
        RefreshSceneTerrains();
    }

    public void RefreshSceneTerrains()
    {
        sceneTerrains = FindObjectsByType<Terrain>();
    }

    void Update()
    {
        if (speedBoostTimer > 0f)
        {
            speedBoostTimer -= Time.deltaTime;
            if (speedBoostTimer <= 0f)
            {
                speedBoostTimer = 0f;
                speedBoostMultiplier = 1f;
            }
        }

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
            float turnStep = turn * turnSpeed * Time.fixedDeltaTime;
            if (Mathf.Abs(turnStep) > 0.0001f)
            {
                float proposedYaw = currentYaw + turnStep;
                if (CanOccupyPose(targetHorizontalPos, proposedYaw) || !CanOccupyPose(targetHorizontalPos, currentYaw))
                    currentYaw = proposedYaw;
            }

            float speedMultiplier = move < 0f ? reverseSpeedMultiplier : 1f;
            Vector3 horizontalForward = new Vector3(
                Mathf.Sin(currentYaw * Mathf.Deg2Rad),
                0f,
                Mathf.Cos(currentYaw * Mathf.Deg2Rad)
            );
            float effectiveSpeed = moveSpeed * (speedBoostTimer > 0f ? speedBoostMultiplier : 1f);
            Vector3 totalMovement = horizontalForward * (move * speedMultiplier) * effectiveSpeed * Time.fixedDeltaTime;

            if (totalMovement.sqrMagnitude > 0.000001f)
            {
                int subSteps = totalMovement.magnitude > 0.35f ? 2 : 1;
                Vector3 stepDelta = totalMovement / subSteps;

                for (int s = 0; s < subSteps; s++)
                {
                    Vector3 candidate = targetHorizontalPos + stepDelta;
                    if (CanOccupyPose(candidate, currentYaw))
                    {
                        targetHorizontalPos = candidate;
                    }
                    else
                    {
                        Vector3 slideX = new Vector3(candidate.x, baseWaterY, targetHorizontalPos.z);
                        Vector3 slideZ = new Vector3(targetHorizontalPos.x, baseWaterY, candidate.z);

                        if (Mathf.Abs(stepDelta.x) > 0.0001f && CanOccupyPose(slideX, currentYaw))
                            targetHorizontalPos = slideX;
                        else if (Mathf.Abs(stepDelta.z) > 0.0001f && CanOccupyPose(slideZ, currentYaw))
                            targetHorizontalPos = slideZ;
                        else
                            break;
                    }
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

    private bool CanOccupyPose(Vector3 waterPos, float yaw)
    {
        Quaternion rot = Quaternion.Euler(0f, yaw, 0f);

        if (IsOverShallowTerrain(waterPos, rot))
            return false;

        Vector3 boxCenter = waterPos + (rot * hullCenterOffset);
        int hitCount = Physics.OverlapBoxNonAlloc(
            boxCenter,
            hullHalfExtents,
            overlapResults,
            rot,
            ~0,
            QueryTriggerInteraction.Ignore
        );

        for (int i = 0; i < hitCount; i++)
        {
            if (!IsOwnOrWaterCollider(overlapResults[i]))
                return false;
        }

        return true;
    }

    private bool IsOverShallowTerrain(Vector3 waterPos, Quaternion rot)
    {
        if (sceneTerrains == null || sceneTerrains.Length == 0)
            return false;

        Vector3 forward = rot * Vector3.forward;
        Vector3 right = rot * Vector3.right;
        float bowDist = hullHalfExtents.z;
        float beamDist = hullHalfExtents.x;

        Vector3[] samplePoints = new Vector3[]
        {
            waterPos,
            waterPos + forward * bowDist,
            waterPos - forward * bowDist,
            waterPos + right * beamDist,
            waterPos - right * beamDist
        };

        for (int tIdx = 0; tIdx < sceneTerrains.Length; tIdx++)
        {
            Terrain t = sceneTerrains[tIdx];
            if (t == null || !t.gameObject.activeInHierarchy || t.terrainData == null)
                continue;

            Vector3 origin = t.transform.position;
            Vector3 size = t.terrainData.size;

            for (int pIdx = 0; pIdx < samplePoints.Length; pIdx++)
            {
                Vector3 pt = samplePoints[pIdx];
                if (pt.x < origin.x || pt.x > origin.x + size.x || pt.z < origin.z || pt.z > origin.z + size.z)
                    continue;

                float groundY = t.SampleHeight(pt) + origin.y;
                if (groundY > maxAllowedTerrainHeight)
                    return true;
            }
        }

        return false;
    }

    private bool IsOwnOrWaterCollider(Collider col)
    {
        if (col == null || col.isTrigger)
            return true;

        if (col.gameObject.name.IndexOf("OceanSurface", System.StringComparison.OrdinalIgnoreCase) >= 0)
            return true;

        if (ownColliders != null)
        {
            for (int i = 0; i < ownColliders.Length; i++)
            {
                if (ownColliders[i] == col)
                    return true;
            }
        }

        return false;
    }

    public void SyncBaseTransform()
    {
        baseWaterY = transform.position.y;
        currentYaw = transform.eulerAngles.y;
        RefreshSceneTerrains();
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
