using UnityEngine;

/// <summary>
/// Animates the 3D wooden quiz board appearance, question transition pulse,
/// correct-answer celebration bounce/particles, and wrong-answer wooden shake.
/// </summary>
public class QuizBoardAnimator : MonoBehaviour
{
    [Header("Appear Animation")]
    public float appearDuration = 0.35f;
    public float appearDropDistance = 0.22f;
    public float appearSwingAngle = 14f;

    [Header("Feedback Animation")]
    public float shakeDuration = 0.4f;
    public float shakeMagnitude = 0.055f;
    public float correctBounceAmount = 0.045f;
    public ParticleSystem correctBurstParticles;

    private Vector3 baseLocalPos;
    private Quaternion baseLocalRot;
    private Vector3 baseLocalScale;
    private bool initialized = false;

    private float appearTimer = 0f;
    private float pulseTimer = 0f;
    private float shakeTimer = 0f;
    private float bounceTimer = 0f;

    void Awake()
    {
        CacheBaseTransform();
    }

    void OnEnable()
    {
        CacheBaseTransform();
    }

    private void CacheBaseTransform()
    {
        if (initialized)
            return;

        baseLocalPos = transform.localPosition;
        baseLocalRot = transform.localRotation;
        baseLocalScale = transform.localScale;
        initialized = true;
    }

    void Update()
    {
        if (!initialized)
            return;

        Vector3 offsetPos = Vector3.zero;
        Quaternion offsetRot = Quaternion.identity;
        float scaleMultiplier = 1f;

        if (appearTimer > 0f)
        {
            appearTimer = Mathf.Max(0f, appearTimer - Time.deltaTime);
            float t = 1f - (appearTimer / Mathf.Max(0.01f, appearDuration));
            float smooth = 1f - Mathf.Pow(1f - t, 3f);

            offsetPos.y -= Mathf.Lerp(appearDropDistance, 0f, smooth);
            scaleMultiplier *= Mathf.Lerp(0.88f, 1f, smooth);
            offsetRot *= Quaternion.Euler(Mathf.Lerp(appearSwingAngle, 0f, smooth), 0f, 0f);
        }

        if (pulseTimer > 0f)
        {
            pulseTimer = Mathf.Max(0f, pulseTimer - Time.deltaTime);
            float norm = pulseTimer / 0.22f;
            float bump = Mathf.Sin(norm * Mathf.PI) * 0.035f;
            scaleMultiplier += bump;
        }

        if (bounceTimer > 0f)
        {
            bounceTimer = Mathf.Max(0f, bounceTimer - Time.deltaTime);
            float norm = bounceTimer / 0.35f;
            offsetPos.y += Mathf.Sin(norm * Mathf.PI) * correctBounceAmount;
            scaleMultiplier += Mathf.Sin(norm * Mathf.PI) * 0.03f;
        }

        if (shakeTimer > 0f)
        {
            shakeTimer = Mathf.Max(0f, shakeTimer - Time.deltaTime);
            float norm = shakeTimer / Mathf.Max(0.01f, shakeDuration);
            float wave = Mathf.Sin(Time.time * 48f);
            offsetPos.x += wave * shakeMagnitude * norm;
            offsetRot *= Quaternion.Euler(0f, 0f, wave * 2.5f * norm);
        }

        transform.localPosition = baseLocalPos + offsetPos;
        transform.localRotation = baseLocalRot * offsetRot;
        transform.localScale = baseLocalScale * scaleMultiplier;
    }

    public void PlayAppear()
    {
        CacheBaseTransform();
        appearTimer = appearDuration;
        pulseTimer = 0f;
        shakeTimer = 0f;
        bounceTimer = 0f;
    }

    public void PlayNextQuestionPulse()
    {
        CacheBaseTransform();
        pulseTimer = 0.22f;
        shakeTimer = 0f;
        bounceTimer = 0f;
    }

    public void PlayCorrectFeedback()
    {
        CacheBaseTransform();
        bounceTimer = 0.35f;
        if (correctBurstParticles != null)
            correctBurstParticles.Play();
    }

    public void PlayWrongFeedback()
    {
        CacheBaseTransform();
        shakeTimer = shakeDuration;
    }

    public void ResetVisualState()
    {
        if (!initialized)
            return;

        appearTimer = 0f;
        pulseTimer = 0f;
        shakeTimer = 0f;
        bounceTimer = 0f;
        transform.localPosition = baseLocalPos;
        transform.localRotation = baseLocalRot;
        transform.localScale = baseLocalScale;
    }
}
