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
        ScreenFeedbackFX.EnsureInstance();
        IslandMinimapUI minimap = IslandMinimapUI.EnsureInScene();
        GameAudioManager audioMgr = GameAudioManager.EnsureInstance();
        PauseAndSettingsUI.EnsureInstance();
        audioMgr.SetIslandAmbience(islandIndex);
        ChestQuizManager chestQuiz = ChestQuizManager.EnsureInScene(islandIndex);
        chestQuiz.RefreshForIsland(islandIndex);

        CleanupDuplicateSceneSystems();
        SpawnLandmarkBeacons();

        if (player == null)
            player = FindAnyObjectByType<IslandPlayerController>();

        if (player != null)
        {
            if (playerSpawnPoint != null)
                player.TeleportToSpawn(playerSpawnPoint.position, playerSpawnPoint.rotation);
            player.RefreshInteractables();
        }

        compass.RefreshTargetsNow();
        minimap.RefreshSceneMarkers();

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
        AudioListener[] listeners = FindObjectsByType<AudioListener>();
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

        EventSystem[] eventSystems = FindObjectsByType<EventSystem>();
        for (int i = 1; i < eventSystems.Length; i++)
            Destroy(eventSystems[i].gameObject);
    }

    private void SpawnLandmarkBeacons()
    {
        IslandBoatReturn boatRet = FindAnyObjectByType<IslandBoatReturn>();
        if (boatRet != null)
        {
            CreateBeam("DockReturnBeacon", boatRet.transform.position + Vector3.up * 16f, new Color(0.22f, 0.88f, 0.88f, 0.24f), 0.7f, false, "");
        }

        TreasureChest[] chests = FindObjectsByType<TreasureChest>();
        for (int i = 0; i < chests.Length; i++)
        {
            TreasureChest tc = chests[i];
            if (tc != null && tc.gameObject.activeInHierarchy && tc.isSpecialChest)
            {
                CreateBeam("SpecialChestBeacon", tc.transform.position + Vector3.up * 18f, new Color(1f, 0.86f, 0.22f, 0.34f), 0.85f, true, tc.chestId);
                break;
            }
        }
    }

    private void CreateBeam(string name, Vector3 pos, Color col, float radius, bool requireKey, string chestId)
    {
        GameObject beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        beam.name = name;
        beam.transform.position = pos;
        beam.transform.localScale = new Vector3(radius, 18f, radius);

        Collider c = beam.GetComponent<Collider>();
        if (c != null)
            Destroy(c);

        Renderer mr = beam.GetComponent<Renderer>();
        if (mr != null)
        {
            mr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            Shader s = Shader.Find("Universal Render Pipeline/Unlit");
            if (s == null) s = Shader.Find("Standard");
            if (s != null)
            {
                Material mat = new Material(s);
                mat.SetFloat("_Surface", 1f);
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = 3000;
                if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", col);
                if (mat.HasProperty("_Color")) mat.SetColor("_Color", col);
                mr.sharedMaterial = mat;
            }
        }

        LandmarkBeacon lb = beam.AddComponent<LandmarkBeacon>();
        lb.requireIslandKey = requireKey;
        lb.islandIndex = islandIndex;
        lb.linkedChestId = chestId;
    }
}
