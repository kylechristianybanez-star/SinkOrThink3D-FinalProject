using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Idempotent Editor tool to configure realistic wooden/brass details, materials,
/// ParticleSystems, and animation components on OldWoodenRowboat_Complete and QuizBoard3D.
/// Safe to rerun without creating duplicate objects or overwriting GUIDs.
/// </summary>
public static class Phase3BoatAndBoardSetup
{
    private const string MaterialsFolder = "Assets/Materials";
    private const string PlankTexturePath = "Assets/Textures/Planks023A_1K-JPG_Color.jpg";

    [MenuItem("Tools/SinkOrThink3D/Phase 3 - Setup Realistic Boat & Quiz Board")]
    public static void SetupBoatAndBoard()
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

        int configuredItems = ApplyToScene(activeScene);

        Debug.Log("Phase 3 Setup complete on " + configuredItems + " target root(s). Press Ctrl+S to save GameScene.");
        EditorUtility.DisplayDialog(
            "Phase 3 Setup Complete",
            "Configured realistic wooden/brass Quiz Board details, Boat animations, and lightweight URP ParticleSystems.\n\nPress Ctrl+S in Unity to save GameScene!",
            "OK"
        );
    }

    public static int ApplyToScene(Scene activeScene)
    {
        EnsureFolder(MaterialsFolder);

        Texture2D plankTex = AssetDatabase.LoadAssetAtPath<Texture2D>(PlankTexturePath);
        Material frameWoodMat = GetOrCreateLitMaterial("QuizBoardFrameWood.mat", plankTex, new Color(0.42f, 0.27f, 0.16f, 1f), 0f, 0.18f);
        Material brassMat = GetOrCreateLitMaterial("QuizBoardBrass.mat", null, new Color(0.76f, 0.58f, 0.24f, 1f), 0.85f, 0.55f);
        Material ropeMat = GetOrCreateLitMaterial("QuizBoardRope.mat", null, new Color(0.66f, 0.54f, 0.36f, 1f), 0f, 0.1f);
        Material plaqueMat = GetOrCreateLitMaterial("QuizBoardPlaque.mat", null, new Color(0.18f, 0.12f, 0.08f, 1f), 0.05f, 0.22f);
        Material lanternGlowMat = GetOrCreateEmissiveMaterial("BoatLanternGlow.mat", new Color(1f, 0.78f, 0.38f, 1f), new Color(1f, 0.68f, 0.22f, 1f) * 1.6f);
        Material splashParticleMat = GetOrCreateParticleMaterial("BoatSplashParticle.mat", new Color(0.86f, 0.96f, 1f, 0.7f));
        Material sparkleParticleMat = GetOrCreateParticleMaterial("BoatSparkleParticle.mat", new Color(1f, 0.86f, 0.32f, 0.9f));

        QuizManager quizManager = Object.FindFirstObjectByType<QuizManager>();
        GameObject quizBoard = quizManager != null && quizManager.quizBoard != null
            ? quizManager.quizBoard
            : FindSceneObjectByName(activeScene, "QuizBoard3D");

        GameObject boat = FindSceneObjectByName(activeScene, "OldWoodenRowboat_Complete");

        int configuredItems = 0;

        if (quizBoard != null)
        {
            ConfigureQuizBoard(quizBoard, quizManager, frameWoodMat, brassMat, ropeMat, plaqueMat, sparkleParticleMat);
            configuredItems++;
        }
        else
        {
            Debug.LogWarning("Phase 3 Setup: Could not find QuizBoard3D in scene.");
        }

        if (boat != null)
        {
            ConfigureBoat(boat, frameWoodMat, brassMat, ropeMat, lanternGlowMat, splashParticleMat, sparkleParticleMat);
            configuredItems++;
        }
        else
        {
            Debug.LogWarning("Phase 3 Setup: Could not find OldWoodenRowboat_Complete in scene.");
        }

        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(activeScene);
        return configuredItems;
    }

    private static void ConfigureQuizBoard(
        GameObject quizBoard,
        QuizManager quizManager,
        Material frameWoodMat,
        Material brassMat,
        Material ropeMat,
        Material plaqueMat,
        Material sparkleMat)
    {
        QuizBoardAnimator boardAnimator = quizBoard.GetComponent<QuizBoardAnimator>();
        if (boardAnimator == null)
            boardAnimator = quizBoard.AddComponent<QuizBoardAnimator>();

        if (quizManager != null)
        {
            Undo.RecordObject(quizManager, "Wire QuizBoardAnimator");
            quizManager.quizBoardAnimator = boardAnimator;
            EditorUtility.SetDirty(quizManager);
        }

        GameObject details = GetOrCreateChild(quizBoard.transform, "BoardVisualDetails");
        details.transform.localPosition = Vector3.zero;
        details.transform.localRotation = Quaternion.identity;
        details.transform.localScale = Vector3.one;

        // Board scale is (2.4, 1.8, 0.05); scale details inversely so borders look uniform.
        CreateOrUpdateBox(details.transform, "Frame_Top", new Vector3(0f, 0.515f, -0.25f), new Vector3(1.07f, 0.045f, 1.65f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "Frame_Bottom", new Vector3(0f, -0.515f, -0.25f), new Vector3(1.07f, 0.045f, 1.65f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "Frame_Left", new Vector3(-0.515f, 0f, -0.25f), new Vector3(0.034f, 1.03f, 1.65f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "Frame_Right", new Vector3(0.515f, 0f, -0.25f), new Vector3(0.034f, 1.03f, 1.65f), frameWoodMat);

        // Brass corner brackets
        CreateOrUpdateBox(details.transform, "Bracket_TL", new Vector3(-0.505f, 0.505f, -0.26f), new Vector3(0.06f, 0.08f, 1.85f), brassMat);
        CreateOrUpdateBox(details.transform, "Bracket_TR", new Vector3(0.505f, 0.505f, -0.26f), new Vector3(0.06f, 0.08f, 1.85f), brassMat);
        CreateOrUpdateBox(details.transform, "Bracket_BL", new Vector3(-0.505f, -0.505f, -0.26f), new Vector3(0.06f, 0.08f, 1.85f), brassMat);
        CreateOrUpdateBox(details.transform, "Bracket_BR", new Vector3(0.505f, -0.505f, -0.26f), new Vector3(0.06f, 0.08f, 1.85f), brassMat);

        // Support pegs & rope lashings at bottom corners
        CreateOrUpdateCylinder(details.transform, "SupportPost_Left", new Vector3(-0.34f, -0.60f, -0.2f), Vector3.zero, new Vector3(0.03f, 0.11f, 1.2f), frameWoodMat);
        CreateOrUpdateCylinder(details.transform, "SupportPost_Right", new Vector3(0.34f, -0.60f, -0.2f), Vector3.zero, new Vector3(0.03f, 0.11f, 1.2f), frameWoodMat);
        CreateOrUpdateCylinder(details.transform, "RopeLashing_Left", new Vector3(-0.34f, -0.53f, -0.2f), new Vector3(0f, 0f, 90f), new Vector3(0.035f, 0.035f, 1.4f), ropeMat);
        CreateOrUpdateCylinder(details.transform, "RopeLashing_Right", new Vector3(0.34f, -0.53f, -0.2f), new Vector3(0f, 0f, 90f), new Vector3(0.035f, 0.035f, 1.4f), ropeMat);

        // Dark header plaque behind OceanChallengeTitle (y = 0.46) and trim frames behind the 4 answer buttons
        CreateOrUpdateBox(details.transform, "HeaderPlaque", new Vector3(0f, 0.46f, -0.52f), new Vector3(0.76f, 0.095f, 0.35f), plaqueMat);
        CreateOrUpdateBox(details.transform, "ButtonTrim_A", new Vector3(-0.25f, 0.15f, -0.52f), new Vector3(0.475f, 0.265f, 0.3f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "ButtonTrim_B", new Vector3(0.25f, 0.15f, -0.52f), new Vector3(0.475f, 0.265f, 0.3f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "ButtonTrim_C", new Vector3(-0.25f, -0.14f, -0.52f), new Vector3(0.475f, 0.265f, 0.3f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "ButtonTrim_D", new Vector3(0.25f, -0.14f, -0.52f), new Vector3(0.475f, 0.265f, 0.3f), frameWoodMat);

        // Ensure AnswerButton3D colliders stay enabled and easy to click
        string[] buttonNames = new string[] { "AnswerButton3D_A", "AnswerButton3D_B", "AnswerButton3D_C", "AnswerButton3D_D" };
        for (int i = 0; i < buttonNames.Length; i++)
        {
            Transform btn = quizBoard.transform.Find(buttonNames[i]);
            if (btn != null)
            {
                BoxCollider col = btn.GetComponent<BoxCollider>();
                if (col != null)
                {
                    col.enabled = true;
                    col.isTrigger = false;
                    col.size = new Vector3(1.02f, 1.05f, 1.5f);
                }
            }
        }

        // Correct-answer celebration sparkles
        ParticleSystem correctFx = ConfigureBurstParticleSystem(
            details.transform,
            "QuizCorrectSparkles",
            new Vector3(0f, 0.05f, -0.9f),
            sparkleMat,
            new Color(1f, 0.88f, 0.3f, 1f),
            24,
            0.06f,
            1.6f
        );
        boardAnimator.correctBurstParticles = correctFx;
        EditorUtility.SetDirty(boardAnimator);
    }

    private static void ConfigureBoat(
        GameObject boat,
        Material frameWoodMat,
        Material brassMat,
        Material ropeMat,
        Material lanternGlowMat,
        Material splashMat,
        Material sparkleMat)
    {
        BoatAnimator boatAnimator = boat.GetComponent<BoatAnimator>();
        if (boatAnimator == null)
            boatAnimator = boat.AddComponent<BoatAnimator>();

        if (boatAnimator.leftPaddle == null)
            boatAnimator.leftPaddle = boat.transform.Find("Paddle");
        if (boatAnimator.rightPaddle == null)
            boatAnimator.rightPaddle = boat.transform.Find("Paddle (1)");

        GameObject details = GetOrCreateChild(boat.transform, "BoatVisualDetails");
        details.transform.localPosition = Vector3.zero;
        details.transform.localRotation = Quaternion.identity;
        details.transform.localScale = Vector3.one;

        // Decorative bow mooring details (no colliders)
        CreateOrUpdateCylinder(details.transform, "BowRopeCoil", new Vector3(0f, 0.42f, 1.65f), Vector3.zero, new Vector3(0.28f, 0.05f, 0.28f), ropeMat);
        CreateOrUpdateCylinder(details.transform, "BowBrassPost", new Vector3(0f, 0.50f, 1.65f), Vector3.zero, new Vector3(0.07f, 0.10f, 0.07f), brassMat);

        // Stern deck lantern prop (emissive glow without heavy shadow-casting point light)
        CreateOrUpdateBox(details.transform, "SternLanternBase", new Vector3(0.45f, 0.56f, -1.55f), new Vector3(0.14f, 0.05f, 0.14f), frameWoodMat);
        CreateOrUpdateBox(details.transform, "SternLanternGlass", new Vector3(0.45f, 0.67f, -1.55f), new Vector3(0.11f, 0.16f, 0.11f), lanternGlowMat);
        CreateOrUpdateBox(details.transform, "SternLanternCap", new Vector3(0.45f, 0.77f, -1.55f), new Vector3(0.14f, 0.04f, 0.14f), brassMat);

        // Lightweight URP ParticleSystems under BoatFX
        GameObject fxRoot = GetOrCreateChild(boat.transform, "BoatFX");
        fxRoot.transform.localPosition = Vector3.zero;
        fxRoot.transform.localRotation = Quaternion.identity;
        fxRoot.transform.localScale = Vector3.one;

        ParticleSystem wakeFx = ConfigureLoopingWaterParticles(
            fxRoot.transform,
            "WakeParticles",
            new Vector3(0f, 0.05f, -2.0f),
            new Vector3(0f, 180f, 0f),
            splashMat,
            new Color(0.86f, 0.96f, 1f, 0.65f),
            45,
            0.22f,
            1.2f
        );

        ParticleSystem bowFx = ConfigureLoopingWaterParticles(
            fxRoot.transform,
            "BowSplashParticles",
            new Vector3(0f, 0.08f, 1.9f),
            new Vector3(-35f, 0f, 0f),
            splashMat,
            new Color(0.92f, 0.98f, 1f, 0.75f),
            35,
            0.14f,
            1.8f
        );

        ParticleSystem damageFx = ConfigureBurstParticleSystem(
            fxRoot.transform,
            "DamageSplashParticles",
            new Vector3(0f, 0.25f, 0f),
            splashMat,
            new Color(0.75f, 0.92f, 1f, 0.85f),
            35,
            0.22f,
            3.2f
        );

        ParticleSystem repairFx = ConfigureBurstParticleSystem(
            fxRoot.transform,
            "RepairParticles",
            new Vector3(0f, 0.45f, 0f),
            sparkleMat,
            new Color(0.45f, 1f, 0.55f, 0.95f),
            28,
            0.14f,
            2.2f
        );

        boatAnimator.wakeParticles = wakeFx;
        boatAnimator.bowSplashParticles = bowFx;
        boatAnimator.damageSplashParticles = damageFx;
        boatAnimator.repairParticles = repairFx;

        EditorUtility.SetDirty(boatAnimator);
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

    private static GameObject CreateOrUpdateBox(Transform parent, string name, Vector3 localPos, Vector3 localScale, Material mat)
    {
        return CreateOrUpdatePrimitive(parent, name, PrimitiveType.Cube, localPos, Vector3.zero, localScale, mat);
    }

    private static GameObject CreateOrUpdateCylinder(Transform parent, string name, Vector3 localPos, Vector3 localEuler, Vector3 localScale, Material mat)
    {
        return CreateOrUpdatePrimitive(parent, name, PrimitiveType.Cylinder, localPos, localEuler, localScale, mat);
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

        // Remove collider so decorative details never interfere with physics or raycast clicks.
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

    private static ParticleSystem ConfigureLoopingWaterParticles(
        Transform parent,
        string name,
        Vector3 localPos,
        Vector3 localEuler,
        Material mat,
        Color startColor,
        int maxParticles,
        float startSize,
        float startSpeed)
    {
        GameObject go = GetOrCreateChild(parent, name);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(localEuler);
        go.transform.localScale = Vector3.one;

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = true;
        main.duration = 2f;
        main.startLifetime = 0.9f;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = maxParticles;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 28f;
        shape.radius = 0.28f;

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        if (pr != null && mat != null)
            pr.sharedMaterial = mat;

        return ps;
    }

    private static ParticleSystem ConfigureBurstParticleSystem(
        Transform parent,
        string name,
        Vector3 localPos,
        Material mat,
        Color startColor,
        int burstCount,
        float startSize,
        float startSpeed)
    {
        GameObject go = GetOrCreateChild(parent, name);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.Euler(-90f, 0f, 0f);
        go.transform.localScale = Vector3.one;

        ParticleSystem ps = go.GetComponent<ParticleSystem>();
        if (ps == null)
            ps = go.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = ps.main;
        main.playOnAwake = false;
        main.loop = false;
        main.duration = 0.8f;
        main.startLifetime = 0.75f;
        main.startSpeed = startSpeed;
        main.startSize = startSize;
        main.startColor = startColor;
        main.gravityModifier = 0.55f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = burstCount + 10;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.enabled = true;
        emission.rateOverTime = 0f;
        emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, (short)burstCount) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.45f;

        ParticleSystemRenderer pr = go.GetComponent<ParticleSystemRenderer>();
        if (pr != null && mat != null)
            pr.sharedMaterial = mat;

        return ps;
    }

    private static Material GetOrCreateLitMaterial(string fileName, Texture2D mainTex, Color color, float metallic, float smoothness)
    {
        string path = MaterialsFolder + "/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Standard");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mainTex != null && mat.HasProperty("_BaseMap"))
            mat.SetTexture("_BaseMap", mainTex);
        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);
        if (mat.HasProperty("_Metallic"))
            mat.SetFloat("_Metallic", metallic);
        if (mat.HasProperty("_Smoothness"))
            mat.SetFloat("_Smoothness", smoothness);

        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateEmissiveMaterial(string fileName, Color baseColor, Color emissionColor)
    {
        Material mat = GetOrCreateLitMaterial(fileName, null, baseColor, 0f, 0.35f);
        mat.EnableKeyword("_EMISSION");
        if (mat.HasProperty("_EmissionColor"))
            mat.SetColor("_EmissionColor", emissionColor);
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static Material GetOrCreateParticleMaterial(string fileName, Color color)
    {
        string path = MaterialsFolder + "/" + fileName;
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (shader == null)
                shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null)
                shader = Shader.Find("Universal Render Pipeline/Unlit");

            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor"))
            mat.SetColor("_BaseColor", color);
        if (mat.HasProperty("_Color"))
            mat.SetColor("_Color", color);

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
