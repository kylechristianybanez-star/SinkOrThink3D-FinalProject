using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Controls Main Menu navigation and initializes a fresh GameStateManager on Play.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    public GameObject howToPlayPanel;

    public void PlayGame()
    {
        Time.timeScale = 1f;
        GameStateManager.EnsureInstance().StartNewGame();
        SceneManager.LoadScene("GameScene");
    }

    public void ShowHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(true);
    }

    public void CloseHowToPlay()
    {
        if (howToPlayPanel != null)
            howToPlayPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Debug.Log("Quit Game");
        Application.Quit();
    }
}
