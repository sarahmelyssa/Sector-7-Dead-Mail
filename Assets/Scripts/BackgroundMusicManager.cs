using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Mantem musica de fundo entre cenas e faz crossfade entre menu, briefing
/// e gameplay para evitar cortes bruscos.
/// </summary>
[DisallowMultipleComponent]
public class BackgroundMusicManager : MonoBehaviour
{
    public static BackgroundMusicManager Instance { get; private set; }

    private const string MenuMusicResourcePath = "Audio/Music/music_menu_loop";
    private const string GameplayMusicResourcePath = "Audio/Music/factory_sound";
    private const string LegacyGameplayMusicResourcePath = "Audio/Music/music_gameplay_user_loop";
    private const string DefaultGameplayMusicResourcePath = "Audio/Music/music_gameplay_loop";
    private const string BackgroundLoopResourcePath = "Audio/Music/music_background_loop";

    [Header("Sources")]
    [SerializeField] private AudioSource primarySource = null;
    [SerializeField] private AudioSource secondarySource = null;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.78f;
    [SerializeField, Range(0f, 1f)] private float menuTrackVolume = 0.22f;
    [SerializeField, Range(0f, 1f)] private float gameplayTrackVolume = 0.32f;
    [SerializeField] private float fadeDuration = 1.35f;

    [Header("Clips")]
    [SerializeField] private AudioClip menuMusicClip = null;
    [SerializeField] private AudioClip gameplayMusicClip = null;

    private AudioClip generatedMenuMusicClip;
    private AudioClip generatedGameplayMusicClip;
    private AudioClip backgroundLoopClip;
    private AudioSource activeSource;
    private AudioSource idleSource;
    private Coroutine fadeRoutine;
    private bool currentSceneIsMenu;
    private bool musicSuppressed;
    private bool keepBriefingMusicForNextScene;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        // Criado antes da cena carregar para a musica nao reiniciar entre menus/telas.
        if (Instance != null || Object.FindFirstObjectByType<BackgroundMusicManager>() != null)
        {
            return;
        }

        var managerObject = new GameObject("BackgroundMusicManager");
        managerObject.AddComponent<BackgroundMusicManager>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        primarySource = primarySource != null ? primarySource : gameObject.AddComponent<AudioSource>();
        secondarySource = secondarySource != null ? secondarySource : gameObject.AddComponent<AudioSource>();
        ConfigureMusicSource(primarySource);
        ConfigureMusicSource(secondarySource);

        activeSource = primarySource;
        idleSource = secondarySource;

        LoadClipsIfNeeded();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        ApplySceneMusic(SceneManager.GetActiveScene(), true);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Instance = null;
        }
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        RefreshActiveVolume();
    }

    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        RefreshActiveVolume();
    }

    public void SetMusicSuppressed(bool suppressed)
    {
        if (musicSuppressed == suppressed)
        {
            return;
        }

        musicSuppressed = suppressed;
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        if (idleSource != null)
        {
            idleSource.Stop();
            idleSource.volume = 0f;
        }

        RefreshActiveVolume();
    }

    public void PlayBriefingMusic(bool immediate = false)
    {
        currentSceneIsMenu = true;
        AudioClip targetClip = menuMusicClip != null ? menuMusicClip : generatedMenuMusicClip;
        PlayRequestedTrack(targetClip, immediate);
    }

    public void PlayGameplayMusic(bool immediate = false)
    {
        keepBriefingMusicForNextScene = false;
        currentSceneIsMenu = false;
        AudioClip targetClip = gameplayMusicClip != null ? gameplayMusicClip : generatedGameplayMusicClip;
        PlayRequestedTrack(targetClip, immediate);
    }

    public void KeepBriefingMusicForNextScene()
    {
        keepBriefingMusicForNextScene = true;
        PlayBriefingMusic(false);
    }

    public void FadeCurrentMusicToSilence(float duration)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        fadeRoutine = StartCoroutine(FadeAllSourcesToSilence(duration));
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ApplySceneMusic(scene, false);
    }

    private void ApplySceneMusic(Scene scene, bool immediate)
    {
        // Briefing usa a musica do menu; gameplay usa o loop industrial da fase.
        bool sceneIsMenu = scene.name.ToLowerInvariant().Contains("menu");
        bool sceneIsBriefing = keepBriefingMusicForNextScene || (!sceneIsMenu
            && NightStoryManager.Instance != null
            && NightStoryManager.Instance.ShouldDelayGameplayStart());
        currentSceneIsMenu = sceneIsMenu || sceneIsBriefing;
        AudioClip targetClip = currentSceneIsMenu
            ? (menuMusicClip != null ? menuMusicClip : generatedMenuMusicClip)
            : (gameplayMusicClip != null ? gameplayMusicClip : generatedGameplayMusicClip);

        PlayRequestedTrack(targetClip, immediate);
        keepBriefingMusicForNextScene = false;
    }

    private void PlayRequestedTrack(AudioClip targetClip, bool immediate)
    {
        if (targetClip == null)
        {
            return;
        }

        float targetVolume = GetTargetVolume();
        if (activeSource != null && activeSource.clip == targetClip && activeSource.isPlaying)
        {
            activeSource.volume = targetVolume;
            return;
        }

        CrossfadeTo(targetClip, targetVolume, immediate);
    }

    private void CrossfadeTo(AudioClip clip, float targetVolume, bool immediate)
    {
        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
            fadeRoutine = null;
        }

        // Duas fontes permitem uma musica baixar enquanto a outra entra.
        AudioSource nextSource = idleSource != null && idleSource != activeSource ? idleSource : secondarySource;
        AudioSource previousSource = activeSource;

        nextSource.clip = clip;
        nextSource.volume = immediate ? targetVolume : 0f;
        nextSource.loop = true;
        nextSource.Play();

        activeSource = nextSource;
        idleSource = previousSource;

        if (immediate)
        {
            if (idleSource != null)
            {
                idleSource.Stop();
                idleSource.volume = 0f;
            }

            return;
        }

        fadeRoutine = StartCoroutine(FadeSources(previousSource, nextSource, targetVolume));
    }

    private IEnumerator FadeSources(AudioSource previousSource, AudioSource nextSource, float targetVolume)
    {
        float duration = Mathf.Max(0.05f, fadeDuration);
        float elapsed = 0f;
        float previousStartVolume = previousSource != null ? previousSource.volume : 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);

            if (previousSource != null)
            {
                previousSource.volume = Mathf.Lerp(previousStartVolume, 0f, eased);
            }

            if (nextSource != null)
            {
                nextSource.volume = Mathf.Lerp(0f, targetVolume, eased);
            }

            yield return null;
        }

        if (previousSource != null)
        {
            previousSource.Stop();
            previousSource.volume = 0f;
        }

        if (nextSource != null)
        {
            nextSource.volume = targetVolume;
        }

        fadeRoutine = null;
    }

    private IEnumerator FadeAllSourcesToSilence(float duration)
    {
        float fadeDuration = Mathf.Max(0.05f, duration);
        float elapsed = 0f;
        float primaryStartVolume = primarySource != null ? primarySource.volume : 0f;
        float secondaryStartVolume = secondarySource != null ? secondarySource.volume : 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = t * t * (3f - 2f * t);

            if (primarySource != null)
            {
                primarySource.volume = Mathf.Lerp(primaryStartVolume, 0f, eased);
            }

            if (secondarySource != null)
            {
                secondarySource.volume = Mathf.Lerp(secondaryStartVolume, 0f, eased);
            }

            yield return null;
        }

        if (primarySource != null)
        {
            primarySource.volume = 0f;
        }

        if (secondarySource != null)
        {
            secondarySource.volume = 0f;
        }

        fadeRoutine = null;
    }

    private void RefreshActiveVolume()
    {
        if (activeSource != null)
        {
            activeSource.volume = GetTargetVolume();
        }
    }

    private float GetTargetVolume()
    {
        if (musicSuppressed)
        {
            return 0f;
        }

        float trackVolume = currentSceneIsMenu ? menuTrackVolume : gameplayTrackVolume;
        return masterVolume * musicVolume * trackVolume;
    }

    private void ConfigureMusicSource(AudioSource source)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.priority = 96;
        source.ignoreListenerPause = true;
        source.volume = 0f;
    }

    private void LoadClipsIfNeeded()
    {
        generatedMenuMusicClip = CreateGeneratedMusicClip("Generated Menu Music Loop", true);
        generatedGameplayMusicClip = CreateGeneratedMusicClip("Generated Gameplay Music Loop", false);
        backgroundLoopClip = Resources.Load<AudioClip>(BackgroundLoopResourcePath);
        menuMusicClip = menuMusicClip != null ? menuMusicClip : backgroundLoopClip != null ? backgroundLoopClip : Resources.Load<AudioClip>(MenuMusicResourcePath);
        if (gameplayMusicClip == null)
        {
            gameplayMusicClip = Resources.Load<AudioClip>(GameplayMusicResourcePath);
        }

        if (gameplayMusicClip == null)
        {
            gameplayMusicClip = Resources.Load<AudioClip>(LegacyGameplayMusicResourcePath);
        }

        if (gameplayMusicClip == null)
        {
            gameplayMusicClip = Resources.Load<AudioClip>(DefaultGameplayMusicResourcePath);
        }
    }

    private AudioClip CreateGeneratedMusicClip(string clipName, bool menu)
    {
        const int sampleRate = 44100;
        const float duration = 8f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        float root = menu ? 43.65f : 36.71f;
        float pulseRate = menu ? 0.46f : 0.36f;
        float musicBoxRoot = menu ? 659.25f : 493.88f;
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float phase = Mathf.Repeat(time / duration, 1f);
            float edge = Mathf.Min(1f, Mathf.Min(phase, 1f - phase) * 18f);
            edge = edge * edge * (3f - 2f * edge);
            float pulse = 0.58f + Mathf.Sin(2f * Mathf.PI * pulseRate * time) * 0.42f;
            float bass = Mathf.Sin(2f * Mathf.PI * root * time) * 0.043f;
            float detunedBass = Mathf.Sin(2f * Mathf.PI * (root * 1.012f) * time) * 0.034f;
            float fifth = Mathf.Sin(2f * Mathf.PI * root * 1.5f * time) * 0.015f;
            float musicBoxEnvelope = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * (menu ? 0.25f : 0.18f) * time)), 12f);
            float musicBox = Mathf.Sin(2f * Mathf.PI * musicBoxRoot * time) * 0.040f * musicBoxEnvelope;
            float musicBoxGhost = Mathf.Sin(2f * Mathf.PI * musicBoxRoot * 1.122f * time) * 0.023f * musicBoxEnvelope;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, menu ? 0.008f : 0.012f);
            float texture = previousNoise * (menu ? 0.009f : 0.015f);
            samples[i] = Mathf.Clamp(((bass + detunedBass + fifth) * pulse + musicBox + musicBoxGhost + texture) * edge, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
