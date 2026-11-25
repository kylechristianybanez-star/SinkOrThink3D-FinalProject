using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent background music, environmental ambience, and gameplay SFX manager
/// with PlayerPrefs volume controls and automatic procedural fallback synthesis.
/// </summary>
public class GameAudioManager : MonoBehaviour
{
    public static GameAudioManager Instance { get; private set; }

    public const string MasterVolumeKey = "SOT_MasterVolume";
    public const string MusicVolumeKey = "SOT_MusicVolume";
    public const string AmbientVolumeKey = "SOT_AmbientVolume";
    public const string SfxVolumeKey = "SOT_SfxVolume";

    [Header("Background Music Clips")]
    public AudioClip adventureMusicClip;
    public AudioClip islandMusicClip;
    public AudioClip gameOverMusicClip;
    public AudioClip victoryMusicClip;

    [Header("Ambience Clips")]
    public AudioClip oceanAmbienceClip;
    public AudioClip discoveryAmbienceClip;
    public AudioClip vikingAmbienceClip;
    public AudioClip finalAmbienceClip;

    [Header("Gameplay SFX Clips")]
    public AudioClip correctClip;
    public AudioClip wrongClip;
    public AudioClip chestOpenClip;
    public AudioClip chestLockedClip;
    public AudioClip treasureCollectClip;
    public AudioClip repairClip;
    public AudioClip dockingClip;
    public AudioClip footstepGroundClip;
    public AudioClip footstepWoodClip;
    public AudioClip splashClip;
    public AudioClip victoryClip;
    public AudioClip gameOverClip;
    public AudioClip uiClickClip;
    public AudioClip hintChimeClip;
    public AudioClip speedBoostClip;
    public AudioClip hullCreakClip;

    private AudioSource musicSource;
    private AudioSource ambientSource;
    private AudioSource sfxSource;

    public float MasterVolume { get; private set; } = 0.85f;
    public float MusicVolume { get; private set; } = 0.42f;
    public float AmbientVolume { get; private set; } = 0.35f;
    public float SfxVolume { get; private set; } = 0.65f;

    public static GameAudioManager EnsureInstance()
    {
        if (Instance != null)
            return Instance;

        GameAudioManager existing = FindAnyObjectByType<GameAudioManager>();
        if (existing != null)
        {
            Instance = existing;
            return Instance;
        }

        GameObject go = new GameObject("GameAudioManager");
        Instance = go.AddComponent<GameAudioManager>();
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
        DontDestroyOnLoad(gameObject);

        musicSource = CreateChildAudioSource("MusicSource", true);
        ambientSource = CreateChildAudioSource("AmbientSource", true);
        sfxSource = CreateChildAudioSource("SfxSource", false);

        EnsureFallbackClips();
        LoadAndApplySavedVolumes();
    }

    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void Start()
    {
        UpdateAmbienceForScene(SceneManager.GetActiveScene().name);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateAmbienceForScene(scene.name);
    }

    private void UpdateAmbienceForScene(string sceneName)
    {
        if (string.Equals(sceneName, "MainMenu") || string.Equals(sceneName, "GameScene"))
        {
            SetOceanAmbience();
            SetAdventureMusic();
        }
        else if (string.Equals(sceneName, "IslandScene"))
        {
            GameStateManager state = GameStateManager.EnsureInstance();
            SetIslandAmbience(Mathf.Clamp(state.selectedIslandIndex, 0, 2));
            SetIslandMusic();
        }
    }

    public void LoadAndApplySavedVolumes()
    {
        float m = PlayerPrefs.GetFloat(MasterVolumeKey, 0.85f);
        float mu = PlayerPrefs.GetFloat(MusicVolumeKey, 0.42f);
        float a = PlayerPrefs.GetFloat(AmbientVolumeKey, 0.35f);
        float s = PlayerPrefs.GetFloat(SfxVolumeKey, 0.65f);
        ApplyVolumes(m, mu, a, s, false);
    }

    public void ApplyVolumes(float master, float ambient, float sfx, bool saveToPrefs = true)
    {
        ApplyVolumes(master, MusicVolume, ambient, sfx, saveToPrefs);
    }

    public void ApplyVolumes(float master, float music, float ambient, float sfx, bool saveToPrefs = true)
    {
        MasterVolume = Mathf.Clamp01(master);
        MusicVolume = Mathf.Clamp01(music);
        AmbientVolume = Mathf.Clamp01(ambient);
        SfxVolume = Mathf.Clamp01(sfx);

        AudioListener.volume = MasterVolume;
        if (musicSource != null)
            musicSource.volume = MusicVolume;
        if (ambientSource != null)
            ambientSource.volume = AmbientVolume;
        if (sfxSource != null)
            sfxSource.volume = SfxVolume;

        if (saveToPrefs)
        {
            PlayerPrefs.SetFloat(MasterVolumeKey, MasterVolume);
            PlayerPrefs.SetFloat(MusicVolumeKey, MusicVolume);
            PlayerPrefs.SetFloat(AmbientVolumeKey, AmbientVolume);
            PlayerPrefs.SetFloat(SfxVolumeKey, SfxVolume);
            PlayerPrefs.Save();
        }
    }

    public void SetAdventureMusic()
    {
        PlayMusicClip(adventureMusicClip);
    }

    public void SetIslandMusic()
    {
        PlayMusicClip(islandMusicClip);
    }

    public void SetGameOverMusic()
    {
        PlayMusicClip(gameOverMusicClip);
    }

    public void SetVictoryMusic()
    {
        PlayMusicClip(victoryMusicClip);
    }

    private void PlayMusicClip(AudioClip clip)
    {
        if (musicSource == null || clip == null)
            return;

        if (musicSource.clip == clip && musicSource.isPlaying)
            return;

        musicSource.clip = clip;
        musicSource.volume = MusicVolume;
        musicSource.Play();
    }

    public void SetOceanAmbience()
    {
        PlayAmbienceClip(oceanAmbienceClip);
    }

    public void SetIslandAmbience(int islandIndex)
    {
        if (islandIndex == 1)
            PlayAmbienceClip(vikingAmbienceClip);
        else if (islandIndex == 2)
            PlayAmbienceClip(finalAmbienceClip);
        else
            PlayAmbienceClip(discoveryAmbienceClip);
    }

    private void PlayAmbienceClip(AudioClip clip)
    {
        if (ambientSource == null || clip == null)
            return;

        if (ambientSource.clip == clip && ambientSource.isPlaying)
            return;

        ambientSource.clip = clip;
        ambientSource.volume = AmbientVolume;
        ambientSource.Play();
    }

    public void PlayCorrect() => PlaySfx(correctClip, 0.72f);
    public void PlayWrong() => PlaySfx(wrongClip, 0.7f);
    public void PlayChestOpen() => PlaySfx(chestOpenClip, 0.75f);
    public void PlayChestLocked() => PlaySfx(chestLockedClip, 0.65f);
    public void PlayTreasureCollect() => PlaySfx(treasureCollectClip, 0.78f);
    public void PlayRepair() => PlaySfx(repairClip, 0.75f);
    public void PlayDocking() => PlaySfx(dockingClip, 0.75f);
    public void PlaySplash() => PlaySfx(splashClip, 0.55f);
    public void PlayVictory() => PlaySfx(victoryClip, 0.85f);
    public void PlayGameOver() => PlaySfx(gameOverClip, 0.75f);
    public void PlayClick() => PlaySfx(uiClickClip, 0.5f);
    public void PlayHintChime() => PlaySfx(hintChimeClip, 0.68f);
    public void PlaySpeedBoost() => PlaySfx(speedBoostClip, 0.72f);
    public void PlayHullCreak() => PlaySfx(hullCreakClip, 0.65f);

    public void PlayFootstep(bool isWoodDock)
    {
        if (sfxSource == null)
            return;

        AudioClip clip = isWoodDock ? footstepWoodClip : footstepGroundClip;
        if (clip == null)
            return;

        sfxSource.pitch = Random.Range(0.94f, 1.06f);
        sfxSource.PlayOneShot(clip, 0.42f);
    }

    private void PlaySfx(AudioClip clip, float scale)
    {
        if (sfxSource == null || clip == null)
            return;

        sfxSource.pitch = 1f;
        sfxSource.PlayOneShot(clip, Mathf.Clamp01(scale));
    }

    private AudioSource CreateChildAudioSource(string childName, bool loop)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        AudioSource src = child.AddComponent<AudioSource>();
        src.loop = loop;
        src.playOnAwake = false;
        src.spatialBlend = 0f;
        return src;
    }

    private void EnsureFallbackClips()
    {
        if (adventureMusicClip == null)
        {
            adventureMusicClip = CreateMusicLoopClip("Fallback_BGM_Adventure", 8.0f, 0.22f,
                new float[] { 261.63f, 329.63f, 392.00f, 329.63f, 293.66f, 349.23f, 440.00f, 349.23f, 392.00f, 493.88f, 587.33f, 493.88f, 261.63f, 329.63f, 392.00f, 523.25f },
                new float[] { 130.81f, 146.83f, 196.00f, 130.81f }, true);
        }
        if (islandMusicClip == null)
        {
            islandMusicClip = CreateMusicLoopClip("Fallback_BGM_Island", 8.0f, 0.20f,
                new float[] { 349.23f, 440.00f, 523.25f, 440.00f, 293.66f, 349.23f, 440.00f, 349.23f, 261.63f, 329.63f, 392.00f, 329.63f, 349.23f, 440.00f, 523.25f, 392.00f },
                new float[] { 174.61f, 146.83f, 130.81f, 174.61f }, true);
        }
        if (gameOverMusicClip == null)
        {
            gameOverMusicClip = CreateMusicLoopClip("Fallback_BGM_GameOver", 8.0f, 0.24f,
                new float[] { 220.00f, 261.63f, 329.63f, 261.63f, 174.61f, 220.00f, 261.63f, 220.00f, 146.83f, 174.61f, 220.00f, 174.61f, 164.81f, 196.00f, 246.94f, 196.00f },
                new float[] { 110.00f, 87.31f, 73.42f, 82.41f }, false);
        }
        if (victoryMusicClip == null)
        {
            victoryMusicClip = CreateMusicLoopClip("Fallback_BGM_Victory", 6.0f, 0.26f,
                new float[] { 523.25f, 659.25f, 783.99f, 1046.50f, 698.46f, 880.00f, 1046.50f, 880.00f, 783.99f, 987.77f, 1174.66f, 987.77f, 1046.50f, 783.99f, 659.25f, 523.25f },
                new float[] { 261.63f, 349.23f, 392.00f, 261.63f }, true);
        }

        if (oceanAmbienceClip == null) oceanAmbienceClip = CreateAmbientNoiseClip("Fallback_Ocean", 4.0f, 0.16f, 0.045f, 0f);
        if (discoveryAmbienceClip == null) discoveryAmbienceClip = CreateAmbientNoiseClip("Fallback_Discovery", 4.0f, 0.12f, 0.07f, 2100f);
        if (vikingAmbienceClip == null) vikingAmbienceClip = CreateAmbientNoiseClip("Fallback_VikingWind", 4.0f, 0.15f, 0.035f, 0f);
        if (finalAmbienceClip == null) finalAmbienceClip = CreateAmbientNoiseClip("Fallback_FinalMystic", 4.0f, 0.14f, 0.025f, 110f);

        if (correctClip == null) correctClip = CreateArpeggioClip("Fallback_Correct", new float[] { 523.25f, 659.25f, 783.99f }, 0.36f, 0.28f);
        if (wrongClip == null) wrongClip = CreateArpeggioClip("Fallback_Wrong", new float[] { 246.94f, 196.00f }, 0.34f, 0.26f);
        if (chestOpenClip == null) chestOpenClip = CreateArpeggioClip("Fallback_ChestOpen", new float[] { 392.00f, 493.88f, 587.33f }, 0.42f, 0.25f);
        if (chestLockedClip == null) chestLockedClip = CreatePercussionClip("Fallback_ChestLocked", 0.16f, 190f, 0.28f);
        if (treasureCollectClip == null) treasureCollectClip = CreateArpeggioClip("Fallback_Treasure", new float[] { 659.25f, 783.99f, 987.77f, 1174.66f }, 0.48f, 0.28f);
        if (repairClip == null) repairClip = CreatePercussionClip("Fallback_Repair", 0.26f, 260f, 0.3f);
        if (dockingClip == null) dockingClip = CreateArpeggioClip("Fallback_Docking", new float[] { 329.63f, 392.00f }, 0.38f, 0.24f);
        if (footstepGroundClip == null) footstepGroundClip = CreatePercussionClip("Fallback_StepGround", 0.11f, 130f, 0.22f);
        if (footstepWoodClip == null) footstepWoodClip = CreatePercussionClip("Fallback_StepWood", 0.12f, 240f, 0.26f);
        if (splashClip == null) splashClip = CreateAmbientNoiseClip("Fallback_Splash", 0.45f, 0.22f, 0.18f, 0f);
        if (victoryClip == null) victoryClip = CreateArpeggioClip("Fallback_Victory", new float[] { 523.25f, 659.25f, 783.99f, 1046.50f }, 0.85f, 0.32f);
        if (gameOverClip == null) gameOverClip = CreateArpeggioClip("Fallback_GameOver", new float[] { 293.66f, 261.63f, 220.00f, 174.61f }, 0.72f, 0.28f);
        if (uiClickClip == null) uiClickClip = CreatePercussionClip("Fallback_Click", 0.06f, 620f, 0.18f);
        if (hintChimeClip == null) hintChimeClip = CreateArpeggioClip("Fallback_HintChime", new float[] { 659.25f, 880.00f, 1174.66f }, 0.34f, 0.24f);
        if (speedBoostClip == null) speedBoostClip = CreateArpeggioClip("Fallback_SpeedBoost", new float[] { 392.00f, 523.25f, 659.25f, 987.77f }, 0.44f, 0.26f);
        if (hullCreakClip == null) hullCreakClip = CreatePercussionClip("Fallback_HullCreak", 0.24f, 95f, 0.32f);
    }

    private static AudioClip CreateMusicLoopClip(string clipName, float duration, float amplitude, float[] notesHz, float[] bassHz, bool brightStyle)
    {
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

            float mixed = (melody * 0.55f + bass * 0.65f) * amplitude;
            data[i] = Mathf.Clamp(mixed, -0.88f, 0.88f);
        }

        int fade = Mathf.Min(2048, samples / 8);
        for (int i = 0; i < fade; i++)
        {
            float blend = i / (float)fade;
            data[i] = Mathf.Lerp(data[samples - fade + i], data[i], blend);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateAmbientNoiseClip(string clipName, float duration, float amplitude, float smoothing, float subtleToneHz)
    {
        const int sampleRate = 22050;
        int samples = Mathf.Max(128, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        System.Random rng = new System.Random(clipName.GetHashCode());
        float lp = 0f;

        for (int i = 0; i < samples; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);
            lp += smoothing * (white - lp);
            float t = i / (float)sampleRate;
            float waveMod = 0.65f + 0.35f * Mathf.Sin(2f * Mathf.PI * (t / duration));
            float sample = lp * amplitude * waveMod;

            if (subtleToneHz > 0f)
            {
                float chirpEnv = Mathf.Max(0f, Mathf.Sin(t * 2.4f) - 0.72f) * 0.25f;
                sample += Mathf.Sin(2f * Mathf.PI * subtleToneHz * t) * chirpEnv * amplitude;
            }

            data[i] = Mathf.Clamp(sample, -0.9f, 0.9f);
        }

        int fade = Mathf.Min(1024, samples / 8);
        for (int i = 0; i < fade; i++)
        {
            float blend = i / (float)fade;
            data[i] = Mathf.Lerp(data[samples - fade + i], data[i], blend);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreateArpeggioClip(string clipName, float[] notesHz, float duration, float amplitude)
    {
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
            float s = Mathf.Sin(2f * Mathf.PI * notesHz[idx] * t) * attack * decay * amplitude;
            data[i] = Mathf.Clamp(s, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }

    private static AudioClip CreatePercussionClip(string clipName, float duration, float baseHz, float amplitude)
    {
        const int sampleRate = 22050;
        int samples = Mathf.Max(64, Mathf.RoundToInt(sampleRate * duration));
        float[] data = new float[samples];
        System.Random rng = new System.Random(clipName.GetHashCode());

        for (int i = 0; i < samples; i++)
        {
            float t = i / (float)sampleRate;
            float env = Mathf.Exp(-t * 24f);
            float tone = Mathf.Sin(2f * Mathf.PI * baseHz * (1f - 0.35f * (t / duration)) * t);
            float clickNoise = (float)(rng.NextDouble() * 2.0 - 1.0) * 0.25f;
            data[i] = Mathf.Clamp((tone * 0.8f + clickNoise) * env * amplitude, -0.9f, 0.9f);
        }

        AudioClip clip = AudioClip.Create(clipName, samples, 1, sampleRate, false);
        clip.SetData(data, 0);
        return clip;
    }
}
