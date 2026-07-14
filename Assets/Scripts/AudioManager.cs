using System.Collections;
using UnityEngine;

/// <summary>
/// Centraliza os efeitos sonoros da gameplay: caixas, botoes, reports,
/// ambiente, corredor, fita cassete e feedback de acerto/erro.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource audioSource = null;
    [SerializeField] private AudioSource spatialSfxSource = null;
    [SerializeField] private AudioSource reportCassetteSource = null;
    [SerializeField] private AudioSource reportSfxSource = null;
    [SerializeField] private AudioSource conveyorLoopSource = null;
    [SerializeField] private AudioSource uiSource = null;
    [SerializeField] private AudioSource voiceSource = null;
    [SerializeField] private AudioSource briefingStaticSource = null;

    [Header("Mix")]
    [SerializeField, Range(0f, 1f)] private float masterVolume = 1f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.84f;
    [SerializeField, Range(0f, 1f)] private float ambienceVolume = 0.24f;
    [SerializeField, Range(0f, 1f)] private float uiVolume = 0.74f;
    [SerializeField, Range(0f, 1f)] private float voiceVolume = 0.92f;
    [SerializeField, Range(0f, 1f)] private float reportCassetteVolume = 0.48f;
    [SerializeField, Range(0f, 1f)] private float briefingStaticVolume = 0.30f;
    [SerializeField, Range(0.05f, 1f)] private float ambienceDuckDuringBriefing = 0.42f;
    [SerializeField, Range(0f, 1f)] private float conveyorLoopVolume = 0.58f;
    [SerializeField, Range(0f, 1f)] private float corridorIdleVolume = 0.025f;
    [SerializeField, Range(0f, 1f)] private float corridorFocusedVolume = 0.27f;
    [SerializeField] private float corridorFadeSpeed = 1.65f;

    [Header("Package")]
    [SerializeField] private AudioClip packageGeneratedClip = null;
    [SerializeField] private AudioClip packageArrivedClip = null;
    [SerializeField] private AudioClip packageExitClip = null;
    [SerializeField] private AudioClip reportOpenClip = null;
    [SerializeField] private AudioClip reportCloseClip = null;
    [SerializeField, Range(0f, 0.55f)] private float reportSoundStartOffset = 0.38f;
    [SerializeField] private AudioClip acceptButtonClip = null;
    [SerializeField] private AudioClip rejectButtonClip = null;
    [SerializeField] private AudioClip physicalButtonClickClip = null;
    [SerializeField] private AudioClip correctResponseClip = null;
    [SerializeField] private AudioClip wrongResponseClip = null;

    [Header("Threats")]
    [SerializeField] private AudioClip doorCreakClip = null;
    [SerializeField] private AudioClip anomalyActivatedClip = null;
    [SerializeField] private AudioClip mobAppearsClip = null;
    [SerializeField] private AudioClip mobDisappearsClip = null;
    [SerializeField] private AudioClip damageReceivedClip = null;
    [SerializeField] private AudioClip lightFlickerClip = null;
    [SerializeField] private AudioClip breathingBehindClip = null;
    [SerializeField] private AudioClip whistleCueClip = null;
    [SerializeField] private AudioClip lookBackClip = null;
    [SerializeField] private AudioClip lookForwardClip = null;
    [SerializeField] private AudioClip[] childLaughClips = null;
    [SerializeField] private AudioClip[] randomKnockClips = null;

    [Header("Game State")]
    [SerializeField] private AudioClip gameOverClip = null;
    [SerializeField] private AudioClip victoryClip = null;

    [Header("UI")]
    [SerializeField] private AudioClip uiButtonHoverClip = null;
    [SerializeField] private AudioClip uiButtonClickClip = null;
    [SerializeField] private AudioClip uiPauseClip = null;

    [Header("Cassette Voice")]
    [SerializeField] private AudioClip cassetteInsertClip = null;
    [SerializeField] private AudioClip cassettePlayClip = null;
    [SerializeField] private AudioClip cassetteStopClip = null;
    [SerializeField] private AudioClip staticNoiseClip = null;
    [SerializeField] private AudioClip phoneGuyCassetteVoiceClip = null;
    [SerializeField] private float phoneVoiceLowPassCutoff = 2600f;

    [Header("Ambience")]
    [SerializeField] private AudioSource ambienceSource = null;
    [SerializeField] private AudioSource corridorSource = null;
    [SerializeField] private AudioClip roomAmbienceClip = null;
    [SerializeField] private AudioClip corridorAmbienceClip = null;
    [SerializeField] private AudioClip reportCassetteLoopClip = null;
    [SerializeField] private AudioClip conveyorLoopClip = null;

    private AudioClip[] generatedKnockClips;
    private AudioClip[] generatedChildLaughClips;
    private AudioClip generatedButtonClickClip;
    private AudioClip generatedPackageMoveClip;
    private AudioClip generatedPackageArrivedClip;
    private AudioClip generatedReportCassetteLoopClip;
    private AudioClip generatedConveyorLoopClip;
    private AudioClip generatedReportOpenClip;
    private AudioClip generatedReportCloseClip;
    private AudioClip generatedLightFlickerClip;
    private AudioClip generatedAnomalyActivatedClip;
    private AudioClip generatedRoomAmbienceClip;
    private AudioClip generatedCorridorAmbienceClip;
    private AudioClip generatedCorrectResponseClip;
    private AudioClip generatedWrongResponseClip;
    private AudioClip generatedLookBackClip;
    private AudioClip generatedLookForwardClip;
    private AudioClip generatedBreathingClip;
    private AudioClip generatedWhistleClip;
    private AudioClip generatedUiHoverClip;
    private AudioClip generatedCassetteInsertClip;
    private AudioClip generatedCassettePlayClip;
    private AudioClip generatedCassetteStopClip;
    private AudioClip generatedStaticNoiseClip;
    private float corridorTargetVolume;
    private float nextChildLaughTime;
    private float briefingCassetteFade = 1f;
    private bool briefingCassetteActive;
    private Coroutine briefingCassetteRoutine;
    private AudioLowPassFilter voiceLowPassFilter;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        Configure2DSource(audioSource);
        LoadDefaultProjectClipsIfNeeded();

        // Fallbacks gerados em codigo: se algum asset faltar, o jogo ainda tem som.
        generatedKnockClips = new[]
        {
            CreateKnockClip("Generated Knock Low", 72f),
            CreateKnockClip("Generated Knock Mid", 91f),
            CreateKnockClip("Generated Knock Hard", 116f)
        };
        generatedChildLaughClips = new[]
        {
            CreateChildLaughClip("Generated Child Laugh Far", 0.72f, 0.75f),
            CreateChildLaughClip("Generated Child Laugh Broken", 0.96f, 0.58f)
        };
        generatedButtonClickClip = CreateButtonClickClip();
        generatedPackageMoveClip = CreateNoiseClip("Generated Package Move", 0.46f, 0.12f, 7f);
        generatedPackageArrivedClip = CreateImpactClip();
        generatedReportCassetteLoopClip = CreateCassetteLoopClip();
        generatedConveyorLoopClip = CreateConveyorLoopClip();
        generatedReportOpenClip = CreatePaperFlipClip("Generated Report Paper Open", 0.34f, 1f);
        generatedReportCloseClip = CreatePaperFlipClip("Generated Report Paper Close", 0.24f, 0.82f);
        generatedLightFlickerClip = CreateNoiseClip("Generated Light Flicker", 0.05f, 0.05f, 95f);
        generatedAnomalyActivatedClip = CreateAnomalyStingerClip();
        generatedRoomAmbienceClip = CreateHumClip();
        generatedCorridorAmbienceClip = CreateCorridorHumClip();
        generatedCorrectResponseClip = CreateDecisionToneClip("Generated Correct Response", 270f, 0.14f, false);
        generatedWrongResponseClip = CreateDecisionToneClip("Generated Wrong Response", 82f, 0.36f, true);
        generatedLookBackClip = CreateLookTurnClip("Generated Look Back Turn", 0.72f, 0.65f);
        generatedLookForwardClip = CreateLookTurnClip("Generated Look Forward Turn", 0.46f, 0.38f);
        generatedBreathingClip = CreateBreathingClip();
        generatedWhistleClip = CreateWhistleClip();
        generatedUiHoverClip = CreateDecisionToneClip("Generated UI Hover", 620f, 0.055f, false);
        generatedCassetteInsertClip = CreateCassetteMechanicClip("Generated Cassette Insert", 0.42f, 0.86f, true);
        generatedCassettePlayClip = CreateCassetteMechanicClip("Generated Cassette Play", 0.22f, 0.52f, false);
        generatedCassetteStopClip = CreateCassetteMechanicClip("Generated Cassette Stop", 0.20f, 0.48f, false);
        generatedStaticNoiseClip = CreateStaticNoiseLoopClip();

        StartRoomAmbience();
        StartCorridorAmbience();
        ScheduleNextChildLaugh(8f, 22f);
    }

    private void Update()
    {
        ApplyLoopVolumes();
        TryPlayChildLaugh();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void PlayPackageGenerated()
    {
        Play(packageGeneratedClip != null ? packageGeneratedClip : generatedPackageMoveClip, 0.34f);
    }

    public void PlayPackageGenerated(Vector3 position)
    {
        // Som 3D para a caixa parecer vir da mesa/esteira.
        PlaySpatial(packageGeneratedClip != null ? packageGeneratedClip : generatedPackageMoveClip, position, 0.34f, 0.65f, 5.5f);
    }

    public void PlayPackageArrived()
    {
        Play(packageArrivedClip != null ? packageArrivedClip : generatedPackageArrivedClip, 0.55f);
    }

    public void PlayPackageArrived(Vector3 position)
    {
        PlaySpatial(packageArrivedClip != null ? packageArrivedClip : generatedPackageArrivedClip, position, 0.55f, 0.65f, 5.5f);
    }

    public void PlayPackageExiting()
    {
        Play(packageExitClip != null ? packageExitClip : generatedPackageMoveClip, 0.40f);
    }

    public void PlayPackageExiting(Vector3 position)
    {
        PlaySpatial(packageExitClip != null ? packageExitClip : generatedPackageMoveClip, position, 0.40f, 0.65f, 5.5f);
    }

    public void PlayReportOpen()
    {
        PlayReportSound(reportOpenClip != null ? reportOpenClip : generatedReportOpenClip, 0.78f);
    }

    public void PlayReportClose()
    {
        PlayReportSound(reportCloseClip != null ? reportCloseClip : generatedReportCloseClip, 0.58f);
    }

    public void PlayAcceptButton()
    {
        AudioClip clip = acceptButtonClip != null ? acceptButtonClip : physicalButtonClickClip != null ? physicalButtonClickClip : generatedButtonClickClip;
        Play(clip, 0.68f);
    }

    public void PlayRejectButton()
    {
        AudioClip clip = rejectButtonClip != null ? rejectButtonClip : physicalButtonClickClip != null ? physicalButtonClickClip : generatedButtonClickClip;
        Play(clip, 0.68f);
    }

    public void PlayButtonClick()
    {
        Play(physicalButtonClickClip != null ? physicalButtonClickClip : generatedButtonClickClip, 0.72f);
    }

    public void PlayCorrectResponse()
    {
        Play(correctResponseClip != null ? correctResponseClip : generatedCorrectResponseClip, 0.88f);
    }

    public void PlayWrongResponse()
    {
        Play(wrongResponseClip != null ? wrongResponseClip : generatedWrongResponseClip, 0.82f);
    }

    public void PlayDoorCreak()
    {
        PlayAtCameraRear(doorCreakClip, 0.46f);
    }

    public void PlayDoorCreak(Vector3 position)
    {
        PlaySpatial(doorCreakClip, position, 0.46f, 0.75f, 8f);
    }

    public void PlayAnomalyActivated()
    {
    }

    public void PlayMobAppears()
    {
        Play(mobAppearsClip);
    }

    public void PlayMobDisappears()
    {
        Play(mobDisappearsClip);
    }

    public void PlayDamageReceived()
    {
        Play(damageReceivedClip);
    }

    public void PlayLightFlicker()
    {
    }

    public void PlayLightFlicker(Vector3 position)
    {
    }

    public void PlayBreathingBehind()
    {
        PlayAtCameraRear(breathingBehindClip != null ? breathingBehindClip : generatedBreathingClip, 0.72f);
    }

    public void PlayWhistleCue()
    {
        PlayAtCameraRear(whistleCueClip != null ? whistleCueClip : generatedWhistleClip, 0.58f);
    }

    public void PlayLookTurn(bool lookingBack)
    {
        SetCorridorFocus(lookingBack);
        AudioClip clip = lookingBack
            ? (lookBackClip != null ? lookBackClip : generatedLookBackClip)
            : (lookForwardClip != null ? lookForwardClip : generatedLookForwardClip);
        PlayNearCamera(clip, lookingBack ? 0.48f : 0.32f);
    }

    public void SetCorridorFocus(bool focused)
    {
        corridorTargetVolume = focused ? corridorFocusedVolume : corridorIdleVolume;
    }

    public void SetMasterVolume(float volume)
    {
        masterVolume = Mathf.Clamp01(volume);
        ApplyLoopVolumes();
    }

    public void SetSfxVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public void SetUiVolume(float volume)
    {
        uiVolume = Mathf.Clamp01(volume);
    }

    public void SetVoiceVolume(float volume)
    {
        voiceVolume = Mathf.Clamp01(volume);
        ApplyLoopVolumes();
    }

    public void SetAmbienceVolume(float volume)
    {
        ambienceVolume = Mathf.Clamp01(volume);
        ApplyLoopVolumes();
    }

    public void PlaySFX(AudioClip clip)
    {
        Play(clip, 1f);
    }

    public void PlayUISound(AudioClip clip)
    {
        PlayUi(clip, 1f);
    }

    public void PlayUiHover()
    {
        PlayUi(uiButtonHoverClip != null ? uiButtonHoverClip : generatedUiHoverClip, 0.42f);
    }

    public void PlayUiClick()
    {
        PlayUi(uiButtonClickClip != null ? uiButtonClickClip : generatedButtonClickClip, 0.78f);
    }

    public void PlayUiPause()
    {
        PlayUi(uiPauseClip != null ? uiPauseClip : generatedCassetteStopClip, 0.72f);
    }

    public void PlayVoice(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        EnsureVoiceSource();
        voiceSource.Stop();
        voiceSource.clip = clip;
        voiceSource.loop = false;
        voiceSource.volume = masterVolume * voiceVolume * briefingCassetteFade;
        voiceSource.Play();
    }

    public void StopVoice()
    {
        if (voiceSource != null)
        {
            voiceSource.Stop();
        }
    }

    public void PlayAmbience(AudioClip clip, bool loop = true)
    {
        if (clip == null)
        {
            return;
        }

        if (ambienceSource == null)
        {
            ambienceSource = CreateChildSource("Room Ambience Source", false);
        }

        ambienceSource.clip = clip;
        ambienceSource.loop = loop;
        ambienceSource.Play();
        ApplyLoopVolumes();
    }

    public void StopAmbience()
    {
        if (ambienceSource != null)
        {
            ambienceSource.Stop();
        }
    }

    public void PlayBriefingCassette()
    {
        if (briefingCassetteRoutine != null)
        {
            StopCoroutine(briefingCassetteRoutine);
            briefingCassetteRoutine = null;
        }

        StopBriefingCassette(false);
        briefingCassetteRoutine = StartCoroutine(PlayBriefingCassetteRoutine());
    }

    public void SetBriefingCassetteFade(float fade)
    {
        briefingCassetteFade = Mathf.Clamp01(fade);
        ApplyLoopVolumes();
    }

    public void StopBriefingCassette()
    {
        StopBriefingCassette(true);
    }

    public void StartReportCassetteLoop()
    {
        if (reportCassetteSource == null)
        {
            reportCassetteSource = CreateChildSource("Report Cassette Source", false);
        }

        reportCassetteSource.clip = reportCassetteLoopClip != null
            ? reportCassetteLoopClip
            : staticNoiseClip != null ? staticNoiseClip : generatedReportCassetteLoopClip;
        reportCassetteSource.loop = true;
        reportCassetteSource.spatialBlend = 0f;
        reportCassetteSource.volume = ScaleSfxVolume(reportCassetteVolume);

        if (reportCassetteSource.clip != null && !reportCassetteSource.isPlaying)
        {
            reportCassetteSource.Play();
        }
    }

    public void StopReportCassetteLoop()
    {
        if (reportCassetteSource != null && reportCassetteSource.isPlaying)
        {
            reportCassetteSource.Stop();
        }
    }

    private IEnumerator PlayBriefingCassetteRoutine()
    {
        briefingCassetteActive = true;
        briefingCassetteFade = 1f;
        BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
        ApplyLoopVolumes();

        AudioClip insert = cassetteInsertClip != null ? cassetteInsertClip : generatedCassetteInsertClip;
        Play(insert, 0.82f);
        yield return new WaitForSecondsRealtime(GetShortClipDelay(insert, 0.38f));

        AudioClip play = cassettePlayClip != null ? cassettePlayClip : generatedCassettePlayClip;
        Play(play, 0.58f);
        StartBriefingStaticLoop();

        yield return new WaitForSecondsRealtime(GetShortClipDelay(play, 0.18f) * 0.55f);

        AudioClip voiceClip = phoneGuyCassetteVoiceClip;
        if (voiceClip != null)
        {
            PlayVoice(voiceClip);
            while (briefingCassetteActive && voiceSource != null && voiceSource.isPlaying)
            {
                yield return null;
            }

            if (briefingCassetteActive && briefingStaticSource != null)
            {
                briefingStaticSource.volume = ScaleSfxVolume(briefingStaticVolume * 0.58f * briefingCassetteFade);
            }
        }

        briefingCassetteRoutine = null;
    }

    private void StartBriefingStaticLoop()
    {
        if (briefingStaticSource == null)
        {
            briefingStaticSource = CreateChildSource("Briefing Static Source", false);
        }

        briefingStaticSource.clip = staticNoiseClip != null
            ? staticNoiseClip
            : reportCassetteLoopClip != null ? reportCassetteLoopClip : generatedStaticNoiseClip;
        briefingStaticSource.loop = true;
        briefingStaticSource.spatialBlend = 0f;
        briefingStaticSource.volume = ScaleSfxVolume(briefingStaticVolume * briefingCassetteFade);

        if (briefingStaticSource.clip != null && !briefingStaticSource.isPlaying)
        {
            briefingStaticSource.Play();
        }
    }

    private void StopBriefingCassette(bool playStopSound)
    {
        if (briefingCassetteRoutine != null)
        {
            StopCoroutine(briefingCassetteRoutine);
            briefingCassetteRoutine = null;
        }

        bool wasActive = briefingCassetteActive
            || (briefingStaticSource != null && briefingStaticSource.isPlaying)
            || (voiceSource != null && voiceSource.isPlaying);

        briefingCassetteActive = false;
        briefingCassetteFade = 1f;

        if (briefingStaticSource != null)
        {
            briefingStaticSource.Stop();
        }

        if (voiceSource != null)
        {
            voiceSource.Stop();
        }

        BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
        ApplyLoopVolumes();

        if (playStopSound && wasActive)
        {
            Play(cassetteStopClip != null ? cassetteStopClip : generatedCassetteStopClip, 0.52f);
        }
    }

    private float GetShortClipDelay(AudioClip clip, float fallback)
    {
        if (clip == null)
        {
            return fallback;
        }

        return Mathf.Clamp(clip.length, 0.08f, 0.85f);
    }

    public void StartConveyorLoop(Vector3 position)
    {
        if (conveyorLoopSource == null)
        {
            conveyorLoopSource = CreateChildSource("Conveyor Loop Source", true);
        }

        conveyorLoopSource.transform.position = position;
        conveyorLoopSource.clip = conveyorLoopClip != null ? conveyorLoopClip : generatedConveyorLoopClip;
        conveyorLoopSource.loop = true;
        conveyorLoopSource.spatialBlend = 0.75f;
        conveyorLoopSource.minDistance = 0.75f;
        conveyorLoopSource.maxDistance = 6.5f;
        conveyorLoopSource.volume = ScaleSfxVolume(conveyorLoopVolume);

        if (conveyorLoopSource.clip != null && !conveyorLoopSource.isPlaying)
        {
            conveyorLoopSource.Play();
        }
    }

    public void UpdateConveyorLoopPosition(Vector3 position)
    {
        if (conveyorLoopSource != null)
        {
            conveyorLoopSource.transform.position = position;
        }
    }

    public void StopConveyorLoop()
    {
        if (conveyorLoopSource != null && conveyorLoopSource.isPlaying)
        {
            conveyorLoopSource.Stop();
        }
    }

    public void PlayRandomKnock(int dangerLevel)
    {
        if (audioSource == null)
        {
            return;
        }

        AudioClip clip = PickClip(randomKnockClips);
        if (clip == null)
        {
            clip = PickClip(generatedKnockClips);
        }

        if (clip == null)
        {
            return;
        }

        float volume = Mathf.Clamp01(0.45f + dangerLevel * 0.08f);
        float pitch = Random.Range(0.86f, 1.08f);
        float previousPitch = audioSource.pitch;
        audioSource.pitch = pitch;
        audioSource.PlayOneShot(clip, ScaleSfxVolume(volume));
        audioSource.pitch = previousPitch;
    }

    public void PlayGameOver()
    {
        StopActiveGameplayLoopsForEnding();
        PlayUi(gameOverClip != null ? gameOverClip : generatedWrongResponseClip, 0.88f);
    }

    public void PlayVictory()
    {
        StopActiveGameplayLoopsForEnding();
        PlayUi(victoryClip != null ? victoryClip : generatedCorrectResponseClip, 0.74f);
    }

    private void Play(AudioClip clip)
    {
        Play(clip, 1f);
    }

    private void Play(AudioClip clip, float volume)
    {
        if (audioSource == null || clip == null)
        {
            return;
        }

        audioSource.PlayOneShot(clip, ScaleSfxVolume(volume));
    }

    private void PlayReportSound(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (reportSfxSource == null)
        {
            reportSfxSource = CreateChildSource("Report SFX Source", false);
        }

        reportSfxSource.Stop();
        reportSfxSource.clip = clip;
        reportSfxSource.loop = false;
        reportSfxSource.volume = ScaleSfxVolume(volume);
        float offset = Mathf.Clamp(reportSoundStartOffset, 0f, Mathf.Max(0f, clip.length - 0.01f));
        if (offset > 0f)
        {
            reportSfxSource.time = offset;
        }

        reportSfxSource.Play();
    }

    private void PlayUi(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        if (uiSource == null)
        {
            uiSource = CreateChildSource("UI Audio Source", false);
        }

        uiSource.PlayOneShot(clip, ScaleUiVolume(volume));
    }

    private void StopActiveGameplayLoopsForEnding()
    {
        StopConveyorLoop();
        StopReportCassetteLoop();
        StopBriefingCassette(false);
        SetCorridorFocus(false);
    }

    private void PlayAtCameraRear(AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            Play(clip, volume);
            return;
        }

        Vector3 rearPosition = camera.transform.position - camera.transform.forward * 0.45f;
        PlaySpatial(clip, rearPosition, volume, 0.25f, 4.5f);
    }

    private void PlayNearCamera(AudioClip clip, float volume)
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            Play(clip, volume);
            return;
        }

        PlaySpatial(clip, camera.transform.position, volume, 0.15f, 3.5f);
    }

    private void PlaySpatial(AudioClip clip, Vector3 position, float volume, float minDistance, float maxDistance)
    {
        if (clip == null)
        {
            return;
        }

        if (spatialSfxSource == null)
        {
            spatialSfxSource = CreateChildSource("Spatial SFX Source", true);
        }

        spatialSfxSource.transform.position = position;
        spatialSfxSource.minDistance = minDistance;
        spatialSfxSource.maxDistance = maxDistance;
        spatialSfxSource.PlayOneShot(clip, ScaleSfxVolume(volume));
    }

    private void StartRoomAmbience()
    {
        if (ambienceSource == null)
        {
            ambienceSource = CreateChildSource("Room Ambience Source", false);
        }

        // Ambiente 2D discreto para manter tensao constante durante a partida.
        ambienceSource.clip = roomAmbienceClip != null ? roomAmbienceClip : generatedRoomAmbienceClip;
        ambienceSource.loop = true;
        ambienceSource.playOnAwake = false;
        ambienceSource.spatialBlend = 0f;

        if (ambienceSource.clip != null && !ambienceSource.isPlaying)
        {
            ambienceSource.Play();
        }

        ApplyLoopVolumes();
    }

    private void StartCorridorAmbience()
    {
        if (corridorSource == null)
        {
            corridorSource = CreateChildSource("Corridor Ambience Source", true);
        }

        corridorSource.transform.position = new Vector3(0f, 1.45f, -7.35f);
        corridorSource.clip = corridorAmbienceClip;
        corridorSource.loop = true;
        corridorSource.playOnAwake = false;
        corridorSource.minDistance = 1.0f;
        corridorSource.maxDistance = 11.5f;
        corridorSource.spatialBlend = 1f;
        corridorSource.rolloffMode = AudioRolloffMode.Linear;
        corridorTargetVolume = corridorIdleVolume;
        corridorSource.volume = masterVolume * corridorIdleVolume;

        if (corridorSource.clip != null && !corridorSource.isPlaying)
        {
            corridorSource.Play();
        }

        ApplyLoopVolumes();
    }

    private void ApplyLoopVolumes()
    {
        if (ambienceSource != null)
        {
            float duck = briefingCassetteActive ? ambienceDuckDuringBriefing : 1f;
            ambienceSource.volume = masterVolume * ambienceVolume * duck;
        }

        if (corridorSource != null)
        {
            float targetVolume = masterVolume * corridorTargetVolume;
            corridorSource.volume = Mathf.MoveTowards(corridorSource.volume, targetVolume, Time.unscaledDeltaTime * corridorFadeSpeed);
        }

        if (reportCassetteSource != null && reportCassetteSource.isPlaying)
        {
            reportCassetteSource.volume = ScaleSfxVolume(reportCassetteVolume);
        }

        if (conveyorLoopSource != null && conveyorLoopSource.isPlaying)
        {
            conveyorLoopSource.volume = ScaleSfxVolume(conveyorLoopVolume);
        }

        if (briefingStaticSource != null && briefingStaticSource.isPlaying)
        {
            briefingStaticSource.volume = ScaleSfxVolume(briefingStaticVolume * briefingCassetteFade);
        }

        if (voiceSource != null && voiceSource.isPlaying)
        {
            voiceSource.volume = masterVolume * voiceVolume * briefingCassetteFade;
        }
    }

    private AudioSource CreateChildSource(string sourceName, bool spatial)
    {
        var sourceObject = new GameObject(sourceName);
        sourceObject.transform.SetParent(transform, false);
        AudioSource source = sourceObject.AddComponent<AudioSource>();
        ConfigureSource(source, spatial);
        return source;
    }

    private void Configure2DSource(AudioSource source)
    {
        ConfigureSource(source, false);
    }

    private void ConfigureSource(AudioSource source, bool spatial)
    {
        if (source == null)
        {
            return;
        }

        source.playOnAwake = false;
        source.loop = false;
        source.volume = 1f;
        source.pitch = 1f;
        source.spatialBlend = spatial ? 1f : 0f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = spatial ? 0.35f : 1f;
        source.maxDistance = spatial ? 8f : 500f;
    }

    private float ScaleSfxVolume(float volume)
    {
        return Mathf.Clamp01(volume) * masterVolume * sfxVolume;
    }

    private float ScaleUiVolume(float volume)
    {
        return Mathf.Clamp01(volume) * masterVolume * uiVolume;
    }

    private void EnsureVoiceSource()
    {
        if (voiceSource == null)
        {
            voiceSource = CreateChildSource("Voice Cassette Source", false);
        }

        voiceSource.spatialBlend = 0f;
        voiceSource.loop = false;

        if (voiceLowPassFilter == null)
        {
            voiceLowPassFilter = voiceSource.GetComponent<AudioLowPassFilter>();
            if (voiceLowPassFilter == null)
            {
                voiceLowPassFilter = voiceSource.gameObject.AddComponent<AudioLowPassFilter>();
            }
        }

        voiceLowPassFilter.cutoffFrequency = Mathf.Clamp(phoneVoiceLowPassCutoff, 900f, 6000f);
        voiceLowPassFilter.lowpassResonanceQ = 1.18f;
    }

    private void LoadDefaultProjectClipsIfNeeded()
    {
        // Primeiro tenta usar os audios importados no projeto; se nao existirem, usa fallbacks.
        roomAmbienceClip = roomAmbienceClip != null ? roomAmbienceClip : LoadNamedProjectAudioClip("Ambience", "ambience_room_loop");
        corridorAmbienceClip = corridorAmbienceClip != null ? corridorAmbienceClip : LoadNamedProjectAudioClip("Ambience", "ambience_corridor_loop");
        packageGeneratedClip = packageGeneratedClip != null ? packageGeneratedClip : LoadNamedProjectAudioClip("SFX", "sfx_box_move", "sfx_box_slide");
        packageArrivedClip = packageArrivedClip != null ? packageArrivedClip : LoadNamedProjectAudioClip("SFX", "sfx_box_arrive");
        packageExitClip = packageExitClip != null ? packageExitClip : LoadNamedProjectAudioClip("SFX", "sfx_box_exit", "sfx_box_slide");
        AudioClip customButtonClick = LoadNamedProjectAudioClip("SFX", "sfx_button_press_custom", "dragon-studio-button-press-382713");
        AudioClip customReportBook = LoadNamedProjectAudioClip("SFX", "sfx_report_book_custom", "freesounds123-book-opening-345808");
        physicalButtonClickClip = customButtonClick != null ? customButtonClick : physicalButtonClickClip != null ? physicalButtonClickClip : LoadNamedProjectAudioClip("SFX", "sfx_button_click");
        acceptButtonClip = customButtonClick != null ? customButtonClick : acceptButtonClip != null ? acceptButtonClip : physicalButtonClickClip;
        rejectButtonClip = customButtonClick != null ? customButtonClick : rejectButtonClip != null ? rejectButtonClip : physicalButtonClickClip;
        reportOpenClip = customReportBook != null ? customReportBook : reportOpenClip != null ? reportOpenClip : LoadNamedProjectAudioClip("SFX", "sfx_report_open", "sfx_report_page", "sfx_paper_flip");
        reportCloseClip = customReportBook != null ? customReportBook : reportCloseClip != null ? reportCloseClip : LoadNamedProjectAudioClip("SFX", "sfx_report_close", "sfx_report_page", "sfx_paper_flip");
        AudioClip customCorrect = LoadNamedProjectAudioClip("SFX", "sfx_correct_custom", "chrisiex1-correct-156911");
        AudioClip customWrong = LoadNamedProjectAudioClip("SFX", "sfx_wrong_custom", "freesound_community-training-program-incorrect1-88736");
        correctResponseClip = customCorrect != null ? customCorrect : correctResponseClip != null ? correctResponseClip : LoadNamedProjectAudioClip("SFX", "sfx_success");
        wrongResponseClip = customWrong != null ? customWrong : wrongResponseClip != null ? wrongResponseClip : LoadNamedProjectAudioClip("SFX", "sfx_error");
        doorCreakClip = doorCreakClip != null ? doorCreakClip : LoadNamedProjectAudioClip("SFX", "sfx_door_creak_custom", "ellvdr-rechinar-de-puerta-squeaking-door-7-337113", "sfx_door_creak", "sfx_door_close");
        lookBackClip = lookBackClip != null ? lookBackClip : LoadNamedProjectAudioClip("SFX", "sfx_look_back");
        lookForwardClip = lookForwardClip != null ? lookForwardClip : LoadNamedProjectAudioClip("SFX", "sfx_look_forward");
        lightFlickerClip = lightFlickerClip != null ? lightFlickerClip : LoadNamedProjectAudioClip("SFX", "sfx_light_flicker");
        anomalyActivatedClip = anomalyActivatedClip != null ? anomalyActivatedClip : LoadNamedProjectAudioClip("SFX", "sfx_anomaly_activate");
        reportCassetteLoopClip = reportCassetteLoopClip != null ? reportCassetteLoopClip : LoadNamedProjectAudioClip("SFX", "sfx_report_cassette_loop", "sfx_cassette_play");
        conveyorLoopClip = conveyorLoopClip != null ? conveyorLoopClip : LoadNamedProjectAudioClip("SFX", "sfx_conveyor_loop");
        cassetteInsertClip = cassetteInsertClip != null ? cassetteInsertClip : LoadNamedProjectAudioClip("SFX", "sfx_cassette_insert");
        cassettePlayClip = cassettePlayClip != null ? cassettePlayClip : LoadNamedProjectAudioClip("SFX", "sfx_cassette_play");
        cassetteStopClip = cassetteStopClip != null ? cassetteStopClip : LoadNamedProjectAudioClip("SFX", "sfx_cassette_stop");
        staticNoiseClip = staticNoiseClip != null ? staticNoiseClip : LoadNamedProjectAudioClip("SFX", "sfx_static_noise");
        uiButtonHoverClip = uiButtonHoverClip != null ? uiButtonHoverClip : LoadNamedProjectAudioClip("UI", "ui_button_hover");
        uiButtonClickClip = customButtonClick != null ? customButtonClick : uiButtonClickClip != null ? uiButtonClickClip : LoadNamedProjectAudioClip("UI", "ui_button_click");
        uiPauseClip = customButtonClick != null ? customButtonClick : uiPauseClip != null ? uiPauseClip : LoadNamedProjectAudioClip("UI", "ui_pause");
        gameOverClip = gameOverClip != null ? gameOverClip : LoadNamedProjectAudioClip("UI", "ui_game_over");
        victoryClip = victoryClip != null ? victoryClip : LoadNamedProjectAudioClip("UI", "ui_win");
        phoneGuyCassetteVoiceClip = phoneGuyCassetteVoiceClip != null ? phoneGuyCassetteVoiceClip : LoadNamedProjectAudioClip("Voice", "voice_phone_guy_cassette");

        if (childLaughClips == null || childLaughClips.Length == 0)
        {
            childLaughClips = new[]
            {
                LoadNamedProjectAudioClip("SFX", "sfx_child_laugh_01", "sfx_anomaly_laugh"),
                LoadNamedProjectAudioClip("SFX", "sfx_child_laugh_02", "sfx_anomaly_laugh")
            };
        }
    }

    private AudioClip LoadNamedProjectAudioClip(string folder, params string[] clipNames)
    {
        if (clipNames == null)
        {
            return null;
        }

        foreach (string clipName in clipNames)
        {
            if (string.IsNullOrWhiteSpace(clipName))
            {
                continue;
            }

            AudioClip resourceClip = Resources.Load<AudioClip>("Audio/" + folder + "/" + clipName);
            if (resourceClip != null)
            {
                return resourceClip;
            }

#if UNITY_EDITOR
            string folderPath = "Assets/Audio/" + folder;
            if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
            {
                continue;
            }

            string[] guids = UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(path);
                if (fileName == clipName)
                {
                    AudioClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
                    if (clip != null)
                    {
                        return clip;
                    }
                }
            }
#endif
        }

        return null;
    }

    private AudioClip PickClip(AudioClip[] clips)
    {
        if (clips == null || clips.Length == 0)
        {
            return null;
        }

        int validCount = 0;
        foreach (AudioClip clip in clips)
        {
            if (clip != null)
            {
                validCount++;
            }
        }

        if (validCount == 0)
        {
            return null;
        }

        int targetIndex = Random.Range(0, validCount);
        foreach (AudioClip clip in clips)
        {
            if (clip == null)
            {
                continue;
            }

            if (targetIndex == 0)
            {
                return clip;
            }

            targetIndex--;
        }

        return null;
    }

    private void TryPlayChildLaugh()
    {
        if (Time.unscaledTime < nextChildLaughTime)
        {
            return;
        }

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null && !gameManager.IsPlaying)
        {
            ScheduleNextChildLaugh(10f, 22f);
            return;
        }

        AudioClip clip = PickClip(childLaughClips);
        if (clip == null)
        {
            clip = PickClip(generatedChildLaughClips);
        }

        if (clip == null)
        {
            ScheduleNextChildLaugh(18f, 36f);
            return;
        }

        Camera camera = Camera.main;
        Vector3 position = camera != null
            ? camera.transform.position - camera.transform.forward * Random.Range(1.7f, 3.1f) + camera.transform.right * Random.Range(-1.2f, 1.2f)
            : transform.position;
        int dangerLevel = gameManager != null ? gameManager.dangerLevel : 0;
        PlaySpatial(clip, position, Mathf.Clamp01(0.24f + dangerLevel * 0.045f), 0.55f, 8f);
        ScheduleNextChildLaugh(18f, 42f);
    }

    private void ScheduleNextChildLaugh(float minimum, float maximum)
    {
        nextChildLaughTime = Time.unscaledTime + Random.Range(minimum, maximum);
    }

    private AudioClip CreateKnockClip(string clipName, float frequency)
    {
        const int sampleRate = 44100;
        const float duration = 0.24f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Exp(-time * 18f);
            float body = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope;
            float slap = Random.Range(-0.22f, 0.22f) * Mathf.Exp(-time * 70f);
            samples[i] = Mathf.Clamp((body + slap) * 0.75f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateButtonClickClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.08f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Exp(-time * 55f);
            float tone = Mathf.Sin(2f * Mathf.PI * 1200f * time) * envelope;
            samples[i] = Mathf.Clamp(tone * 0.35f, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Button Click", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateNoiseClip(string clipName, float duration, float volume, float decay)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float envelope = Mathf.Exp(-time * decay);
            samples[i] = Random.Range(-1f, 1f) * envelope * volume;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateImpactClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.22f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float thumpEnvelope = Mathf.Exp(-time * 22f);
            float scrapeEnvelope = Mathf.Exp(-time * 9f);
            float thump = Mathf.Sin(2f * Mathf.PI * 76f * time) * 0.16f * thumpEnvelope;
            float metal = Mathf.Sin(2f * Mathf.PI * 310f * time) * 0.035f * thumpEnvelope;
            float scrape = Random.Range(-1f, 1f) * 0.035f * scrapeEnvelope;
            samples[i] = Mathf.Clamp(thump + metal + scrape, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Package Arrive", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCassetteLoopClip()
    {
        const int sampleRate = 44100;
        const float duration = 3.2f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;
        float voicePhase = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.090f);
            float tapeHiss = previousNoise * 0.060f;
            float motor = Mathf.Sin(2f * Mathf.PI * 58f * time) * 0.024f;
            float wobble = Mathf.Sin(2f * Mathf.PI * (92f + Mathf.Sin(time * 4.1f) * 4f) * time) * 0.007f;
            float click = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 3.7f * time)), 22f) * 0.020f;
            float speechGate = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 2.15f * time + 0.5f)), 2.8f);
            float mouthShape = 0.65f + Mathf.PerlinNoise(time * 1.5f, 2.1f) * 0.55f;
            voicePhase += 2f * Mathf.PI * (118f + Mathf.Sin(time * 4.2f) * 16f) / sampleRate;
            float voiceFundamental = Mathf.Sin(voicePhase) * 0.022f;
            float voiceFormantA = Mathf.Sin(voicePhase * 2.16f) * 0.012f;
            float voiceFormantB = Mathf.Sin(voicePhase * 3.72f) * 0.007f;
            float consonants = previousNoise * 0.042f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 6.4f * time)), 3.2f);
            float muffledVoice = (voiceFundamental + voiceFormantA + voiceFormantB + consonants) * speechGate * mouthShape;
            samples[i] = Mathf.Clamp(tapeHiss + motor + wobble + click + muffledVoice, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Report Cassette Voice Loop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCassetteMechanicClip(string clipName, float duration, float brightness, bool insert)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / Mathf.Max(0.001f, duration);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.42f);
            float scrapeEnvelope = insert ? Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI) : Mathf.Exp(-t * 6.0f);
            float clickEnvelope = Mathf.Exp(-Mathf.Pow((t - (insert ? 0.68f : 0.18f)) * 18f, 2f));
            float clack = Mathf.Sin(2f * Mathf.PI * 116f * time) * clickEnvelope * 0.24f;
            float plastic = Mathf.Sin(2f * Mathf.PI * 760f * time) * clickEnvelope * 0.055f;
            float scrape = previousNoise * scrapeEnvelope * 0.105f;
            float motor = Mathf.Sin(2f * Mathf.PI * 58f * time) * Mathf.Exp(-t * 4.5f) * 0.025f;
            samples[i] = Mathf.Clamp((clack + plastic + scrape + motor) * brightness, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateStaticNoiseLoopClip()
    {
        const int sampleRate = 44100;
        const float duration = 2.4f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.055f);
            float hiss = previousNoise * 0.066f;
            float headHum = Mathf.Sin(2f * Mathf.PI * 60f * time) * 0.018f;
            float warble = Mathf.Sin(2f * Mathf.PI * (88f + Mathf.Sin(time * 3.8f) * 6f) * time) * 0.006f;
            samples[i] = Mathf.Clamp(hiss + headHum + warble, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Cassette Static Loop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateConveyorLoopClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.4f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.075f);
            float motor = Mathf.Sin(2f * Mathf.PI * 44f * time) * 0.050f;
            float belt = Mathf.Sin(2f * Mathf.PI * 118f * time) * 0.025f;
            float rolling = previousNoise * 0.038f;
            float clack = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 8.6f * time)), 18f) * 0.024f;
            samples[i] = Mathf.Clamp(motor + belt + rolling + clack, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Conveyor Loop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateChildLaughClip(string clipName, float duration, float pitchShift)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / Mathf.Max(0.001f, duration);
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float syllables = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 5.8f * time)), 3.4f);
            float vibrato = Mathf.Sin(2f * Mathf.PI * 7.5f * time) * 24f;
            float voice = Mathf.Sin(2f * Mathf.PI * ((520f * pitchShift) + vibrato) * time) * 0.042f;
            float throat = Mathf.Sin(2f * Mathf.PI * (230f * pitchShift) * time) * 0.026f;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.035f);
            float breath = previousNoise * 0.045f;
            samples[i] = Mathf.Clamp((voice + throat + breath) * syllables * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateAnomalyStingerClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.28f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float swell = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float hit = Mathf.Exp(-time * 9.5f);
            float tail = Mathf.Exp(-Mathf.Max(0f, time - 0.18f) * 2.2f);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.045f);
            float subDrop = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(78f, 29f, t) * time) * 0.22f * tail;
            float bell = Mathf.Sin(2f * Mathf.PI * 739f * time) * 0.045f * Mathf.Exp(-time * 3.1f);
            float detunedBell = Mathf.Sin(2f * Mathf.PI * 706f * time) * 0.035f * Mathf.Exp(-time * 2.8f);
            float staticRush = previousNoise * 0.18f * swell * tail;
            float impact = Random.Range(-1f, 1f) * 0.22f * hit;
            samples[i] = Mathf.Clamp(subDrop + bell + detunedBell + staticRush + impact, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Anomaly Activated", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateDecisionToneClip(string clipName, float frequency, float duration, bool negative)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / Mathf.Max(0.001f, duration);
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);

            if (negative)
            {
                float firstPulse = Mathf.Exp(-Mathf.Pow((t - 0.24f) * 8f, 2f));
                float secondPulse = Mathf.Exp(-Mathf.Pow((t - 0.68f) * 9f, 2f));
                float firstTone = Mathf.Sin(2f * Mathf.PI * 260f * time) * firstPulse;
                float secondTone = Mathf.Sin(2f * Mathf.PI * 145f * time) * secondPulse;
                float lowBody = Mathf.Sin(2f * Mathf.PI * 72f * time) * envelope * 0.18f;
                samples[i] = Mathf.Clamp((firstTone * 0.18f) + (secondTone * 0.20f) + lowBody, -1f, 1f);
                continue;
            }

            float tone = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope;
            float undertone = Mathf.Sin(2f * Mathf.PI * frequency * 0.5f * time) * envelope * 0.38f;
            samples[i] = Mathf.Clamp((tone * 0.13f) + (undertone * 0.08f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateLookTurnClip(string clipName, float duration, float intensity)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / Mathf.Max(0.001f, duration);
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.035f);
            float lowSweep = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(58f, 32f, t) * time) * 0.11f;
            float chairCreak = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(185f, 125f, t) * time) * 0.025f;
            float air = previousNoise * 0.105f;
            samples[i] = Mathf.Clamp((lowSweep + chairCreak + air) * envelope * intensity, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreatePaperFlipClip(string clipName, float duration, float brightness)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / Mathf.Max(0.001f, duration);
            float scrapeEnvelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float snapEnvelope = Mathf.Exp(-Mathf.Pow((t - 0.18f) * 12f, 2f)) + Mathf.Exp(-Mathf.Pow((t - 0.78f) * 15f, 2f)) * 0.65f;
            float rawNoise = Random.Range(-1f, 1f);
            float roughNoise = Mathf.Lerp(previousNoise, rawNoise, 0.48f * brightness);
            previousNoise = roughNoise;

            float lowRustle = Mathf.Sin(2f * Mathf.PI * Random.Range(55f, 95f) * time) * 0.018f * scrapeEnvelope;
            float paperScrape = roughNoise * scrapeEnvelope * 0.115f;
            float edgeSnap = rawNoise * snapEnvelope * 0.075f;
            samples[i] = Mathf.Clamp((paperScrape + edgeSnap + lowRustle) * brightness, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateHumClip()
    {
        const int sampleRate = 44100;
        const float duration = 2f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float lowHum = Mathf.Sin(2f * Mathf.PI * 46f * time) * 0.045f;
            float electric = Mathf.Sin(2f * Mathf.PI * 92f * time) * 0.018f;
            float noise = Random.Range(-0.006f, 0.006f);
            samples[i] = Mathf.Clamp(lowHum + electric + noise, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Room Ambience", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCorridorHumClip()
    {
        const int sampleRate = 44100;
        const float duration = 3.25f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.018f);
            float lowHum = Mathf.Sin(2f * Mathf.PI * 34f * time) * 0.052f;
            float electric = Mathf.Sin(2f * Mathf.PI * 68f * time) * 0.024f;
            float unstableBuzz = Mathf.Sin(2f * Mathf.PI * (121f + Mathf.Sin(time * 5.5f) * 7f) * time) * 0.010f;
            float staticBed = previousNoise * 0.020f;
            samples[i] = Mathf.Clamp(lowHum + electric + unstableBuzz + staticBed, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Corridor Ambience", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateBreathingClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.35f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.055f);
            float chest = Mathf.Sin(2f * Mathf.PI * 78f * time) * 0.028f;
            float air = previousNoise * 0.16f;
            samples[i] = Mathf.Clamp((air + chest) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Close Breathing", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateWhistleClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.05f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float vibrato = Mathf.Sin(2f * Mathf.PI * 5.5f * time) * 18f;
            float tone = Mathf.Sin(2f * Mathf.PI * (880f + vibrato) * time);
            float whisperNoise = Random.Range(-0.03f, 0.03f) * envelope;
            samples[i] = Mathf.Clamp(tone * envelope * 0.18f + whisperNoise, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Do Not Turn Whistle", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
