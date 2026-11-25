using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls Main Menu navigation, Continue saved voyage button, and GameStateManager initialization on Play.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public GameObject howToPlayPanel;

    void Start()
    {
        Time.timeScale = 1f;
        GameAudioManager.EnsureInstance();
        PauseAndSettingsUI.EnsureInstance();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureContinueButton();
    }

    public void PlayGame()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        SceneManager.LoadScene("GameScene");
    }

    public void ContinueGame()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        Time.timeScale = 1f;
        if (GameStateManager.EnsureInstance().LoadProgress())
            SceneManager.LoadScene("GameScene");
        else
            PlayGame();
    }

    private void EnsureContinueButton()
    {
        GameObject canvasGo = GameObject.Find("MainMenuCanvas");
        if (canvasGo == null)
            return;

        Transform existing = canvasGo.transform.Find("ContinueButton");
        if (!GameStateManager.HasSavedProgress())
        {
            if (existing != null)
                existing.gameObject.SetActive(false);
            return;
        }

        if (existing != null)
        {
            existing.gameObject.SetActive(true);
            return;
        }

        Transform playTf = canvasGo.transform.Find("PlayButton");
        if (playTf == null)
            return;

        GameObject contGo = Instantiate(playTf.gameObject, canvasGo.transform);
        contGo.name = "ContinueButton";
        RectTransform playRt = playTf.GetComponent<RectTransform>();
        RectTransform contRt = contGo.GetComponent<RectTransform>();
        if (playRt != null && contRt != null)
        {
            contRt.anchoredPosition = playRt.anchoredPosition + new Vector2(0f, playRt.sizeDelta.y + 14f);
            contRt.sizeDelta = playRt.sizeDelta;
        }

        Image contImg = contGo.GetComponent<Image>();
        if (contImg != null)
            contImg.color = new Color(0.18f, 0.56f, 0.72f, 1f);

        TMP_Text label = contGo.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
            label.text = "Continue Voyage";

        Button btn = contGo.GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick = new Button.ButtonClickedEvent();
            btn.onClick.AddListener(ContinueGame);
        }
    }

    public void ShowHowToPlay()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    public void OpenSettings()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        PauseAndSettingsUI.EnsureInstance().OpenSettingsStandalone();
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
