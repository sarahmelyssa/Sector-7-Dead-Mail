using System.Collections;
using UnityEngine;

public class ShadowInDoorAnomaly : AnomalyEvent
{
    [SerializeField] private GameObject shadowObject = null;
    [SerializeField] private float buildUpDuration = 0.85f;
    [SerializeField] private float buildUpJitter = 0.012f;

    private Renderer[] renderers;
    private Light[] lights;
    private Color[] baseColors;
    private float[] baseLightIntensities;
    private Vector3 baseScale = Vector3.one;
    private Vector3 baseLocalPosition;
    private Coroutine buildUpRoutine;

    private void Awake()
    {
        ApplyDefaultsIfNeeded();
    }

    private void Reset()
    {
        ApplyDefaultsIfNeeded(true);
        shadowObject = gameObject;
    }

    private void ApplyDefaultsIfNeeded(bool force = false)
    {
        anomalyName = "Shadow In Door";
        if (force || duration <= 0f)
        {
            duration = 5f;
        }
        requiresLookingBack = true;
        if (force || sanityDamageIfIgnored <= 0)
        {
            sanityDamageIfIgnored = 1;
        }
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        if (shadowObject == null)
        {
            shadowObject = gameObject;
        }

        shadowObject.SetActive(true);
        CacheVisualTargets();
        SetVisualStrength(0f);

        if (buildUpRoutine != null)
        {
            StopCoroutine(buildUpRoutine);
        }

        buildUpRoutine = StartCoroutine(BuildShadowIn());
    }

    public override void EndAnomaly()
    {
        if (buildUpRoutine != null)
        {
            StopCoroutine(buildUpRoutine);
            buildUpRoutine = null;
        }

        if (shadowObject == null)
        {
            shadowObject = gameObject;
        }

        SetVisualStrength(0f);
        shadowObject.transform.localPosition = baseLocalPosition;
        shadowObject.SetActive(false);
        base.EndAnomaly();
    }

    public override void OnPlayerLookedBack()
    {
        base.OnPlayerLookedBack();
        if (WasResolved())
        {
            if (shadowObject == null)
            {
                shadowObject = gameObject;
            }

            if (buildUpRoutine != null)
            {
                StopCoroutine(buildUpRoutine);
                buildUpRoutine = null;
            }

            SetVisualStrength(0f);
            shadowObject.transform.localPosition = baseLocalPosition;
            shadowObject.SetActive(false);
            audioManager?.PlayMobDisappears();
            HorrorEffectsManager.Instance?.PlayVhsDistortion(0.20f, 0.16f);
        }
    }

    private IEnumerator BuildShadowIn()
    {
        float elapsed = 0f;
        while (elapsed < buildUpDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, buildUpDuration));
            float eased = t * t * (3f - 2f * t);
            SetVisualStrength(eased);

            if (shadowObject != null)
            {
                shadowObject.transform.localScale = Vector3.Lerp(baseScale * 0.82f, baseScale, eased);
                shadowObject.transform.localPosition = baseLocalPosition + new Vector3(
                    Random.Range(-buildUpJitter, buildUpJitter) * eased,
                    Random.Range(-buildUpJitter, buildUpJitter) * eased,
                    Random.Range(-buildUpJitter, buildUpJitter) * eased * 0.35f
                );
            }

            yield return null;
        }

        SetVisualStrength(1f);
        if (shadowObject != null)
        {
            shadowObject.transform.localScale = baseScale;
            shadowObject.transform.localPosition = baseLocalPosition;
        }

        buildUpRoutine = null;
    }

    private void CacheVisualTargets()
    {
        if (shadowObject == null)
        {
            shadowObject = gameObject;
        }

        renderers = shadowObject.GetComponentsInChildren<Renderer>(true);
        lights = shadowObject.GetComponentsInChildren<Light>(true);
        baseColors = new Color[renderers.Length];
        baseLightIntensities = new float[lights.Length];
        baseScale = shadowObject.transform.localScale == Vector3.zero ? Vector3.one : shadowObject.transform.localScale;
        baseLocalPosition = shadowObject.transform.localPosition;

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].material : null;
            if (material != null && material.HasProperty("_BaseColor"))
            {
                baseColors[i] = material.GetColor("_BaseColor");
            }
            else if (material != null && material.HasProperty("_Color"))
            {
                baseColors[i] = material.GetColor("_Color");
            }
            else
            {
                baseColors[i] = Color.black;
            }
        }

        for (int i = 0; i < lights.Length; i++)
        {
            baseLightIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;
        }
    }

    private void SetVisualStrength(float strength)
    {
        strength = Mathf.Clamp01(strength);

        if (renderers != null)
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer targetRenderer = renderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                Material material = targetRenderer.material;
                Color color = Color.Lerp(Color.black, baseColors[i], strength);
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
                    material.SetColor("_EmissionColor", color * Mathf.Lerp(0f, 2.2f, strength));
                    material.EnableKeyword("_EMISSION");
                }
            }
        }

        if (lights == null)
        {
            return;
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].intensity = baseLightIntensities[i] * strength;
            }
        }
    }
}
