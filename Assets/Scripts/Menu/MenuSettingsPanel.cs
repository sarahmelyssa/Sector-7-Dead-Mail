using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class MenuSettingsPanel : MonoBehaviour
{
    private const string SettingsBackgroundResourcePath = "MainMenuGenerated/settings_menu_background";
    private const string MasterVolumeKey = "Sector7_Settings_MasterVolume";
    private const string BrightnessKey = "Sector7_Settings_Brightness";
    private const string MouseSensitivityKey = "Sector7_Settings_MouseSensitivity";
    private const string FullscreenKey = "Sector7_Settings_Fullscreen";

    [SerializeField] private float transitionTime = 0.24f;
    [SerializeField] private Vector2 hiddenOffset = new Vector2(110f, 0f);

    private MainMenuController controller;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Image brightnessOverlay;
    private Slider volumeSlider;
    private Slider brightnessSlider;
    private Slider sensitivitySlider;
    private MenuSettingsGlow fullscreenOnGlow;
    private MenuSettingsGlow fullscreenOffGlow;
    private Coroutine transitionRoutine;
    private bool isBuilt;

    private readonly Color rowGlow = new Color(0.64f, 0.20f, 0.95f, 1f);
    private readonly Color invisible = new Color(1f, 1f, 1f, 0f);

    public void Initialize(MainMenuController owner)
    {
        controller = owner;
        EnsureBuilt();
        ApplySavedSettings();
        HideImmediate();
    }

    public void Show()
    {
        EnsureBuilt();
        ApplySavedSettings();
        gameObject.SetActive(true);
        StartTransition(true);
    }

    public void Hide()
    {
        if (!gameObject.activeSelf)
        {
            return;
        }

        StartTransition(false);
    }

    private void EnsureBuilt()
    {
        if (isBuilt)
        {
            return;
        }

        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        ConfigureRootImage();

        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = Vector2.zero;

        ClearExistingChildren();
        CreateBrightnessOverlay();
        CreateControls();
        isBuilt = true;
    }

    private Sprite LoadSettingsBackgroundSprite()
    {
        Sprite sprite = Resources.Load<Sprite>(SettingsBackgroundResourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(SettingsBackgroundResourcePath);
        if (texture == null)
        {
            Debug.LogWarning("Settings background not found. Put the image at Assets/Resources/MainMenuGenerated/settings_menu_background.png.");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void CreateControls()
    {
        brightnessSlider = CreateSlider("BrightnessControl", new Vector2(0.560f, 0.515f), new Vector2(0.938f, 0.606f));
        sensitivitySlider = CreateSlider("MouseSensitivityControl", new Vector2(0.560f, 0.404f), new Vector2(0.938f, 0.495f));
        volumeSlider = CreateSlider("MasterVolumeControl", new Vector2(0.560f, 0.294f), new Vector2(0.938f, 0.385f));

        volumeSlider.wholeNumbers = true;
        brightnessSlider.wholeNumbers = true;
        sensitivitySlider.wholeNumbers = true;

        volumeSlider.minValue = 0f;
        brightnessSlider.minValue = 0f;
        sensitivitySlider.minValue = 0f;

        volumeSlider.maxValue = 100f;
        brightnessSlider.maxValue = 100f;
        sensitivitySlider.maxValue = 100f;

        ApplySavedSettingsToControls();

        volumeSlider.onValueChanged.AddListener(OnVolumeChanged);
        brightnessSlider.onValueChanged.AddListener(OnBrightnessChanged);
        sensitivitySlider.onValueChanged.AddListener(OnSensitivityChanged);

        CreateFullscreenControl();
        CreateBackButton();
    }

    private Slider CreateSlider(string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject root = CreateRect(name, transform, anchorMin, anchorMax);
        Image rootImage = root.AddComponent<Image>();
        rootImage.color = invisible;
        rootImage.raycastTarget = true;
        AddGlow(root.transform, Vector2.zero, Vector2.one, 0.075f, 0.12f, 0f);

        Slider slider = root.AddComponent<Slider>();

        GameObject fillArea = CreateRect("FillArea", root.transform, Vector2.zero, Vector2.one);
        GameObject fill = CreateRect("Fill", fillArea.transform, Vector2.zero, Vector2.one);
        Image fillImage = fill.AddComponent<Image>();
        fillImage.color = invisible;
        fillImage.raycastTarget = false;

        GameObject handleArea = CreateRect("HandleSlideArea", root.transform, Vector2.zero, Vector2.one);
        GameObject handle = CreateRect("Handle", handleArea.transform, new Vector2(0f, 0f), new Vector2(0.012f, 1f));
        Image handleImage = handle.AddComponent<Image>();
        handleImage.color = invisible;
        handleImage.raycastTarget = false;

        slider.targetGraphic = rootImage;
        slider.fillRect = fill.GetComponent<RectTransform>();
        slider.handleRect = handle.GetComponent<RectTransform>();
        slider.direction = Slider.Direction.LeftToRight;
        return slider;
    }

    private void CreateFullscreenControl()
    {
        Button onButton = CreateInvisibleButton("FullscreenOn", new Vector2(0.765f, 0.205f), new Vector2(0.827f, 0.278f), out fullscreenOnGlow, 0.12f);
        Button offButton = CreateInvisibleButton("FullscreenOff", new Vector2(0.835f, 0.205f), new Vector2(0.901f, 0.278f), out fullscreenOffGlow, 0.12f);

        onButton.onClick.AddListener(() => SetFullscreen(true));
        offButton.onClick.AddListener(() => SetFullscreen(false));
        UpdateFullscreenGlow();
    }

    private Button CreateInvisibleButton(string name, Vector2 anchorMin, Vector2 anchorMax, out MenuSettingsGlow glow, float selectedAlpha = 0f)
    {
        GameObject buttonObject = CreateRect(name, transform, anchorMin, anchorMax);
        Image image = buttonObject.AddComponent<Image>();
        image.color = invisible;
        image.raycastTarget = true;
        glow = AddGlow(buttonObject.transform, Vector2.zero, Vector2.one, 0.075f, 0.12f, selectedAlpha);

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        return button;
    }

    private void CreateBackButton()
    {
        Button button = CreateInvisibleButton("BackButton", new Vector2(0.565f, 0.078f), new Vector2(0.935f, 0.168f), out _);
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(() =>
        {
            if (controller != null)
            {
                controller.CloseSettings();
            }
            else
            {
                Hide();
            }
        });
    }

    private GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = Vector2.zero;
        rect.localScale = Vector3.one;
        return gameObject;
    }

    private MenuSettingsGlow AddGlow(Transform parent, Vector2 anchorMin, Vector2 anchorMax, float hoverAlpha, float pressedAlpha, float selectedAlpha)
    {
        GameObject glowObject = CreateRect("SelectedGlow", parent, anchorMin, anchorMax);
        Image image = glowObject.AddComponent<Image>();
        image.color = invisible;
        image.raycastTarget = false;

        MenuSettingsGlow glow = parent.gameObject.AddComponent<MenuSettingsGlow>();
        glow.Configure(image, rowGlow, hoverAlpha, pressedAlpha, selectedAlpha);
        return glow;
    }

    private void StartTransition(bool show)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(TransitionRoutine(show));
    }

    private IEnumerator TransitionRoutine(bool show)
    {
        canvasGroup.blocksRaycasts = show;
        canvasGroup.interactable = show;

        Vector2 startPosition = rectTransform.anchoredPosition;
        Vector2 endPosition = show ? Vector2.zero : hiddenOffset;
        float startAlpha = canvasGroup.alpha;
        float endAlpha = show ? 1f : 0f;
        float elapsed = 0f;

        while (elapsed < transitionTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / transitionTime);
            float eased = t * t * (3f - 2f * t);
            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, endPosition, eased);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, endAlpha, eased);
            yield return null;
        }

        rectTransform.anchoredPosition = endPosition;
        canvasGroup.alpha = endAlpha;
        transitionRoutine = null;

        if (!show)
        {
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
            gameObject.SetActive(false);
        }
    }

    private void HideImmediate()
    {
        if (rectTransform == null || canvasGroup == null)
        {
            return;
        }

        rectTransform.anchoredPosition = hiddenOffset;
        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
        gameObject.SetActive(false);
    }

    private void OnVolumeChanged(float value)
    {
        AudioListener.volume = Mathf.Clamp01(value / 100f);
        PlayerPrefs.SetFloat(MasterVolumeKey, AudioListener.volume);
        PlayerPrefs.Save();
    }

    private void OnBrightnessChanged(float value)
    {
        ApplyBrightness(value / 100f);
        PlayerPrefs.SetFloat(BrightnessKey, Mathf.Clamp01(value / 100f));
        PlayerPrefs.Save();
    }

    private void OnSensitivityChanged(float value)
    {
        PlayerPrefs.SetFloat(MouseSensitivityKey, Mathf.Clamp01(value / 100f));
        PlayerPrefs.Save();
    }

    private void SetFullscreen(bool fullscreen)
    {
        Screen.fullScreen = fullscreen;
        PlayerPrefs.SetInt(FullscreenKey, fullscreen ? 1 : 0);
        PlayerPrefs.Save();
        UpdateFullscreenGlow();
    }

    private void ApplySavedSettings()
    {
        AudioListener.volume = PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f);
        ApplyBrightness(PlayerPrefs.GetFloat(BrightnessKey, 0.7f));
        ApplySavedSettingsToControls();

        if (PlayerPrefs.HasKey(FullscreenKey))
        {
            Screen.fullScreen = PlayerPrefs.GetInt(FullscreenKey, Screen.fullScreen ? 1 : 0) == 1;
        }

        UpdateFullscreenGlow();
    }

    private void ApplySavedSettingsToControls()
    {
        float savedVolume = Mathf.RoundToInt(PlayerPrefs.GetFloat(MasterVolumeKey, 0.8f) * 100f);
        float savedBrightness = Mathf.RoundToInt(PlayerPrefs.GetFloat(BrightnessKey, 0.7f) * 100f);
        float savedSensitivity = Mathf.RoundToInt(PlayerPrefs.GetFloat(MouseSensitivityKey, 0.6f) * 100f);

        volumeSlider?.SetValueWithoutNotify(savedVolume);
        brightnessSlider?.SetValueWithoutNotify(savedBrightness);
        sensitivitySlider?.SetValueWithoutNotify(savedSensitivity);

    }

    private void ApplyBrightness(float brightness)
    {
        if (brightnessOverlay == null)
        {
            return;
        }

        float alpha = Mathf.Lerp(0.46f, 0f, Mathf.Clamp01(brightness));
        brightnessOverlay.color = new Color(0f, 0f, 0f, alpha);
    }

    private void UpdateFullscreenGlow()
    {
        fullscreenOnGlow?.SetSelected(Screen.fullScreen);
        fullscreenOffGlow?.SetSelected(!Screen.fullScreen);
    }

    private void CreateBrightnessOverlay()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas == null)
        {
            return;
        }

        Transform existing = canvas.transform.Find("MenuBrightnessOverlay");
        if (existing != null)
        {
            brightnessOverlay = existing.GetComponent<Image>();
            return;
        }

        GameObject overlay = CreateRect("MenuBrightnessOverlay", canvas.transform, Vector2.zero, Vector2.one);
        brightnessOverlay = overlay.AddComponent<Image>();
        brightnessOverlay.color = new Color(0f, 0f, 0f, 0f);
        brightnessOverlay.raycastTarget = false;
        overlay.transform.SetAsLastSibling();
    }

    private void ConfigureRootImage()
    {
        Image image = GetComponent<Image>();
        if (image == null)
        {
            image = gameObject.AddComponent<Image>();
        }

        image.sprite = LoadSettingsBackgroundSprite();
        image.color = image.sprite != null ? Color.white : new Color(0.005f, 0f, 0.018f, 0.94f);
        image.raycastTarget = true;
        image.preserveAspect = false;
    }

    private void ClearExistingChildren()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}

public class MenuSettingsGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler, ISelectHandler, IDeselectHandler
{
    private Image glowImage;
    private Color glowColor;
    private Coroutine fadeRoutine;
    private float hoverAlpha;
    private float pressedAlpha;
    private float selectedAlpha;
    private bool hovering;
    private bool selected;

    public void Configure(Image image, Color color, float hover, float pressed, float persistent)
    {
        glowImage = image;
        glowColor = color;
        hoverAlpha = hover;
        pressedAlpha = pressed;
        selectedAlpha = persistent;
        SetAlpha(0f);
    }

    public void SetSelected(bool value)
    {
        selected = value;
        FadeTo(CurrentIdleAlpha());
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        hovering = true;
        FadeTo(Mathf.Max(hoverAlpha, CurrentIdleAlpha()));
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        hovering = false;
        FadeTo(CurrentIdleAlpha());
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        FadeTo(Mathf.Max(pressedAlpha, CurrentIdleAlpha()));
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        FadeTo(hovering ? Mathf.Max(hoverAlpha, CurrentIdleAlpha()) : CurrentIdleAlpha());
    }

    public void OnSelect(BaseEventData eventData)
    {
        hovering = true;
        FadeTo(Mathf.Max(hoverAlpha, CurrentIdleAlpha()));
    }

    public void OnDeselect(BaseEventData eventData)
    {
        hovering = false;
        FadeTo(CurrentIdleAlpha());
    }

    private float CurrentIdleAlpha()
    {
        return selected ? selectedAlpha : 0f;
    }

    private void FadeTo(float alpha)
    {
        if (glowImage == null)
        {
            return;
        }

        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            SetAlpha(alpha);
            fadeRoutine = null;
            return;
        }

        if (fadeRoutine != null)
        {
            StopCoroutine(fadeRoutine);
        }

        fadeRoutine = StartCoroutine(FadeRoutine(alpha));
    }

    private IEnumerator FadeRoutine(float targetAlpha)
    {
        float startAlpha = glowImage.color.a;
        float elapsed = 0f;
        const float duration = 0.10f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, t));
            yield return null;
        }

        SetAlpha(targetAlpha);
        fadeRoutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (glowImage == null)
        {
            return;
        }

        glowImage.color = new Color(glowColor.r, glowColor.g, glowColor.b, alpha);
    }
}
