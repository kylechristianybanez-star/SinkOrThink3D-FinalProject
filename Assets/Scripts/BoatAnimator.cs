using UnityEngine;

/// <summary>
/// Animates the rowboat's wave bobbing, rocking, turn banking, oar rowing,
/// wake/bow water splashes, and damage/repair particle effects.
/// Physics movement stays in BoatMovement so decorative motion never fights collisions.
/// </summary>
public class BoatAnimator : MonoBehaviour
{
    [Header("Wave Floating & Rocking")]
    public float bobAmplitude = 0.08f;
    public float bobSpeed = 1.6f;
    public float pitchAmplitude = 1.8f;
    public float rollAmplitude = 2.4f;
    public float rockSpeed = 1.25f;
    public float turnBankAngle = 6.5f;
    public float movePitchAngle = -2.2f;
    public float quizMotionScale = 0.3f;
    public float dockedMotionScale = 0.45f;

    [Header("Oar Rowing")]
    public Transform leftPaddle;
    public Transform rightPaddle;
    public float rowSpeed = 4.5f;
    public float rowSweepAngle = 18f;
    public float rowDipAngle = 10f;

    [Header("Particle Effects")]
    public ParticleSystem wakeParticles;
    public ParticleSystem bowSplashParticles;
    public ParticleSystem damageSplashParticles;
    public ParticleSystem repairParticles;

    private BoatMovement boatMovement;
    private BoatSink boatSink;
    private Quaternion leftPaddleBaseRot;
    private Quaternion rightPaddleBaseRot;
    private bool hasLeftPaddle = false;
    private bool hasRightPaddle = false;

    private float rowPhase = 0f;
    private float damageShakeTimer = 0f;
    private float repairLiftTimer = 0f;
    private float smoothedBank = 0f;
    private float smoothedMovePitch = 0f;
    private bool isDocked = false;

    void Awake()
    {
        boatMovement = GetComponent<BoatMovement>();
        boatSink = GetComponent<BoatSink>();

        if (leftPaddle == null)
            leftPaddle = transform.Find("Paddle");
        if (rightPaddle == null)
            rightPaddle = transform.Find("Paddle (1)");

        if (leftPaddle != null)
        {
            leftPaddleBaseRot = leftPaddle.localRotation;
            hasLeftPaddle = true;
        }

        if (rightPaddle != null)
        {
            rightPaddleBaseRot = rightPaddle.localRotation;
            hasRightPaddle = true;
        }
    }

    void Update()
    {
        if (Time.timeScale == 0f)
            return;

        if (damageShakeTimer > 0f)
            damageShakeTimer = Mathf.Max(0f, damageShakeTimer - Time.deltaTime);

        if (repairLiftTimer > 0f)
            repairLiftTimer = Mathf.Max(0f, repairLiftTimer - Time.deltaTime);

        bool sinking = boatSink != null && (boatSink.isSinking || boatSink.currentHealth <= 0);
        float moveInput = (!sinking && !isDocked && boatMovement != null && boatMovement.canMove)
            ? boatMovement.CurrentMoveInput
            : 0f;
        float turnInput = (!sinking && !isDocked && boatMovement != null && boatMovement.canMove)
            ? boatMovement.CurrentTurnInput
            : 0f;

        UpdateOars(moveInput, turnInput, sinking);
        UpdateWaterParticles(moveInput, turnInput, sinking);
    }

    public float GetWaveBobOffset(bool isQuizActive)
    {
        float scale = GetMotionScale(isQuizActive);
        float wave = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude * scale;
        float repairLift = repairLiftTimer > 0f ? Mathf.Sin((repairLiftTimer / 0.6f) * Mathf.PI) * 0.14f : 0f;
        return wave + repairLift;
    }

    public Quaternion GetWaveTiltRotation(float moveInput, float turnInput, bool isQuizActive)
    {
        float scale = GetMotionScale(isQuizActive);

        float targetBank = -turnInput * turnBankAngle * scale;
        float targetMovePitch = moveInput * movePitchAngle * scale;
        smoothedBank = Mathf.Lerp(smoothedBank, targetBank, Time.fixedDeltaTime * 5f);
        smoothedMovePitch = Mathf.Lerp(smoothedMovePitch, targetMovePitch, Time.fixedDeltaTime * 5f);

        float naturalPitch = Mathf.Sin(Time.time * rockSpeed) * pitchAmplitude * scale;
        float naturalRoll = Mathf.Cos(Time.time * rockSpeed * 0.85f) * rollAmplitude * scale;
        float damageRoll = damageShakeTimer > 0f
            ? Mathf.Sin(Time.time * 28f) * 5.5f * (damageShakeTimer / 0.55f)
            : 0f;

        return Quaternion.Euler(
            naturalPitch + smoothedMovePitch,
            0f,
            naturalRoll + smoothedBank + damageRoll
        );
    }

    public void SetDocked(bool docked)
    {
        isDocked = docked;
    }

    public void PlayDamageSplash()
    {
        damageShakeTimer = 0.55f;
        if (damageSplashParticles != null)
            damageSplashParticles.Play();
    }

    public void PlayRepairEffect()
    {
        repairLiftTimer = 0.6f;
        if (repairParticles != null)
            repairParticles.Play();
    }

    private float GetMotionScale(bool isQuizActive)
    {
        if (isDocked)
            return dockedMotionScale;
        if (isQuizActive)
            return quizMotionScale;
        return 1f;
    }

    private void UpdateOars(float moveInput, float turnInput, bool sinking)
    {
        float drive = Mathf.Clamp01(Mathf.Abs(moveInput) + Mathf.Abs(turnInput) * 0.7f);

        if (sinking)
            drive = 0f;

        if (drive > 0.05f)
            rowPhase += Time.deltaTime * rowSpeed * (0.75f + drive * 0.6f);

        float idleSway = Mathf.Sin(Time.time * 1.4f) * 2.5f;

        if (hasLeftPaddle)
        {
            float leftEffort = Mathf.Clamp(moveInput + turnInput * 0.8f, -1f, 1f);
            float sweep = drive > 0.05f ? Mathf.Sin(rowPhase) * rowSweepAngle * Mathf.Sign(leftEffort == 0f ? 1f : leftEffort) * Mathf.Clamp01(Mathf.Abs(leftEffort) + 0.35f) : idleSway;
            float dip = drive > 0.05f ? Mathf.Cos(rowPhase) * rowDipAngle : 0f;
            Quaternion target = leftPaddleBaseRot * Quaternion.Euler(0f, sweep, dip);
            leftPaddle.localRotation = Quaternion.Lerp(leftPaddle.localRotation, target, Time.deltaTime * 8f);
        }

        if (hasRightPaddle)
        {
            float rightEffort = Mathf.Clamp(moveInput - turnInput * 0.8f, -1f, 1f);
            float sweep = drive > 0.05f ? Mathf.Sin(rowPhase) * rowSweepAngle * Mathf.Sign(rightEffort == 0f ? 1f : rightEffort) * Mathf.Clamp01(Mathf.Abs(rightEffort) + 0.35f) : -idleSway;
            float dip = drive > 0.05f ? Mathf.Cos(rowPhase) * rowDipAngle : 0f;
            Quaternion target = rightPaddleBaseRot * Quaternion.Euler(0f, -sweep, dip);
            rightPaddle.localRotation = Quaternion.Lerp(rightPaddle.localRotation, target, Time.deltaTime * 8f);
        }
    }

    private void UpdateWaterParticles(float moveInput, float turnInput, bool sinking)
    {
        float speedFactor = sinking ? 0f : Mathf.Clamp01(Mathf.Abs(moveInput) + Mathf.Abs(turnInput) * 0.5f);
        bool moving = speedFactor > 0.08f;

        SetLoopingParticleActive(wakeParticles, moving, 18f * speedFactor);
        SetLoopingParticleActive(bowSplashParticles, moving && moveInput > 0.05f, 14f * speedFactor);
    }

    private void SetLoopingParticleActive(ParticleSystem ps, bool active, float rate)
    {
        if (ps == null)
            return;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = active ? rate : 0f;

        if (active && !ps.isPlaying)
            ps.Play();
        else if (!active && ps.isPlaying)
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
    }
}
