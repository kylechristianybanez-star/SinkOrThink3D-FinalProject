using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Provides smooth screen fade transitions between scenes and animated dock arrival banners.
/// Persists across scene loads without blocking UI clicks when transparent.
/// </summary>
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance { get; private set; }

    private Image fadeImage;
    private CanvasGroup bannerCanvasGroup;
    private TMP_Text bannerText;

    private Coroutine activeFadeCoroutine;
    private Coroutine activeBannerCoroutine;
    private bool isLoadingScene = false;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        BuildOverlayCanvas();
    }

    public static ScreenFader EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        ScreenFader existing = FindAnyObjectByType<ScreenFader>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("ScreenFader");
        Instance = go.AddComponent<ScreenFader>();
        return Instance;
    }

    private void BuildOverlayCanvas()
    {
        Canvas canvas = gameObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Full-screen black fade overlay
        GameObject fadeGo = new GameObject("FadeOverlay");
        fadeGo.transform.SetParent(transform, false);
        RectTransform fadeRect = fadeGo.AddComponent<RectTransform>();
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;

        fadeImage = fadeGo.AddComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0f);
        fadeImage.raycastTarget = false;

        // Arrival notification banner
        GameObject bannerGo = new GameObject("ArrivalBanner");
        bannerGo.transform.SetParent(transform, false);
        RectTransform bannerRect = bannerGo.AddComponent<RectTransform>();
        bannerRect.anchorMin = new Vector2(0.5f, 0.82f);
        bannerRect.anchorMax = new Vector2(0.5f, 0.82f);
        bannerRect.sizeDelta = new Vector2(680f, 96f);

        Image bannerBg = bannerGo.AddComponent<Image>();
        bannerBg.color = new Color(0.14f, 0.09f, 0.05f, 0.92f);
        bannerBg.raycastTarget = false;

        bannerCanvasGroup = bannerGo.AddComponent<CanvasGroup>();
        bannerCanvasGroup.alpha = 0f;
        bannerCanvasGroup.blocksRaycasts = false;
        bannerCanvasGroup.interactable = false;

        GameObject textGo = new GameObject("BannerText");
        textGo.transform.SetParent(bannerGo.transform, false);
        RectTransform textRect = textGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(20f, 10f);
        textRect.offsetMax = new Vector2(-20f, -10f);

        bannerText = textGo.AddComponent<TextMeshProUGUI>();
        bannerText.alignment = TextAlignmentOptions.Center;
        bannerText.fontSize = 32f;
        bannerText.fontStyle = FontStyles.Bold;
        bannerText.color = new Color(1f, 0.88f, 0.56f, 1f);
        bannerText.raycastTarget = false;
    }

    public Coroutine FadeOut(float duration = 0.6f)
    {
        if (activeFadeCoroutine != null)
            StopCoroutine(activeFadeCoroutine);
        activeFadeCoroutine = StartCoroutine(FadeRoutine(1f, duration));
        return activeFadeCoroutine;
    }

    public Coroutine FadeIn(float duration = 0.6f)
    {
        if (activeFadeCoroutine != null)
            StopCoroutine(activeFadeCoroutine);
        activeFadeCoroutine = StartCoroutine(FadeRoutine(0f, duration));
        return activeFadeCoroutine;
    }

    public void FadeAndLoadScene(string sceneName, float duration = 0.6f)
    {
        if (isLoadingScene || string.IsNullOrEmpty(sceneName))
            return;

        StartCoroutine(FadeAndLoadRoutine(sceneName, duration));
    }

    public void ShowArrivalBanner(string message, float duration = 2.5f)
    {
        if (bannerCanvasGroup == null || bannerText == null)
            return;

        if (activeBannerCoroutine != null)
            StopCoroutine(activeBannerCoroutine);

        bannerText.text = message;
        activeBannerCoroutine = StartCoroutine(BannerRoutine(duration));
    }

    private IEnumerator FadeAndLoadRoutine(string sceneName, float duration)
    {
        isLoadingScene = true;
        yield return FadeOut(duration);
        SceneManager.LoadScene(sceneName);
        yield return null;
        yield return FadeIn(duration);
        isLoadingScene = false;
    }

    private IEnumerator FadeRoutine(float targetAlpha, float duration)
    {
        if (fadeImage == null)
            yield break;

        float startAlpha = fadeImage.color.a;
        fadeImage.raycastTarget = targetAlpha > 0.05f;

        float elapsed = 0f;
        float safeDuration = Mathf.Max(0.05f, duration);

        while (elapsed < safeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / safeDuration);
            float alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            fadeImage.color = new Color(0f, 0f, 0f, alpha);
            yield return null;
        }

        fadeImage.color = new Color(0f, 0f, 0f, targetAlpha);
        fadeImage.raycastTarget = targetAlpha > 0.05f;
        activeFadeCoroutine = null;
    }

    private IEnumerator BannerRoutine(float holdDuration)
    {
        float fadeTime = 0.35f;
        float elapsed = 0f;

        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            bannerCanvasGroup.alpha = Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }

        bannerCanvasGroup.alpha = 1f;
        yield return new WaitForSecondsRealtime(Mathf.Max(0.2f, holdDuration));

        elapsed = 0f;
        while (elapsed < fadeTime)
        {
            elapsed += Time.unscaledDeltaTime;
            bannerCanvasGroup.alpha = 1f - Mathf.Clamp01(elapsed / fadeTime);
            yield return null;
        }

        bannerCanvasGroup.alpha = 0f;
        activeBannerCoroutine = null;
    }
}
