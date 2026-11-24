using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Manages boat health, smooth health bar updates, sinking animation, and healing.
/// </summary>
public class BoatSink : MonoBehaviour
{
    public float sinkSpeed = 0.5f;
    public bool isSinking = false;

    public int maxHealth = 100;
    public int currentHealth = 100;
    public int damage = 20;

    public Slider healthBar;
    public TMP_Text healthText;

    [Header("Smooth Health & Sinking Polish")]
    public float healthLerpSpeed = 6f;
    public float maxSinkDepth = 3.5f;
    public float sinkTiltAngle = 14f;
    public Image healthBarFill;

    private float displayedHealth = 100f;
    private float startY = 0f;
    private Quaternion startRotation;
    private Rigidbody rb;
    private BoatAnimator boatAnimator;

    void Start()
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        state.maxBoatHealth = maxHealth;
        currentHealth = Mathf.Clamp(state.boatHealth, 0, maxHealth);
        displayedHealth = currentHealth;
        isSinking = currentHealth <= 0;

        startY = transform.position.y;
        startRotation = transform.rotation;
        rb = GetComponent<Rigidbody>();
        boatAnimator = GetComponent<BoatAnimator>();

        if (healthBar != null)
        {
            healthBar.maxValue = maxHealth;
            healthBar.value = currentHealth;

            if (healthBarFill == null && healthBar.fillRect != null)
                healthBarFill = healthBar.fillRect.GetComponent<Image>();
        }

        if (healthText != null)
            healthText.raycastTarget = false;

        UpdateHealthUIImmediate();
    }

    void Update()
    {
        if (healthBar != null)
        {
            displayedHealth = Mathf.Lerp(displayedHealth, currentHealth, Time.deltaTime * healthLerpSpeed);
            if (Mathf.Abs(displayedHealth - currentHealth) < 0.05f)
                displayedHealth = currentHealth;

            healthBar.value = displayedHealth;
            UpdateHealthBarColor(displayedHealth / Mathf.Max(1, maxHealth));
        }

        if (isSinking)
        {
            float targetY = startY - maxSinkDepth;
            Vector3 currentPos = rb != null ? rb.position : transform.position;

            if (currentPos.y > targetY)
            {
                float nextY = Mathf.MoveTowards(currentPos.y, targetY, sinkSpeed * Time.deltaTime);
                Vector3 nextPos = new Vector3(currentPos.x, nextY, currentPos.z);

                if (rb != null && rb.isKinematic)
                    rb.MovePosition(nextPos);
                else
                    transform.position = nextPos;
            }

            Quaternion targetRot = startRotation * Quaternion.Euler(sinkTiltAngle * 0.5f, 0f, sinkTiltAngle);
            Quaternion nextRot = Quaternion.Lerp(transform.rotation, targetRot, Time.deltaTime * 1.2f);
            if (rb != null && rb.isKinematic)
                rb.MoveRotation(nextRot);
            else
                transform.rotation = nextRot;
        }
    }

    /// <summary>
    /// Preserved method called on wrong quiz answer or timeout.
    /// </summary>
    public void SinkBoat()
    {
        ApplyDamage(damage);
    }

    public void ApplyDamage(int amount)
    {
        if (currentHealth <= 0 || amount <= 0)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        currentHealth = state.DamageBoat(amount);

        if (boatAnimator != null)
            boatAnimator.PlayDamageSplash();

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            isSinking = true;
        }

        UpdateHealthText();
        Debug.Log("Boat Health: " + currentHealth);
    }

    public int HealBoat(int amount)
    {
        if (isSinking || currentHealth <= 0 || currentHealth >= maxHealth || amount <= 0)
            return 0;

        GameStateManager state = GameStateManager.EnsureInstance();
        int restored = state.HealBoat(amount);
        currentHealth = state.boatHealth;

        if (restored > 0)
        {
            UpdateHealthText();
            if (boatAnimator != null)
                boatAnimator.PlayRepairEffect();
        }

        return restored;
    }

    public void SyncFromGameState(bool playRepairFx)
    {
        GameStateManager state = GameStateManager.EnsureInstance();
        currentHealth = Mathf.Clamp(state.boatHealth, 0, maxHealth);
        UpdateHealthText();

        if (playRepairFx && boatAnimator != null)
            boatAnimator.PlayRepairEffect();
    }

    private void UpdateHealthUIImmediate()
    {
        UpdateHealthText();
        UpdateHealthBarColor((float)currentHealth / Mathf.Max(1, maxHealth));
    }

    private void UpdateHealthText()
    {
        if (healthText != null)
            healthText.text = "Boat Health: " + currentHealth + "/" + maxHealth;
    }

    private void UpdateHealthBarColor(float ratio)
    {
        if (healthBarFill == null)
            return;

        if (ratio > 0.5f)
            healthBarFill.color = new Color(0.22f, 0.78f, 0.34f);
        else if (ratio > 0.25f)
            healthBarFill.color = new Color(0.95f, 0.75f, 0.18f);
        else
            healthBarFill.color = new Color(0.88f, 0.22f, 0.22f);
    }
}
