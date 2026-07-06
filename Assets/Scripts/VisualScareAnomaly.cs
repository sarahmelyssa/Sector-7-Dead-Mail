using System.Collections;
using UnityEngine;

public class VisualScareAnomaly : AnomalyEvent
{
    [SerializeField] private GameObject visualRoot = null;
    [SerializeField] private float revealDuration = 0.45f;
    [SerializeField] private float localJitter = 0.012f;

    private Coroutine visualRoutine;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseLocalPosition;

    private void Awake()
    {
        ApplyDefaultsIfNeeded();
    }

    private void Reset()
    {
        ApplyDefaultsIfNeeded(true);
        visualRoot = gameObject;
    }

    private void ApplyDefaultsIfNeeded(bool force = false)
    {
        anomalyName = "Visual Scare";
        if (force || duration <= 0f)
        {
            duration = 4.5f;
        }

        requiresLookingBack = false;
        sanityDamageIfIgnored = 0;
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        if (visualRoot == null)
        {
            visualRoot = gameObject;
        }

        visualRoot.SetActive(true);
        baseScale = visualRoot.transform.localScale == Vector3.zero ? Vector3.one : visualRoot.transform.localScale;
        baseLocalPosition = visualRoot.transform.localPosition;
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.22f, 0.12f);

        if (visualRoutine != null)
        {
            StopCoroutine(visualRoutine);
        }

        visualRoutine = StartCoroutine(RevealAndDistort());
    }

    public override void EndAnomaly()
    {
        if (visualRoutine != null)
        {
            StopCoroutine(visualRoutine);
            visualRoutine = null;
        }

        if (visualRoot == null)
        {
            visualRoot = gameObject;
        }

        visualRoot.transform.localScale = baseScale;
        visualRoot.transform.localPosition = baseLocalPosition;
        visualRoot.SetActive(false);
        Resolve();
        base.EndAnomaly();
    }

    private IEnumerator RevealAndDistort()
    {
        float elapsed = 0f;
        while (isRunning && visualRoot != null)
        {
            elapsed += Time.deltaTime;
            float build = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, revealDuration));
            float pulse = (Mathf.Sin(Time.time * 9.5f) + 1f) * 0.5f;
            visualRoot.transform.localScale = baseScale * Mathf.Lerp(0.86f, 1f + pulse * 0.018f, build);
            visualRoot.transform.localPosition = baseLocalPosition + new Vector3(
                Random.Range(-localJitter, localJitter) * build,
                Random.Range(-localJitter, localJitter) * build,
                Random.Range(-localJitter, localJitter) * build * 0.5f
            );

            yield return null;
        }
    }
}
