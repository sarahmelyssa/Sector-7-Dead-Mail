using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReportGlitchAnomaly : AnomalyEvent
{
    [SerializeField] private ReportPanel reportPanel = null;
    [SerializeField] private float glitchDuration = 1f;
    [SerializeField] private float flickerStep = 0.055f;
    [SerializeField] private float imageJitter = 14f;

    private Coroutine glitchRoutine;
    private Vector2 originalAnchoredPosition;
    private Color originalColor = Color.white;

    private void Awake()
    {
        ApplyDefaultsIfNeeded();
    }

    private void Reset()
    {
        ApplyDefaultsIfNeeded(true);
    }

    private void ApplyDefaultsIfNeeded(bool force = false)
    {
        anomalyName = "Report Glitch";
        if (force || duration <= 0f)
        {
            duration = 1f;
        }
        requiresLookingBack = false;
        sanityDamageIfIgnored = 0;
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.42f, 0.22f);
        UIManager.Instance?.PlayBriefingTextGlitch(glitchDuration);

        if (reportPanel == null)
        {
            reportPanel = ReportPanel.Instance != null ? ReportPanel.Instance : Object.FindFirstObjectByType<ReportPanel>();
        }

        if (reportPanel != null)
        {
            glitchRoutine = StartCoroutine(GlitchReport());
        }
    }

    public override void EndAnomaly()
    {
        if (glitchRoutine != null)
        {
            StopCoroutine(glitchRoutine);
            glitchRoutine = null;
        }

        RestoreReportImages();
        Resolve();
        base.EndAnomaly();
    }

    private IEnumerator GlitchReport()
    {
        float elapsed = 0f;
        Image reportImage = reportPanel != null ? reportPanel.ReportImage : null;
        if (reportImage == null)
        {
            yield break;
        }

        RectTransform imageRect = reportImage.rectTransform;
        originalAnchoredPosition = imageRect != null ? imageRect.anchoredPosition : Vector2.zero;
        originalColor = reportImage.color;
        HorrorEffectsManager.Instance?.PlayVhsDistortion(glitchDuration, 0.22f);

        while (elapsed < glitchDuration)
        {
            elapsed += flickerStep;
            float alpha = Random.value > 0.5f ? 0.42f : 1f;
            Color color = Color.Lerp(originalColor, new Color(0.62f, 0.48f, 0.82f, originalColor.a), Random.Range(0.12f, 0.36f));
            color.a = alpha;
            reportImage.color = color;

            if (imageRect != null)
            {
                imageRect.anchoredPosition = originalAnchoredPosition + new Vector2(Random.Range(-imageJitter, imageJitter), Random.Range(-imageJitter * 0.35f, imageJitter * 0.35f));
            }

            yield return new WaitForSeconds(flickerStep);
        }

        Resolve();
        RestoreReportImages();
    }

    private void RestoreReportImages()
    {
        if (reportPanel == null)
        {
            return;
        }

        Image image = reportPanel.ReportImage;
        if (image != null)
        {
            image.color = originalColor.a <= 0f ? Color.white : originalColor;
            if (image.rectTransform != null)
            {
                image.rectTransform.anchoredPosition = originalAnchoredPosition;
            }
        }
    }
}
