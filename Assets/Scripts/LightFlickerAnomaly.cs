using System.Collections;
using UnityEngine;

public class LightFlickerAnomaly : AnomalyEvent
{
    [SerializeField] private Light targetLight = null;
    [SerializeField] private float minIntensity = 0.05f;
    [SerializeField] private float maxIntensity = 2.4f;
    [SerializeField] private float flickerStep = 0.06f;

    private float originalIntensity;
    private Coroutine flickerRoutine;

    private void Awake()
    {
        ApplyDefaultsIfNeeded();
    }

    private void Reset()
    {
        ApplyDefaultsIfNeeded(true);
        targetLight = GetComponent<Light>();
    }

    private void ApplyDefaultsIfNeeded(bool force = false)
    {
        anomalyName = "Light Flicker";
        if (force || duration <= 0f)
        {
            duration = 3.5f;
        }
        requiresLookingBack = false;
        sanityDamageIfIgnored = 0;
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();

        if (targetLight == null)
        {
            GameObject backLight = GameObject.Find("BackFlickerLight");
            targetLight = backLight != null ? backLight.GetComponent<Light>() : Object.FindFirstObjectByType<Light>();
        }

        if (targetLight != null)
        {
            originalIntensity = targetLight.intensity;
            flickerRoutine = StartCoroutine(FlickerRoutine());
        }

        audioManager?.PlayLightFlicker();
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.28f, 0.12f);
    }

    public override void EndAnomaly()
    {
        if (flickerRoutine != null)
        {
            StopCoroutine(flickerRoutine);
            flickerRoutine = null;
        }

        if (targetLight != null)
        {
            targetLight.intensity = originalIntensity;
        }

        Resolve();
        base.EndAnomaly();
    }

    private IEnumerator FlickerRoutine()
    {
        while (isRunning && targetLight != null)
        {
            float stutter = Random.value > 0.78f ? 0f : 1f;
            targetLight.intensity = Random.Range(minIntensity, maxIntensity) * stutter;
            yield return new WaitForSeconds(Random.Range(flickerStep * 0.5f, flickerStep * 1.6f));
        }
    }
}
