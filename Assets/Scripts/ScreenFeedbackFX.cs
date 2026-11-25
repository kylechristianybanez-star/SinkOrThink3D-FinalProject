using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Persistent screen-space feedback overlay & camera shake controller.
/// Triggers crimson hull damage flash + camera shake when the boat takes damage,
/// and emerald shimmer feedback when repairing the boat.
/// </summary>
public class ScreenFeedbackFX : MonoBehaviour
{
    public static ScreenFeedbackFX Instance { get; private set; }

    private Canvas rootCanvas;
    private Image damageFlashImg;
    private Image repairFlashImg;

    private float damageTimer = 0f;
    private float damageDuration = 0.55f;
    private float repairTimer = 0f;
    private float repairDuration = 0.45f;

    private float shakeTimer = 0f;
    private float shakeDuration = 0.35f;
    private float shakeIntensity = 0f;
    private Camera lastShakenCam;
    private Vector3 lastAppliedShakeOffset = Vector3.zero;

    public static ScreenFeedbackFX EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        ScreenFeedbackFX existing = FindAnyObjectByType<ScreenFeedbackFX>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("ScreenFeedbackFX");
        Instance = go.AddComponent<ScreenFeedbackFX>();
        return Instance;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlayUI();
    }

    void Update()
    {
        float dt = Time.unscaledDeltaTime;

        if (damageTimer > 0f && damageFlashImg != null)
        {
            damageTimer = Mathf.Max(0f, damageTimer - dt);
            float alpha = Mathf.Clamp01(damageTimer / damageDuration) * 0.42f;
            damageFlashImg.color = new Color(0.75f, 0.1f, 0.1f, alpha);
        }

        if (repairTimer > 0f && repairFlashImg != null)
        {
            repairTimer = Mathf.Max(0f, repairTimer - dt);
            float alpha = Mathf.Clamp01(repairTimer / repairDuration) * 0.28f;
            repairFlashImg.color = new Color(0.18f, 0.85f, 0.38f, alpha);
        }
    }

    void LateUpdate()
    {
        Camera cam = Camera.main;
        if (lastShakenCam != null && lastAppliedShakeOffset.sqrMagnitude > 0f)
        {
            lastShakenCam.transform.localPosition -= lastAppliedShakeOffset;
            lastAppliedShakeOffset = Vector3.zero;
        }

        if (shakeTimer <= 0f || cam == null || Time.timeScale == 0f)
            return;

        shakeTimer = Mathf.Max(0f, shakeTimer - Time.unscaledDeltaTime);
        float progress = shakeDuration > 0f ? (shakeTimer / shakeDuration) : 0f;
        float amp = shakeIntensity * progress;
        Vector3 offset = new Vector3(
            Random.Range(-amp, amp),
            Random.Range(-amp * 0.7f, amp * 0.7f),
            0f
        );

        cam.transform.localPosition += offset;
        lastShakenCam = cam;
        lastAppliedShakeOffset = offset;
    }

    public void TriggerDamageFeedback(float shakeMag = 0.20f)
    {
        damageTimer = damageDuration;
        shakeIntensity = Mathf.Max(shakeIntensity, shakeMag);
        shakeTimer = shakeDuration;
        GameAudioManager.EnsureInstance().PlayHullCreak();
    }

    public void TriggerRepairFeedback()
    {
        repairTimer = repairDuration;
    }

    private void BuildOverlayUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 150;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        damageFlashImg = CreateFullscreenImage("DamageFlash", new Color(0.75f, 0.1f, 0.1f, 0f));
        repairFlashImg = CreateFullscreenImage("RepairFlash", new Color(0.18f, 0.85f, 0.38f, 0f));
    }

    private Image CreateFullscreenImage(string name, Color initialColor)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(transform, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;

        Image img = go.AddComponent<Image>();
        img.color = initialColor;
        img.raycastTarget = false;
        return img;
    }
}
