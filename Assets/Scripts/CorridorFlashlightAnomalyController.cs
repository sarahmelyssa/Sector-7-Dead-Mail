using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Controla o perigo do corredor: lanterna movel, pistas sonoras, olhos,
/// criatura roxa e jumpscare quando o jogador ignora a anomalia.
/// </summary>
public class CorridorFlashlightAnomalyController : MonoBehaviour
{
    private enum CorridorCueType
    {
        None,
        // Creature usa batida/hit e precisa ser encontrada com a lanterna.
        Creature,
        // Eyes usa vozes e olhos brilhantes que somem quando iluminados.
        Eyes
    }

    [Header("Flashlight")]
    [SerializeField] private Light flashlight = null;
    [SerializeField] private float flashlightIntensity = 38f;
    [SerializeField] private float flashlightRange = 8.4f;
    [SerializeField] private float flashlightSpotAngle = 22f;
    [SerializeField] private float flashlightAimSpeed = 42f;
    [SerializeField] private float flashlightAimSensitivity = 0.30f;
    [SerializeField] private float flashlightMaxAngleX = 32f;
    [SerializeField] private float flashlightMaxAngleY = 17f;
    [SerializeField] private float anomalyRevealAngle = 2.8f;
    [SerializeField] private Color flashlightColor = new Color(1.000f, 0.875f, 0.610f);
    [SerializeField] private Light[] corridorPurpleLights = null;

    [Header("Anomaly")]
    [SerializeField] private GameObject anomalyRoot = null;
    [SerializeField] private GameObject eyesRoot = null;
    [SerializeField] private Vector3 anomalyPosition = new Vector3(0.52f, 1.02f, -9.75f);
    [SerializeField] private Vector3 corridorSoundPosition = new Vector3(0f, 1.24f, -9.90f);
    [SerializeField] private Vector2 anomalyXRange = new Vector2(-0.62f, 0.62f);
    [SerializeField] private Vector2 creatureYRange = new Vector2(0.72f, 1.05f);
    [SerializeField] private Vector2 eyesYRange = new Vector2(1.10f, 1.56f);
    [SerializeField] private Vector2 anomalyZRange = new Vector2(-7.35f, -10.15f);
    [SerializeField] private float anomalyVisibleDuration = 0.48f;
    [SerializeField] private float anomalyActiveWindow = 16f;
    [SerializeField] private float lateShiftActiveWindow = 9.5f;
    [SerializeField] private float creatureActiveWindow = 7.25f;
    [SerializeField] private float lateShiftCreatureActiveWindow = 3.65f;
    [SerializeField] private int minimumDecisionsBeforeFirstCue = 2;
    [SerializeField] private int maxAnomalyCuesPerShift = 10;
    [SerializeField] private float jumpscareProgressThreshold = 0.55f;
    [SerializeField] private int ignoredCuesBeforeForcedJumpscare = 2;
    [SerializeField] private float jumpscareDelay = 0.65f;

    [Header("Timing")]
    [SerializeField] private Vector2 firstCueDelayRange = new Vector2(16f, 26f);
    [SerializeField] private Vector2 repeatCueDelayRange = new Vector2(14f, 28f);

    [Header("Audio")]
    [SerializeField] private AudioClip flashlightClickClip = null;
    [SerializeField] private AudioClip flashlightHumClip = null;
    [SerializeField] private AudioClip corridorWhisperClip = null;
    [SerializeField] private AudioClip corridorNoiseClip = null;
    [SerializeField] private AudioClip corridorVoiceLoopClip = null;
    [SerializeField] private AudioClip corridorHitClip = null;
    [SerializeField] private AudioClip slowTensionClip = null;
    [SerializeField] private AudioClip anomalyLaughClip = null;
    [SerializeField] private AudioClip[] anomalyLaughClips = null;
    [SerializeField] private AudioClip jumpscareClip = null;
    [SerializeField, Range(0f, 1f)] private float corridorVoiceMinVolume = 0.018f;
    [SerializeField, Range(0f, 1f)] private float corridorVoiceMaxVolume = 0.32f;
    [SerializeField] private float corridorVoiceRiseSpeed = 0.12f;
    [SerializeField] private float corridorVoiceFallSpeed = 0.55f;
    [SerializeField, Range(0f, 1f)] private float slowTensionVolume = 0.16f;
    [SerializeField] private float slowTensionFadeSpeed = 0.18f;

    private ViewSwitcher viewSwitcher;
    private Camera playerCamera;
    private GameManager gameManager;
    private CanvasGroup promptGroup;
    private TMP_Text promptText;
    private AudioSource corridorAudioSource;
    private AudioSource flashlightAudioSource;
    private AudioSource flashlightHumSource;
    private AudioSource corridorVoiceSource;
    private AudioSource corridorHitSource;
    private AudioSource slowTensionSource;
    private AudioClip generatedFlashlightClickClip;
    private AudioClip generatedFlashlightHumClip;
    private AudioClip generatedCorridorWhisperClip;
    private AudioClip generatedCorridorNoiseClip;
    private AudioClip generatedAnomalyLaughClip;
    private Coroutine anomalyRoutine;
    private Coroutine jumpscareRoutine;
    private FlickeringLight[] corridorPurpleFlickers;
    private Transform[] anomalyPartTransforms;
    private Vector3[] anomalyPartBasePositions;
    private Vector3[] anomalyPartBaseScales;
    private Quaternion[] anomalyPartBaseRotations;
    private Renderer[] anomalyRenderers;
    private Light anomalyGlowLight;
    private Transform[] eyeTransforms;
    private Vector3[] eyeBaseScales;
    private float nextCueTime = -1f;
    private float anomalyReadyUntil;
    private float anomalyCueStartedAt;
    private float targetAimYaw;
    private float targetAimPitch;
    private float currentAimYaw;
    private float currentAimPitch;
    private bool flashlightOn;
    private bool anomalyReady;
    private bool eyesVisible;
    private bool anomalyVisible;
    private bool jumpscareTriggered;
    private bool lookedBackDuringCurrentCue;
    private CorridorCueType activeCueType = CorridorCueType.None;
    private int resolvedDecisionCount;
    private int anomalyCueCount;
    private int ignoredCueCount;

    private bool CanUseFlashlight => viewSwitcher != null
        && viewSwitcher.IsLookingBack
        && !viewSwitcher.IsTurning
        && IsGameplayActive();

    public void Configure(Vector3 configuredAnomalyPosition, Vector3 configuredSoundPosition)
    {
        anomalyPosition = configuredAnomalyPosition;
        corridorSoundPosition = configuredSoundPosition;
        if (anomalyRoot != null)
        {
            anomalyRoot.transform.position = anomalyPosition;
        }
    }

    private void Awake()
    {
        // Resolve/cria dependencias para a cena poder ser montada manualmente.
        ResolveReferences();
        LoadDefaultProjectClipsIfNeeded();
        generatedFlashlightClickClip = CreateFlashlightClickClip();
        generatedFlashlightHumClip = CreateFlashlightHumClip();
        generatedCorridorWhisperClip = CreateCorridorWhisperClip();
        generatedCorridorNoiseClip = CreateCorridorNoiseClip();
        generatedAnomalyLaughClip = CreateAnomalyLaughClip();
        EnsureAudioSources();
        EnsurePrompt();
        EnsureFlashlight();
        EnsureAnomaly();
        EnsureEyes();
        SetFlashlight(false, false);
    }

    private void OnEnable()
    {
        ViewSwitcher.ViewTurnStarted += OnViewTurnStarted;
        ViewSwitcher.ViewChanged += OnViewChanged;
        DecisionManager.DecisionResolved += OnDecisionResolved;
    }

    private void OnDisable()
    {
        ViewSwitcher.ViewTurnStarted -= OnViewTurnStarted;
        ViewSwitcher.ViewChanged -= OnViewChanged;
        DecisionManager.DecisionResolved -= OnDecisionResolved;
        SetCorridorPurpleLights(true);
        StopCorridorVoiceLoop();
        StopCorridorHitCue();
        StopSlowTensionLoop();
    }

    private void Update()
    {
        ResolveReferences();
        KeepFlashlightAttachedToCamera();
        UpdateFlashlightAim();
        UpdatePrompt();
        UpdateCorridorVoiceLoop();
        UpdateSlowTensionLoop();

        if (!IsGameplayActive())
        {
            SetFlashlight(false, false);
            return;
        }

        if (flashlightOn && !CanUseFlashlight)
        {
            SetFlashlight(false, false);
        }

        if (CanUseFlashlight && Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame)
        {
            SetFlashlight(!flashlightOn, true);
        }

        UpdateActiveAnomalyVisuals();

        // A anomalia so e resolvida se a lanterna estiver realmente apontada para ela.
        if (flashlightOn && anomalyReady && activeCueType == CorridorCueType.Creature && IsAnomalyInsideFlashlightCone())
        {
            ResolveCreatureAnomaly();
        }
        else if (flashlightOn && anomalyReady && activeCueType == CorridorCueType.Eyes && eyesVisible && IsAnomalyInsideFlashlightCone())
        {
            ResolveEyesAnomaly();
        }

        UpdateCorridorCueSchedule();
    }

    private void OnViewChanged(bool lookingBack)
    {
        if (!lookingBack)
        {
            SetFlashlight(false, false);
            HideEyes();
            HideCreature();
            return;
        }

        MarkCueCheckedFromBehind();
        UpdateActiveAnomalyVisuals();
    }

    private void OnViewTurnStarted(bool turningBack)
    {
        if (!turningBack)
        {
            return;
        }

        MarkCueCheckedFromBehind();
    }

    private void MarkCueCheckedFromBehind()
    {
        if (anomalyReady)
        {
            lookedBackDuringCurrentCue = true;
        }
    }

    private void OnDecisionResolved(bool correct)
    {
        resolvedDecisionCount++;
        if (resolvedDecisionCount >= minimumDecisionsBeforeFirstCue && nextCueTime < 0f && !anomalyReady && anomalyCueCount < maxAnomalyCuesPerShift)
        {
            ScheduleNextCue(new Vector2(5f, 14f));
        }
    }

    private void UpdateCorridorCueSchedule()
    {
        if (anomalyVisible)
        {
            return;
        }

        if (resolvedDecisionCount < minimumDecisionsBeforeFirstCue || anomalyCueCount >= maxAnomalyCuesPerShift)
        {
            return;
        }

        if (anomalyReady)
        {
            if (Time.time >= anomalyReadyUntil)
            {
                TriggerJumpscareGameOver();
                return;
            }

            return;
        }

        if (nextCueTime < 0f)
        {
            ScheduleNextCue(firstCueDelayRange);
            return;
        }

        if (Time.time >= nextCueTime)
        {
            TriggerCorridorCue();
        }
    }

    private void TriggerCorridorCue()
    {
        anomalyReady = true;
        anomalyCueCount++;
        activeCueType = PickNextCueType();
        anomalyPosition = PickAnomalyPosition(activeCueType);
        corridorSoundPosition = anomalyPosition + new Vector3(0f, 0.22f, 0f);
        anomalyReadyUntil = Time.time + GetCurrentAnomalyActiveWindow();
        anomalyCueStartedAt = Time.time;
        lookedBackDuringCurrentCue = viewSwitcher != null && viewSwitcher.IsLookingBack;
        nextCueTime = -1f;
        HideEyes();
        HideCreature();
        StopCorridorVoiceLoop();
        StopCorridorHitCue();
        StopSlowTensionLoop();

        float pressure = GetDifficultyProgress();
        if (activeCueType == CorridorCueType.Creature)
        {
            AudioClip hitClip = corridorHitClip != null ? corridorHitClip : corridorNoiseClip != null ? corridorNoiseClip : generatedCorridorNoiseClip;
            PlayCorridorHitCue(hitClip, Mathf.Lerp(0.70f, 1.0f, pressure), Random.Range(0.86f, 1.02f));
        }
        else
        {
            AudioClip whisperClip = corridorWhisperClip != null ? corridorWhisperClip : generatedCorridorWhisperClip;
            PlayCorridorClip(whisperClip, Mathf.Lerp(0.36f, 0.62f, pressure), Random.Range(0.92f, 1.05f));
        }

        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.10f + pressure * 0.08f, 0.08f + pressure * 0.10f);

        UpdateActiveAnomalyVisuals();
    }

    private CorridorCueType PickNextCueType()
    {
        float pressure = GetDifficultyProgress();
        float creatureChance = Mathf.Lerp(0.58f, 0.46f, pressure);
        return Random.value <= creatureChance ? CorridorCueType.Creature : CorridorCueType.Eyes;
    }

    private Vector3 PickAnomalyPosition(CorridorCueType cueType)
    {
        Vector2 yRange = cueType == CorridorCueType.Creature ? creatureYRange : eyesYRange;
        return new Vector3(
            Random.Range(Mathf.Min(anomalyXRange.x, anomalyXRange.y), Mathf.Max(anomalyXRange.x, anomalyXRange.y)),
            Random.Range(Mathf.Min(yRange.x, yRange.y), Mathf.Max(yRange.x, yRange.y)),
            Random.Range(Mathf.Min(anomalyZRange.x, anomalyZRange.y), Mathf.Max(anomalyZRange.x, anomalyZRange.y))
        );
    }

    private void ResolveEyesAnomaly()
    {
        if (anomalyRoutine != null)
        {
            StopCoroutine(anomalyRoutine);
        }

        anomalyRoutine = StartCoroutine(ResolveEyesAnomalyRoutine());
    }

    private void ResolveCreatureAnomaly()
    {
        if (anomalyRoutine != null)
        {
            StopCoroutine(anomalyRoutine);
        }

        anomalyRoutine = StartCoroutine(ResolveCreatureAnomalyRoutine());
    }

    private IEnumerator ResolveEyesAnomalyRoutine()
    {
        anomalyReady = false;
        ignoredCueCount = 0;
        anomalyVisible = true;
        anomalyReadyUntil = 0f;
        activeCueType = CorridorCueType.None;
        HideEyes();
        HideCreature();
        StopCorridorHitCue();
        StopCorridorVoiceLoop();
        StopSlowTensionLoop();

        PlayCorridorClip(corridorWhisperClip != null ? corridorWhisperClip : generatedCorridorWhisperClip, 0.34f, Random.Range(0.94f, 1.08f));

        yield return new WaitForSeconds(0.12f);

        anomalyVisible = false;
        anomalyRoutine = null;
        if (anomalyCueCount < maxAnomalyCuesPerShift)
        {
            ScheduleNextCue(repeatCueDelayRange);
        }
    }

    private IEnumerator ResolveCreatureAnomalyRoutine()
    {
        anomalyReady = false;
        ignoredCueCount = 0;
        anomalyVisible = true;
        anomalyReadyUntil = 0f;
        activeCueType = CorridorCueType.None;
        HideEyes();
        StopCorridorHitCue();
        StopCorridorVoiceLoop();
        StopSlowTensionLoop();

        Vector3 startPosition = anomalyPosition;
        Vector3 endPosition = anomalyPosition + new Vector3(Random.value > 0.5f ? 0.42f : -0.42f, -0.07f, -2.15f);
        if (anomalyRoot != null)
        {
            anomalyRoot.transform.position = startPosition;
            anomalyRoot.transform.localScale = Vector3.one;
            anomalyRoot.transform.localRotation = Quaternion.identity;
            ResetAnomalyVisualPose();
            anomalyRoot.SetActive(true);
            AnimateAnomalyVisuals(Time.time, 0.46f);
        }

        PlayCorridorClip(GetRandomAnomalyLaughClip(), 0.72f, Random.Range(0.94f, 1.05f));

        float elapsed = 0f;
        float duration = Mathf.Clamp(anomalyVisibleDuration, 0.34f, 0.62f);
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            if (anomalyRoot != null)
            {
                float panic = 1f - t;
                Vector3 jitter = new Vector3(
                    Mathf.Sin(elapsed * 57f) * 0.010f,
                    Mathf.Sin(elapsed * 71f) * 0.006f,
                    Mathf.Sin(elapsed * 43f) * 0.008f
                ) * panic;
                anomalyRoot.transform.position = Vector3.Lerp(startPosition, endPosition, eased) + jitter;
                anomalyRoot.transform.localRotation = Quaternion.Euler(
                    Mathf.Sin(elapsed * 31f) * 1.2f * panic,
                    Mathf.Sin(elapsed * 19f) * 4.0f * panic,
                    Mathf.Sin(elapsed * 37f) * 2.4f * panic
                );
                anomalyRoot.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.one * 0.78f, eased);
                AnimateAnomalyVisuals(elapsed, Mathf.Lerp(0.46f, 0.04f, eased));
            }

            yield return null;
        }

        if (anomalyRoot != null)
        {
            anomalyRoot.SetActive(false);
            anomalyRoot.transform.position = anomalyPosition;
            anomalyRoot.transform.localScale = Vector3.one;
            anomalyRoot.transform.localRotation = Quaternion.identity;
            ResetAnomalyVisualPose();
        }

        anomalyVisible = false;
        anomalyRoutine = null;
        if (anomalyCueCount < maxAnomalyCuesPerShift)
        {
            ScheduleNextCue(repeatCueDelayRange);
        }
    }

    private void TriggerJumpscareGameOver()
    {
        if (jumpscareTriggered)
        {
            return;
        }

        jumpscareTriggered = true;
        anomalyReady = false;
        nextCueTime = -1f;
        StopCorridorHitCue();
        StopCorridorVoiceLoop();
        StopSlowTensionLoop();

        if (jumpscareRoutine != null)
        {
            StopCoroutine(jumpscareRoutine);
        }

        jumpscareRoutine = StartCoroutine(TriggerJumpscareGameOverRoutine());
    }

    private IEnumerator TriggerJumpscareGameOverRoutine()
    {
        CorridorCueType scareType = activeCueType;
        if (scareType == CorridorCueType.Creature)
        {
            if (anomalyRoot != null)
            {
                anomalyRoot.transform.position = anomalyPosition + new Vector3(0f, 0f, 0.72f);
                anomalyRoot.transform.localScale = Vector3.one * 1.35f;
                anomalyRoot.SetActive(true);
            }
        }
        else
        {
            ShowEyes();
        }

        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.72f, 0.78f);
        AudioClip scareClip = jumpscareClip != null
            ? jumpscareClip
            : anomalyLaughClip != null ? anomalyLaughClip : generatedAnomalyLaughClip;
        PlayCorridorClip(scareClip, 1.18f, Random.Range(0.86f, 0.96f));

        yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, jumpscareDelay));

        HideEyes();
        HideCreature();
        activeCueType = CorridorCueType.None;
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        gameManager?.GameOver("A anomalia entrou na sala.");
    }

    private void SetFlashlight(bool enabled, bool playClick)
    {
        flashlightOn = enabled && CanUseFlashlight;
        if (flashlight != null)
        {
            flashlight.enabled = flashlightOn;
        }

        UpdateFlashlightHum();
        SetCorridorPurpleLights(!flashlightOn);

        if (!flashlightOn)
        {
            ResetFlashlightAim();
        }

        if (playClick)
        {
            PlayFlashlightClick();
        }

        if (flashlightOn && anomalyReady && activeCueType == CorridorCueType.Eyes && eyesVisible && IsAnomalyInsideFlashlightCone())
        {
            ResolveEyesAnomaly();
        }
    }

    private void UpdatePrompt()
    {
        if (promptGroup == null)
        {
            return;
        }

        if (promptText != null)
        {
            promptText.text = flashlightOn ? "F Off" : "F Flashlight";
        }

        float targetAlpha = CanUseFlashlight ? 0.55f : 0f;
        promptGroup.alpha = Mathf.MoveTowards(promptGroup.alpha, targetAlpha, Time.unscaledDeltaTime * 3.6f);
    }

    private void ScheduleNextCue(Vector2 range)
    {
        Vector2 scaledRange = GetScaledCueDelayRange(range);
        float min = Mathf.Max(1f, Mathf.Min(scaledRange.x, scaledRange.y));
        float max = Mathf.Max(min, Mathf.Max(scaledRange.x, scaledRange.y));
        nextCueTime = Time.time + Random.Range(min, max);
    }

    private Vector2 GetScaledCueDelayRange(Vector2 baseRange)
    {
        float pressure = GetDifficultyProgress();
        return new Vector2(
            Mathf.Lerp(baseRange.x, 3.2f, pressure),
            Mathf.Lerp(baseRange.y, 7.8f, pressure)
        );
    }

    private float GetCurrentAnomalyActiveWindow()
    {
        if (activeCueType == CorridorCueType.Creature)
        {
            return Mathf.Lerp(Mathf.Max(1f, creatureActiveWindow), Mathf.Max(1f, lateShiftCreatureActiveWindow), GetDifficultyProgress());
        }

        return Mathf.Lerp(Mathf.Max(1f, anomalyActiveWindow), Mathf.Max(1f, lateShiftActiveWindow), GetDifficultyProgress());
    }

    private bool ShouldTriggerJumpscareFromIgnoredCue()
    {
        return GetDifficultyProgress() >= jumpscareProgressThreshold
            && ignoredCueCount >= Mathf.Max(1, ignoredCuesBeforeForcedJumpscare);
    }

    private float GetDifficultyProgress()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || gameManager.quotaNecessaria <= 1)
        {
            return 0f;
        }

        return Mathf.Clamp01(gameManager.quotaAtual / Mathf.Max(1f, gameManager.quotaNecessaria - 1f));
    }

    private bool IsGameplayActive()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return false;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsBlockingScreenOpen)
        {
            return false;
        }

        if (NightStoryManager.Instance != null && NightStoryManager.Instance.IsBriefingOpen)
        {
            return false;
        }

        return true;
    }

    private void PlayFlashlightClick()
    {
        EnsureAudioSources();
        AudioClip clip = flashlightClickClip != null ? flashlightClickClip : generatedFlashlightClickClip;
        if (flashlightAudioSource != null && clip != null)
        {
            flashlightAudioSource.pitch = Random.Range(0.96f, 1.04f);
            flashlightAudioSource.PlayOneShot(clip, 0.78f);
        }
    }

    private void UpdateFlashlightHum()
    {
        EnsureAudioSources();
        if (flashlightHumSource == null)
        {
            return;
        }

        if (flashlightOn)
        {
            flashlightHumSource.clip = flashlightHumClip != null ? flashlightHumClip : generatedFlashlightHumClip;
            flashlightHumSource.loop = true;
            flashlightHumSource.volume = 0.12f;
            if (flashlightHumSource.clip != null && !flashlightHumSource.isPlaying)
            {
                flashlightHumSource.Play();
            }
            return;
        }

        if (flashlightHumSource.isPlaying)
        {
            flashlightHumSource.Stop();
        }
    }

    private void UpdateCorridorVoiceLoop()
    {
        EnsureAudioSources();
        AudioClip voiceClip = corridorVoiceLoopClip != null ? corridorVoiceLoopClip : generatedCorridorWhisperClip;
        if (corridorVoiceSource == null || voiceClip == null)
        {
            return;
        }

        if (activeCueType != CorridorCueType.Eyes || IsCorridorHitPlaying())
        {
            StopCorridorVoiceLoop();
            return;
        }

        bool shouldPlay = anomalyReady
            && IsGameplayActive();
        float targetVolume = shouldPlay ? GetCorridorVoiceTargetVolume() : 0f;

        corridorVoiceSource.transform.position = corridorSoundPosition;
        if (targetVolume > 0.001f && !corridorVoiceSource.isPlaying)
        {
            corridorVoiceSource.clip = voiceClip;
            corridorVoiceSource.loop = true;
            corridorVoiceSource.volume = 0f;
            corridorVoiceSource.Play();
        }

        float fadeSpeed = targetVolume > corridorVoiceSource.volume ? corridorVoiceRiseSpeed : corridorVoiceFallSpeed;
        corridorVoiceSource.volume = Mathf.MoveTowards(corridorVoiceSource.volume, targetVolume, Time.unscaledDeltaTime * fadeSpeed);

        if (targetVolume <= 0.001f && corridorVoiceSource.volume <= 0.001f && corridorVoiceSource.isPlaying)
        {
            corridorVoiceSource.Stop();
        }
    }

    private float GetCorridorVoiceTargetVolume()
    {
        float pressure = GetDifficultyProgress();
        float activeWindow = Mathf.Max(0.1f, GetCurrentAnomalyActiveWindow());
        float cueProgress = anomalyReady ? Mathf.Clamp01((Time.time - anomalyCueStartedAt) / activeWindow) : 0f;
        float maxForDifficulty = Mathf.Lerp(corridorVoiceMaxVolume * 0.62f, corridorVoiceMaxVolume, pressure);
        float target = Mathf.Lerp(corridorVoiceMinVolume, maxForDifficulty, cueProgress);
        if (anomalyReady)
        {
            target += Mathf.Lerp(0.015f, 0.055f, pressure);
        }

        return Mathf.Clamp01(target);
    }

    private void StopCorridorVoiceLoop()
    {
        if (corridorVoiceSource != null)
        {
            corridorVoiceSource.Stop();
            corridorVoiceSource.volume = 0f;
        }
    }

    private bool IsCorridorHitPlaying()
    {
        return corridorHitSource != null && corridorHitSource.isPlaying;
    }

    private void PlayCorridorHitCue(AudioClip clip, float volume, float pitch)
    {
        EnsureAudioSources();
        StopCorridorVoiceLoop();
        StopSlowTensionLoop();
        if (corridorHitSource == null || clip == null)
        {
            return;
        }

        corridorHitSource.Stop();
        corridorHitSource.transform.position = corridorSoundPosition;
        corridorHitSource.clip = clip;
        corridorHitSource.loop = false;
        corridorHitSource.pitch = pitch;
        corridorHitSource.volume = Mathf.Clamp01(volume);
        corridorHitSource.Play();
    }

    private void StopCorridorHitCue()
    {
        if (corridorHitSource != null)
        {
            corridorHitSource.Stop();
            corridorHitSource.volume = 0f;
        }
    }

    private void UpdateSlowTensionLoop()
    {
        EnsureAudioSources();
        if (slowTensionSource == null || slowTensionClip == null)
        {
            return;
        }

        bool shouldPlay = false;
        float targetVolume = shouldPlay ? slowTensionVolume : 0f;

        slowTensionSource.transform.position = corridorSoundPosition;
        if (targetVolume > 0.001f && !slowTensionSource.isPlaying)
        {
            slowTensionSource.clip = slowTensionClip;
            slowTensionSource.loop = true;
            slowTensionSource.volume = 0f;
            slowTensionSource.Play();
        }

        slowTensionSource.volume = Mathf.MoveTowards(
            slowTensionSource.volume,
            targetVolume,
            Time.unscaledDeltaTime * slowTensionFadeSpeed
        );

        if (targetVolume <= 0.001f && slowTensionSource.volume <= 0.001f && slowTensionSource.isPlaying)
        {
            slowTensionSource.Stop();
        }
    }

    private void StopSlowTensionLoop()
    {
        if (slowTensionSource != null)
        {
            slowTensionSource.Stop();
            slowTensionSource.volume = 0f;
        }
    }

    private void PlayCorridorClip(AudioClip clip, float volume, float pitch)
    {
        EnsureAudioSources();
        if (corridorAudioSource == null || clip == null)
        {
            return;
        }

        corridorAudioSource.transform.position = corridorSoundPosition;
        corridorAudioSource.pitch = pitch;
        corridorAudioSource.PlayOneShot(clip, volume);
    }

    private void EnsureFlashlight()
    {
        if (flashlight != null)
        {
            ConfigureFlashlight();
            return;
        }

        var flashlightObject = new GameObject("BackCorridor_PlayerFlashlight");
        flashlight = flashlightObject.AddComponent<Light>();
        KeepFlashlightAttachedToCamera();
        ConfigureFlashlight();
    }

    private void ConfigureFlashlight()
    {
        if (flashlight == null)
        {
            return;
        }

        flashlight.type = LightType.Spot;
        flashlight.color = flashlightColor;
        flashlight.intensity = flashlightIntensity;
        flashlight.range = flashlightRange;
        flashlight.spotAngle = flashlightSpotAngle;
        flashlight.innerSpotAngle = flashlightSpotAngle * 0.54f;
        flashlight.shadows = LightShadows.None;
        flashlight.cookie = null;
        flashlight.enabled = false;
    }

    private void SetCorridorPurpleLights(bool visible)
    {
        CacheCorridorPurpleLights();

        if (corridorPurpleLights == null)
        {
            return;
        }

        for (int i = 0; i < corridorPurpleLights.Length; i++)
        {
            Light purpleLight = corridorPurpleLights[i];
            if (purpleLight == null)
            {
                continue;
            }

            if (corridorPurpleFlickers != null && i < corridorPurpleFlickers.Length && corridorPurpleFlickers[i] != null)
            {
                corridorPurpleFlickers[i].enabled = visible;
            }

            purpleLight.enabled = visible;
        }
    }

    private void CacheCorridorPurpleLights()
    {
        if (corridorPurpleLights != null && corridorPurpleLights.Length > 0 && !ContainsMissingLight(corridorPurpleLights))
        {
            EnsureCorridorPurpleFlickerCache();
            return;
        }

        Light[] allLights = Object.FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        var foundLights = new List<Light>();

        foreach (Light light in allLights)
        {
            if (light == null || !light.name.StartsWith("BackCorridor_PurpleFlicker", System.StringComparison.Ordinal))
            {
                continue;
            }

            foundLights.Add(light);
        }

        corridorPurpleLights = foundLights.ToArray();
        EnsureCorridorPurpleFlickerCache();
    }

    private bool ContainsMissingLight(Light[] lights)
    {
        foreach (Light light in lights)
        {
            if (light == null)
            {
                return true;
            }
        }

        return false;
    }

    private void EnsureCorridorPurpleFlickerCache()
    {
        if (corridorPurpleLights == null)
        {
            corridorPurpleFlickers = null;
            return;
        }

        if (corridorPurpleFlickers != null && corridorPurpleFlickers.Length == corridorPurpleLights.Length)
        {
            return;
        }

        corridorPurpleFlickers = new FlickeringLight[corridorPurpleLights.Length];
        for (int i = 0; i < corridorPurpleLights.Length; i++)
        {
            corridorPurpleFlickers[i] = corridorPurpleLights[i] != null
                ? corridorPurpleLights[i].GetComponent<FlickeringLight>()
                : null;
        }
    }

    private void KeepFlashlightAttachedToCamera()
    {
        if (flashlight == null)
        {
            return;
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            return;
        }

        Transform flashlightTransform = flashlight.transform;
        if (flashlightTransform.parent != playerCamera.transform)
        {
            flashlightTransform.SetParent(playerCamera.transform, false);
        }

        flashlightTransform.localPosition = new Vector3(0.12f, -0.12f, 0.18f);
    }

    private void UpdateFlashlightAim()
    {
        if (flashlight == null)
        {
            return;
        }

        if (!flashlightOn || !CanUseFlashlight)
        {
            flashlight.transform.localRotation = GetFlashlightAimRotation();
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse != null)
        {
            Vector2 delta = mouse.delta.ReadValue();
            targetAimYaw = Mathf.Clamp(targetAimYaw + delta.x * flashlightAimSensitivity, -flashlightMaxAngleX, flashlightMaxAngleX);
            targetAimPitch = Mathf.Clamp(targetAimPitch - delta.y * flashlightAimSensitivity, -flashlightMaxAngleY, flashlightMaxAngleY);
        }

        float t = 1f - Mathf.Exp(-flashlightAimSpeed * Time.deltaTime);
        currentAimYaw = Mathf.Lerp(currentAimYaw, targetAimYaw, t);
        currentAimPitch = Mathf.Lerp(currentAimPitch, targetAimPitch, t);
        flashlight.transform.localRotation = GetFlashlightAimRotation();
    }

    private Quaternion GetFlashlightAimRotation()
    {
        return Quaternion.Euler(currentAimPitch, currentAimYaw, 0f);
    }

    private void ResetFlashlightAim()
    {
        targetAimYaw = 0f;
        targetAimPitch = 0f;
        currentAimYaw = 0f;
        currentAimPitch = 0f;
        if (flashlight != null)
        {
            flashlight.transform.localRotation = Quaternion.identity;
        }
    }

    private bool IsAnomalyInsideFlashlightCone()
    {
        if (flashlight == null)
        {
            return false;
        }

        Vector3 anomalyCenter = anomalyPosition + Vector3.up * (activeCueType == CorridorCueType.Creature ? 1.02f : 0.72f);
        Vector3 toAnomaly = anomalyCenter - flashlight.transform.position;
        float distance = toAnomaly.magnitude;
        if (distance <= 0.01f || distance > flashlight.range)
        {
            return false;
        }

        float angle = Vector3.Angle(flashlight.transform.forward, toAnomaly);
        return angle <= Mathf.Min(anomalyRevealAngle, flashlight.spotAngle * 0.5f);
    }

    private void UpdateActiveAnomalyVisuals()
    {
        bool lookingBack = viewSwitcher != null && viewSwitcher.IsLookingBack;
        bool canShowEyes = anomalyReady
            && activeCueType == CorridorCueType.Eyes
            && lookingBack
            && ShouldShowAnomalyEyesNow();

        if (canShowEyes)
        {
            ShowEyes();
        }
        else
        {
            HideEyes();
        }

        if (!eyesVisible || eyeTransforms == null || eyeBaseScales == null)
        {
            if (anomalyVisible && anomalyRoot != null && anomalyRoot.activeInHierarchy)
            {
                AnimateAnomalyVisuals(Time.time, 0.18f);
            }

            return;
        }

        float pressure = GetDifficultyProgress();
        float pulse = 1f
            + Mathf.Sin(Time.time * Mathf.Lerp(7.7f, 14.0f, pressure)) * Mathf.Lerp(0.055f, 0.145f, pressure)
            + Mathf.Sin(Time.time * 17.0f) * Mathf.Lerp(0.018f, 0.055f, pressure);
        for (int i = 0; i < eyeTransforms.Length; i++)
        {
            if (eyeTransforms[i] != null && i < eyeBaseScales.Length)
            {
                eyeTransforms[i].localScale = eyeBaseScales[i] * pulse;
            }
        }
    }

    private bool ShouldShowAnomalyEyesNow()
    {
        if (activeCueType != CorridorCueType.Eyes)
        {
            return false;
        }

        if (flashlightOn)
        {
            return IsAnomalyInsideFlashlightCone();
        }

        float pressure = GetDifficultyProgress();
        float elapsed = Mathf.Max(0f, Time.time - anomalyCueStartedAt);
        if (elapsed < 0.22f)
        {
            return false;
        }

        float flicker = Mathf.PerlinNoise(elapsed * Mathf.Lerp(3.2f, 6.0f, pressure), anomalyCueCount * 0.317f);
        float pulse = Mathf.Repeat(elapsed * Mathf.Lerp(1.25f, 2.55f, pressure), 1f);
        return flicker > Mathf.Lerp(0.66f, 0.48f, pressure) || pulse > Mathf.Lerp(0.88f, 0.76f, pressure);
    }

    private void ShowEyes()
    {
        EnsureEyes();
        if (eyesRoot == null)
        {
            return;
        }

        eyesRoot.transform.position = anomalyPosition + new Vector3(0f, 0.62f, 0f);
        eyesRoot.SetActive(true);
        eyesVisible = true;
    }

    private void HideEyes()
    {
        if (eyesRoot != null)
        {
            eyesRoot.SetActive(false);
        }

        eyesVisible = false;
    }

    private void HideCreature()
    {
        if (anomalyRoot != null)
        {
            anomalyRoot.SetActive(false);
            anomalyRoot.transform.position = anomalyPosition;
            anomalyRoot.transform.localScale = Vector3.one;
            anomalyRoot.transform.localRotation = Quaternion.identity;
            ResetAnomalyVisualPose();
        }
    }

    private void EnsureAnomaly()
    {
        if (anomalyRoot != null)
        {
            anomalyRoot.SetActive(false);
            CacheAnomalyVisuals();
            return;
        }

        anomalyRoot = new GameObject("BackCorridor_FirstFlashlightAnomaly");
        anomalyRoot.transform.SetParent(transform, false);
        anomalyRoot.transform.position = anomalyPosition;
        anomalyRoot.tag = "Anomaly";

        Material shadowMaterial = CreateAnomalyMaterial();
        Material edgeMaterial = CreateAnomalyEdgeMaterial();
        Material eyeMaterial = CreateEyeMaterial();
        Material mouthMaterial = CreateAnomalyMouthMaterial();

        CreateAnomalyPart("Anomaly_Spine", PrimitiveType.Capsule, new Vector3(0.020f, 0.660f, 0.000f), new Vector3(0.230f, 1.040f, 0.145f), shadowMaterial, new Vector3(0f, 0f, -4f));
        CreateAnomalyPart("Anomaly_RibCage", PrimitiveType.Sphere, new Vector3(0.000f, 0.850f, 0.018f), new Vector3(0.430f, 0.540f, 0.170f), shadowMaterial, new Vector3(0f, 0f, 3f));
        CreateAnomalyPart("Anomaly_HollowBelly", PrimitiveType.Cube, new Vector3(0.018f, 0.460f, 0.038f), new Vector3(0.300f, 0.380f, 0.070f), edgeMaterial, new Vector3(0f, 0f, -2f));
        CreateAnomalyPart("Anomaly_ShoulderHunch", PrimitiveType.Cube, new Vector3(0.000f, 1.115f, -0.010f), new Vector3(0.780f, 0.135f, 0.125f), shadowMaterial, new Vector3(0f, 0f, -1f));
        CreateAnomalyPart("Anomaly_Neck", PrimitiveType.Capsule, new Vector3(0.000f, 1.245f, 0.020f), new Vector3(0.115f, 0.300f, 0.090f), shadowMaterial, new Vector3(0f, 0f, 5f));
        CreateAnomalyPart("Anomaly_Head", PrimitiveType.Sphere, new Vector3(0.000f, 1.430f, 0.060f), new Vector3(0.300f, 0.365f, 0.225f), shadowMaterial, new Vector3(4f, 0f, -3f));
        CreateAnomalyPart("Anomaly_Jaw", PrimitiveType.Cube, new Vector3(0.012f, 1.235f, 0.105f), new Vector3(0.225f, 0.150f, 0.070f), shadowMaterial, new Vector3(6f, 0f, -2f));
        CreateAnomalyPart("Anomaly_MouthGlow", PrimitiveType.Cube, new Vector3(0.015f, 1.250f, 0.148f), new Vector3(0.142f, 0.032f, 0.026f), mouthMaterial, new Vector3(0f, 0f, -2f));
        CreateAnomalyPart("Anomaly_LeftEye", PrimitiveType.Sphere, new Vector3(-0.073f, 1.470f, 0.190f), new Vector3(0.046f, 0.035f, 0.020f), eyeMaterial);
        CreateAnomalyPart("Anomaly_RightEye", PrimitiveType.Sphere, new Vector3(0.073f, 1.470f, 0.190f), new Vector3(0.046f, 0.035f, 0.020f), eyeMaterial);

        CreateAnomalyPart("Anomaly_LeftUpperArm", PrimitiveType.Cube, new Vector3(-0.350f, 0.900f, 0.012f), new Vector3(0.105f, 0.620f, 0.082f), shadowMaterial, new Vector3(0f, 0f, -22f));
        CreateAnomalyPart("Anomaly_LeftForearm", PrimitiveType.Cube, new Vector3(-0.485f, 0.420f, 0.050f), new Vector3(0.082f, 0.650f, 0.070f), shadowMaterial, new Vector3(0f, 0f, -8f));
        CreateAnomalyPart("Anomaly_RightUpperArm", PrimitiveType.Cube, new Vector3(0.350f, 0.900f, 0.012f), new Vector3(0.105f, 0.620f, 0.082f), shadowMaterial, new Vector3(0f, 0f, 22f));
        CreateAnomalyPart("Anomaly_RightForearm", PrimitiveType.Cube, new Vector3(0.485f, 0.420f, 0.050f), new Vector3(0.082f, 0.650f, 0.070f), shadowMaterial, new Vector3(0f, 0f, 8f));

        CreateClaw("Anomaly_LeftClaw", new Vector3(-0.520f, 0.090f, 0.072f), -1f, edgeMaterial);
        CreateClaw("Anomaly_RightClaw", new Vector3(0.520f, 0.090f, 0.072f), 1f, edgeMaterial);
        CreateBackSpikes(edgeMaterial);

        anomalyGlowLight = anomalyRoot.AddComponent<Light>();
        anomalyGlowLight.type = LightType.Point;
        anomalyGlowLight.color = new Color(0.420f, 0.070f, 0.820f);
        anomalyGlowLight.intensity = 0.0f;
        anomalyGlowLight.range = 1.05f;
        anomalyGlowLight.shadows = LightShadows.None;
        anomalyGlowLight.lightmapBakeType = LightmapBakeType.Realtime;

        CacheAnomalyVisuals();
        anomalyRoot.SetActive(false);
    }

    private void EnsureEyes()
    {
        if (eyesRoot != null)
        {
            eyesRoot.SetActive(false);
            CacheEyeTransforms();
            return;
        }

        eyesRoot = new GameObject("BackCorridor_WhiteEyesAnomaly");
        eyesRoot.transform.SetParent(transform, false);
        eyesRoot.transform.position = anomalyPosition + new Vector3(0f, 0.62f, 0f);

        Material eyeMaterial = CreateEyeMaterial();
        CreateEye("Anomaly_LeftWhiteEye", new Vector3(-0.070f, 0f, 0f), eyeMaterial);
        CreateEye("Anomaly_RightWhiteEye", new Vector3(0.070f, 0f, 0f), eyeMaterial);

        Light eyeGlow = eyesRoot.AddComponent<Light>();
        eyeGlow.type = LightType.Point;
        eyeGlow.color = new Color(0.820f, 0.900f, 1.000f);
        eyeGlow.intensity = 0.24f;
        eyeGlow.range = 0.58f;
        eyeGlow.shadows = LightShadows.None;
        eyeGlow.lightmapBakeType = LightmapBakeType.Realtime;

        CacheEyeTransforms();
        eyesRoot.SetActive(false);
    }

    private void CreateEye(string eyeName, Vector3 localPosition, Material material)
    {
        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = eyeName;
        eye.transform.SetParent(eyesRoot.transform, false);
        eye.transform.localPosition = localPosition;
        eye.transform.localScale = new Vector3(0.045f, 0.030f, 0.018f);

        Renderer renderer = eye.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = material;
        }

        Collider collider = eye.GetComponent<Collider>();
        if (collider != null)
        {
            Destroy(collider);
        }
    }

    private void CacheEyeTransforms()
    {
        if (eyesRoot == null)
        {
            eyeTransforms = null;
            eyeBaseScales = null;
            return;
        }

        eyeTransforms = new Transform[eyesRoot.transform.childCount];
        eyeBaseScales = new Vector3[eyesRoot.transform.childCount];
        for (int i = 0; i < eyesRoot.transform.childCount; i++)
        {
            eyeTransforms[i] = eyesRoot.transform.GetChild(i);
            eyeBaseScales[i] = eyeTransforms[i].localScale;
        }
    }

    private void CreateClaw(string clawName, Vector3 palmPosition, float side, Material material)
    {
        CreateAnomalyPart(clawName + "_Palm", PrimitiveType.Cube, palmPosition, new Vector3(0.108f, 0.050f, 0.052f), material, new Vector3(0f, 0f, side * 5f));
        for (int i = 0; i < 4; i++)
        {
            float spread = (i - 1.5f) * 0.034f;
            Vector3 fingerPosition = palmPosition + new Vector3(side * (0.034f + i * 0.012f), -0.092f, 0.020f + spread);
            CreateAnomalyPart(clawName + "_Finger_" + i.ToString("00"), PrimitiveType.Cube, fingerPosition, new Vector3(0.020f, 0.190f + i * 0.012f, 0.018f), material, new Vector3(side * 9f, 0f, side * (8f + i * 5f)));
        }
    }

    private void CreateBackSpikes(Material material)
    {
        for (int i = 0; i < 5; i++)
        {
            float t = i / 4f;
            Vector3 position = new Vector3(Mathf.Lerp(-0.220f, 0.220f, t), Mathf.Lerp(1.050f, 0.530f, t), -0.100f);
            Vector3 scale = new Vector3(0.050f, Mathf.Lerp(0.180f, 0.105f, t), 0.044f);
            CreateAnomalyPart("Anomaly_BackSpike_" + i.ToString("00"), PrimitiveType.Cube, position, scale, material, new Vector3(Mathf.Lerp(-18f, 18f, t), 0f, Mathf.Lerp(-32f, 32f, t)));
        }
    }

    private GameObject CreateAnomalyPart(string partName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Material material)
    {
        return CreateAnomalyPart(partName, primitive, localPosition, localScale, material, Vector3.zero);
    }

    private GameObject CreateAnomalyPart(string partName, PrimitiveType primitive, Vector3 localPosition, Vector3 localScale, Material material, Vector3 localEulerAngles)
    {
        GameObject part = GameObject.CreatePrimitive(primitive);
        part.name = partName;
        part.transform.SetParent(anomalyRoot.transform, false);
        part.transform.localPosition = localPosition;
        part.transform.localRotation = Quaternion.Euler(localEulerAngles);
        part.transform.localScale = localScale;

        Renderer partRenderer = part.GetComponent<Renderer>();
        if (partRenderer != null)
        {
            partRenderer.sharedMaterial = material;
        }

        Collider partCollider = part.GetComponent<Collider>();
        if (partCollider != null)
        {
            Destroy(partCollider);
        }

        return part;
    }

    private void CacheAnomalyVisuals()
    {
        if (anomalyRoot == null)
        {
            anomalyPartTransforms = null;
            anomalyPartBasePositions = null;
            anomalyPartBaseScales = null;
            anomalyPartBaseRotations = null;
            anomalyRenderers = null;
            anomalyGlowLight = null;
            return;
        }

        Transform[] allTransforms = anomalyRoot.GetComponentsInChildren<Transform>(true);
        var transforms = new List<Transform>();
        foreach (Transform child in allTransforms)
        {
            if (child != null && child != anomalyRoot.transform)
            {
                transforms.Add(child);
            }
        }

        anomalyPartTransforms = transforms.ToArray();
        anomalyPartBasePositions = new Vector3[anomalyPartTransforms.Length];
        anomalyPartBaseScales = new Vector3[anomalyPartTransforms.Length];
        anomalyPartBaseRotations = new Quaternion[anomalyPartTransforms.Length];
        for (int i = 0; i < anomalyPartTransforms.Length; i++)
        {
            anomalyPartBasePositions[i] = anomalyPartTransforms[i].localPosition;
            anomalyPartBaseScales[i] = anomalyPartTransforms[i].localScale;
            anomalyPartBaseRotations[i] = anomalyPartTransforms[i].localRotation;
        }

        anomalyRenderers = anomalyRoot.GetComponentsInChildren<Renderer>(true);
        anomalyGlowLight = anomalyRoot.GetComponent<Light>();
    }

    private void ResetAnomalyVisualPose()
    {
        if (anomalyPartTransforms == null || anomalyPartBasePositions == null || anomalyPartBaseScales == null || anomalyPartBaseRotations == null)
        {
            return;
        }

        for (int i = 0; i < anomalyPartTransforms.Length; i++)
        {
            Transform part = anomalyPartTransforms[i];
            if (part == null)
            {
                continue;
            }

            part.localPosition = anomalyPartBasePositions[i];
            part.localScale = anomalyPartBaseScales[i];
            part.localRotation = anomalyPartBaseRotations[i];
        }

        if (anomalyGlowLight != null)
        {
            anomalyGlowLight.intensity = 0f;
        }
    }

    private void AnimateAnomalyVisuals(float time, float intensity)
    {
        if (anomalyPartTransforms == null || anomalyPartBasePositions == null || anomalyPartBaseScales == null || anomalyPartBaseRotations == null)
        {
            return;
        }

        float clampedIntensity = Mathf.Clamp01(intensity);
        for (int i = 0; i < anomalyPartTransforms.Length; i++)
        {
            Transform part = anomalyPartTransforms[i];
            if (part == null)
            {
                continue;
            }

            float seed = i * 1.371f;
            float snap = Mathf.PerlinNoise(time * 22f + seed, anomalyCueCount * 0.137f) - 0.5f;
            float twitch = Mathf.Sin(time * (26f + i * 0.43f) + seed) * clampedIntensity;
            Vector3 offset = new Vector3(
                snap * 0.016f,
                Mathf.Sin(time * 31f + seed) * 0.010f,
                Mathf.Sin(time * 19f + seed) * 0.012f
            ) * clampedIntensity;
            part.localPosition = anomalyPartBasePositions[i] + offset;
            part.localRotation = anomalyPartBaseRotations[i] * Quaternion.Euler(twitch * 3.7f, snap * 6.2f, twitch * 4.8f);

            float stretch = 1f + Mathf.Sin(time * 18f + seed) * 0.055f * clampedIntensity;
            part.localScale = Vector3.Scale(anomalyPartBaseScales[i], new Vector3(1f + snap * 0.045f * clampedIntensity, stretch, 1f));
        }

        if (anomalyGlowLight != null)
        {
            anomalyGlowLight.intensity = Mathf.Lerp(0.01f, 0.20f, clampedIntensity) + Mathf.Abs(Mathf.Sin(time * 23f)) * 0.035f * clampedIntensity;
        }
    }

    private Material CreateAnomalyMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        Color color = new Color(0.012f, 0.005f, 0.026f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", new Color(0.220f, 0.025f, 0.520f) * 0.18f);
            material.EnableKeyword("_EMISSION");
        }

        return material;
    }

    private Material CreateAnomalyEdgeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        Color color = new Color(0.090f, 0.012f, 0.190f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", new Color(0.520f, 0.050f, 0.900f) * 0.28f);
            material.EnableKeyword("_EMISSION");
        }

        return material;
    }

    private Material CreateAnomalyMouthMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        Color color = new Color(0.720f, 0.040f, 0.135f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 1.15f);
            material.EnableKeyword("_EMISSION");
        }

        return material;
    }

    private Material CreateEyeMaterial()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        Color color = new Color(0.900f, 0.940f, 1.000f, 1f);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * 2.9f);
            material.EnableKeyword("_EMISSION");
        }

        return material;
    }

    private void EnsurePrompt()
    {
        if (promptGroup != null)
        {
            return;
        }

        var canvasObject = new GameObject("Flashlight Prompt Canvas");
        canvasObject.transform.SetParent(transform, false);
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 82;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var promptObject = new GameObject("F Flashlight Prompt");
        promptObject.transform.SetParent(canvasObject.transform, false);
        RectTransform rect = promptObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-86f, 58f);
        rect.sizeDelta = new Vector2(340f, 58f);

        promptText = promptObject.AddComponent<TextMeshProUGUI>();
        promptText.text = "F Flashlight";
        promptText.fontSize = 34f;
        promptText.alignment = TextAlignmentOptions.Right;
        promptText.color = new Color(0.860f, 0.820f, 0.940f, 0.78f);
        promptText.characterSpacing = 1.8f;
        promptText.outlineWidth = 0.12f;
        promptText.outlineColor = Color.black;
        promptText.raycastTarget = false;

        promptGroup = promptObject.AddComponent<CanvasGroup>();
        promptGroup.alpha = 0f;
        promptGroup.blocksRaycasts = false;
        promptGroup.interactable = false;
    }

    private void EnsureAudioSources()
    {
        if (corridorAudioSource == null)
        {
            GameObject sourceObject = new GameObject("Back Corridor OneShot Audio");
            sourceObject.transform.SetParent(transform, false);
            sourceObject.transform.position = corridorSoundPosition;
            corridorAudioSource = sourceObject.AddComponent<AudioSource>();
            ConfigureSpatialSource(corridorAudioSource, 1.0f, 11.5f);
        }

        if (flashlightAudioSource == null)
        {
            GameObject sourceObject = new GameObject("Flashlight Click Audio");
            sourceObject.transform.SetParent(transform, false);
            flashlightAudioSource = sourceObject.AddComponent<AudioSource>();
            flashlightAudioSource.spatialBlend = 0f;
            flashlightAudioSource.playOnAwake = false;
        }

        if (flashlightHumSource == null)
        {
            GameObject sourceObject = new GameObject("Flashlight Hum Audio");
            sourceObject.transform.SetParent(transform, false);
            flashlightHumSource = sourceObject.AddComponent<AudioSource>();
            flashlightHumSource.spatialBlend = 0f;
            flashlightHumSource.playOnAwake = false;
            flashlightHumSource.loop = true;
        }

        if (corridorVoiceSource == null)
        {
            GameObject sourceObject = new GameObject("Back Corridor Whisper Voices");
            sourceObject.transform.SetParent(transform, false);
            sourceObject.transform.position = corridorSoundPosition;
            corridorVoiceSource = sourceObject.AddComponent<AudioSource>();
            ConfigureSpatialSource(corridorVoiceSource, 0.9f, 13.5f);
            corridorVoiceSource.loop = true;
            corridorVoiceSource.volume = 0f;
        }

        if (corridorHitSource == null)
        {
            GameObject sourceObject = new GameObject("Back Corridor Hit Cue");
            sourceObject.transform.SetParent(transform, false);
            sourceObject.transform.position = corridorSoundPosition;
            corridorHitSource = sourceObject.AddComponent<AudioSource>();
            ConfigureSpatialSource(corridorHitSource, 0.9f, 13.5f);
            corridorHitSource.loop = false;
            corridorHitSource.volume = 0f;
        }

        if (slowTensionSource == null)
        {
            GameObject sourceObject = new GameObject("Back Corridor Slow Tension");
            sourceObject.transform.SetParent(transform, false);
            sourceObject.transform.position = corridorSoundPosition;
            slowTensionSource = sourceObject.AddComponent<AudioSource>();
            ConfigureSpatialSource(slowTensionSource, 0.9f, 14.5f);
            slowTensionSource.loop = true;
            slowTensionSource.volume = 0f;
        }
    }

    private void ConfigureSpatialSource(AudioSource source, float minDistance, float maxDistance)
    {
        source.playOnAwake = false;
        source.loop = false;
        source.spatialBlend = 1f;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = minDistance;
        source.maxDistance = maxDistance;
    }

    private void ResolveReferences()
    {
        if (viewSwitcher == null)
        {
            viewSwitcher = ViewSwitcher.Instance != null ? ViewSwitcher.Instance : Object.FindFirstObjectByType<ViewSwitcher>();
        }

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }
    }

    private void LoadDefaultProjectClipsIfNeeded()
    {
        AudioClip customFlashlightClick = LoadSfxClip("sfx_flashlight_click_custom");
        flashlightClickClip = customFlashlightClick != null ? customFlashlightClick : flashlightClickClip != null ? flashlightClickClip : LoadSfxClip("sfx_flashlight_click", "flashlight_click", "lanterna", "laterna", "som da lanterna", "som da laterna");
        flashlightHumClip = flashlightHumClip != null ? flashlightHumClip : LoadSfxClip("sfx_flashlight_hum", "flashlight_hum", "lanterna_hum", "laterna_hum");
        corridorVoiceLoopClip = corridorVoiceLoopClip != null ? corridorVoiceLoopClip : LoadSfxClip("sfx_corridor_voices_custom", "helamangile-whisper-voices-1-193087", "sfx_corridor_voices");
        corridorHitClip = corridorHitClip != null ? corridorHitClip : LoadSfxClip("sfx_corridor_hit_custom", "nematoki-metal-hit-2-287908", "sfx_corridor_hit", "sfx_metal_hit");
        slowTensionClip = slowTensionClip != null ? slowTensionClip : LoadSfxClip("sfx_slow_tension_custom", "freesound_community-slow-scream-98485", "sfx_slow_stream", "sfx_tension_drone");
        corridorWhisperClip = corridorWhisperClip != null ? corridorWhisperClip : LoadSfxClip("sfx_corridor_whisper");
        corridorNoiseClip = corridorNoiseClip != null ? corridorNoiseClip : LoadSfxClip("sfx_corridor_noise");
        if (anomalyLaughClips == null || anomalyLaughClips.Length == 0)
        {
            anomalyLaughClips = LoadSfxClips(
                "sfx_anomaly_laugh_01_custom",
                "sfx_anomaly_laugh_02_custom",
                "sfx_anomaly_laugh_03_custom"
            );
        }

        anomalyLaughClip = anomalyLaughClip != null ? anomalyLaughClip : LoadSfxClip("sfx_anomaly_laugh_01_custom", "sfx_anomaly_laugh");
        jumpscareClip = jumpscareClip != null ? jumpscareClip : LoadSfxClip("sfx_jumpscare");
    }

    private AudioClip GetRandomAnomalyLaughClip()
    {
        if (anomalyLaughClips != null && anomalyLaughClips.Length > 0)
        {
            int validCount = 0;
            for (int i = 0; i < anomalyLaughClips.Length; i++)
            {
                if (anomalyLaughClips[i] != null)
                {
                    validCount++;
                }
            }

            if (validCount > 0)
            {
                int targetIndex = Random.Range(0, validCount);
                for (int i = 0; i < anomalyLaughClips.Length; i++)
                {
                    if (anomalyLaughClips[i] == null)
                    {
                        continue;
                    }

                    if (targetIndex == 0)
                    {
                        return anomalyLaughClips[i];
                    }

                    targetIndex--;
                }
            }
        }

        return anomalyLaughClip != null ? anomalyLaughClip : generatedAnomalyLaughClip;
    }

    private AudioClip[] LoadSfxClips(params string[] clipNames)
    {
        var clips = new List<AudioClip>();
        if (clipNames == null)
        {
            return clips.ToArray();
        }

        foreach (string clipName in clipNames)
        {
            AudioClip clip = LoadSfxClip(clipName);
            if (clip != null && !clips.Contains(clip))
            {
                clips.Add(clip);
            }
        }

        return clips.ToArray();
    }

    private AudioClip LoadSfxClip(params string[] clipNames)
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

            AudioClip resourceClip = Resources.Load<AudioClip>("Audio/SFX/" + clipName);
            if (resourceClip != null)
            {
                return resourceClip;
            }

#if UNITY_EDITOR
            const string folderPath = "Assets/Audio/SFX";
            if (!AssetDatabase.IsValidFolder(folderPath))
            {
                continue;
            }

            string[] guids = AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { folderPath });
            foreach (string guid in guids)
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                if (System.IO.Path.GetFileNameWithoutExtension(path) == clipName)
                {
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(path);
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

    private AudioClip CreateFlashlightClickClip()
    {
        const int sampleRate = 44100;
        const float duration = 0.24f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.38f);
            float switchSnap = rawNoise * Mathf.Exp(-time * 84f) * 0.36f;
            float secondContact = Random.Range(-1f, 1f) * Mathf.Exp(-Mathf.Max(0f, time - 0.055f) * 105f) * 0.24f;
            float metalBody = Mathf.Sin(2f * Mathf.PI * 185f * time) * Mathf.Exp(-time * 22f) * 0.12f;
            float springClick = Mathf.Sin(2f * Mathf.PI * 960f * time) * Mathf.Exp(-time * 48f) * 0.055f;
            float bulbBuzz = Mathf.Sin(2f * Mathf.PI * 1280f * time) * Mathf.Exp(-Mathf.Max(0f, time - 0.035f) * 18f) * 0.018f;
            float contactDust = previousNoise * Mathf.Exp(-Mathf.Max(0f, time - 0.020f) * 30f) * 0.030f;
            samples[i] = Mathf.Clamp((switchSnap + secondContact + metalBody + springClick + bulbBuzz + contactDust) * (1f - t * 0.08f), -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Flashlight Click", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateFlashlightHumClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.6f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float phase = Mathf.Repeat(time / duration, 1f);
            float edge = Mathf.Min(1f, Mathf.Min(phase, 1f - phase) * 16f);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.025f);
            float filament = Mathf.Sin(2f * Mathf.PI * 118f * time) * 0.018f;
            float electric = Mathf.Sin(2f * Mathf.PI * 236f * time) * 0.008f;
            float hiss = previousNoise * 0.012f;
            samples[i] = Mathf.Clamp((filament + electric + hiss) * edge, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Flashlight Hum", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCorridorWhisperClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.72f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.032f);
            float syllables = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 4.2f * time + 0.8f)), 2.4f);
            float throat = Mathf.Sin(2f * Mathf.PI * (128f + Mathf.Sin(time * 9f) * 12f) * time) * 0.022f;
            float air = previousNoise * 0.145f;
            samples[i] = Mathf.Clamp((air + throat) * syllables * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Corridor Whisper", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateCorridorNoiseClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.1f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.05f);
            float metalDrag = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(92f, 54f, t) * time) * 0.08f;
            float buzz = Mathf.Sin(2f * Mathf.PI * (184f + Mathf.Sin(time * 13f) * 23f) * time) * 0.030f;
            samples[i] = Mathf.Clamp((previousNoise * 0.11f + metalDrag + buzz) * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Corridor Noise", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private AudioClip CreateAnomalyLaughClip()
    {
        const int sampleRate = 44100;
        const float duration = 1.04f;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        var samples = new float[sampleCount];
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / duration;
            float envelope = Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            float rawNoise = Random.Range(-1f, 1f);
            previousNoise = Mathf.Lerp(previousNoise, rawNoise, 0.040f);
            float giggleGate = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 6.8f * time)), 3.0f);
            float pitchWobble = Mathf.Sin(2f * Mathf.PI * 8.5f * time) * 21f;
            float voice = Mathf.Sin(2f * Mathf.PI * (430f + pitchWobble) * time) * 0.095f;
            float secondVoice = Mathf.Sin(2f * Mathf.PI * (302f - pitchWobble * 0.35f) * time) * 0.050f;
            float breath = previousNoise * 0.052f;
            samples[i] = Mathf.Clamp((voice + secondVoice + breath) * giggleGate * envelope, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Anomaly Laugh", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }
}
