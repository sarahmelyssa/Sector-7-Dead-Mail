using UnityEngine;

public class AnomalyVisualPulse : MonoBehaviour
{
    [SerializeField] private float pulseSpeed = 7.5f;
    [SerializeField] private float scalePulse = 0.022f;
    [SerializeField] private float lightPulse = 0.38f;
    [SerializeField] private float positionTwitch = 0.010f;
    [SerializeField] private float rotationTwitch = 1.35f;
    [SerializeField] private Color pulseTint = new Color(0.360f, 0.030f, 0.110f);

    private Renderer[] renderers;
    private Light[] lights;
    private Color[] baseColors;
    private float[] baseIntensities;
    private Vector3 baseScale;
    private Vector3 basePosition;
    private Quaternion baseRotation;

    private void Awake()
    {
        CacheTargets();
    }

    private void OnEnable()
    {
        CacheTargets();
        baseScale = transform.localScale;
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
    }

    private void Update()
    {
        if (renderers == null || lights == null)
        {
            CacheTargets();
        }

        float wave = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        float twitch = Random.value > 0.965f ? Random.Range(0.78f, 1.12f) : 1f;
        Vector3 distortion = new Vector3(
            1f + scalePulse * wave * 0.45f,
            1f + scalePulse * wave * 1.25f,
            1f - scalePulse * wave * 0.30f
        );
        transform.localScale = Vector3.Scale(baseScale, distortion);

        if (positionTwitch > 0f && Random.value > 0.93f)
        {
            transform.localPosition = basePosition + new Vector3(Random.Range(-positionTwitch, positionTwitch), Random.Range(-positionTwitch, positionTwitch), 0f);
            transform.localRotation = baseRotation * Quaternion.Euler(
                Random.Range(-rotationTwitch, rotationTwitch),
                Random.Range(-rotationTwitch, rotationTwitch),
                Random.Range(-rotationTwitch, rotationTwitch)
            );
        }
        else
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition, Time.deltaTime * 8f);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation, Time.deltaTime * 8f);
        }

        for (int i = 0; i < lights.Length; i++)
        {
            if (lights[i] != null)
            {
                lights[i].intensity = baseIntensities[i] * Mathf.Lerp(1f - lightPulse, 1f + lightPulse * 0.45f, wave) * twitch;
            }
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer targetRenderer = renderers[i];
            if (targetRenderer == null || targetRenderer.sharedMaterial == null)
            {
                continue;
            }

            Material material = targetRenderer.material;
            Color color = Color.Lerp(baseColors[i], pulseTint, wave * 0.22f);
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
                material.SetColor("_EmissionColor", color * Mathf.Lerp(0.12f, 1.05f, wave));
                material.EnableKeyword("_EMISSION");
            }
        }
    }

    private void OnDisable()
    {
        transform.localScale = baseScale == Vector3.zero ? Vector3.one : baseScale;
        transform.localPosition = basePosition;
        transform.localRotation = baseRotation;

        if (lights != null)
        {
            for (int i = 0; i < lights.Length; i++)
            {
                if (lights[i] != null)
                {
                    lights[i].intensity = baseIntensities[i];
                }
            }
        }
    }

    private void CacheTargets()
    {
        renderers = GetComponentsInChildren<Renderer>(true);
        lights = GetComponentsInChildren<Light>(true);
        baseColors = new Color[renderers.Length];
        baseIntensities = new float[lights.Length];

        for (int i = 0; i < renderers.Length; i++)
        {
            Material material = renderers[i] != null ? renderers[i].sharedMaterial : null;
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
                baseColors[i] = Color.white;
            }
        }

        for (int i = 0; i < lights.Length; i++)
        {
            baseIntensities[i] = lights[i] != null ? lights[i].intensity : 0f;
        }

        if (baseScale == Vector3.zero)
        {
            baseScale = transform.localScale;
        }
    }
}
