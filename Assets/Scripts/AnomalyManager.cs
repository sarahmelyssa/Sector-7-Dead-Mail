using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnomalyManager : MonoBehaviour
{
    public static AnomalyManager Instance { get; private set; }

    [Header("References")]
    [SerializeField] private GameManager gameManager = null;
    [SerializeField] private ViewSwitcher viewSwitcher = null;
    [SerializeField] private SanityManager sanityManager = null;
    [SerializeField] private AudioManager audioManager = null;

    [Header("Anomalies")]
    [SerializeField] private List<AnomalyEvent> anomalies = new List<AnomalyEvent>();
    [SerializeField] private float baseAnomalyInterval = 24f;
    [SerializeField] private float minAnomalyInterval = 7f;
    [SerializeField] private int currentDangerLevel;
    [SerializeField] private bool anomalyActive;

    [Header("Chance On Wrong Decision")]
    [SerializeField, Range(0f, 1f)] private float baseActivationChance = 0.18f;
    [SerializeField, Range(0f, 1f)] private float chancePerDangerLevel = 0.12f;

    [Header("Legacy Scene Objects")]
    [SerializeField] private List<GameObject> anomalyObjects = new List<GameObject>();

    private AnomalyEvent activeAnomaly;
    private Coroutine anomalyRoutine;
    private float nextAnomalyTime;
    private bool wasLookingBack;

    public bool AnomalyActive => anomalyActive;
    public int CurrentDangerLevel => currentDangerLevel;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        ResolveReferences();
        CollectSceneAnomalies();
        DeactivateLegacyObjects();
        ScheduleNextAnomaly();
    }

    private void OnEnable()
    {
        ViewSwitcher.ViewChanged += OnViewChanged;
    }

    private void OnDisable()
    {
        ViewSwitcher.ViewChanged -= OnViewChanged;
    }

    private void Update()
    {
        ResolveReferences();

        if (gameManager != null && !gameManager.IsPlaying)
        {
            StopActiveAnomaly();
            return;
        }

        currentDangerLevel = gameManager != null ? gameManager.dangerLevel : currentDangerLevel;

        bool isLookingBack = viewSwitcher != null && viewSwitcher.IsLookingBack;
        if (isLookingBack && !wasLookingBack)
        {
            PlayerLookedBack();
        }
        wasLookingBack = isLookingBack;

        if (!anomalyActive && Time.time >= nextAnomalyTime)
        {
            TryStartRandomAnomaly();
        }
    }

    public void SetAnomalyObjects(List<GameObject> objects)
    {
        anomalyObjects = objects ?? new List<GameObject>();

        foreach (GameObject anomalyObject in anomalyObjects)
        {
            if (anomalyObject == null)
            {
                continue;
            }

            AnomalyEvent anomalyEvent = anomalyObject.GetComponent<AnomalyEvent>();
            if (anomalyEvent == null)
            {
                anomalyEvent = anomalyObject.AddComponent<ShadowInDoorAnomaly>();
            }

            if (!anomalies.Contains(anomalyEvent))
            {
                anomalies.Add(anomalyEvent);
            }
        }

        DeactivateLegacyObjects();
    }

    public void SetActivationSettings(float baseChance, float chancePerDanger, int maxAnomaliesPerActivation)
    {
        baseActivationChance = Mathf.Clamp01(baseChance);
        chancePerDangerLevel = Mathf.Clamp01(chancePerDanger);
    }

    public bool TryActivateAnomaly(int dangerLevel)
    {
        currentDangerLevel = Mathf.Max(0, dangerLevel);

        if (!CanStartAnomaly())
        {
            return false;
        }

        float chance = Mathf.Clamp01(baseActivationChance + currentDangerLevel * chancePerDangerLevel);
        if (Random.value > chance)
        {
            nextAnomalyTime = Mathf.Min(nextAnomalyTime, Time.time + GetCurrentInterval() * 0.5f);
            return false;
        }

        return TryStartRandomAnomaly();
    }

    public void OnDangerLevelChanged(int dangerLevel)
    {
        TryActivateAnomaly(dangerLevel);
        MobManager mobManager = Object.FindFirstObjectByType<MobManager>();
        mobManager?.ConsiderSpawnFromDanger(dangerLevel);
    }

    public bool DebugTriggerRandomAnomaly()
    {
        return TryStartRandomAnomaly();
    }

    public void PlayerLookedBack()
    {
        if (!anomalyActive || activeAnomaly == null)
        {
            return;
        }

        activeAnomaly.OnPlayerLookedBack();
    }

    private bool TryStartRandomAnomaly()
    {
        if (!CanStartAnomaly())
        {
            return false;
        }

        AnomalyEvent anomaly = PickAnomaly();
        if (anomaly == null)
        {
            ScheduleNextAnomaly();
            return false;
        }

        anomalyRoutine = StartCoroutine(RunAnomaly(anomaly));
        return true;
    }

    private IEnumerator RunAnomaly(AnomalyEvent anomaly)
    {
        anomalyActive = true;
        activeAnomaly = anomaly;
        anomaly.Configure(gameManager, viewSwitcher, sanityManager, audioManager);
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.38f, 0.18f);
        anomaly.StartAnomaly();
        audioManager?.PlayAnomalyActivated();

        float elapsed = 0f;
        while (elapsed < anomaly.Duration && !anomaly.WasResolved())
        {
            if (gameManager != null && !gameManager.IsPlaying)
            {
                break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        bool ignored = anomaly.RequiresLookingBack && !anomaly.WasResolved();
        if (ignored && sanityManager != null && anomaly.SanityDamageIfIgnored > 0 && (gameManager == null || gameManager.IsPlaying))
        {
            sanityManager.TakeSanityDamage(anomaly.SanityDamageIfIgnored);
            HorrorEffectsManager.Instance?.OnPlayerDamaged();
        }

        anomaly.EndAnomaly();
        activeAnomaly = null;
        anomalyActive = false;
        anomalyRoutine = null;
        ScheduleNextAnomaly();
    }

    private bool CanStartAnomaly()
    {
        if (anomalyActive)
        {
            return false;
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return false;
        }

        if (anomalies == null || anomalies.Count == 0)
        {
            CollectSceneAnomalies();
        }

        return anomalies != null && anomalies.Count > 0;
    }

    private AnomalyEvent PickAnomaly()
    {
        var available = new List<AnomalyEvent>();
        foreach (AnomalyEvent anomaly in anomalies)
        {
            if (anomaly != null && !anomaly.IsRunning)
            {
                available.Add(anomaly);
            }
        }

        if (available.Count == 0)
        {
            return null;
        }

        return available[Random.Range(0, available.Count)];
    }

    private void ScheduleNextAnomaly()
    {
        float interval = GetCurrentInterval();
        nextAnomalyTime = Time.time + Random.Range(interval * 0.75f, interval * 1.25f);
    }

    private float GetCurrentInterval()
    {
        float dangerReduction = Mathf.Max(0f, currentDangerLevel) * 3.5f;
        return Mathf.Max(minAnomalyInterval, baseAnomalyInterval - dangerReduction);
    }

    public void StopAllAnomalies()
    {
        StopActiveAnomaly();
    }

    private void StopActiveAnomaly()
    {
        if (anomalyRoutine != null)
        {
            StopCoroutine(anomalyRoutine);
            anomalyRoutine = null;
        }

        if (activeAnomaly != null)
        {
            activeAnomaly.EndAnomaly();
        }

        activeAnomaly = null;
        anomalyActive = false;
        DeactivateLegacyObjects();
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (viewSwitcher == null)
        {
            viewSwitcher = ViewSwitcher.Instance != null ? ViewSwitcher.Instance : Object.FindFirstObjectByType<ViewSwitcher>();
        }

        if (sanityManager == null)
        {
            sanityManager = Object.FindFirstObjectByType<SanityManager>();
            if (sanityManager == null)
            {
                sanityManager = gameObject.AddComponent<SanityManager>();
            }
        }

        if (audioManager == null)
        {
            audioManager = AudioManager.Instance != null ? AudioManager.Instance : Object.FindFirstObjectByType<AudioManager>();
        }
    }

    private void CollectSceneAnomalies()
    {
        AnomalyEvent[] sceneAnomalies = Object.FindObjectsByType<AnomalyEvent>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (AnomalyEvent anomaly in sceneAnomalies)
        {
            if (anomaly != null && !anomalies.Contains(anomaly))
            {
                anomalies.Add(anomaly);
            }
        }
    }

    private void DeactivateLegacyObjects()
    {
        foreach (GameObject anomalyObject in anomalyObjects)
        {
            if (anomalyObject != null)
            {
                AnomalyEvent anomaly = anomalyObject.GetComponent<AnomalyEvent>();
                if (anomaly is LightFlickerAnomaly || anomaly is ReportGlitchAnomaly || anomaly is BoxWhisperAnomaly)
                {
                    continue;
                }

                anomalyObject.SetActive(false);
            }
        }
    }

    private void OnViewChanged(bool lookingBack)
    {
        if (lookingBack)
        {
            PlayerLookedBack();
        }
    }
}
