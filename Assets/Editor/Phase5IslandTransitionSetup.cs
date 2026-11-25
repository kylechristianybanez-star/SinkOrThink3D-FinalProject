using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Idempotent Editor tool to build the Phase 5 playable Discovery Island foundation
/// in Assets/Scenes/IslandScene.unity (terrain, water, dock, ramp, return boat, player, HUD)
/// and enable scene transition in Assets/Scenes/GameScene.unity.
/// </summary>
public static class Phase5IslandTransitionSetup
{
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string TerrainDataPath = "Assets/Free Island Collection/Environment/Terrain/Terrains/Terrain 1.asset";
    private const string SkyMaterialPath = "Assets/Free Island Collection/Environment/Skies/Sky Material.mat";
    private const string DockPrefabPath = "Assets/Island/Prefabs/Dock.prefab";
    private const string BoatPrefabPath = "Assets/Boats/Prefabs/OldWoodenRowboat_Complete.prefab";
    private const string GameSceneWaterMatGuid = "379410282482c54498410674d0c4391d";
    private const string FallbackWaterMatPath = "Assets/Island/Textures/Materials/Water.mat";

    [MenuItem("Tools/SinkOrThink3D/Phase 5 - Setup Island Scene & Transitions")]
    public static void SetupIslandTransitions()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        // 1. Setup IslandScene.unity
        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        ConfigureIslandScene(islandScene);
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        // 2. Enable scene transition in GameScene.unity
        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        EnableGameSceneDockTransition(gameScene);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        Debug.Log("Phase 5 Setup complete! Configured IslandScene.unity and enabled transitionToIslandScene in GameScene.unity.");
        EditorUtility.DisplayDialog(
            "Phase 5 Setup Complete",
            "Configured IslandScene.unity (Terrain, Ocean, Dock, Walkable Ramp, Docked Return Boat, IslandPlayer, HUD) and enabled Dock -> Island transition in GameScene.unity.\n\nBoth scenes have been saved!",
            "OK"
        );
    }

    private static void ConfigureIslandScene(Scene scene)
    {
        // Remove default standalone Main Camera if it's not part of IslandPlayer
        GameObject strayCam = FindRootObject(scene, "Main Camera");
        if (strayCam != null && strayCam.GetComponentInParent<IslandPlayerController>() == null)
            Object.DestroyImmediate(strayCam);

        // Lighting & Skybox
        Material skyMat = AssetDatabase.LoadAssetAtPath<Material>(SkyMaterialPath);
        if (skyMat != null)
            RenderSettings.skybox = skyMat;

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.67f, 0.83f, 0.93f, 1f);
        RenderSettings.fogDensity = 0.0020f;

        Light dirLight = Object.FindAnyObjectByType<Light>();
        if (dirLight != null && dirLight.type == LightType.Directional)
        {
            dirLight.intensity = 1.25f;
            dirLight.color = new Color(1f, 0.95f, 0.85f);
            dirLight.transform.rotation = Quaternion.Euler(46f, -35f, 0f);
        }

        // Terrain
        TerrainData terrainData = AssetDatabase.LoadAssetAtPath<TerrainData>(TerrainDataPath);
        GameObject terrainGo = FindOrCreateRoot(scene, "DiscoveryIsland_Terrain");
        terrainGo.transform.position = new Vector3(-312f, -9f, 150f);

        Terrain terrain = terrainGo.GetComponent<Terrain>();
        if (terrain == null)
            terrain = terrainGo.AddComponent<Terrain>();
        terrain.terrainData = terrainData;

        TerrainCollider terrainCol = terrainGo.GetComponent<TerrainCollider>();
        if (terrainCol == null)
            terrainCol = terrainGo.AddComponent<TerrainCollider>();
        terrainCol.terrainData = terrainData;

        // Ocean around island
        GameObject oceanGo = FindRootObject(scene, "OceanSurface");
        if (oceanGo == null)
        {
            oceanGo = GameObject.CreatePrimitive(PrimitiveType.Plane);
            oceanGo.name = "OceanSurface";
        }
        oceanGo.transform.position = Vector3.zero;
        oceanGo.transform.rotation = Quaternion.identity;
        oceanGo.transform.localScale = new Vector3(300f, 1f, 300f);

        MeshRenderer oceanMr = oceanGo.GetComponent<MeshRenderer>();
        if (oceanMr != null)
        {
            oceanMr.shadowCastingMode = ShadowCastingMode.Off;
            string waterPath = AssetDatabase.GUIDToAssetPath(GameSceneWaterMatGuid);
            Material waterMat = !string.IsNullOrEmpty(waterPath)
                ? AssetDatabase.LoadAssetAtPath<Material>(waterPath)
                : AssetDatabase.LoadAssetAtPath<Material>(FallbackWaterMatPath);
            if (waterMat != null)
                oceanMr.sharedMaterial = waterMat;
        }
        if (oceanGo.GetComponent<OceanWaterAnimator>() == null)
            oceanGo.AddComponent<OceanWaterAnimator>();

        // Shoreline Dock (matching GameScene world coordinates)
        GameObject dockGo = PlaceOrGetPrefabRoot(scene, "DiscoveryDock", DockPrefabPath);
        dockGo.transform.position = new Vector3(14.0f, 0.6f, 144.19f);
        dockGo.transform.rotation = Quaternion.Euler(0f, 94f, 0f);

        // Smooth walkable wooden ramp from dock end onto island terrain
        float beachTargetY = terrainData != null
            ? terrain.SampleHeight(new Vector3(14f, 0f, 156.5f)) + terrainGo.transform.position.y
            : 0.4f;
        float dockEndY = 0.85f;
        Vector3 rampStart = new Vector3(14.0f, dockEndY, 149.8f);
        Vector3 rampEnd = new Vector3(14.0f, Mathf.Max(0.15f, beachTargetY), 156.8f);
        Vector3 rampMid = (rampStart + rampEnd) * 0.5f - new Vector3(0f, 0.1f, 0f);
        float rampLen = Vector3.Distance(rampStart, rampEnd);

        GameObject rampGo = FindRootObject(scene, "DockBeachRamp");
        if (rampGo == null)
        {
            rampGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            rampGo.name = "DockBeachRamp";
        }
        rampGo.transform.position = rampMid;
        rampGo.transform.LookAt(rampEnd);
        rampGo.transform.localScale = new Vector3(4.2f, 0.28f, rampLen + 0.6f);

        Material woodMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/QuizBoardFrameWood.mat");
        if (woodMat != null && rampGo.GetComponent<MeshRenderer>() != null)
            rampGo.GetComponent<MeshRenderer>().sharedMaterial = woodMat;

        // Docked rowboat for returning to ocean
        GameObject returnBoatGo = PlaceOrGetPrefabRoot(scene, "DockedBoat_Return", BoatPrefabPath);
        returnBoatGo.transform.position = new Vector3(13.8f, 0.1f, 136.5f);
        returnBoatGo.transform.rotation = Quaternion.Euler(0f, 180f, 0f);

        // Remove ocean-only boat scripts if any exist
        BoatMovement bm = returnBoatGo.GetComponent<BoatMovement>();
        if (bm != null) Object.DestroyImmediate(bm);
        BoatSink bs = returnBoatGo.GetComponent<BoatSink>();
        if (bs != null) Object.DestroyImmediate(bs);

        if (returnBoatGo.GetComponent<GentleFloat>() == null)
        {
            GentleFloat gf = returnBoatGo.AddComponent<GentleFloat>();
            gf.bobAmplitude = 0.06f;
            gf.tiltAngle = 2.5f;
        }

        IslandBoatReturn boatReturn = returnBoatGo.GetComponent<IslandBoatReturn>();
        if (boatReturn == null)
            boatReturn = returnBoatGo.AddComponent<IslandBoatReturn>();
        boatReturn.islandIndex = 0;
        boatReturn.oceanSceneName = "GameScene";
        boatReturn.interactRange = 8.5f;

        // Spawn Point on dock deck
        GameObject spawnGo = FindOrCreateRoot(scene, "IslandSpawnPoint");
        spawnGo.transform.position = new Vector3(14.0f, 1.45f, 144.8f);
        spawnGo.transform.rotation = Quaternion.identity;

        // IslandPlayer with CharacterController and single Main Camera
        GameObject playerGo = FindOrCreateRoot(scene, "IslandPlayer");
        playerGo.transform.position = spawnGo.transform.position;
        playerGo.transform.rotation = spawnGo.transform.rotation;

        CharacterController cc = playerGo.GetComponent<CharacterController>();
        if (cc == null)
            cc = playerGo.AddComponent<CharacterController>();
        cc.height = 1.8f;
        cc.radius = 0.4f;
        cc.center = new Vector3(0f, 0.9f, 0f);
        cc.stepOffset = 0.45f;
        cc.slopeLimit = 50f;

        IslandPlayerController playerCtrl = playerGo.GetComponent<IslandPlayerController>();
        if (playerCtrl == null)
            playerCtrl = playerGo.AddComponent<IslandPlayerController>();

        Transform camChild = playerGo.transform.Find("PlayerCamera");
        GameObject camGo;
        if (camChild != null)
        {
            camGo = camChild.gameObject;
        }
        else
        {
            camGo = new GameObject("PlayerCamera");
            camGo.transform.SetParent(playerGo.transform, false);
        }
        camGo.tag = "MainCamera";
        camGo.transform.localPosition = new Vector3(0f, 1.55f, 0f);
        camGo.transform.localRotation = Quaternion.identity;

        Camera cam = camGo.GetComponent<Camera>();
        if (cam == null) cam = camGo.AddComponent<Camera>();
        if (camGo.GetComponent<AudioListener>() == null) camGo.AddComponent<AudioListener>();
        if (camGo.GetComponent<UniversalAdditionalCameraData>() == null) camGo.AddComponent<UniversalAdditionalCameraData>();

        playerCtrl.cameraTransform = camGo.transform;

        // Single EventSystem
        EventSystem[] existingSystems = Object.FindObjectsByType<EventSystem>();
        if (existingSystems.Length == 0)
        {
            GameObject esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>();
        }

        // Island HUD Canvas
        GameObject hudGo = FindOrCreateRoot(scene, "IslandHUDCanvas");
        Canvas hudCanvas = hudGo.GetComponent<Canvas>();
        if (hudCanvas == null) hudCanvas = hudGo.AddComponent<Canvas>();
        hudCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = hudGo.GetComponent<CanvasScaler>();
        if (scaler == null) scaler = hudGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        if (hudGo.GetComponent<GraphicRaycaster>() == null) hudGo.AddComponent<GraphicRaycaster>();

        TMP_Text statusText = CreateOrUpdateHudText(
            hudGo.transform,
            "IslandStatusText",
            new Vector2(0f, 1f),
            new Vector2(20f, -18f),
            new Vector2(760f, 44f),
            new Vector2(0f, 1f),
            "Discovery Island  |  Boat HP: 100/100",
            22f,
            TextAlignmentOptions.Left,
            new Color(1f, 0.94f, 0.78f, 1f)
        );

        CreateOrUpdateHudText(
            hudGo.transform,
            "ControlsHintText",
            new Vector2(1f, 1f),
            new Vector2(-20f, -18f),
            new Vector2(740f, 40f),
            new Vector2(1f, 1f),
            "WASD Move  |  Shift Sprint  |  Space Jump  |  E Interact  |  F Board Boat",
            18f,
            TextAlignmentOptions.Right,
            new Color(0.92f, 0.92f, 0.92f, 0.9f)
        );

        // Center Crosshair
        GameObject crosshairGo = GetOrCreateChild(hudGo.transform, "CrosshairDot");
        RectTransform crossRect = EnsureRectTransform(crosshairGo);
        crossRect.anchorMin = new Vector2(0.5f, 0.5f);
        crossRect.anchorMax = new Vector2(0.5f, 0.5f);
        crossRect.sizeDelta = new Vector2(8f, 8f);
        crossRect.anchoredPosition = Vector2.zero;
        Image crossImg = crosshairGo.GetComponent<Image>();
        if (crossImg == null) crossImg = crosshairGo.AddComponent<Image>();
        crossImg.color = new Color(1f, 1f, 1f, 0.85f);
        crossImg.raycastTarget = false;

        // Interaction Prompt Panel
        GameObject promptPanel = GetOrCreateChild(hudGo.transform, "InteractionPromptPanel");
        RectTransform promptRect = EnsureRectTransform(promptPanel);
        promptRect.anchorMin = new Vector2(0.5f, 0.14f);
        promptRect.anchorMax = new Vector2(0.5f, 0.14f);
        promptRect.sizeDelta = new Vector2(620f, 58f);
        promptRect.anchoredPosition = Vector2.zero;
        Image promptBg = promptPanel.GetComponent<Image>();
        if (promptBg == null) promptBg = promptPanel.AddComponent<Image>();
        promptBg.color = new Color(0.14f, 0.09f, 0.05f, 0.88f);
        promptBg.raycastTarget = false;

        TMP_Text promptText = CreateOrUpdateHudText(
            promptPanel.transform,
            "InteractionPromptText",
            new Vector2(0.5f, 0.5f),
            Vector2.zero,
            new Vector2(590f, 48f),
            new Vector2(0.5f, 0.5f),
            "Press E to Interact",
            24f,
            TextAlignmentOptions.Center,
            new Color(1f, 0.9f, 0.6f, 1f)
        );
        promptPanel.SetActive(false);

        playerCtrl.interactionPromptPanel = promptPanel;
        playerCtrl.interactionPromptText = promptText;
        playerCtrl.crosshairObject = crosshairGo;

        // Scene Controller
        GameObject ctrlGo = FindOrCreateRoot(scene, "IslandSceneController");
        IslandSceneController sceneCtrl = ctrlGo.GetComponent<IslandSceneController>();
        if (sceneCtrl == null)
            sceneCtrl = ctrlGo.AddComponent<IslandSceneController>();
        sceneCtrl.islandIndex = 0;
        sceneCtrl.islandDisplayName = "Discovery Island";
        sceneCtrl.playerSpawnPoint = spawnGo.transform;
        sceneCtrl.player = playerCtrl;
        sceneCtrl.islandStatusText = statusText;
    }

    private static void EnableGameSceneDockTransition(Scene gameScene)
    {
        BoatDocking docking = Object.FindAnyObjectByType<BoatDocking>();
        if (docking == null)
        {
            Debug.LogWarning("Phase 5 Setup: BoatDocking not found in GameScene.");
            return;
        }

        Undo.RecordObject(docking, "Enable Island Scene Transition");
        docking.transitionToIslandScene = true;
        docking.targetIslandSceneName = "IslandScene";
        docking.autoMatchCurrentIsland = true;
        EditorUtility.SetDirty(docking);
    }

    private static TMP_Text CreateOrUpdateHudText(
        Transform parent,
        string name,
        Vector2 anchor,
        Vector2 anchoredPos,
        Vector2 sizeDelta,
        Vector2 pivot,
        string text,
        float fontSize,
        TextAlignmentOptions alignment,
        Color color)
    {
        GameObject go = GetOrCreateChild(parent, name);
        RectTransform rect = EnsureRectTransform(go);
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPos;
        rect.sizeDelta = sizeDelta;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        if (tmp == null)
            tmp = go.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject PlaceOrGetPrefabRoot(Scene scene, string objectName, string prefabPath)
    {
        GameObject existing = FindRootObject(scene, objectName);
        if (existing != null)
            return existing;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
            return FindOrCreateRoot(scene, objectName);

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, scene);
        instance.name = objectName;
        return instance;
    }

    private static GameObject FindOrCreateRoot(Scene scene, string name)
    {
        GameObject existing = FindRootObject(scene, name);
        if (existing != null)
            return existing;

        GameObject go = new GameObject(name);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static GameObject FindRootObject(Scene scene, string name)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
                return roots[i];
        }
        return null;
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static RectTransform EnsureRectTransform(GameObject go)
    {
        RectTransform rt = go.GetComponent<RectTransform>();
        if (rt == null)
            rt = go.AddComponent<RectTransform>();
        return rt;
    }
}
