using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HorrorEffectsManager : MonoBehaviour
{
    public static HorrorEffectsManager Instance { get; private set; }

    [Header("Damage Flash")]
    [SerializeField] private Color damageFlashColor = new Color(0.75f, 0f, 0f, 0.26f);
    [SerializeField] private float damageFlashDuration = 0.18f;
    [SerializeField] private float lowSanityMaxDarkness = 0.36f;
    [SerializeField] private float lowSanityFadeSpeed = 4f;

    [Header("Feedback Lights")]
    [SerializeField] private Light correctDecisionLight = null;
    [SerializeField] private Light wrongDecisionLight = null;
    [SerializeField] private Light backFlickerLight = null;
    [SerializeField] private float feedbackFlashIntensity = 2.8f;
    [SerializeField] private float feedbackFlashDuration = 0.18f;

    [Header("Strange Text")]
    [SerializeField] private bool enableStrangeText = false;
    [SerializeField] private float strangeTextChance = 0.25f;
    [SerializeField] private float strangeTextDuration = 1f;
    [SerializeField] private string[] strangeMessages =
    {
        "NAO ERA PARA ESTAR AQUI",
        "OLHA OUTRA VEZ",
        "A CAIXA RESPIRA",
        "O RELATORIO MENTIU",
        "ELE ESTA ATRAS"
    };

    [Header("Ambient Light")]
    [SerializeField] private float ambientDimPerDangerLevel = 0.12f;
    [SerializeField] private float minimumAmbientMultiplier = 0.25f;
    [SerializeField] private float ambientFadeSpeed = 2.8f;

    [Header("VHS Distortion")]
    [SerializeField] private bool enableVhsDistortion = false;
    [SerializeField] private Color vhsTintColor = new Color(0.340f, 0.085f, 0.620f, 0.18f);
    [SerializeField] private Color vhsTearColor = new Color(0.620f, 0.320f, 1f, 0.28f);

    private Image damageFlashImage;
    private Image lowSanityImage;
    private Image vhsDistortionImage;
    private Image vhsTearImage;
    private RectTransform vhsTearRect;
    private TMP_Text strangeText;
    private Coroutine damageFlashRoutine;
    private Coroutine strangeTextRoutine;
    private Coroutine vhsDistortionRoutine;
    private Coroutine correctFlashRoutine;
    private Coroutine wrongFlashRoutine;
    private Coroutine backFlashRoutine;
    private Color baseAmbientLight;
    private int currentDangerLevel;
    private GameManager gameManager;
    private PlayerHealth playerHealth;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildOverlayUi();
    }

    private void Start()
    {
        baseAmbientLight = RenderSettings.ambientLight;
        gameManager = Object.FindFirstObjectByType<GameManager>();
        playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        ResolveFeedbackLights();
        currentDangerLevel = gameManager != null ? gameManager.dangerLevel : 0;
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && currentDangerLevel != gameManager.dangerLevel)
        {
            currentDangerLevel = gameManager.dangerLevel;
        }

        float targetMultiplier = Mathf.Clamp(1f - currentDangerLevel * ambientDimPerDangerLevel, minimumAmbientMultiplier, 1f);
        Color targetAmbient = baseAmbientLight * targetMultiplier;
        RenderSettings.ambientLight = Color.Lerp(RenderSettings.ambientLight, targetAmbient, Time.deltaTime * ambientFadeSpeed);

        UpdateLowSanityDarkness();
    }

    public void OnPlayerDamaged()
    {
        if (damageFlashRoutine != null)
        {
            StopCoroutine(damageFlashRoutine);
        }

        damageFlashRoutine = StartCoroutine(FlashDamageScreen());
    }

    public void OnDangerLevelIncreased(int dangerLevel)
    {
        currentDangerLevel = Mathf.Max(0, dangerLevel);
        AudioManager.Instance?.PlayRandomKnock(currentDangerLevel);

        if (enableStrangeText && Random.value <= strangeTextChance)
        {
            ShowStrangeText();
        }
    }

    public void OnDecisionResolved(bool correct)
    {
        ResolveFeedbackLights();
        if (correct)
        {
            StartLightFlash(correctDecisionLight, ref correctFlashRoutine, new Color(0.1f, 1f, 0.25f), feedbackFlashIntensity, feedbackFlashDuration);
        }
        else
        {
            StartLightFlash(wrongDecisionLight, ref wrongFlashRoutine, new Color(1f, 0.05f, 0.02f), feedbackFlashIntensity, feedbackFlashDuration * 1.3f);
        }
    }

    public void OnMobAppeared()
    {
        ResolveFeedbackLights();
        StartLightFlash(backFlickerLight, ref backFlashRoutine, new Color(1f, 0.04f, 0.02f), feedbackFlashIntensity * 1.35f, 0.42f);
        PlayVhsDistortion(0.55f, 0.28f);
    }

    public void MaybeShowStrangeText(float chanceMultiplier = 1f)
    {
        if (enableStrangeText && Random.value <= strangeTextChance * Mathf.Max(0f, chanceMultiplier))
        {
            ShowStrangeText();
        }
    }

    public void PlayVhsDistortion(float duration = 0.42f, float intensity = 0.25f)
    {
        if (!enableVhsDistortion)
        {
            ClearVhsDistortion();
            return;
        }

        if (vhsDistortionImage == null || vhsTearImage == null)
        {
            return;
        }

        if (vhsDistortionRoutine != null)
        {
            StopCoroutine(vhsDistortionRoutine);
        }

        vhsDistortionRoutine = StartCoroutine(VhsDistortionRoutine(Mathf.Max(0.05f, duration), Mathf.Clamp01(intensity)));
    }

    private void ClearVhsDistortion()
    {
        if (vhsDistortionRoutine != null)
        {
            StopCoroutine(vhsDistortionRoutine);
            vhsDistortionRoutine = null;
        }

        if (vhsDistortionImage != null)
        {
            vhsDistortionImage.color = Color.clear;
        }

        if (vhsTearImage != null)
        {
            vhsTearImage.color = Color.clear;
        }
    }

    private IEnumerator FlashDamageScreen()
    {
        if (damageFlashImage == null)
        {
            yield break;
        }

        float elapsed = 0f;
        damageFlashImage.color = damageFlashColor;

        while (elapsed < damageFlashDuration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(damageFlashColor.a, 0f, elapsed / damageFlashDuration);
            damageFlashImage.color = new Color(damageFlashColor.r, damageFlashColor.g, damageFlashColor.b, alpha);
            yield return null;
        }

        damageFlashImage.color = Color.clear;
        damageFlashRoutine = null;
    }

    private void UpdateLowSanityDarkness()
    {
        if (lowSanityImage == null)
        {
            return;
        }

        if (playerHealth == null)
        {
            playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        }

        float targetAlpha = 0f;
        if (playerHealth != null && playerHealth.MaxHealth > 0)
        {
            float missingRatio = 1f - playerHealth.CurrentHealth / (float)playerHealth.MaxHealth;
            targetAlpha = Mathf.Clamp01(missingRatio) * lowSanityMaxDarkness;
        }

        Color color = lowSanityImage.color;
        color.a = Mathf.Lerp(color.a, targetAlpha, Time.deltaTime * lowSanityFadeSpeed);
        lowSanityImage.color = color;
    }

    private IEnumerator VhsDistortionRoutine(float duration, float intensity)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float fade = 1f - Mathf.Clamp01(elapsed / duration);
            float pulse = Random.Range(0.30f, 1f) * intensity * fade;

            vhsDistortionImage.color = new Color(vhsTintColor.r, vhsTintColor.g, vhsTintColor.b, vhsTintColor.a * pulse);
            vhsTearImage.color = new Color(vhsTearColor.r, vhsTearColor.g, vhsTearColor.b, vhsTearColor.a * Mathf.Lerp(0.25f, 1f, pulse));

            if (vhsTearRect != null)
            {
                vhsTearRect.anchoredPosition = new Vector2(Random.Range(-18f, 18f), Random.Range(-360f, 360f));
                vhsTearRect.sizeDelta = new Vector2(0f, Random.Range(8f, 24f));
            }

            yield return new WaitForSeconds(Random.Range(0.026f, 0.070f));
        }

        vhsDistortionImage.color = Color.clear;
        vhsTearImage.color = Color.clear;
        vhsDistortionRoutine = null;
    }

    private void StartLightFlash(Light targetLight, ref Coroutine routine, Color color, float intensity, float duration)
    {
        if (targetLight == null)
        {
            return;
        }

        if (routine != null)
        {
            StopCoroutine(routine);
        }

        routine = StartCoroutine(FlashLight(targetLight, color, intensity, duration));
    }

    private IEnumerator FlashLight(Light targetLight, Color color, float intensity, float duration)
    {
        if (targetLight == null)
        {
            yield break;
        }

        bool wasEnabled = targetLight.enabled;
        Color originalColor = targetLight.color;
        float originalIntensity = targetLight.intensity;

        targetLight.enabled = true;
        targetLight.color = color;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float pulse = Mathf.PingPong(elapsed * 22f, 1f);
            targetLight.intensity = Mathf.Lerp(0.15f, intensity, pulse);
            yield return null;
        }

        targetLight.color = originalColor;
        targetLight.intensity = originalIntensity;
        targetLight.enabled = wasEnabled;
    }

    private void ShowStrangeText()
    {
        if (strangeTextRoutine != null)
        {
            StopCoroutine(strangeTextRoutine);
        }

        strangeTextRoutine = StartCoroutine(ShowStrangeTextForSeconds());
    }

    private IEnumerator ShowStrangeTextForSeconds()
    {
        if (strangeText == null)
        {
            yield break;
        }

        strangeText.text = Pick(strangeMessages);
        strangeText.rectTransform.anchoredPosition = new Vector2(Random.Range(-260f, 260f), Random.Range(-110f, 140f));
        strangeText.color = new Color(1f, 0.302f, 0.427f, 0.95f);
        strangeText.gameObject.SetActive(true);

        yield return new WaitForSeconds(strangeTextDuration);

        strangeText.gameObject.SetActive(false);
        strangeTextRoutine = null;
    }

    private void BuildOverlayUi()
    {
        var canvasObject = new GameObject("Horror Effects UI");
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 90;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var flashObject = new GameObject("Damage Flash");
        flashObject.transform.SetParent(canvasObject.transform, false);
        var flashRect = flashObject.AddComponent<RectTransform>();
        flashRect.anchorMin = Vector2.zero;
        flashRect.anchorMax = Vector2.one;
        flashRect.offsetMin = Vector2.zero;
        flashRect.offsetMax = Vector2.zero;
        damageFlashImage = flashObject.AddComponent<Image>();
        damageFlashImage.color = Color.clear;
        damageFlashImage.raycastTarget = false;

        var lowSanityObject = new GameObject("Low Sanity Darkness");
        lowSanityObject.transform.SetParent(canvasObject.transform, false);
        var lowSanityRect = lowSanityObject.AddComponent<RectTransform>();
        lowSanityRect.anchorMin = Vector2.zero;
        lowSanityRect.anchorMax = Vector2.one;
        lowSanityRect.offsetMin = Vector2.zero;
        lowSanityRect.offsetMax = Vector2.zero;
        lowSanityImage = lowSanityObject.AddComponent<Image>();
        lowSanityImage.color = new Color(0f, 0f, 0f, 0f);
        lowSanityImage.raycastTarget = false;

        if (enableVhsDistortion)
        {
            var vhsObject = new GameObject("VHS Distortion Overlay");
            vhsObject.transform.SetParent(canvasObject.transform, false);
            var vhsRect = vhsObject.AddComponent<RectTransform>();
            vhsRect.anchorMin = Vector2.zero;
            vhsRect.anchorMax = Vector2.one;
            vhsRect.offsetMin = Vector2.zero;
            vhsRect.offsetMax = Vector2.zero;
            vhsDistortionImage = vhsObject.AddComponent<Image>();
            vhsDistortionImage.color = Color.clear;
            vhsDistortionImage.raycastTarget = false;

            var tearObject = new GameObject("VHS Horizontal Tear Line");
            tearObject.transform.SetParent(canvasObject.transform, false);
            vhsTearRect = tearObject.AddComponent<RectTransform>();
            vhsTearRect.anchorMin = new Vector2(0f, 0.5f);
            vhsTearRect.anchorMax = new Vector2(1f, 0.5f);
            vhsTearRect.offsetMin = Vector2.zero;
            vhsTearRect.offsetMax = Vector2.zero;
            vhsTearImage = tearObject.AddComponent<Image>();
            vhsTearImage.color = Color.clear;
            vhsTearImage.raycastTarget = false;
        }

        if (enableStrangeText)
        {
            var textObject = new GameObject("Strange Text");
            textObject.transform.SetParent(canvasObject.transform, false);
            var textRect = textObject.AddComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0.5f, 0.5f);
            textRect.anchorMax = new Vector2(0.5f, 0.5f);
            textRect.sizeDelta = new Vector2(760f, 120f);
            strangeText = textObject.AddComponent<TextMeshProUGUI>();
            strangeText.fontSize = 54f;
            strangeText.alignment = TextAlignmentOptions.Center;
            strangeText.color = new Color(1f, 0.302f, 0.427f, 0.95f);
            strangeText.outlineWidth = 0.18f;
            strangeText.outlineColor = Color.black;
            strangeText.raycastTarget = false;
            textObject.SetActive(false);
        }
    }

    private string Pick(string[] options)
    {
        if (options == null || options.Length == 0)
        {
            return "";
        }

        return options[Random.Range(0, options.Length)];
    }

    private void ResolveFeedbackLights()
    {
        correctDecisionLight = correctDecisionLight != null ? correctDecisionLight : FindLight("DecisionGreenLight");
        wrongDecisionLight = wrongDecisionLight != null ? wrongDecisionLight : FindLight("DecisionRedLight");
        backFlickerLight = backFlickerLight != null ? backFlickerLight : FindLight("BackFlickerLight");
    }

    private Light FindLight(string objectName)
    {
        GameObject lightObject = GameObject.Find(objectName);
        return lightObject != null ? lightObject.GetComponent<Light>() : null;
    }
}
