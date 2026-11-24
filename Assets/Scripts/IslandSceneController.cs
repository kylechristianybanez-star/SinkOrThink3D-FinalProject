using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Coordinates island scene initialization, prevents duplicate cameras/EventSystems,
/// places the player at the dock spawn, and updates island HUD status.
/// </summary>
public class IslandSceneController : MonoBehaviour
{
    public int islandIndex = 0;
    public string islandDisplayName = "Discovery Island";
    public Transform playerSpawnPoint;
    public IslandPlayerController player;
    public TMP_Text islandStatusText;

    void OnEnable()
    {
        GameStateManager.OnStateChanged += RefreshHud;
    }

    void OnDisable()
    {
        GameStateManager.OnStateChanged -= RefreshHud;
    }

    void Start()
    {
        Time.timeScale = 1f;
        GameStateManager state = GameStateManager.EnsureInstance();
        ScreenFader fader = ScreenFader.EnsureInstance();
        InventoryHotbarUI.EnsureInstance();
        ChestQuizManager.EnsureInScene(islandIndex);

        CleanupDuplicateSceneSystems();

        if (player == null)
            player = FindFirstObjectByType<IslandPlayerController>();

        if (player != null && playerSpawnPoint != null)
            player.TeleportToSpawn(playerSpawnPoint.position, playerSpawnPoint.rotation);

        if (islandStatusText != null)
            islandStatusText.raycastTarget = false;

        RefreshHud();
        fader.ShowArrivalBanner("Exploring " + islandDisplayName, 2.2f);
    }

    public void RefreshHud()
    {
        if (islandStatusText == null)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        string keyText = (islandIndex >= 0 && islandIndex < 3 && state.hasIslandKey[islandIndex]) ? "Yes" : "No";
        string compassText = state.hasMagicCompass ? "  |  Compass: Yes" : "";
        islandStatusText.text =
            islandDisplayName +
            "  |  Boat HP: " + state.boatHealth + "/" + state.maxBoatHealth +
            "  |  Coins: " + state.coins +
            "  |  Key: " + keyText +
            compassText;
    }

    private void CleanupDuplicateSceneSystems()
    {
        AudioListener[] listeners = FindObjectsByType<AudioListener>(FindObjectsSortMode.None);
        bool keptFirstListener = false;
        for (int i = 0; i < listeners.Length; i++)
        {
            if (!keptFirstListener && listeners[i].enabled)
            {
                keptFirstListener = true;
                continue;
            }
            listeners[i].enabled = false;
        }

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>(FindObjectsSortMode.None);
        for (int i = 1; i < eventSystems.Length; i++)
            Destroy(eventSystems[i].gameObject);
    }
}
