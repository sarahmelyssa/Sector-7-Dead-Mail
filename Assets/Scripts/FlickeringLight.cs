using UnityEngine;

public class FlickeringLight : MonoBehaviour
{
    [SerializeField] private Light targetLight = null;
    [SerializeField] private float minimumIntensity = 0.15f;
    [SerializeField] private float maximumIntensity = 1.8f;
    [SerializeField] private float flickerSpeed = 1f;
    [SerializeField] private float dangerSpeedMultiplier = 0.35f;
    [SerializeField] private float smoothness = 18f;
    [SerializeField] private bool flickerAllowed = true;
    [SerializeField] private bool requireDangerLevel = true;

    private GameManager gameManager;
    private float defaultIntensity;
    private float nextFlickerTime;
    private float targetIntensity;
    private float nextFlickerSoundTime;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        if (targetLight != null)
        {
            defaultIntensity = targetLight.intensity;
            targetIntensity = defaultIntensity;
        }

        gameManager = Object.FindFirstObjectByType<GameManager>();
        ScheduleNextFlicker(0);
    }

    private void Update()
    {
        if (targetLight == null)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        int dangerLevel = gameManager != null ? gameManager.dangerLevel : 0;
        bool shouldFlicker = flickerAllowed
            && (!requireDangerLevel || dangerLevel > 0)
            && (gameManager == null || gameManager.IsPlaying);

        if (!shouldFlicker)
        {
            targetIntensity = defaultIntensity;
            targetLight.intensity = Mathf.Lerp(targetLight.intensity, defaultIntensity, Time.deltaTime * smoothness);
            return;
        }

        if (Time.time >= nextFlickerTime)
        {
            targetIntensity = Random.Range(minimumIntensity, maximumIntensity);
            if (Time.time >= nextFlickerSoundTime && Mathf.Abs(targetLight.intensity - targetIntensity) > 0.35f)
            {
                AudioManager.Instance?.PlayLightFlicker(targetLight.transform.position);
                nextFlickerSoundTime = Time.time + Random.Range(0.45f, 0.9f);
            }

            ScheduleNextFlicker(dangerLevel);
        }

        targetLight.intensity = Mathf.Lerp(targetLight.intensity, targetIntensity, Time.deltaTime * smoothness);
    }

    public void SetTargetLight(Light lightToFlicker)
    {
        targetLight = lightToFlicker;

        if (targetLight != null)
        {
            defaultIntensity = targetLight.intensity;
            targetIntensity = defaultIntensity;
        }
    }

    public void SetFlickerAllowed(bool allowed)
    {
        flickerAllowed = allowed;
    }

    public void Configure(float minimum, float maximum, float speed, bool onlyWhenDangerous)
    {
        minimumIntensity = Mathf.Max(0f, minimum);
        maximumIntensity = Mathf.Max(minimumIntensity, maximum);
        flickerSpeed = Mathf.Max(0.01f, speed);
        requireDangerLevel = onlyWhenDangerous;

        if (targetLight != null)
        {
            defaultIntensity = targetLight.intensity;
            targetIntensity = defaultIntensity;
        }
    }

    private void ScheduleNextFlicker(int dangerLevel)
    {
        float dangerSpeed = 1f + dangerLevel * dangerSpeedMultiplier;
        float interval = Random.Range(0.04f, 0.22f) / Mathf.Max(0.01f, flickerSpeed * dangerSpeed);
        nextFlickerTime = Time.time + interval;
    }
}
