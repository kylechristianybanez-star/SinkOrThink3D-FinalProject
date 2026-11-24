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

    [Header("Reusable Island Environments")]
    public GameObject discoveryIslandRoot;
    public GameObject vikingIslandRoot;
    public GameObject finalTreasureIslandRoot;
    public Transform discoverySpawnPoint;
    public Transform vikingSpawnPoint;
    public Transform finalSpawnPoint;

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

        int targetIdx = Mathf.Clamp(state.selectedIslandIndex, 0, 2);
        ActivateSelectedIsland(targetIdx);

        InventoryHotbarUI.EnsureInstance();
        MagicCompassUI compass = MagicCompassUI.EnsureInstance();
        ChestQuizManager chestQuiz = ChestQuizManager.EnsureInScene(islandIndex);
        chestQuiz.RefreshForIsland(islandIndex);

        CleanupDuplicateSceneSystems();

        if (player == null)
            player = FindFirstObjectByType<IslandPlayerController>();

        if (player != null)
        {
            if (playerSpawnPoint != null)
                player.TeleportToSpawn(playerSpawnPoint.position, playerSpawnPoint.rotation);
            player.RefreshInteractables();
        }

        compass.RefreshTargetsNow();

        if (islandStatusText != null)
            islandStatusText.raycastTarget = false;

        RefreshHud();
        fader.ShowArrivalBanner("Exploring " + islandDisplayName, 2.2f);
    }

    private void ActivateSelectedIsland(int targetIdx)
    {
        GameObject[] roots = new GameObject[] { discoveryIslandRoot, vikingIslandRoot, finalTreasureIslandRoot };
        Transform[] spawns = new Transform[] { discoverySpawnPoint, vikingSpawnPoint, finalSpawnPoint };

        // Fall back safely to DiscoveryIsland if the target island isn't built yet
        if (roots[targetIdx] == null)
            targetIdx = 0;

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i] != null)
                roots[i].SetActive(i == targetIdx);
        }

        islandIndex = targetIdx;
        islandDisplayName = GameStateManager.EnsureInstance().GetIslandDisplayName(targetIdx);

        if (spawns[targetIdx] != null)
            playerSpawnPoint = spawns[targetIdx];
    }

    public void RefreshHud()
    {
        if (islandStatusText == null)
            return;

        GameStateManager state = GameStateManager.EnsureInstance();
        string keyText = (islandIndex >= 0 && islandIndex < 3 && state.hasIslandKey[islandIndex]) ? "Yes" : "No";
        string compassText = state.hasMagicCompass ? "  |  Compass: Active" : "";
        int openedCount = state.GetOpenedChestCountForIsland(islandIndex);
        islandStatusText.text =
            islandDisplayName +
            "  |  Boat HP: " + state.boatHealth + "/" + state.maxBoatHealth +
            "  |  Coins: " + state.coins +
            "  |  Key: " + keyText +
            "  |  Chests: " + openedCount + "/6" +
            compassText +
            "\n" + state.GetCurrentObjectiveText();
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
