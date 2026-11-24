using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls Main Menu navigation and initializes a fresh GameStateManager on Play.
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
    }

    public void PlayGame()
    {
        GameAudioManager.EnsureInstance().PlayClick();
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        SceneManager.LoadScene("GameScene");
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
