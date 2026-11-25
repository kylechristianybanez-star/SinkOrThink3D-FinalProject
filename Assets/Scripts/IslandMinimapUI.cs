using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Bottom-right [M]-toggleable Island Minimap HUD showing Player heading, Harbor Return Boat,
/// unopened Regular Chests, Special Chest, and Island Guide NPCs within radar range.
/// </summary>
public class IslandMinimapUI : MonoBehaviour
{
    public static IslandMinimapUI Instance { get; private set; }

    public float radarRange = 110f;

    private Canvas rootCanvas;
    private GameObject mapFrame;
    private RectTransform radarArea;
    private RectTransform playerMarker;
    private RectTransform dockMarker;
    private RectTransform npcMarker;
    private readonly List<RectTransform> chestMarkers = new List<RectTransform>();
    private readonly List<TreasureChest> trackedChests = new List<TreasureChest>();

    private IslandPlayerController player;
    private IslandBoatReturn boatReturn;
    private IslandGuideNPC guideNpc;
    private ChestQuizManager chestQuizMgr;
    private bool mapVisible = true;

    public static IslandMinimapUI EnsureInScene()
    {
        if (Instance != null)
            return Instance;

        IslandMinimapUI existing = FindAnyObjectByType<IslandMinimapUI>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("IslandMinimapUI");
        Instance = go.AddComponent<IslandMinimapUI>();
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
        BuildMinimapUI();
    }

    void Start()
    {
        RefreshSceneMarkers();
    }

    public void RefreshSceneMarkers()
    {
        player = FindAnyObjectByType<IslandPlayerController>();
        boatReturn = FindAnyObjectByType<IslandBoatReturn>();
        guideNpc = FindAnyObjectByType<IslandGuideNPC>();
        chestQuizMgr = FindAnyObjectByType<ChestQuizManager>();

        for (int i = 0; i < chestMarkers.Count; i++)
        {
            if (chestMarkers[i] != null)
                Destroy(chestMarkers[i].gameObject);
        }
        chestMarkers.Clear();
        trackedChests.Clear();

        TreasureChest[] chests = FindObjectsByType<TreasureChest>();
        for (int i = 0; i < chests.Length; i++)
        {
            TreasureChest tc = chests[i];
            if (tc == null || !tc.gameObject.activeInHierarchy)
                continue;

            trackedChests.Add(tc);
            float sz = tc.isSpecialChest ? 12f : 8f;
            Color col = tc.isSpecialChest ? new Color(1f, 0.88f, 0.2f, 1f) : new Color(0.95f, 0.65f, 0.16f, 1f);
            chestMarkers.Add(CreateDot("ChestDot_" + i, col, sz));
        }

        if (dockMarker != null)
            dockMarker.gameObject.SetActive(boatReturn != null);
        if (npcMarker != null)
            npcMarker.gameObject.SetActive(guideNpc != null);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            bool blocked = (PauseAndSettingsUI.Instance != null && PauseAndSettingsUI.Instance.IsOpen) ||
                           (InventoryHotbarUI.Instance != null && InventoryHotbarUI.Instance.IsInventoryOpen) ||
                           IslandGuideNPC.AnyDialogueOpen ||
                           (chestQuizMgr != null && chestQuizMgr.IsQuizOpen);
            if (!blocked && mapFrame != null)
            {
                mapVisible = !mapVisible;
                mapFrame.SetActive(mapVisible);
            }
        }

        if (!mapVisible || mapFrame == null || !mapFrame.activeSelf || player == null)
            return;

        float halfBox = 74f;
        Vector3 pPos = player.transform.position;
        float yawRad = player.transform.eulerAngles.y * Mathf.Deg2Rad;
        float cosYaw = Mathf.Cos(yawRad);
        float sinYaw = Mathf.Sin(yawRad);

        if (boatReturn != null && dockMarker != null)
            PlaceWorldMarker(dockMarker, boatReturn.transform.position, pPos, cosYaw, sinYaw, halfBox);

        if (guideNpc != null && npcMarker != null)
            PlaceWorldMarker(npcMarker, guideNpc.transform.position, pPos, cosYaw, sinYaw, halfBox);

        for (int i = 0; i < trackedChests.Count && i < chestMarkers.Count; i++)
        {
            TreasureChest tc = trackedChests[i];
            RectTransform dot = chestMarkers[i];
            if (tc == null || dot == null)
                continue;

            bool show = tc.gameObject.activeInHierarchy && !tc.IsOpened;
            if (dot.gameObject.activeSelf != show)
                dot.gameObject.SetActive(show);

            if (show)
                PlaceWorldMarker(dot, tc.transform.position, pPos, cosYaw, sinYaw, halfBox);
        }
    }

    private void PlaceWorldMarker(RectTransform marker, Vector3 worldPos, Vector3 playerPos, float cosYaw, float sinYaw, float halfBox)
    {
        float dx = worldPos.x - playerPos.x;
        float dz = worldPos.z - playerPos.z;

        // Rotate into player-heading-up minimap coordinates
        float localX = dx * cosYaw - dz * sinYaw;
        float localY = dx * sinYaw + dz * cosYaw;

        float scale = halfBox / Mathf.Max(20f, radarRange);
        Vector2 uiPos = new Vector2(localX * scale, localY * scale);
        uiPos.x = Mathf.Clamp(uiPos.x, -halfBox, halfBox);
        uiPos.y = Mathf.Clamp(uiPos.y, -halfBox, halfBox);
        marker.anchoredPosition = uiPos;
    }

    private void BuildMinimapUI()
    {
        rootCanvas = gameObject.AddComponent<Canvas>();
        rootCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        rootCanvas.sortingOrder = 44;

        CanvasScaler scaler = gameObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);

        // Placed cleanly in bottom-right corner above screen edge so it never overlaps Compass or Hotbar
        mapFrame = CreateRect("IslandMinimapPanel", transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-116f, 128f), new Vector2(176f, 198f));
        Image frameImg = mapFrame.AddComponent<Image>();
        frameImg.color = new Color(0.24f, 0.16f, 0.08f, 0.92f);
        frameImg.raycastTarget = false;

        GameObject inner = CreateRect("RadarBg", mapFrame.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 11f), new Vector2(158f, 158f));
        radarArea = inner.GetComponent<RectTransform>();
        Image innerImg = inner.AddComponent<Image>();
        innerImg.color = new Color(0.08f, 0.14f, 0.18f, 0.94f);
        innerImg.raycastTarget = false;

        dockMarker = CreateDot("DockMarker", new Color(0.22f, 0.88f, 0.84f, 1f), 11f);
        npcMarker = CreateDot("NpcMarker", new Color(0.45f, 0.95f, 0.55f, 1f), 9f);
        playerMarker = CreateDot("PlayerCenter", Color.white, 9f);
        playerMarker.anchoredPosition = Vector2.zero;

        GameObject captionGo = CreateRect("Caption", mapFrame.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 12f), new Vector2(168f, 22f));
        TextMeshProUGUI capTxt = captionGo.AddComponent<TextMeshProUGUI>();
        capTxt.text = "Island Map [M]";
        capTxt.fontSize = 14f;
        capTxt.fontStyle = FontStyles.Bold;
        capTxt.alignment = TextAlignmentOptions.Center;
        capTxt.color = new Color(1f, 0.92f, 0.68f, 1f);
        capTxt.raycastTarget = false;
    }

    private RectTransform CreateDot(string name, Color col, float size)
    {
        GameObject go = CreateRect(name, radarArea, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size, size));
        Image img = go.AddComponent<Image>();
        img.color = col;
        img.raycastTarget = false;
        return go.GetComponent<RectTransform>();
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 aMin, Vector2 aMax, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        RectTransform rt = go.AddComponent<RectTransform>();
        rt.anchorMin = aMin;
        rt.anchorMax = aMax;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
        return go;
    }
}
