using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class AudioIds
{
    public const string MenuBgm = "menu_bgm";
    public const string GameBgm = "game_bgm";
    public const string ButtonClick = "button_click";
    public const string MoneyChange = "money_change";
    public const string MoneyGain = MoneyChange;
    public const string PlayerStep = "player_step";
    public const string PlayerStepDuck = "player_step_duck";
    public const string PlayerStepRabbit = "player_step_rabbit";
    public const string PlayerStepPanda = "player_step_panda";
    public const string PlayerStepDog = "player_step_dog";
    public const string DiceRoll = "dice_roll";
    public const string QuestionOpen = "question_open";
    public const string AnswerCorrect = "answer_correct";
    public const string AnswerWrong = "answer_wrong";
    public const string ToolGain = "tool_gain";
    public const string PropertyBuy = "property_buy";
    public const string Transition = "transition";
    public const string VictoryMain = "victory_main";
    public const string VictoryLayer = "victory_layer";
}

public class AudioManager : MonoBehaviour
{
    [Serializable]
    public class AudioConfig
    {
        public float masterVolume = 1f;
        public float bgmVolume = 0.7f;
        public float sfxVolume = 1f;
        public float uiVolume = 1f;
        public SoundConfig[] sounds;
    }

    [Serializable]
    public class SoundConfig
    {
        public string id;
        public string resourcePath;
        public string category = "Sfx";
        public float volume = 1f;
        public float pitch = 1f;
        public float pitchRandom = 0f;
        public float delaySeconds = 0f;
        public float startOffsetSeconds = 0f;
        public float maxPlaySeconds = 0f;
        public float cooldownSeconds = 0f;
        public bool loop;
        public bool enabled = true;
        public float spatialBlend = 0f;
        public Vector3 worldOffset = Vector3.zero;
    }

    private const string DefaultConfigResourcePath = "Audio/audio_config";

    private static AudioManager instance;

    [Header("Config")]
    public bool loadConfigFromResources = true;
    public string configResourcePath = DefaultConfigResourcePath;
    public float masterVolume = 1f;
    public float bgmVolume = 0.7f;
    public float sfxVolume = 1f;
    public float uiVolume = 1f;

    [Header("Per Sound Calibration")]
    public List<SoundConfig> sounds = new List<SoundConfig>();

    private readonly Dictionary<string, SoundConfig> soundById = new Dictionary<string, SoundConfig>();
    private readonly Dictionary<string, AudioClip> clipByPath = new Dictionary<string, AudioClip>();
    private readonly Dictionary<string, float> lastPlayTimeById = new Dictionary<string, float>();
    private readonly List<AudioSource> sfxSources = new List<AudioSource>();
    private AudioSource bgmSource;
    private string currentBgmId;
    private bool initialized;

    public static AudioManager Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<AudioManager>();
            }

            if (instance == null)
            {
                GameObject audioObject = new GameObject("AudioManager");
                instance = audioObject.AddComponent<AudioManager>();
            }

            instance.EnsureInitialized();
            return instance;
        }
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
        DontDestroyOnLoad(gameObject);
        EnsureInitialized();
    }

    public void PlayBgm(string id = AudioIds.GameBgm, bool restart = false)
    {
        EnsureInitialized();
        if (!TryGetSound(id, out SoundConfig sound))
        {
            return;
        }

        AudioClip clip = LoadClip(sound);
        if (clip == null)
        {
            return;
        }

        if (!restart && currentBgmId == id && bgmSource.isPlaying)
        {
            return;
        }

        currentBgmId = id;
        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.pitch = GetPitch(sound);
        bgmSource.volume = GetFinalVolume(sound, 1f);
        bgmSource.spatialBlend = 0f;
        bgmSource.time = Mathf.Clamp(sound.startOffsetSeconds, 0f, Mathf.Max(0f, clip.length - 0.01f));

        if (sound.delaySeconds > 0f)
        {
            bgmSource.PlayDelayed(sound.delaySeconds);
        }
        else
        {
            bgmSource.Play();
        }
    }

    public void StopBgm()
    {
        EnsureInitialized();
        currentBgmId = string.Empty;
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

    public void PlaySfx(string id, float volumeScale = 1f)
    {
        PlaySound(id, volumeScale, false, Vector3.zero);
    }

    public void PlaySfxAt(string id, Vector3 worldPosition, float volumeScale = 1f)
    {
        PlaySound(id, volumeScale, true, worldPosition);
    }

    public void PlayUi(string id, float volumeScale = 1f)
    {
        PlaySfx(id, volumeScale);
    }

    public void ReloadConfig()
    {
        initialized = false;
        EnsureInitialized();
    }

    private void PlaySound(string id, float volumeScale, bool useWorldPosition, Vector3 worldPosition)
    {
        EnsureInitialized();
        if (!TryGetSound(id, out SoundConfig sound) || !CanPlayByCooldown(sound))
        {
            return;
        }

        AudioClip clip = LoadClip(sound);
        if (clip == null)
        {
            return;
        }

        AudioSource source = GetAvailableSfxSource();
        source.Stop();
        source.clip = clip;
        source.loop = sound.loop;
        source.pitch = GetPitch(sound);
        source.volume = GetFinalVolume(sound, volumeScale);
        source.spatialBlend = useWorldPosition ? Mathf.Clamp01(sound.spatialBlend) : 0f;
        source.transform.position = useWorldPosition ? worldPosition + sound.worldOffset : transform.position;
        source.time = Mathf.Clamp(sound.startOffsetSeconds, 0f, Mathf.Max(0f, clip.length - 0.01f));

        lastPlayTimeById[id] = Time.unscaledTime;

        if (sound.delaySeconds > 0f)
        {
            source.PlayDelayed(sound.delaySeconds);
        }
        else
        {
            source.Play();
        }

        if (!sound.loop)
        {
            float remainingDuration = Mathf.Max(0.01f, clip.length - source.time);
            if (sound.maxPlaySeconds > 0f)
            {
                remainingDuration = Mathf.Min(remainingDuration, sound.maxPlaySeconds);
            }

            float duration = Mathf.Max(0.01f, remainingDuration / Mathf.Max(0.01f, Mathf.Abs(source.pitch)));
            StartCoroutine(ReleaseSfxSourceAfter(source, duration + Mathf.Max(0f, sound.delaySeconds) + 0.1f));
        }
    }

    private void EnsureInitialized()
    {
        if (initialized)
        {
            return;
        }

        initialized = true;

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }

        LoadConfig();
        RebuildLookup();
    }

    private void LoadConfig()
    {
        if (sounds == null)
        {
            sounds = new List<SoundConfig>();
        }

        if (loadConfigFromResources)
        {
            TextAsset textAsset = Resources.Load<TextAsset>(string.IsNullOrWhiteSpace(configResourcePath)
                ? DefaultConfigResourcePath
                : configResourcePath);

            if (textAsset != null)
            {
                try
                {
                    AudioConfig config = JsonUtility.FromJson<AudioConfig>(textAsset.text);
                    if (config != null)
                    {
                        masterVolume = Mathf.Clamp01(config.masterVolume);
                        bgmVolume = Mathf.Clamp01(config.bgmVolume);
                        sfxVolume = Mathf.Clamp01(config.sfxVolume);
                        uiVolume = Mathf.Clamp01(config.uiVolume);
                        sounds = config.sounds != null
                            ? new List<SoundConfig>(config.sounds)
                            : new List<SoundConfig>();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[AudioManager] Failed to load audio config: {e.Message}");
                }
            }
        }

        if (sounds.Count == 0)
        {
            sounds.AddRange(CreateDefaultSounds());
        }
    }

    private void RebuildLookup()
    {
        soundById.Clear();
        for (int i = 0; i < sounds.Count; i++)
        {
            SoundConfig sound = sounds[i];
            if (sound == null || string.IsNullOrWhiteSpace(sound.id))
            {
                continue;
            }

            soundById[sound.id] = sound;
        }
    }

    private bool TryGetSound(string id, out SoundConfig sound)
    {
        sound = null;
        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        if (!soundById.TryGetValue(id, out sound) || sound == null || !sound.enabled)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(sound.resourcePath);
    }

    private bool CanPlayByCooldown(SoundConfig sound)
    {
        if (sound.cooldownSeconds <= 0f || string.IsNullOrWhiteSpace(sound.id))
        {
            return true;
        }

        if (!lastPlayTimeById.TryGetValue(sound.id, out float lastPlayTime))
        {
            return true;
        }

        return Time.unscaledTime - lastPlayTime >= sound.cooldownSeconds;
    }

    private AudioClip LoadClip(SoundConfig sound)
    {
        if (sound == null || string.IsNullOrWhiteSpace(sound.resourcePath))
        {
            return null;
        }

        if (clipByPath.TryGetValue(sound.resourcePath, out AudioClip cachedClip))
        {
            return cachedClip;
        }

        AudioClip clip = Resources.Load<AudioClip>(sound.resourcePath);
        if (clip == null)
        {
            Debug.LogWarning($"[AudioManager] Missing AudioClip at Resources/{sound.resourcePath}.");
            return null;
        }

        clipByPath[sound.resourcePath] = clip;
        return clip;
    }

    private AudioSource GetAvailableSfxSource()
    {
        for (int i = 0; i < sfxSources.Count; i++)
        {
            AudioSource source = sfxSources[i];
            if (source != null && !source.isPlaying)
            {
                return source;
            }
        }

        AudioSource newSource = gameObject.AddComponent<AudioSource>();
        newSource.playOnAwake = false;
        sfxSources.Add(newSource);
        return newSource;
    }

    private float GetPitch(SoundConfig sound)
    {
        float pitch = Mathf.Approximately(sound.pitch, 0f) ? 1f : sound.pitch;
        if (sound.pitchRandom > 0f)
        {
            pitch += UnityEngine.Random.Range(-sound.pitchRandom, sound.pitchRandom);
        }

        return Mathf.Clamp(pitch, 0.1f, 3f);
    }

    private float GetFinalVolume(SoundConfig sound, float volumeScale)
    {
        float categoryVolume = GetCategoryVolume(sound.category);
        return Mathf.Clamp01(masterVolume * categoryVolume * sound.volume * Mathf.Max(0f, volumeScale));
    }

    private float GetCategoryVolume(string category)
    {
        if (string.Equals(category, "Bgm", StringComparison.OrdinalIgnoreCase))
        {
            return bgmVolume;
        }

        if (string.Equals(category, "Ui", StringComparison.OrdinalIgnoreCase))
        {
            return uiVolume * sfxVolume;
        }

        return sfxVolume;
    }

    private IEnumerator ReleaseSfxSourceAfter(AudioSource source, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (source != null && source.isPlaying)
        {
            source.Stop();
        }
    }

    private static SoundConfig[] CreateDefaultSounds()
    {
        return new[]
        {
            new SoundConfig
            {
                id = AudioIds.MenuBgm,
                resourcePath = "Audio/BGM/menu_bgm",
                category = "Bgm",
                volume = 1f,
                loop = true
            },
            new SoundConfig
            {
                id = AudioIds.GameBgm,
                resourcePath = "Audio/BGM/game_bgm",
                category = "Bgm",
                volume = 1f,
                loop = true
            },
            new SoundConfig
            {
                id = AudioIds.ButtonClick,
                resourcePath = "Audio/SFX/button_click",
                category = "Ui",
                volume = 0.55f,
                pitchRandom = 0.03f,
                maxPlaySeconds = 0.6f,
                cooldownSeconds = 0.03f
            },
            new SoundConfig
            {
                id = AudioIds.MoneyChange,
                resourcePath = "Audio/SFX/money_change",
                category = "Sfx",
                volume = 0.65f,
                maxPlaySeconds = 1f,
                cooldownSeconds = 0.12f
            },
            new SoundConfig
            {
                id = AudioIds.PlayerStep,
                resourcePath = "Audio/SFX/step_duck",
                category = "Sfx",
                volume = 0.48f,
                pitchRandom = 0.05f,
                maxPlaySeconds = 0.32f,
                cooldownSeconds = 0.12f,
                spatialBlend = 0.15f
            },
            new SoundConfig
            {
                id = AudioIds.DiceRoll,
                resourcePath = "Audio/SFX/dice_roll",
                category = "Sfx",
                volume = 0.65f,
                maxPlaySeconds = 1.3f,
                cooldownSeconds = 0.1f
            },
            new SoundConfig
            {
                id = AudioIds.QuestionOpen,
                resourcePath = "Audio/SFX/question_open",
                category = "Sfx",
                volume = 0.62f,
                maxPlaySeconds = 1.2f,
                cooldownSeconds = 0.15f
            },
            new SoundConfig
            {
                id = AudioIds.AnswerCorrect,
                resourcePath = "Audio/SFX/answer_correct",
                category = "Sfx",
                volume = 0.7f,
                maxPlaySeconds = 1.5f,
                cooldownSeconds = 0.1f
            },
            new SoundConfig
            {
                id = AudioIds.AnswerWrong,
                resourcePath = "Audio/SFX/answer_wrong",
                category = "Sfx",
                volume = 0.7f,
                maxPlaySeconds = 1.5f,
                cooldownSeconds = 0.1f
            },
            new SoundConfig
            {
                id = AudioIds.ToolGain,
                resourcePath = "Audio/SFX/tool_gain",
                category = "Sfx",
                volume = 0.7f,
                maxPlaySeconds = 1.2f,
                cooldownSeconds = 0.12f
            },
            new SoundConfig
            {
                id = AudioIds.PropertyBuy,
                resourcePath = "Audio/SFX/property_buy",
                category = "Sfx",
                volume = 0.72f,
                maxPlaySeconds = 1.2f,
                cooldownSeconds = 0.12f
            },
            new SoundConfig
            {
                id = AudioIds.Transition,
                resourcePath = "Audio/SFX/transition",
                category = "Sfx",
                volume = 0.65f,
                maxPlaySeconds = 1.4f,
                cooldownSeconds = 0.2f
            },
            new SoundConfig
            {
                id = AudioIds.VictoryMain,
                resourcePath = "Audio/SFX/victory_main",
                category = "Sfx",
                volume = 0.78f,
                maxPlaySeconds = 5f,
                cooldownSeconds = 0.5f
            },
            new SoundConfig
            {
                id = AudioIds.VictoryLayer,
                resourcePath = "Audio/SFX/victory_layer",
                category = "Sfx",
                volume = 0.72f,
                maxPlaySeconds = 5f,
                cooldownSeconds = 0.5f
            }
        };
    }
}
