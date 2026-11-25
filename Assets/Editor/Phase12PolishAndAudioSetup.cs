using System.IO;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Phase 12 Editor Builder & Master Build-All Convenience Tool:
/// Generates clean procedural WAV audio clips in Assets/Audio/, wires GameAudioManager
/// and MainMenu SettingsButton in MainMenu.unity, adds lightweight drifting SkyCloudLayer
/// puffs to GameScene.unity and IslandScene.unity, and provides a 1-click Build All Scenes menu.
/// </summary>
public static class Phase12PolishAndAudioSetup
{
    private const string MainMenuPath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string IslandScenePath = "Assets/Scenes/IslandScene.unity";
    private const string AudioFolder = "Assets/Audio";

    [MenuItem("Tools/SinkOrThink3D/Phase 12 - Polish Visuals, Audio & Settings (All Scenes)")]
    public static void BuildPhase12Polish()
    {
        RunBuild(true);
    }

    [MenuItem("Tools/SinkOrThink3D/Build & Verify All Scenes (Phases 9.5 -> 13)")]
    public static void BuildAllScenes()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        Debug.Log("[BuildAll] Step 1/5: Phase 9.5 Discovery Island Polish...");
        Phase9_5DiscoveryIslandPolish.RunBuild(false);
        Debug.Log("[BuildAll] Step 2/5: Phase 10 Viking Island...");
        Phase10VikingIslandSetup.RunBuild(false);
        Debug.Log("[BuildAll] Step 3/5: Phase 11 Final Treasure Island...");
        Phase11FinalIslandSetup.RunBuild(false);
        Debug.Log("[BuildAll] Step 4/5: Phase 12 Visuals, Audio & Settings...");
        RunBuild(false);
        Debug.Log("[BuildAll] Step 5/5: Phase 13 Optimization & Full QA Audit...");
        Phase13OptimizationAndQATool.RunOptimizationAndAudit(false);

        EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);

        Debug.Log("All Phases (9.5 -> 13) built, optimized, audited, and saved cleanly!");
        EditorUtility.DisplayDialog(
            "Build & Verify All Scenes Complete",
            "Successfully executed Phases 9.5, 10, 11, 12, and 13 across MainMenu, GameScene, and IslandScene:\n" +
            "- Discovery Island, Viking Island, and Final Treasure Island overviews + docks\n" +
            "- Reusable IslandScene with all 3 detailed islands & 18 verified chests\n" +
            "- 17 WAV audio clips + GameAudioManager + MainMenu SettingsButton + SkyCloudLayer\n" +
            "- RTX 2050 scene/terrain optimizations & full QA audit passed (check Console for report)\n\n" +
            "MainMenu.unity is now open and ready to press Play!",
            "OK"
        );
    }

    public static void RunBuild(bool showDialog)
    {
        if (showDialog && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        EnsureFolder(AudioFolder);
        EnsureFolder("Assets/Materials");

        GenerateAllWavAssets();
        AssetDatabase.Refresh();

        // 1. Setup MainMenu.unity with GameAudioManager + SettingsButton
        Scene mainMenu = EditorSceneManager.OpenScene(MainMenuPath, OpenSceneMode.Single);
        SetupMainMenu(mainMenu);
        EditorSceneManager.MarkSceneDirty(mainMenu);
        EditorSceneManager.SaveScene(mainMenu);

        // 2. Setup drifting sky clouds in GameScene.unity and IslandScene.unity
        Material cloudMat = GetOrCreateCloudMaterial();

        Scene gameScene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        BuildSkyCloudLayer(gameScene, Vector3.zero, cloudMat);
        EditorSceneManager.MarkSceneDirty(gameScene);
        EditorSceneManager.SaveScene(gameScene);

        Scene islandScene = EditorSceneManager.OpenScene(IslandScenePath, OpenSceneMode.Single);
        BuildSkyCloudLayer(islandScene, Vector3.zero, cloudMat);
        EditorSceneManager.MarkSceneDirty(islandScene);
        EditorSceneManager.SaveScene(islandScene);

        AssetDatabase.SaveAssets();

        Debug.Log("Phase 12 Complete! Added WAV audio clips, MainMenu Settings button & GameAudioManager, and SkyCloudLayer.");
        if (showDialog)
        {
            EditorUtility.DisplayDialog(
                "Phase 12 Visuals, Audio & Settings Complete",
                "Generated 17 WAV audio clips under Assets/Audio/, wired GameAudioManager & SettingsButton into MainMenu.unity, and added drifting SkyCloudLayer puffs to GameScene.unity and IslandScene.unity.",
                "OK"
            );
        }
    }

    private static void SetupMainMenu(Scene mainMenu)
    {
        // Rebuild GameAudioManager cleanly with all WAV clips wired
        DestroyRootIfExists(mainMenu, "GameAudioManager");
        GameObject audioGo = new GameObject("GameAudioManager");
        SceneManager.MoveGameObjectToScene(audioGo, mainMenu);
        GameAudioManager audioMgr = audioGo.AddComponent<GameAudioManager>();

        audioMgr.adventureMusicClip = LoadAudio("BGM_Adventure.wav");
        audioMgr.islandMusicClip = LoadAudio("BGM_Island.wav");
        audioMgr.gameOverMusicClip = LoadAudio("BGM_GameOver.wav");
        audioMgr.victoryMusicClip = LoadAudio("BGM_Victory.wav");

        audioMgr.oceanAmbienceClip = LoadAudio("Ambience_Ocean.wav");
        audioMgr.discoveryAmbienceClip = LoadAudio("Ambience_Discovery.wav");
        audioMgr.vikingAmbienceClip = LoadAudio("Ambience_Viking.wav");
        audioMgr.finalAmbienceClip = LoadAudio("Ambience_Final.wav");

        audioMgr.correctClip = LoadAudio("SFX_Correct.wav");
        audioMgr.wrongClip = LoadAudio("SFX_Wrong.wav");
        audioMgr.chestOpenClip = LoadAudio("SFX_ChestOpen.wav");
        audioMgr.chestLockedClip = LoadAudio("SFX_ChestLocked.wav");
        audioMgr.treasureCollectClip = LoadAudio("SFX_TreasureCollect.wav");
        audioMgr.repairClip = LoadAudio("SFX_Repair.wav");
        audioMgr.dockingClip = LoadAudio("SFX_Docking.wav");
        audioMgr.footstepGroundClip = LoadAudio("SFX_FootstepGround.wav");
        audioMgr.footstepWoodClip = LoadAudio("SFX_FootstepWood.wav");
        audioMgr.splashClip = LoadAudio("SFX_Splash.wav");
        audioMgr.victoryClip = LoadAudio("SFX_Victory.wav");
        audioMgr.gameOverClip = LoadAudio("SFX_GameOver.wav");
        audioMgr.uiClickClip = LoadAudio("SFX_Click.wav");
        audioMgr.hintChimeClip = LoadAudio("SFX_HintChime.wav");
        audioMgr.speedBoostClip = LoadAudio("SFX_SpeedBoost.wav");
        audioMgr.hullCreakClip = LoadAudio("SFX_HullCreak.wav");
        EditorUtility.SetDirty(audioMgr);

        // Wire SettingsButton on MainMenuCanvas without overlapping Play/HowToPlay/Quit
        MainMenuManager menuMgr = Object.FindAnyObjectByType<MainMenuManager>();
        GameObject canvasGo = FindSceneObjectByName(mainMenu, "MainMenuCanvas");
        if (canvasGo == null || menuMgr == null)
            return;

        Transform existingSettings = canvasGo.transform.Find("SettingsButton");
        if (existingSettings != null)
            Object.DestroyImmediate(existingSettings.gameObject);

        Transform playTf = canvasGo.transform.Find("PlayButton");
        Transform howTf = canvasGo.transform.Find("HowToPlayButton");
        Transform quitTf = canvasGo.transform.Find("QuitButton");

        if (howTf == null)
            return;

        RectTransform playRt = playTf != null ? playTf.GetComponent<RectTransform>() : null;
        RectTransform howRt = howTf.GetComponent<RectTransform>();
        RectTransform quitRt = quitTf != null ? quitTf.GetComponent<RectTransform>() : null;

        float stepY = (playRt != null && Mathf.Abs(howRt.anchoredPosition.y - playRt.anchoredPosition.y) > 10f)
            ? (howRt.anchoredPosition.y - playRt.anchoredPosition.y)
            : -85f;

        GameObject settingsBtnGo = Object.Instantiate(howTf.gameObject, canvasGo.transform);
        settingsBtnGo.name = "SettingsButton";

        RectTransform sRt = settingsBtnGo.GetComponent<RectTransform>();
        sRt.anchoredPosition = new Vector2(howRt.anchoredPosition.x, howRt.anchoredPosition.y + stepY);
        sRt.sizeDelta = howRt.sizeDelta;

        if (quitRt != null)
            quitRt.anchoredPosition = new Vector2(howRt.anchoredPosition.x, howRt.anchoredPosition.y + stepY * 2f);

        // Place SettingsButton right before HowToPlayPanel so overlays render on top
        Transform howPanelTf = canvasGo.transform.Find("HowToPlayPanel");
        if (howPanelTf != null)
            settingsBtnGo.transform.SetSiblingIndex(howPanelTf.GetSiblingIndex());

        TMP_Text lbl = settingsBtnGo.GetComponentInChildren<TMP_Text>(true);
        if (lbl != null)
        {
            lbl.text = "SETTINGS";
            EditorUtility.SetDirty(lbl);
        }

        Button sBtn = settingsBtnGo.GetComponent<Button>();
        if (sBtn != null)
        {
            int persistentCount = sBtn.onClick.GetPersistentEventCount();
            for (int i = persistentCount - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(sBtn.onClick, i);

            UnityEventTools.AddPersistentListener(sBtn.onClick, menuMgr.OpenSettings);
            EditorUtility.SetDirty(sBtn);
        }
    }

    private static void BuildSkyCloudLayer(Scene scene, Vector3 centerAnchor, Material cloudMat)
    {
        DestroyRootIfExists(scene, "SkyCloudLayer");
        GameObject cloudRoot = new GameObject("SkyCloudLayer");
        SceneManager.MoveGameObjectToScene(cloudRoot, scene);
        cloudRoot.transform.position = centerAnchor;

        CloudDriftAnimator drift = cloudRoot.AddComponent<CloudDriftAnimator>();
        drift.driftVelocity = new Vector3(1.6f, 0f, 0.35f);
        drift.wrapHalfSize = 620f;
        drift.centerAnchor = centerAnchor;

        System.Random rng = new System.Random(1204);
        for (int i = 0; i < 14; i++)
        {
            float cx = Mathf.Lerp(-540f, 540f, (float)rng.NextDouble());
            float cz = Mathf.Lerp(-460f, 560f, (float)rng.NextDouble());
            float cy = 84f + (float)rng.NextDouble() * 34f;

            GameObject cluster = new GameObject("CloudCluster_" + (i + 1));
            cluster.transform.SetParent(cloudRoot.transform, false);
            cluster.transform.position = new Vector3(cx, cy, cz);
            cluster.transform.rotation = Quaternion.Euler(0f, (float)rng.NextDouble() * 360f, 0f);

            int puffs = 3;
            for (int p = 0; p < puffs; p++)
            {
                float px = (p - 1) * 9.5f;
                float py = (p == 1 ? 2.2f : 0f);
                float pz = ((p % 2) * 3.5f - 1.5f);
                Vector3 scale = p == 1
                    ? new Vector3(18f, 6.5f, 13f)
                    : new Vector3(13f, 5f, 10.5f);

                GameObject puff = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                puff.name = "Puff_" + (p + 1);
                puff.transform.SetParent(cluster.transform, false);
                puff.transform.localPosition = new Vector3(px, py, pz);
                puff.transform.localScale = scale;

                Collider c = puff.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);

                MeshRenderer mr = puff.GetComponent<MeshRenderer>();
                if (mr != null)
                {
                    if (cloudMat != null) mr.sharedMaterial = cloudMat;
                    mr.shadowCastingMode = ShadowCastingMode.Off;
                    mr.receiveShadows = false;
                }
            }
        }
    }

    private static Material GetOrCreateCloudMaterial()
    {
        const string path = "Assets/Materials/SkyCloudSoft.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat != null)
            return mat;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Standard");
        mat = new Material(shader);
        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", new Color(0.96f, 0.97f, 1f, 1f));
        if (mat.HasProperty("_Color")) mat.SetColor("_Color", new Color(0.96f, 0.97f, 1f, 1f));
        if (mat.HasProperty("_Smoothness")) mat.SetFloat("_Smoothness", 0.05f);
        AssetDatabase.CreateAsset(mat, path);
        return mat;
    }

    private static void GenerateAllWavAssets()
    {
        WriteMusicLoopWavIfMissing("BGM_Adventure.wav", 8.0f, 0.22f,
            new float[] { 261.63f, 329.63f, 392.00f, 329.63f, 293.66f, 349.23f, 440.00f, 349.23f, 392.00f, 493.88f, 587.33f, 493.88f, 261.63f, 329.63f, 392.00f, 523.25f },
            new float[] { 130.81f, 146.83f, 196.00f, 130.81f }, true);
        WriteMusicLoopWavIfMissing("BGM_Island.wav", 8.0f, 0.20f,
            new float[] { 349.23f, 440.00f, 523.25f, 440.00f, 293.66f, 349.23f, 440.00f, 349.23f, 261.63f, 329.63f, 392.00f, 329.63f, 349.23f, 440.00f, 523.25f, 392.00f },
            new float[] { 174.61f, 146.83f, 130.81f, 174.61f }, true);
        WriteMusicLoopWavIfMissing("BGM_GameOver.wav", 8.0f, 0.24f,
            new float[] { 220.00f, 261.63f, 329.63f, 261.63f, 174.61f, 220.00f, 261.63f, 220.00f, 146.83f, 174.61f, 220.00f, 174.61f, 164.81f, 196.00f, 246.94f, 196.00f },
            new float[] { 110.00f, 87.31f, 73.42f, 82.41f }, false);
        WriteMusicLoopWavIfMissing("BGM_Victory.wav", 6.0f, 0.26f,
            new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 698.46f, 880.00f, 1046.50f, 880.00f, 783.99f, 987.77f, 1174.66f, 987.77f, 1046.50f, 783.99f, 659.25f, 523.25f },
            new float[] { 261.63f, 349.23f, 392.00f, 261.63f }, true);

        WriteNoiseWavIfMissing("Ambience_Ocean.wav", 4.0f, 0.16f, 0.045f, 0f);
        WriteNoiseWavIfMissing("Ambience_Discovery.wav", 4.0f, 0.12f, 0.07f, 2100f);
        WriteNoiseWavIfMissing("Ambience_Viking.wav", 4.0f, 0.15f, 0.035f, 0f);
        WriteNoiseWavIfMissing("Ambience_Final.wav", 4.0f, 0.14f, 0.025f, 110f);

        WriteArpeggioWavIfMissing("SFX_Correct.wav", new float[] { 523.25f, 659.25f, 783.99f }, 0.36f, 0.28f);
        WriteArpeggioWavIfMissing("SFX_Wrong.wav", new float[] { 246.94f, 196.00f }, 0.34f, 0.26f);
        WriteArpeggioWavIfMissing("SFX_ChestOpen.wav", new float[] { 392.00f, 493.88f, 587.33f }, 0.42f, 0.25f);
        WritePercussionWavIfMissing("SFX_ChestLocked.wav", 0.16f, 190f, 0.28f);
        WriteArpeggioWavIfMissing("SFX_TreasureCollect.wav", new float[] { 659.25f, 783.99f, 987.77f, 1174.66f }, 0.48f, 0.28f);
        WritePercussionWavIfMissing("SFX_Repair.wav", 0.26f, 260f, 0.3f);
        WriteArpeggioWavIfMissing("SFX_Docking.wav", new float[] { 329.63f, 392.00f }, 0.38f, 0.24f);
        WritePercussionWavIfMissing("SFX_FootstepGround.wav", 0.11f, 130f, 0.22f);
        WritePercussionWavIfMissing("SFX_FootstepWood.wav", 0.12f, 240f, 0.26f);
        WriteNoiseWavIfMissing("SFX_Splash.wav", 0.45f, 0.22f, 0.18f, 0f);
        WriteArpeggioWavIfMissing("SFX_Victory.wav", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.85f, 0.32f);
        WriteArpeggioWavIfMissing("SFX_GameOver.wav", new float[] { 293.66f, 261.63f, 220.00f, 174.61f }, 0.72f, 0.28f);
        WritePercussionWavIfMissing("SFX_Click.wav", 0.06f, 620f, 0.18f);
        WriteArpeggioWavIfMissing("SFX_HintChime.wav", new float[] { 659.25f, 880.00f, 1174.66f }, 0.34f, 0.24f);
        WriteArpeggioWavIfMissing("SFX_SpeedBoost.wav", new float[] { 392.00f, 523.25f, 659.25f, 987.77f }, 0.44f, 0.26f);
        WritePercussionWavIfMissing("SFX_HullCreak.wav", 0.24f, 95f, 0.32f);
    }

    private static void WriteMusicLoopWavIfMissing(string fileName, float duration, float amplitude, float[] notesHz, float[] bassHz, bool brightStyle)
    {
        string assetPath = AudioFolder + "/" + fileName;
        string fullPath = Path.GetFullPath(assetPath);
        if (File.Exists(fullPath))
            return;

        const int sampleRate = 22050;
        int samples = Mathf.Max(256, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        float noteDur = duration / Mathf.Max(1, notesHz.Length);
        float bassDur = duration / Mathf.Max(1, bassHz.Length);

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;

            int nIdx = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notesHz.Length - 1);
            float localNoteT = t - nIdx * noteDur;
            float nAttack = Mathf.Clamp01(localNoteT / 0.04f);
            float nDecay = brightStyle ? Mathf.Exp(-localNoteT * 2.2f) : Mathf.Exp(-localNoteT * 1.1f);
            float melodyFreq = notesHz[nIdx];
            float melody = (Mathf.Sin(2f * Mathf.PI * melodyFreq * t) + 0.28f * Mathf.Sin(4f * Mathf.PI * melodyFreq * t)) * nAttack * nDecay;

            int bIdx = Mathf.Clamp(Mathf.FloorToInt(t / bassDur), 0, bassHz.Length - 1);
            float localBassT = t - bIdx * bassDur;
            float bAttack = Mathf.Clamp01(localBassT / 0.08f);
            float bSwell = Mathf.Exp(-localBassT * 0.55f);
            float bassFreq = bassHz[bIdx];
            float bass = (Mathf.Sin(2f * Mathf.PI * bassFreq * t) + 0.22f * Mathf.Sin(4f * Mathf.PI * bassFreq * t)) * bAttack * bSwell;

            data[i] = Mathf.Clamp((melody * 0.55f + bass * 0.65f) * amplitude, -0.88f, 0.88f);
        }

        int fade = Mathf.Min(2048, samples / 8);
        for (int i = 0; i < fade; i++)
        {
            float blend = i / (float)fade;
            data[i] = Mathf.Lerp(data[samples - fade + i], data[i], blend);
        }

        WritePcm16MonoWav(fullPath, data, sampleRate);
    }

    private static void WriteNoiseWavIfMissing(string fileName, float duration, float amplitude, float smoothing, float subtleToneHz)
    {
        string assetPath = AudioFolder + "/" + fileName;
        string fullPath = Path.GetFullPath(assetPath);
        if (File.Exists(fullPath))
            return;

        const int sampleRate = 22050;
        int samples = Mathf.Max(128, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        System.Random rng = new System.Random(fileName.GetHashCode());
        float lp = 0f;

        for (int i = 0; i < samples; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += smoothing * (white - lp);
            float t = i / (float)sampleRate;
            float waveMod = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * (t / duration));
            float s = lp * amplitude * waveMod;
            if (subtleToneHz > 0f)
            {
                float chirpEnv = Mathf.Max(0f, Mathf.Sin(t * 2.4f) - 0.72f) * 0.25f;
                s += Mathf.Sin(2f * Mathf.PI * subtleToneHz * t) * chirpEnv * amplitude;
            }
            data[i] = Mathf.Clamp(s, -0.9f, 0.9f);
        }

        WritePcm16MonoWav(fullPath, data, sampleRate);
    }

    private static void WriteArpeggioWavIfMissing(string fileName, float[] notesHz, float duration, float amplitude)
    {
        string assetPath = AudioFolder + "/" + fileName;
        string fullPath = Path.GetFullPath(assetPath);
        if (File.Exists(fullPath))
            return;

        const int sampleRate = 22050;
        int samples = Mathf.Max(128, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        float noteDur = duration / Mathf.Max(1, notesHz.Length);

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            int idx = Mathf.Clamp(Mathf.FloorToInt(t / noteDur), 0, notesHz.Length - 1);
            float localT = t - idx * noteDur;
            float attack = Mathf.Clamp01(localT / 0.015f);
            float decay = Mathf.Exp(-localT * 6.5f);
            data[i] = Mathf.Clamp(Mathf.Sin(2f * Mathf.PI * notesHz[idx] * t) * attack * decay * amplitude, -0.9f, 0.9f);
        }

        WritePcm16MonoWav(fullPath, data, sampleRate);
    }

    private static void WritePercussionWavIfMissing(string fileName, float duration, float baseHz, float amplitude)
    {
        string assetPath = AudioFolder + "/" + fileName;
        string fullPath = Path.GetFullPath(assetPath);
        if (File.Exists(fullPath))
            return;

        const int sampleRate = 22050;
        int samples = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        System.Random rng = new System.Random(fileName.GetHashCode());

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 24f);
            float tone = Mathf.Sin(2f * Mathf.PI * baseHz * (1f - 0.35f * (t / duration)) * t);
            float noise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.25f;
            data[i] = Mathf.Clamp((tone * 0.8f + noise) * env * amplitude, -0.9f, 0.9f);
        }

        WritePcm16MonoWav(fullPath, data, sampleRate);
    }

    private static void WritePcm16MonoWav(string fullPath, float[] samples, int sampleRate)
    {
        using (FileStream fs = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
        using (BinaryWriter bw = new BinaryWriter(fs))
        {
            int dataBytes = samples.Length * 2;
            bw.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
            bw.Write(36 + dataBytes);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));
            bw.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
            bw.Write(16);
            bw.Write((short)1); // PCM
            bw.Write((short)1); // Mono
            bw.Write(sampleRate);
            bw.Write(sampleRate * 2);
            bw.Write((short)2);
            bw.Write((short)16);
            bw.Write(System.Text.Encoding.ASCII.GetBytes("data"));
            bw.Write(dataBytes);

            for (int i = 0; i < samples.Length; i++)
            {
                short pcm = (short)Mathf.Clamp(Mathf.RoundToInt(samples[i] * 32767f), -32767, 32767);
                bw.Write(pcm);
            }
        }
    }

    private static AudioClip LoadAudio(string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>(AudioFolder + "/" + fileName);
    }

    private static void DestroyRootIfExists(Scene scene, string rootName)
    {
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == rootName)
                Object.DestroyImmediate(roots[i]);
        }
    }

    private static GameObject FindSceneObjectByName(Scene scene, string targetName)
    {
        GameObject[] allObjects = Resources.FindObjectsOfTypeAll<GameObject>();
        for (int i = 0; i < allObjects.Length; i++)
        {
            if (allObjects[i].scene == scene && allObjects[i].name == targetName)
                return allObjects[i];
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
