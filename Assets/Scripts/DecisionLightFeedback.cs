using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class DecisionLightFeedback : MonoBehaviour
{
    [SerializeField] private Light targetLight = null;
    [SerializeField] private Color defaultColor = new Color(1f, 0.855f, 0.545f);
    [SerializeField] private Color correctColor = new Color(0.22f, 0.92f, 0.34f);
    [SerializeField] private Color wrongColor = new Color(1f, 0.070f, 0.045f);
    [SerializeField] private float flashDuration = 0.34f;
    [SerializeField] private float flashHoldDuration = 0.08f;

    private Coroutine flashRoutine;

    private void Awake()
    {
        if (targetLight == null)
        {
            targetLight = GetComponent<Light>();
        }

        if (targetLight != null)
        {
            defaultColor = targetLight.color;
        }
    }

    private void OnEnable()
    {
        DecisionManager.DecisionResolved += OnDecisionResolved;
    }

    private void OnDisable()
    {
        DecisionManager.DecisionResolved -= OnDecisionResolved;
    }

    public void Configure(Light lightToFlash, Color normalColor)
    {
        targetLight = lightToFlash;
        defaultColor = normalColor;

        if (targetLight != null)
        {
            targetLight.color = defaultColor;
        }
    }

    private void OnDecisionResolved(bool correct)
    {
        if (targetLight == null)
        {
            return;
        }

        if (flashRoutine != null)
        {
            StopCoroutine(flashRoutine);
        }

        flashRoutine = StartCoroutine(Flash(correct ? correctColor : wrongColor));
    }

    private IEnumerator Flash(Color feedbackColor)
    {
        float fadeDuration = Mathf.Max(0.01f, (flashDuration - flashHoldDuration) * 0.5f);
        float elapsed = 0f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            targetLight.color = Color.Lerp(defaultColor, feedbackColor, t * t * (3f - 2f * t));
            yield return null;
        }

        targetLight.color = feedbackColor;

        if (flashHoldDuration > 0f)
        {
            yield return new WaitForSeconds(flashHoldDuration);
        }

        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            targetLight.color = Color.Lerp(feedbackColor, defaultColor, t * t * (3f - 2f * t));
            yield return null;
        }

        targetLight.color = defaultColor;
        flashRoutine = null;
    }
}
