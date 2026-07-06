using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class GameOverUI : MonoBehaviour
{
    public static GameOverUI Instance { get; private set; }

    private const string DeathBackgroundResourcePath = "Sector7SceneReferences/backgrounds/shift_failed_background";

    [SerializeField] private GameObject panelRoot = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text subtitleText = null;
    [SerializeField] private TMP_Text reasonText = null;
    [SerializeField] private Button retryShiftButton = null;
    [SerializeField] private Button mainMenuButton = null;
    [SerializeField] private Button quitButton = null;

    private CanvasGroup canvasGroup;
    private Image darkOverlay;
    private bool isShowing;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildDefaultUiIfNeeded();
        Hide();
    }

    private void Update()
    {
        if (!isShowing || darkOverlay == null)
        {
            return;
        }

        float pulse = Mathf.PerlinNoise(Time.unscaledTime * 12f, 0.37f);
        darkOverlay.color = new Color(0f, 0f, 0f, Mathf.Lerp(0.28f, 0.42f, pulse));
    }

    public void Show(string reason)
    {
        BuildDefaultUiIfNeeded();

        if (titleText != null)
        {
            titleText.text = "SHIFT TERMINATED";
        }

        if (subtitleText != null)
        {
            subtitleText.text = "Sector 7 has logged your failure.";
        }

        if (reasonText != null)
        {
            reasonText.text = string.IsNullOrWhiteSpace(reason) ? "" : reason;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(true);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        isShowing = true;
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Hide()
    {
        isShowing = false;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        if (panelRoot != null)
        {
            panelRoot.SetActive(false);
        }
    }

    public void RetryShift()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SceneManager.sceneCountInBuildSettings > 0)
        {
            SceneManager.LoadScene(0);
            return;
        }

        SceneManager.LoadScene("MainMenu");
    }

    public void QuitGame()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
        Debug.Log("Quit requested from Game Over screen.");
#else
        Application.Quit();
#endif
    }

    private void BuildDefaultUiIfNeeded()
    {
        EnsureEventSystem();

        if (panelRoot != null)
        {
            canvasGroup = canvasGroup != null ? canvasGroup : panelRoot.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            WireButtons();
            return;
        }

        GameObject root = new GameObject("Runtime_GameOverUI");
        root.transform.SetParent(transform, false);

        GameObject canvasObject = new GameObject("GameOverCanvas");
        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 190;

        CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        panelRoot = new GameObject("GameOverPanel");
        panelRoot.transform.SetParent(canvasObject.transform, false);
        StretchToParent(panelRoot.AddComponent<RectTransform>());

        canvasGroup = panelRoot.AddComponent<CanvasGroup>();

        Sprite deathBackground = LoadSprite(DeathBackgroundResourcePath);
        Image background = CreateFullScreenImage("GameOverBackgroundImage", panelRoot.transform, deathBackground, deathBackground != null ? Color.white : new Color(0.010f, 0.006f, 0.018f, 1f));
        background.raycastTarget = false;

        darkOverlay = CreateFullScreenImage("GameOverDarkOverlay", panelRoot.transform, null, new Color(0f, 0f, 0f, 0.34f));
        darkOverlay.raycastTarget = false;

        titleText = CreateText("GameOverTitle", panelRoot.transform, "SHIFT TERMINATED", new Vector2(0f, 245f), new Vector2(1240f, 108f), 76f, TextAlignmentOptions.Center);
        titleText.fontStyle = FontStyles.Bold;
        titleText.characterSpacing = 7f;
        titleText.color = new Color(0.945f, 0.914f, 1f);

        subtitleText = CreateText("GameOverSubtitle", panelRoot.transform, "Sector 7 has logged your failure.", new Vector2(0f, 170f), new Vector2(1060f, 56f), 30f, TextAlignmentOptions.Center);
        subtitleText.color = new Color(0.760f, 0.660f, 0.870f);

        reasonText = CreateText("GameOverReason", panelRoot.transform, "", new Vector2(0f, 105f), new Vector2(960f, 58f), 24f, TextAlignmentOptions.Center);
        reasonText.color = new Color(0.840f, 0.760f, 0.930f);

        retryShiftButton = CreateButton("RetryShiftButton", panelRoot.transform, "Retry Shift", new Vector2(0f, -95f), RetryShift);
        mainMenuButton = CreateButton("MainMenuButton", panelRoot.transform, "Main Menu", new Vector2(0f, -205f), GoToMainMenu);
        quitButton = CreateButton("QuitButton", panelRoot.transform, "Quit", new Vector2(0f, -315f), QuitGame);
    }

    private Image CreateFullScreenImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        StretchToParent(rect);

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.preserveAspect = false;
        return image;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        TMP_Text tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.outlineWidth = 0.16f;
        tmp.outlineColor = Color.black;
        tmp.lineSpacing = 7f;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        return tmp;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(430f, 78f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.030f, 0.021f, 0.043f, 0.82f);

        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.620f, 0.360f, 0.760f, 0.62f);
        outline.effectDistance = new Vector2(2f, -2f);

        Shadow shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.72f);
        shadow.effectDistance = new Vector2(7f, -7f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.ColorTint;
        button.colors = CreateButtonColors();
        button.onClick.AddListener(onClick);

        TMP_Text labelText = CreateText("Label", buttonObject.transform, label, Vector2.zero, rect.sizeDelta, 28f, TextAlignmentOptions.Center);
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 1.2f;
        labelText.color = new Color(0.945f, 0.914f, 1f);
        return button;
    }

    private ColorBlock CreateButtonColors()
    {
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = new Color(0.030f, 0.021f, 0.043f, 0.82f);
        colors.highlightedColor = new Color(0.310f, 0.140f, 0.430f, 0.94f);
        colors.pressedColor = new Color(0.470f, 0.180f, 0.610f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.060f, 0.050f, 0.070f, 0.45f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        return colors;
    }

    private Sprite LoadSprite(string resourcePath)
    {
        Sprite sprite = Resources.Load<Sprite>(resourcePath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = Resources.Load<Texture2D>(resourcePath);
        if (texture == null)
        {
            Debug.LogWarning("Game Over background image was not found at Resources/" + resourcePath + ".");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private void WireButtons()
    {
        WireButton(retryShiftButton, RetryShift);
        WireButton(mainMenuButton, GoToMainMenu);
        WireButton(quitButton, QuitGame);
    }

    private void WireButton(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveListener(action);
        button.onClick.AddListener(action);
    }

    private void StretchToParent(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
