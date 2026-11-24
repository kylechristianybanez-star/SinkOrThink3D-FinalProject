using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Idempotent Editor tool to configure Ocean animation & coastal fog, realistic Dock props,
/// DockParkingPoint, DockPrompt UI, and eliminate the 27-point-light Shadow Atlas overflow.
/// Safe to rerun without creating duplicates.
/// </summary>
public static class Phase4OceanAndDockSetup
{
    private const string MaterialsFolder = "Assets/Materials";
    private const string BarrelPrefabPath = "Assets/ToonTropical/Prefabs/SM_Island_Barrel.prefab";
    private const string CratePrefabPath = "Assets/ToonTropical/Prefabs/SM_Island_Crate.prefab";

    [MenuItem("Tools/SinkOrThink3D/Phase 4 - Setup Ocean, Dock & Fix Shadow Atlas")]
    public static void SetupOceanAndDock()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (!activeScene.name.Equals("GameScene"))
        {
            bool proceed = EditorUtility.DisplayDialog(
                "GameScene Not Active",
                "Active scene is '" + activeScene.name + "'. Please open Assets/Scenes/GameScene.unity first.\n\nContinue anyway?",
                "Continue",
                "Cancel"
            );
            if (!proceed)
                return;
        }

        int optimizedLights = ApplyToScene(activeScene);

        Debug.Log("Phase 4 Setup complete! Disabled realtime shadows on " + optimizedLights + " decorative point/spot lights. Press Ctrl+S to save GameScene.");
        EditorUtility.DisplayDialog(
            "Phase 4 Setup Complete",
            "Configured Ocean animation & fog, Dock mooring posts/lanterns/guide buoy, Dock Prompt UI, and fixed the Shadow Atlas warning (" + optimizedLights + " decorative point/spot lights optimized).\n\nPress Ctrl+S in Unity to save GameScene!",
            "OK"
        );
    }

    public static int ApplyToScene(Scene activeScene)
    {
        EnsureFolder(MaterialsFolder);

        int optimizedLights = FixAdditionalLightShadows(activeScene);
        ConfigureOceanAtmosphere(activeScene);
        ConfigureDock(activeScene);

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(activeScene);
        return optimizedLights;
    }

    private static int FixAdditionalLightShadows(Scene scene)
    {
        int count = 0;
        Light[] lights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        for (int i = 0; i < lights.Length; i++)
        {
            Light l = lights[i];
            if (l.gameObject.scene != scene)
                continue;

            if (l.type != LightType.Directional && l.shadows != LightShadows.None)
            {
                Undo.RecordObject(l, "Disable decorative light shadows");
                l.shadows = LightShadows.None;
                EditorUtility.SetDirty(l);
                count++;
            }
        }
        return count;
    }

    private static void ConfigureOceanAtmosphere(Scene scene)
    {
        GameObject ocean = FindSceneObjectByName(scene, "OceanSurface");
        if (ocean != null)
        {
            if (ocean.GetComponent<OceanWaterAnimator>() == null)
                ocean.AddComponent<OceanWaterAnimator>();

            MeshRenderer mr = ocean.GetComponent<MeshRenderer>();
            if (mr != null)
            {
                Undo.RecordObject(mr, "Disable Ocean Shadow Casting");
                mr.shadowCastingMode = ShadowCastingMode.Off;
                EditorUtility.SetDirty(mr);
            }
        }
        else
        {
            Debug.LogWarning("Phase 4 Setup: Could not find OceanSurface in GameScene.");
        }

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogColor = new Color(0.67f, 0.83f, 0.93f, 1f);
        RenderSettings.fogDensity = 0.0022f;
    }

    private static void ConfigureDock(Scene scene)
    {
        GameObject dockPointObj = FindSceneObjectByName(scene, "BoatDockPoint");
        if (dockPointObj == null)
        {
            Debug.LogWarning("Phase 4 Setup: Could not find BoatDockPoint in GameScene.");
            return;
        }

        BoatDocking docking = dockPointObj.GetComponent<BoatDocking>();
        if (docking == null)
            docking = dockPointObj.AddComponent<BoatDocking>();

        Undo.RecordObject(docking, "Configure BoatDocking");
        docking.dockingDistance = 9.5f;
        docking.islandDisplayName = "Discovery Island";
        docking.targetIslandSceneName = "IslandScene";
        docking.transitionToIslandScene = false; // Enabled in Phase 5 once IslandScene is built

        if (docking.boat == null)
            docking.boat = FindSceneObjectByName(scene, "OldWoodenRowboat_Complete");

        if (docking.quizManager == null)
            docking.quizManager = Object.FindFirstObjectByType<QuizManager>();

        // Create safe sea-level parking spot next to the dock
        GameObject parkingGo = GetOrCreateChild(dockPointObj.transform, "DockParkingPoint");
        parkingGo.transform.localPosition = new Vector3(0f, -0.6f, -3.5f);
        parkingGo.transform.localRotation = Quaternion.identity;
        docking.dockParkingPoint = parkingGo.transform;

        // Materials (reuse Phase 3 materials if present, otherwise create)
        Material woodMat = GetOrCreateLitMaterial("QuizBoardFrameWood.mat", new Color(0.42f, 0.27f, 0.16f, 1f), 0f, 0.18f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material ropeMat = GetOrCreateLitMaterial("QuizBoardRope.mat", new Color(0.66f, 0.54f, 0.36f, 1f), 0f, 0.1f);
        Material glowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material buoyRedMat = GetOrCreateLitMaterial("DockBuoyRed.mat", new Color(0.82f, 0.18f, 0.14f, 1f), 0.1f, 0.45f);

        // Decorative dock details (no colliders so boat never snags)
        GameObject details = GetOrCreateChild(dockPointObj.transform, "DockVisualDetails");
        details.transform.localPosition = Vector3.zero;
        details.transform.localRotation = Quaternion.identity;

        Vector3[] postOffsets = new Vector3[]
        {
            new Vector3(-2.6f, 0.15f, -1.2f),
            new Vector3(2.6f, 0.15f, -1.2f),
            new Vector3(-2.6f, 0.15f, 2.2f),
            new Vector3(2.6f, 0.15f, 2.2f)
        };

        for (int i = 0; i < postOffsets.Length; i++)
        {
            Vector3 p = postOffsets[i];
            CreateOrUpdatePrimitive(details.transform, "MooringPost_" + (i + 1), PrimitiveType.Cylinder, p, Vector3.zero, new Vector3(0.28f, 1.15f, 0.28f), woodMat);
            CreateOrUpdatePrimitive(details.transform, "MooringRope_" + (i + 1), PrimitiveType.Cylinder, p + new Vector3(0f, 0.35f, 0f), Vector3.zero, new Vector3(0.34f, 0.12f, 0.34f), ropeMat);
            CreateOrUpdatePrimitive(details.transform, "MooringCap_" + (i + 1), PrimitiveType.Cylinder, p + new Vector3(0f, 1.18f, 0f), Vector3.zero, new Vector3(0.30f, 0.04f, 0.30f), brassMat);
        }

        // Two glowing dock lanterns + 1 non-shadow warm point light
        CreateOrUpdatePrimitive(details.transform, "DockLantern_Left", PrimitiveType.Cube, new Vector3(-2.6f, 1.48f, -1.2f), Vector3.zero, new Vector3(0.28f, 0.38f, 0.28f), glowMat);
        CreateOrUpdatePrimitive(details.transform, "DockLantern_Right", PrimitiveType.Cube, new Vector3(2.6f, 1.48f, -1.2f), Vector3.zero, new Vector3(0.28f, 0.38f, 0.28f), glowMat);

        GameObject lightGo = GetOrCreateChild(details.transform, "DockWarmLight");
        lightGo.transform.localPosition = new Vector3(0f, 2.2f, -1.2f);
        Light dockLight = lightGo.GetComponent<Light>();
        if (dockLight == null)
            dockLight = lightGo.AddComponent<Light>();
        dockLight.type = LightType.Point;
        dockLight.color = new Color(1f, 0.78f, 0.45f);
        dockLight.intensity = 2.4f;
        dockLight.range = 15f;
        dockLight.shadows = LightShadows.None;

        // Floating Guide Buoy near the dock entrance
        GameObject buoyRoot = GetOrCreateChild(details.transform, "DockGuideBuoy");
        buoyRoot.transform.localPosition = new Vector3(-4.5f, -0.55f, -5.5f);
        if (buoyRoot.GetComponent<GentleFloat>() == null)
            buoyRoot.AddComponent<GentleFloat>();

        CreateOrUpdatePrimitive(buoyRoot.transform, "BuoyBase", PrimitiveType.Cylinder, Vector3.zero, Vector3.zero, new Vector3(0.85f, 0.32f, 0.85f), buoyRedMat);
        CreateOrUpdatePrimitive(buoyRoot.transform, "BuoyTower", PrimitiveType.Cylinder, new Vector3(0f, 0.55f, 0f), Vector3.zero, new Vector3(0.22f, 0.45f, 0.22f), brassMat);
        CreateOrUpdatePrimitive(buoyRoot.transform, "BuoyBeacon", PrimitiveType.Sphere, new Vector3(0f, 1.05f, 0f), Vector3.zero, new Vector3(0.32f, 0.32f, 0.32f), glowMat);

        // Reuse existing ToonTropical cargo props beside dock posts
        PlaceDecorativePrefab(details.transform, "DockCargo_Barrel", BarrelPrefabPath, new Vector3(-2.05f, 0.1f, 0.4f), Vector3.zero, Vector3.one * 0.9f);
        PlaceDecorativePrefab(details.transform, "DockCargo_Crate", CratePrefabPath, new Vector3(2.0f, 0.1f, 0.5f), new Vector3(0f, 22f, 0f), Vector3.one * 0.85f);

        // Setup Dock Prompt UI & Destination Guide inside HealthCanvas
        ConfigureDockUI(scene, docking);

        EditorUtility.SetDirty(docking);
    }

    private static void ConfigureDockUI(Scene scene, BoatDocking docking)
    {
        GameObject canvasGo = FindSceneObjectByName(scene, "HealthCanvas");
        if (canvasGo == null)
        {
            Canvas anyCanvas = Object.FindFirstObjectByType<Canvas>();
            if (anyCanvas != null)
                canvasGo = anyCanvas.gameObject;
        }

        if (canvasGo == null)
        {
            Debug.LogWarning("Phase 4 Setup: Could not find HealthCanvas for Dock UI.");
            return;
        }

        // Dock Prompt Panel (bottom center)
        GameObject promptPanel = GetOrCreateChild(canvasGo.transform, "DockPromptPanel");
        RectTransform promptRect = promptPanel.GetComponent<RectTransform>();
        if (promptRect == null)
            promptRect = promptPanel.AddComponent<RectTransform>();
        promptRect.anchorMin = new Vector2(0.5f, 0.13f);
        promptRect.anchorMax = new Vector2(0.5f, 0.13f);
        promptRect.sizeDelta = new Vector2(640f, 62f);
        promptRect.anchoredPosition = Vector2.zero;

        Image bg = promptPanel.GetComponent<Image>();
        if (bg == null)
            bg = promptPanel.AddComponent<Image>();
        bg.color = new Color(0.14f, 0.09f, 0.05f, 0.88f);
        bg.raycastTarget = false;

        GameObject promptTextGo = GetOrCreateChild(promptPanel.transform, "DockPromptText");
        RectTransform textRect = promptTextGo.GetComponent<RectTransform>();
        if (textRect == null)
            textRect = promptTextGo.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 6f);
        textRect.offsetMax = new Vector2(-18f, -6f);

        TextMeshProUGUI promptTmp = promptTextGo.GetComponent<TextMeshProUGUI>();
        if (promptTmp == null)
            promptTmp = promptTextGo.AddComponent<TextMeshProUGUI>();
        promptTmp.text = "Press F to Dock";
        promptTmp.fontSize = 24f;
        promptTmp.fontStyle = FontStyles.Bold;
        promptTmp.alignment = TextAlignmentOptions.Center;
        promptTmp.color = new Color(1f, 0.9f, 0.6f, 1f);
        promptTmp.raycastTarget = false;

        promptPanel.SetActive(false);
        docking.promptPanel = promptPanel;
        docking.promptText = promptTmp;

        // Ocean Destination Guide Text (top center)
        GameObject guideGo = GetOrCreateChild(canvasGo.transform, "OceanDestinationGuideText");
        RectTransform guideRect = guideGo.GetComponent<RectTransform>();
        if (guideRect == null)
            guideRect = guideGo.AddComponent<RectTransform>();
        guideRect.anchorMin = new Vector2(0.5f, 0.94f);
        guideRect.anchorMax = new Vector2(0.5f, 0.94f);
        guideRect.sizeDelta = new Vector2(520f, 38f);
        guideRect.anchoredPosition = Vector2.zero;

        TextMeshProUGUI guideTmp = guideGo.GetComponent<TextMeshProUGUI>();
        if (guideTmp == null)
            guideTmp = guideGo.AddComponent<TextMeshProUGUI>();
        guideTmp.text = "Discovery Island Dock";
        guideTmp.fontSize = 20f;
        guideTmp.fontStyle = FontStyles.Bold;
        guideTmp.alignment = TextAlignmentOptions.Center;
        guideTmp.color = new Color(1f, 0.96f, 0.82f, 1f);
        guideTmp.raycastTarget = false;

        docking.distanceGuideText = guideTmp;
    }

    private static void PlaceDecorativePrefab(Transform parent, string name, string assetPath, Vector3 localPos, Vector3 localEuler, Vector3 localScale)
    {
        if (parent.Find(name) != null)
            return;

        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null)
            return;

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, parent);
        instance.name = name;
        instance.transform.localPosition = localPos;
        instance.transform.localRotation = Quaternion.Euler(localEuler);
        instance.transform.localScale = localScale;

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < colliders.Length; i++)
            Object.DestroyImmediate(colliders[i]);

        Undo.RegisterCreatedObjectUndo(instance, "Create " + name);
    }

    private static GameObject GetOrCreateChild(Transform parent, string childName)
    {
        Transform existing = parent.Find(childName);
        if (existing != null)
            return existing.gameObject;

        GameObject go = new GameObject(childName);
        go.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(go, "Create " + childName);
        return go;
    }

    private static GameObject CreateOrUpdatePrimitive(Transform parent, string name, PrimitiveType type, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material mat)
    {
        Transform existing = parent.Find(name);
        GameObject go;

        if (existing != null)
        {
            go = existing.gameObject;
        }
        else
        {
            go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
        }

        Collider col = go.GetComponent<Collider>();
        if (col != null)
            Object.DestroyImmediate(col);

        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = localScale;

        MeshRenderer renderer = go.GetComponent<MeshRenderer>();
        if (renderer != null && mat != null)
            renderer.sharedMaterial = mat;

        return go;
    }

    private static Material GetOrCreateLitMaterial(string fileName, Color color, float metallic, float smoothness)
    {
        string path = MaterialsFolder + "/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");

        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        AssetDatabase.CreateAsset(mat, path);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateEmissiveMaterial(string fileName, Color baseColor, Color emissionColor)
    {
        string path = MaterialsFolder + "/" + fileName;
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
            return existing;

        Material mat = GetOrCreateLitMaterial(fileName, baseColor, 0f, 0.35f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", emissionColor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject FindSceneObjectByName(Scene scene, string targetName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            GameObject go = allObjects[i];
            if (go.scene == scene && go.name == targetName)
                return go;
        }
        return null;
    }

    private static void EnsureFolder(string assetPath)
    {
        if (AssetDatabase.IsValidFolder(assetPath))
            return;

        string[] parts = assetPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
