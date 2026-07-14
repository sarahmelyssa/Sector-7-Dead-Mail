using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Controla as telas de interface que nao fazem parte fisica da sala:
/// briefing/fita, pause, transicoes, telas simples de vitoria/derrota e atalhos.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }
    private static bool showTransitionBlockerOnNextScene;

    [Header("Panels")]
    [SerializeField] private GameObject mainMenuPanel = null;
    [SerializeField] private GameObject briefingPanel = null;
    [SerializeField] private GameObject pausePanel = null;
    [SerializeField] private GameObject settingsPanel = null;
    [SerializeField] private GameObject gameOverPanel = null;
    [SerializeField] private GameObject victoryPanel = null;
    [SerializeField] private GameObject transitionBlockerPanel = null;

    [Header("Scenes")]
    [SerializeField] private string gameSceneName = "";
    [SerializeField] private string menuSceneName = "";

    private Canvas canvas;
    private TMP_Text briefingTitleText;
    private TMP_Text briefingBodyText;
    private Button briefingContinueButton;
    private Image briefingBackgroundImage;
    private Image briefingNextOverlayImage;
    private CanvasGroup briefingCanvasGroup;
    private Button mainMenuStartButton;
    private Button mainMenuQuitButton;
    private TMP_Text conclusionTitleText;
    private TMP_Text conclusionBodyText;
    private Button conclusionContinueButton;
    private Image conclusionBackgroundImage;
    private Image briefingCustomArtImage;
    private Image conclusionCustomArtImage;
    private Coroutine briefingTypingRoutine;
    private Coroutine briefingTransitionRoutine;
    private Coroutine briefingIntroRoutine;
    private Coroutine briefingOutroRoutine;
    private Coroutine briefingGlitchRoutine;
    private AudioSource briefingTapeAudioSource;
    private AudioClip briefingTapeLoopClip;
    private TMP_FontAsset reportFontAsset;
    private readonly List<string> briefingReportPages = new List<string>();
    private readonly List<string> conclusionPages = new List<string>();
    private string fullBriefingText = "";
    private string conclusionFinalButtonText = "CONTINUAR";
    private string activeBriefingBackgroundPath = "";
    private string activeConclusionBackgroundPath = "";
    private bool isBriefingTyping;
    private bool briefingUsesReportScreen;
    private bool instantBriefingIntro;
    private bool isBriefingClosing;
    private int briefingSkipClicks;
    private int briefingReportPageIndex;
    private int conclusionPageIndex;
    private Action mainMenuStartAction;
    private Action briefingContinueAction;
    private Action conclusionContinueAction;
    private bool isPaused;
    private CanvasGroup pauseCanvasGroup;
    private Coroutine pauseFadeRoutine;
    private Coroutine startupVhsRoutine;
    private AudioSource startupVhsAudioSource;
    private AudioClip startupVhsClip;
    private CanvasGroup transitionBlockerCanvasGroup;
    private CursorLockMode cursorLockStateBeforePause;
    private bool cursorVisibleBeforePause;

    private const string PauseMenuImagePath = "PauseMenu/pause";
    private const string StartupVhsAudioPath = "Audio/UI/ui_vhs_insert";
    private const float StartupVhsMaxDuration = 2.35f;
    private const string ButtonGlowResourcePath = "MainMenuGenerated/Effects/button_hover_glow";
    private const string ReportBriefingBlankPath = "StoryReports/report_01_intro_blank";
    private const string ReportBriefingNextPath = "StoryReports/report_01_intro_next";

    public bool IsBlockingScreenOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildDefaultUiIfNeeded();
        HideAllStoryPanels();

        if (showTransitionBlockerOnNextScene)
        {
            ShowTransitionBlocker();
        }
    }

    private void Update()
    {
        if (KeyboardEscapePressed())
        {
            TogglePause();
        }

        HandleReportAdvanceShortcut();
    }

    public void ShowBriefing(string title, string body, Action onContinue)
    {
        ShowBriefingInternal(title, body, onContinue, "");
    }

    public void ShowBriefing(string title, string body, Action onContinue, string backgroundResourcePath)
    {
        ShowBriefingInternal(title, body, onContinue, backgroundResourcePath);
    }

    private void ShowBriefingInternal(string title, string body, Action onContinue, string backgroundResourcePath)
    {
        // O briefing pode aparecer como texto normal ou como relatorio/fita.
        bool shouldSkipIntroFade = showTransitionBlockerOnNextScene;
        HideAllStoryPanels();
        IsBlockingScreenOpen = true;
        briefingContinueAction = onContinue;
        activeBriefingBackgroundPath = backgroundResourcePath ?? "";
        bool delayBriefingForStartupVhs = shouldSkipIntroFade && HasStartupVhsClip();
        instantBriefingIntro = shouldSkipIntroFade && !delayBriefingForStartupVhs;
        briefingUsesReportScreen = ShouldUseReportScreen(title, body);
        ConfigureBriefingMode(briefingUsesReportScreen);

        if (briefingTitleText != null)
        {
            briefingTitleText.text = title;
        }

        if (briefingBodyText != null)
        {
            briefingBodyText.text = briefingUsesReportScreen ? "" : body;
        }

        briefingPanel?.SetActive(true);

        if (delayBriefingForStartupVhs)
        {
            StartStartupVhsIntro(body);
        }
        else if (briefingUsesReportScreen)
        {
            StartBriefingTypewriter(body);
        }

        if (shouldSkipIntroFade && !delayBriefingForStartupVhs)
        {
            HideTransitionBlocker();
        }
    }

    public void ShowBriefing(string title, IReadOnlyList<string> pages, Action onContinue)
    {
        ShowBriefing(title, JoinPages(pages), onContinue);
    }

    public void ShowBriefing(string title, IReadOnlyList<string> pages, Action onContinue, string backgroundResourcePath)
    {
        ShowBriefingInternal(title, JoinPages(pages), onContinue, backgroundResourcePath);
    }

    public void ShowMainMenu(Action onStart)
    {
        HideAllStoryPanels();
        IsBlockingScreenOpen = true;
        Time.timeScale = 0f;
        mainMenuStartAction = onStart;
        mainMenuPanel?.SetActive(true);
    }

    public void ShowConclusion(string title, string body, string buttonText, Action onContinue)
    {
        ShowConclusionInternal(title, body, buttonText, onContinue, "");
    }

    public void ShowConclusion(string title, string body, string buttonText, Action onContinue, string backgroundResourcePath)
    {
        ShowConclusionInternal(title, body, buttonText, onContinue, backgroundResourcePath);
    }

    private void ShowConclusionInternal(string title, string body, string buttonText, Action onContinue, string backgroundResourcePath)
    {
        HideAllStoryPanels();
        IsBlockingScreenOpen = true;
        BackgroundMusicManager.Instance?.PlayBriefingMusic(false);
        conclusionContinueAction = onContinue;
        activeConclusionBackgroundPath = backgroundResourcePath ?? "";
        conclusionFinalButtonText = string.IsNullOrWhiteSpace(buttonText) ? "CONTINUAR" : buttonText;
        conclusionPages.Clear();
        AddPages(conclusionPages, body);
        conclusionPageIndex = 0;
        bool usesCustomBackground = !string.IsNullOrWhiteSpace(activeConclusionBackgroundPath);
        ConfigureConclusionMode(usesCustomBackground);

        if (conclusionTitleText != null)
        {
            conclusionTitleText.text = title;
        }

        if (conclusionBodyText != null)
        {
            conclusionBodyText.text = GetCurrentConclusionPageText();
        }

        if (conclusionBackgroundImage != null)
        {
            string lowerTitle = title != null ? title.ToLowerInvariant() : "";
            string backgroundPath = usesCustomBackground ? activeConclusionBackgroundPath : lowerTitle.Contains("failed") || lowerTitle.Contains("fail") || lowerTitle.Contains("falhou")
                ? "Sector7SceneReferences/backgrounds/shift_failed_background"
                : "Sector7SceneReferences/backgrounds/shift_complete_background";

            if (usesCustomBackground)
            {
                conclusionBackgroundImage.sprite = null;
                conclusionBackgroundImage.color = new Color(0.010f, 0.006f, 0.015f, 1f);
                conclusionBackgroundImage.preserveAspect = false;
            }
            else
            {
                Sprite backgroundSprite = LoadSprite(backgroundPath);
                if (backgroundSprite != null)
                {
                    conclusionBackgroundImage.sprite = backgroundSprite;
                    conclusionBackgroundImage.color = Color.white;
                    conclusionBackgroundImage.preserveAspect = false;
                }
            }
        }

        if (conclusionCustomArtImage != null)
        {
            Sprite customSprite = usesCustomBackground ? LoadSprite(activeConclusionBackgroundPath) : null;
            conclusionCustomArtImage.sprite = customSprite;
            conclusionCustomArtImage.color = customSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            conclusionCustomArtImage.preserveAspect = true;
            conclusionCustomArtImage.gameObject.SetActive(usesCustomBackground && customSprite != null);
            conclusionCustomArtImage.transform.SetSiblingIndex(0);
        }

        UpdateConclusionButtonLabel();

        victoryPanel?.SetActive(true);
    }

    public void ShowConclusion(string title, IReadOnlyList<string> pages, string buttonText, Action onContinue)
    {
        ShowConclusion(title, JoinPages(pages), buttonText, onContinue);
    }

    public void ShowConclusion(string title, IReadOnlyList<string> pages, string buttonText, Action onContinue, string backgroundResourcePath)
    {
        ShowConclusionInternal(title, JoinPages(pages), buttonText, onContinue, backgroundResourcePath);
    }

    public void StartShift()
    {
        if (!string.IsNullOrWhiteSpace(gameSceneName))
        {
            SceneManager.LoadScene(gameSceneName);
        }
    }

    public void OpenBriefing()
    {
        briefingPanel?.SetActive(true);
    }

    public void CloseBriefing()
    {
        if (isBriefingClosing)
        {
            return;
        }

        if (isBriefingTyping)
        {
            if (briefingUsesReportScreen)
            {
                briefingSkipClicks++;
                if (briefingSkipClicks >= 2)
                {
                    CompleteBriefingTypewriter();
                }

                return;
            }

            CompleteBriefingTypewriter();
            return;
        }

        if (briefingUsesReportScreen)
        {
            if (AdvanceReportBriefingPage())
            {
                return;
            }

            briefingOutroRoutine = StartCoroutine(CloseReportBriefingSmoothly());
            return;
        }

        StopBriefingTapeAudio();
        briefingPanel?.SetActive(false);
        IsBlockingScreenOpen = false;
        briefingContinueAction?.Invoke();
    }

    private void HandleReportAdvanceShortcut()
    {
        if (!KeyboardEnterPressed() || !briefingUsesReportScreen || briefingPanel == null || !briefingPanel.activeSelf || isBriefingClosing)
        {
            return;
        }

        if (isBriefingTyping && briefingTypingRoutine == null)
        {
            return;
        }

        CloseBriefing();
    }

    public void CloseMainMenu()
    {
        mainMenuPanel?.SetActive(false);
        IsBlockingScreenOpen = false;
        Time.timeScale = 1f;
        mainMenuStartAction?.Invoke();
    }

    public void TogglePause()
    {
        if (pausePanel == null || (!isPaused && !CanOpenPauseMenu()))
        {
            return;
        }

        // Pause usa imagem pronta com hotspots invisiveis em cima dos botoes desenhados.
        SetPaused(!isPaused);
    }

    public void Resume()
    {
        SetPaused(false);
    }

    public void RestartShift()
    {
        Time.timeScale = 1f;
        NightManager.Instance?.ResetProgressToFirstNight();
        NightStoryManager.PrepareImmediateGameplayRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (!string.IsNullOrWhiteSpace(menuSceneName))
        {
            SceneManager.LoadScene(menuSceneName);
            return;
        }

        if (SceneExistsInBuildSettings("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void ShowGameOver()
    {
        ShowConclusion("TURNO FALHOU", "Seu arquivo não foi fechado. Ele foi arquivado.", "REINICIAR", RestartShift);
    }

    public void ShowVictory()
    {
        ShowConclusion("FIM DO TURNO", "Conformidade aceita. Registro do funcionário atualizado.", "MENU PRINCIPAL", () => NightStoryManager.Instance?.ContinueAfterConclusion());
    }

    public void NextShift()
    {
        NightStoryManager.Instance?.ContinueAfterConclusion();
    }

    public void PrepareForNightSceneReload()
    {
        BlockNextStorySceneUntilBriefing();
    }

    public static void BlockNextStorySceneUntilBriefing()
    {
        // Mantem a tela preta entre cenas para nao mostrar a sala antes da historia.
        showTransitionBlockerOnNextScene = true;
        Instance?.ShowTransitionBlocker();
    }

    public void DebugAdvanceStoryReport()
    {
        if (briefingPanel != null && briefingPanel.activeSelf)
        {
            CloseBriefing();
            return;
        }

        ShowBriefing(
            "RELATÓRIO DE TESTE",
            new[]
            {
                "Página de teste 1.\n\nUse ENTER ou a área do botão do relatório para avançar.",
                "Página de teste 2.\n\nA página final deve fechar e voltar ao jogo."
            },
            () => { }
        );
    }

    private void HideAllStoryPanels()
    {
        StopStartupVhsIntro();
        StopBriefingTapeAudio();
        SetPaused(false);
        mainMenuPanel?.SetActive(false);
        briefingPanel?.SetActive(false);
        settingsPanel?.SetActive(false);
        gameOverPanel?.SetActive(false);
        victoryPanel?.SetActive(false);
        transitionBlockerPanel?.SetActive(false);
        IsBlockingScreenOpen = false;
    }

    private void BuildDefaultUiIfNeeded()
    {
        EnsureEventSystem();

        if (canvas == null)
        {
            var canvasObject = new GameObject("Story UI Canvas");
            canvasObject.transform.SetParent(transform, false);
            canvas = canvasObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 120;

            var scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;
            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (mainMenuPanel == null)
        {
            mainMenuPanel = CreateStoryPanel("MainMenuPanel", "Sector7SceneReferences/backgrounds/main_menu_sector7");
            CreateText("Main Menu Subtitle", mainMenuPanel.transform, new Vector2(0f, -72f), new Vector2(1120f, 86f), 34f, TextAlignmentOptions.Center).text = "CONTRATO DE TRIAGEM NOTURNA";
            mainMenuStartButton = CreateButton("Start Shift Button", mainMenuPanel.transform, "INICIAR TURNO", new Vector2(0f, -268f), () => CloseMainMenu());
            mainMenuQuitButton = CreateButton("Quit Button", mainMenuPanel.transform, "SAIR", new Vector2(0f, -382f), () => QuitGame());
        }

        if (briefingPanel == null)
        {
            briefingPanel = CreateStoryPanel("BriefingPanel", ReportBriefingBlankPath);
            briefingBackgroundImage = briefingPanel.GetComponent<Image>();
            briefingCanvasGroup = briefingPanel.AddComponent<CanvasGroup>();
            briefingCustomArtImage = CreateFullPanelImage("Briefing Custom Art", briefingPanel.transform, "");
            briefingNextOverlayImage = CreateFullPanelImage("Briefing Next Overlay", briefingPanel.transform, ReportBriefingNextPath);
            briefingTitleText = CreateText("Briefing Title", briefingPanel.transform, new Vector2(0f, 235f), new Vector2(1280f, 98f), 62f, TextAlignmentOptions.Center);
            briefingBodyText = CreateText("Briefing Body", briefingPanel.transform, new Vector2(0f, -5f), new Vector2(1160f, 390f), 36f, TextAlignmentOptions.TopLeft);
            briefingContinueButton = CreateButton("Continue Button", briefingPanel.transform, "CONTINUAR", new Vector2(0f, -340f), () => CloseBriefing());
            ConfigureBriefingMode(false);
        }

        if (pausePanel == null)
        {
            pausePanel = CreatePausePanel();
        }

        if (victoryPanel == null)
        {
            victoryPanel = CreateStoryPanel("ConclusionPanel", "Sector7SceneReferences/backgrounds/shift_complete_background");
            conclusionBackgroundImage = victoryPanel.GetComponent<Image>();
            conclusionCustomArtImage = CreateFullPanelImage("Conclusion Custom Art", victoryPanel.transform, "");
            conclusionTitleText = CreateText("Conclusion Title", victoryPanel.transform, new Vector2(0f, 230f), new Vector2(1280f, 98f), 64f, TextAlignmentOptions.Center);
            conclusionBodyText = CreateText("Conclusion Body", victoryPanel.transform, new Vector2(0f, 5f), new Vector2(1160f, 360f), 36f, TextAlignmentOptions.TopLeft);
            conclusionContinueButton = CreateButton("Conclusion Continue Button", victoryPanel.transform, "CONTINUAR", new Vector2(0f, -335f), AdvanceConclusion);
            CreatePauseButtonGlow(conclusionContinueButton.transform);
            conclusionContinueButton.gameObject.AddComponent<MenuButtonGlow>();
        }

        if (transitionBlockerPanel == null)
        {
            transitionBlockerPanel = CreateTransitionBlockerPanel();
        }
    }

    private GameObject CreateTransitionBlockerPanel()
    {
        var panel = new GameObject("Night Transition Blocker");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = panel.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        transitionBlockerCanvasGroup = panel.AddComponent<CanvasGroup>();
        transitionBlockerCanvasGroup.alpha = 1f;
        transitionBlockerCanvasGroup.interactable = false;
        transitionBlockerCanvasGroup.blocksRaycasts = true;

        panel.SetActive(false);
        return panel;
    }

    private void ShowTransitionBlocker()
    {
        if (transitionBlockerPanel == null)
        {
            return;
        }

        transitionBlockerPanel.transform.SetAsLastSibling();
        if (transitionBlockerCanvasGroup == null)
        {
            transitionBlockerCanvasGroup = transitionBlockerPanel.GetComponent<CanvasGroup>();
        }

        if (transitionBlockerCanvasGroup != null)
        {
            transitionBlockerCanvasGroup.alpha = 1f;
            transitionBlockerCanvasGroup.blocksRaycasts = true;
        }

        transitionBlockerPanel.SetActive(true);
    }

    private void HideTransitionBlocker()
    {
        showTransitionBlockerOnNextScene = false;
        transitionBlockerPanel?.SetActive(false);
    }

    private IEnumerator FadeOutTransitionBlocker(float duration)
    {
        showTransitionBlockerOnNextScene = false;
        if (transitionBlockerPanel == null)
        {
            yield break;
        }

        if (transitionBlockerCanvasGroup == null)
        {
            transitionBlockerCanvasGroup = transitionBlockerPanel.GetComponent<CanvasGroup>();
        }

        if (transitionBlockerCanvasGroup == null)
        {
            HideTransitionBlocker();
            yield break;
        }

        transitionBlockerPanel.transform.SetAsLastSibling();
        transitionBlockerPanel.SetActive(true);
        transitionBlockerCanvasGroup.blocksRaycasts = true;

        float startAlpha = transitionBlockerCanvasGroup.alpha;
        float elapsed = 0f;
        float fadeDuration = Mathf.Max(0.05f, duration);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            transitionBlockerCanvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, eased);
            yield return null;
        }

        transitionBlockerCanvasGroup.alpha = 1f;
        transitionBlockerCanvasGroup.blocksRaycasts = true;
        transitionBlockerPanel.SetActive(false);
    }

    private void PrepareTransitionBlockerForFade(float alpha)
    {
        showTransitionBlockerOnNextScene = false;
        if (transitionBlockerPanel == null)
        {
            return;
        }

        if (transitionBlockerCanvasGroup == null)
        {
            transitionBlockerCanvasGroup = transitionBlockerPanel.GetComponent<CanvasGroup>();
        }

        transitionBlockerPanel.transform.SetAsLastSibling();
        transitionBlockerPanel.SetActive(true);
        if (transitionBlockerCanvasGroup != null)
        {
            transitionBlockerCanvasGroup.alpha = Mathf.Clamp01(alpha);
            transitionBlockerCanvasGroup.blocksRaycasts = true;
        }
    }

    private bool HasStartupVhsClip()
    {
        startupVhsClip = startupVhsClip != null ? startupVhsClip : Resources.Load<AudioClip>(StartupVhsAudioPath);
        return startupVhsClip != null;
    }

    private void StartStartupVhsIntro(string briefingBody)
    {
        StopStartupVhsIntro();
        startupVhsRoutine = StartCoroutine(PlayStartupVhsIntroThenReveal(briefingBody));
    }

    private IEnumerator PlayStartupVhsIntroThenReveal(string briefingBody)
    {
        ShowTransitionBlocker();

        if (startupVhsAudioSource == null)
        {
            startupVhsAudioSource = gameObject.AddComponent<AudioSource>();
            startupVhsAudioSource.playOnAwake = false;
            startupVhsAudioSource.loop = false;
            startupVhsAudioSource.spatialBlend = 0f;
            startupVhsAudioSource.ignoreListenerPause = true;
        }

        startupVhsAudioSource.clip = startupVhsClip;
        startupVhsAudioSource.volume = 1f;
        BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
        startupVhsAudioSource.Play();

        float duration = startupVhsClip != null ? Mathf.Clamp(startupVhsClip.length, 0.55f, StartupVhsMaxDuration) : 0.9f;
        yield return new WaitForSecondsRealtime(duration);

        if (startupVhsAudioSource != null && startupVhsAudioSource.isPlaying)
        {
            startupVhsAudioSource.Stop();
        }

        if (briefingUsesReportScreen)
        {
            BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
            instantBriefingIntro = true;
            StartBriefingTypewriter(briefingBody);
            yield return null;
            yield return FadeOutTransitionBlocker(0.48f);
        }
        else
        {
            BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
            yield return FadeOutTransitionBlocker(0.48f);
        }

        BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
        startupVhsRoutine = null;
    }

    private void StopStartupVhsIntro()
    {
        if (startupVhsRoutine != null)
        {
            StopCoroutine(startupVhsRoutine);
            startupVhsRoutine = null;
        }

        if (startupVhsAudioSource != null && startupVhsAudioSource.isPlaying)
        {
            startupVhsAudioSource.Stop();
            BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
        }
    }

    private GameObject CreateStoryPanel(string name, string backgroundResourcePath)
    {
        var panel = new GameObject(name);
        panel.transform.SetParent(canvas.transform, false);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.sprite = LoadSprite(backgroundResourcePath);
        image.color = image.sprite != null ? Color.white : new Color(0.025f, 0.023f, 0.021f, 0.96f);

        return panel;
    }

    private GameObject CreatePausePanel()
    {
        var panel = new GameObject("PausePanel");
        panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image dimBackground = panel.AddComponent<Image>();
        dimBackground.color = Color.black;
        dimBackground.raycastTarget = true;

        pauseCanvasGroup = panel.AddComponent<CanvasGroup>();
        pauseCanvasGroup.alpha = 0f;
        pauseCanvasGroup.interactable = false;
        pauseCanvasGroup.blocksRaycasts = false;

        Sprite pauseSprite = LoadSprite(PauseMenuImagePath);
        GameObject artObject = new GameObject("Pause Menu Art", typeof(RectTransform));
        artObject.transform.SetParent(panel.transform, false);
        RectTransform artRect = artObject.GetComponent<RectTransform>();
        artRect.anchorMin = new Vector2(0.5f, 0.5f);
        artRect.anchorMax = new Vector2(0.5f, 0.5f);
        artRect.pivot = new Vector2(0.5f, 0.5f);
        artRect.anchoredPosition = Vector2.zero;
        artRect.sizeDelta = GetPauseMenuArtSize(pauseSprite);
        artRect.localScale = Vector3.one;

        Image pauseArt = artObject.AddComponent<Image>();
        pauseArt.sprite = pauseSprite;
        pauseArt.color = pauseSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        pauseArt.preserveAspect = true;
        pauseArt.raycastTarget = false;

        CreatePauseHitbox("Continue Hitbox", artObject.transform, new Rect(0.155f, 0.438f, 0.690f, 0.170f), Resume);
        CreatePauseHitbox("Main Menu Hitbox", artObject.transform, new Rect(0.155f, 0.626f, 0.690f, 0.170f), GoToMainMenu);
        CreatePauseHitbox("Quit Hitbox", artObject.transform, new Rect(0.155f, 0.810f, 0.690f, 0.160f), QuitGame);

        panel.SetActive(false);
        return panel;
    }

    private Vector2 GetPauseMenuArtSize(Sprite pauseSprite)
    {
        Vector2 sourceSize = pauseSprite != null ? pauseSprite.rect.size : new Vector2(1536f, 1024f);
        float aspect = sourceSize.x / Mathf.Max(1f, sourceSize.y);
        const float maxWidth = 480f;
        const float maxHeight = 360f;

        float width = maxWidth;
        float height = width / aspect;

        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        return new Vector2(width, height);
    }

    private Button CreatePauseHitbox(string name, Transform parent, Rect normalizedImageRect, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(normalizedImageRect.xMin, 1f - normalizedImageRect.yMax);
        rect.anchorMax = new Vector2(normalizedImageRect.xMax, 1f - normalizedImageRect.yMin);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        CreatePauseButtonGlow(buttonObject.transform);
        buttonObject.AddComponent<MenuButtonGlow>();
        return button;
    }

    private void CreatePauseButtonGlow(Transform parent)
    {
        GameObject glowObject = new GameObject("RuntimeButtonGlow", typeof(RectTransform));
        glowObject.transform.SetParent(parent, false);

        RectTransform rect = glowObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(-10f, -8f);
        rect.localScale = Vector3.one;

        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.sprite = LoadSprite(ButtonGlowResourcePath);
        glowImage.color = Color.white;
        glowImage.raycastTarget = false;

        CanvasGroup glowGroup = glowObject.AddComponent<CanvasGroup>();
        glowGroup.alpha = 0f;
        glowGroup.blocksRaycasts = false;
        glowGroup.interactable = false;
    }

    private Button CreatePauseButton(string name, Transform parent, string icon, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(960f, 105f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.520f, 0.165f, 0.635f, 0.94f);
        image.sprite = CreateRoundedRectSprite(new Color(1f, 1f, 1f, 1f), new Color(0.090f, 0.055f, 0.120f, 1f));
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = 1f;

        var shadow = buttonObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.78f);
        shadow.effectDistance = new Vector2(10f, -10f);

        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.030f, 0.020f, 0.045f, 1f);
        outline.effectDistance = new Vector2(4f, -4f);

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        TMP_Text iconText = CreateText("Icon", buttonObject.transform, new Vector2(-360f, 0f), new Vector2(145f, 88f), 56f, TextAlignmentOptions.Center);
        iconText.text = icon;
        iconText.fontStyle = FontStyles.Bold;
        iconText.color = new Color(0.030f, 0.018f, 0.045f);
        iconText.outlineWidth = 0.06f;

        TMP_Text labelText = CreateText("Label", buttonObject.transform, new Vector2(80f, 0f), new Vector2(640f, 88f), 48f, TextAlignmentOptions.Left);
        labelText.text = label;
        labelText.fontStyle = FontStyles.Bold;
        labelText.characterSpacing = 3f;
        labelText.color = new Color(0.970f, 0.890f, 1f);
        labelText.outlineWidth = 0.16f;

        return button;
    }

    private Image CreatePauseImage(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, Sprite sprite)
    {
        var imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        return image;
    }

    private void SetPaused(bool paused)
    {
        if (isPaused == paused)
        {
            return;
        }

        isPaused = paused;

        if (pausePanel != null)
        {
            if (paused)
            {
                pausePanel.transform.SetAsLastSibling();
                pausePanel.SetActive(true);
                StartPauseFade(1f, 0.25f, false);
            }
            else
            {
                StartPauseFade(0f, 0.2f, true);
            }
        }

        if (paused)
        {
            cursorLockStateBeforePause = Cursor.lockState;
            cursorVisibleBeforePause = Cursor.visible;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Time.timeScale = 0f;
            IsBlockingScreenOpen = true;
            AudioManager.Instance?.PlayUiPause();
            BackgroundMusicManager.Instance?.PlayBriefingMusic(false);
            return;
        }

        Time.timeScale = 1f;
        settingsPanel?.SetActive(false);
        Cursor.lockState = cursorLockStateBeforePause;
        Cursor.visible = cursorVisibleBeforePause;
        IsBlockingScreenOpen = false;
        AudioManager.Instance?.PlayUiClick();
        BackgroundMusicManager.Instance?.PlayGameplayMusic(false);
    }

    private void StartPauseFade(float targetAlpha, float duration, bool deactivateAfterFade)
    {
        if (pauseCanvasGroup == null)
        {
            pauseCanvasGroup = pausePanel != null ? pausePanel.GetComponent<CanvasGroup>() : null;
        }

        if (pauseFadeRoutine != null)
        {
            StopCoroutine(pauseFadeRoutine);
        }

        pauseFadeRoutine = StartCoroutine(FadePausePanel(targetAlpha, duration, deactivateAfterFade));
    }

    private IEnumerator FadePausePanel(float targetAlpha, float duration, bool deactivateAfterFade)
    {
        if (pauseCanvasGroup == null)
        {
            if (pausePanel != null && deactivateAfterFade)
            {
                pausePanel.SetActive(false);
            }

            yield break;
        }

        float startAlpha = pauseCanvasGroup.alpha;
        float elapsed = 0f;
        pauseCanvasGroup.interactable = true;
        pauseCanvasGroup.blocksRaycasts = true;

        if (duration <= 0.001f)
        {
            pauseCanvasGroup.alpha = Mathf.Clamp01(targetAlpha);
        }
        else
        {
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                pauseCanvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
                yield return null;
            }
        }

        pauseCanvasGroup.alpha = Mathf.Clamp01(targetAlpha);
        pauseCanvasGroup.interactable = targetAlpha > 0.001f;
        pauseCanvasGroup.blocksRaycasts = targetAlpha > 0.001f;

        if (deactivateAfterFade && pausePanel != null)
        {
            pausePanel.SetActive(false);
        }

        pauseFadeRoutine = null;
    }

    private bool CanOpenPauseMenu()
    {
        if (IsBlockingScreenOpen)
        {
            return false;
        }

        GameManager gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        return gameManager == null || gameManager.IsPlaying;
    }

    private void GoToMainMenuOrQuit()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SceneExistsInBuildSettings("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        GoToMainMenu();

        if (string.IsNullOrWhiteSpace(menuSceneName))
        {
            QuitGame();
        }
    }

    private bool SceneExistsInBuildSettings(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string scenePath = SceneUtility.GetScenePathByBuildIndex(i);
            if (System.IO.Path.GetFileNameWithoutExtension(scenePath) == sceneName)
            {
                return true;
            }
        }

        return false;
    }

    private Sprite CreateRadialGlowSprite()
    {
        const int size = 256;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        texture.name = "Generated Pause Glow";
        texture.wrapMode = TextureWrapMode.Clamp;

        Vector2 center = new Vector2((size - 1) * 0.5f, (size - 1) * 0.5f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), center) / (size * 0.5f);
                float alpha = Mathf.Clamp01(1f - distance);
                alpha *= alpha;
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
    }

    private Sprite CreateRoundedRectSprite(Color fillColor, Color borderColor)
    {
        const int width = 128;
        const int height = 32;
        const int radius = 8;
        const int border = 3;
        var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
        texture.name = "Generated Pause Button";
        texture.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float cornerX = x < radius ? radius : x >= width - radius ? width - radius - 1 : x;
                float cornerY = y < radius ? radius : y >= height - radius ? height - radius - 1 : y;
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(cornerX, cornerY));
                bool inside = distance <= radius;
                bool isBorder = x < border || y < border || x >= width - border || y >= height - border || distance >= radius - border;

                Color pixel = inside ? (isBorder ? borderColor : fillColor) : Color.clear;
                texture.SetPixel(x, y, pixel);
            }
        }

        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
    }

    private Image CreateFullPanelImage(string name, Transform parent, string resourcePath)
    {
        var imageObject = new GameObject(name);
        imageObject.transform.SetParent(parent, false);
        imageObject.transform.SetSiblingIndex(0);

        RectTransform rect = imageObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = imageObject.AddComponent<Image>();
        image.sprite = LoadSprite(resourcePath);
        image.color = new Color(1f, 1f, 1f, 0f);
        image.preserveAspect = false;
        image.raycastTarget = false;
        image.gameObject.SetActive(false);
        return image;
    }

    private bool ShouldUseReportScreen(string title, string body)
    {
        return (title != null && title.Contains("REPORT #01", StringComparison.OrdinalIgnoreCase))
            || (title != null && title.Contains("POSTAL SECTOR 7", StringComparison.OrdinalIgnoreCase))
            || (title != null && title.Contains("RELATÓRIO", StringComparison.OrdinalIgnoreCase))
            || (title != null && title.Contains("RELATORIO", StringComparison.OrdinalIgnoreCase))
            || (title != null && title.Contains("FITA", StringComparison.OrdinalIgnoreCase))
            || (title != null && title.Contains("SETOR POSTAL", StringComparison.OrdinalIgnoreCase))
            || (body != null && body.Contains("[Gravação Iniciada", StringComparison.OrdinalIgnoreCase));
    }

    private void ConfigureBriefingMode(bool useReportScreen)
    {
        bool hasCustomReportArt = useReportScreen && !string.IsNullOrWhiteSpace(activeBriefingBackgroundPath);

        if (briefingBackgroundImage != null)
        {
            briefingBackgroundImage.sprite = hasCustomReportArt ? null : LoadSprite(useReportScreen ? ReportBriefingBlankPath : "Sector7SceneReferences/backgrounds/night_briefing_background");
            briefingBackgroundImage.color = hasCustomReportArt ? new Color(0.010f, 0.006f, 0.015f, 1f) : Color.white;
            briefingBackgroundImage.preserveAspect = false;
        }

        if (briefingCustomArtImage != null)
        {
            Sprite customSprite = hasCustomReportArt ? LoadSprite(activeBriefingBackgroundPath) : null;
            briefingCustomArtImage.sprite = customSprite;
            briefingCustomArtImage.color = customSprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            briefingCustomArtImage.preserveAspect = true;
            briefingCustomArtImage.raycastTarget = false;
            briefingCustomArtImage.gameObject.SetActive(hasCustomReportArt && customSprite != null);
            briefingCustomArtImage.transform.SetSiblingIndex(0);
        }

        if (briefingNextOverlayImage != null)
        {
            briefingNextOverlayImage.sprite = LoadSprite(ReportBriefingNextPath);
            briefingNextOverlayImage.color = new Color(1f, 1f, 1f, 0f);
            briefingNextOverlayImage.preserveAspect = false;
            briefingNextOverlayImage.gameObject.SetActive(useReportScreen && !hasCustomReportArt);
            briefingNextOverlayImage.transform.SetSiblingIndex(hasCustomReportArt ? 2 : 1);
        }

        if (briefingTitleText != null)
        {
            briefingTitleText.gameObject.SetActive(!useReportScreen);
            RectTransform titleRect = briefingTitleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchoredPosition = hasCustomReportArt ? new Vector2(0f, 318f) : new Vector2(0f, 235f);
                titleRect.sizeDelta = hasCustomReportArt ? new Vector2(920f, 72f) : new Vector2(1280f, 98f);
            }

            briefingTitleText.fontSize = hasCustomReportArt ? 36f : 62f;
            briefingTitleText.alignment = TextAlignmentOptions.Center;
            briefingTitleText.color = hasCustomReportArt ? new Color(0.900f, 0.760f, 1f) : new Color(0.909f, 0.878f, 1f);
        }

        if (briefingCanvasGroup != null && !useReportScreen)
        {
            briefingCanvasGroup.alpha = 1f;
            briefingCanvasGroup.interactable = true;
            briefingCanvasGroup.blocksRaycasts = true;
        }

        if (briefingBodyText != null)
        {
            RectTransform bodyRect = briefingBodyText.GetComponent<RectTransform>();
            if (bodyRect != null)
            {
                bodyRect.anchorMin = new Vector2(0.5f, 0.5f);
                bodyRect.anchorMax = new Vector2(0.5f, 0.5f);
                bodyRect.anchoredPosition = hasCustomReportArt ? new Vector2(0f, -42f) : useReportScreen ? new Vector2(0f, -15f) : new Vector2(0f, -5f);
                bodyRect.sizeDelta = hasCustomReportArt ? new Vector2(900f, 445f) : useReportScreen ? new Vector2(1030f, 650f) : new Vector2(1160f, 390f);
            }

            briefingBodyText.fontSize = hasCustomReportArt ? 30f : useReportScreen ? 30f : 36f;
            briefingBodyText.color = useReportScreen ? new Color(0.925f, 0.920f, 0.940f) : new Color(0.909f, 0.878f, 1f);
            briefingBodyText.alignment = useReportScreen ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            briefingBodyText.lineSpacing = useReportScreen ? 3f : 8f;
            briefingBodyText.characterSpacing = useReportScreen ? 0.5f : 0f;
            briefingBodyText.textWrappingMode = TextWrappingModes.Normal;
            briefingBodyText.overflowMode = useReportScreen ? TextOverflowModes.Truncate : TextOverflowModes.Overflow;
            TMP_FontAsset reportFont = useReportScreen ? GetReportFontAsset() : null;
            if (reportFont != null)
            {
                briefingBodyText.font = reportFont;
            }
            briefingBodyText.outlineWidth = useReportScreen ? 0.10f : 0.15f;
        }

        if (briefingContinueButton != null)
        {
            RectTransform buttonRect = briefingContinueButton.GetComponent<RectTransform>();
            Image buttonImage = briefingContinueButton.GetComponent<Image>();
            TMP_Text buttonLabel = briefingContinueButton.GetComponentInChildren<TMP_Text>(true);

            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = hasCustomReportArt ? new Vector2(0f, -362f) : useReportScreen ? new Vector2(590f, -330f) : new Vector2(0f, -340f);
                buttonRect.sizeDelta = hasCustomReportArt ? new Vector2(430f, 88f) : useReportScreen ? new Vector2(460f, 125f) : new Vector2(360f, 96f);
            }

            if (buttonImage != null)
            {
                buttonImage.color = hasCustomReportArt ? new Color(0.035f, 0.015f, 0.055f, 0.88f) : useReportScreen ? new Color(1f, 1f, 1f, 0.001f) : new Color(0.045f, 0.030f, 0.075f, 0.92f);
            }

            if (buttonLabel != null)
            {
                buttonLabel.gameObject.SetActive(!useReportScreen);
                buttonLabel.text = "CONTINUAR";
                buttonLabel.fontSize = 34f;
                buttonLabel.color = new Color(0.945f, 0.914f, 1f);
            }
        }
    }

    private void ConfigureConclusionMode(bool useImageLayout)
    {
        if (conclusionTitleText != null)
        {
            conclusionTitleText.gameObject.SetActive(!useImageLayout);
            RectTransform titleRect = conclusionTitleText.GetComponent<RectTransform>();
            if (titleRect != null)
            {
                titleRect.anchoredPosition = useImageLayout ? new Vector2(0f, 112f) : new Vector2(0f, 230f);
                titleRect.sizeDelta = useImageLayout ? new Vector2(940f, 82f) : new Vector2(1280f, 98f);
            }

            conclusionTitleText.fontSize = useImageLayout ? 42f : 64f;
            conclusionTitleText.alignment = TextAlignmentOptions.Center;
            conclusionTitleText.color = new Color(0.940f, 0.830f, 1f);
        }

        if (conclusionBodyText != null)
        {
            conclusionBodyText.gameObject.SetActive(!useImageLayout);
            RectTransform bodyRect = conclusionBodyText.GetComponent<RectTransform>();
            if (bodyRect != null)
            {
                bodyRect.anchoredPosition = useImageLayout ? new Vector2(0f, -55f) : new Vector2(0f, 5f);
                bodyRect.sizeDelta = useImageLayout ? new Vector2(900f, 260f) : new Vector2(1160f, 360f);
            }

            conclusionBodyText.fontSize = useImageLayout ? 30f : 36f;
            conclusionBodyText.alignment = useImageLayout ? TextAlignmentOptions.Center : TextAlignmentOptions.TopLeft;
            conclusionBodyText.color = new Color(0.900f, 0.790f, 1f);
            conclusionBodyText.lineSpacing = useImageLayout ? 4f : 8f;
        }

        if (conclusionContinueButton != null)
        {
            RectTransform buttonRect = conclusionContinueButton.GetComponent<RectTransform>();
            if (buttonRect != null)
            {
                buttonRect.anchoredPosition = useImageLayout ? new Vector2(0f, -350f) : new Vector2(0f, -335f);
                buttonRect.sizeDelta = useImageLayout ? new Vector2(620f, 120f) : new Vector2(360f, 96f);
            }

            Image buttonImage = conclusionContinueButton.GetComponent<Image>();
            if (buttonImage != null)
            {
                buttonImage.color = useImageLayout ? new Color(1f, 1f, 1f, 0.001f) : new Color(0.045f, 0.030f, 0.075f, 0.92f);
            }

            TMP_Text buttonLabel = conclusionContinueButton.GetComponentInChildren<TMP_Text>(true);
            if (buttonLabel != null)
            {
                buttonLabel.gameObject.SetActive(!useImageLayout);
                buttonLabel.fontSize = useImageLayout ? 30f : 34f;
                buttonLabel.color = new Color(0.965f, 0.910f, 1f);
            }
        }
    }

    private void StartBriefingTypewriter(string text)
    {
        if (briefingTypingRoutine != null)
        {
            StopCoroutine(briefingTypingRoutine);
        }

        if (briefingTransitionRoutine != null)
        {
            StopCoroutine(briefingTransitionRoutine);
            briefingTransitionRoutine = null;
        }

        if (briefingIntroRoutine != null)
        {
            StopCoroutine(briefingIntroRoutine);
            briefingIntroRoutine = null;
        }

        if (briefingOutroRoutine != null)
        {
            StopCoroutine(briefingOutroRoutine);
            briefingOutroRoutine = null;
        }

        isBriefingClosing = false;
        briefingSkipClicks = 0;
        briefingReportPageIndex = 0;
        PrepareReportPages(text);
        fullBriefingText = GetCurrentReportPageText();
        isBriefingTyping = true;
        UpdateBriefingContinueButtonLabel();
        if (briefingBodyText != null)
        {
            briefingBodyText.text = "";
        }

        SetReportNextOverlayAlpha(0f);
        if (briefingContinueButton != null)
        {
            briefingContinueButton.interactable = false;
        }

        PlayBriefingTapeAudio();
        briefingIntroRoutine = StartCoroutine(FadeInReportBriefingThenType());
    }

    private bool AdvanceReportBriefingPage()
    {
        if (!briefingUsesReportScreen || briefingReportPageIndex >= briefingReportPages.Count - 1)
        {
            return false;
        }

        briefingReportPageIndex++;
        AudioManager.Instance?.PlayReportOpen();
        fullBriefingText = GetCurrentReportPageText();
        isBriefingTyping = true;
        briefingSkipClicks = 0;
        UpdateBriefingContinueButtonLabel();

        if (briefingTransitionRoutine != null)
        {
            StopCoroutine(briefingTransitionRoutine);
            briefingTransitionRoutine = null;
        }

        SetReportNextOverlayAlpha(0f);

        if (briefingBodyText != null)
        {
            briefingBodyText.text = "";
        }

        if (briefingTypingRoutine != null)
        {
            StopCoroutine(briefingTypingRoutine);
        }

        briefingTypingRoutine = StartCoroutine(TypeBriefingText());
        return true;
    }

    private IEnumerator CloseReportBriefingSmoothly()
    {
        isBriefingClosing = true;
        AudioManager.Instance?.PlayReportClose();

        if (briefingContinueButton != null)
        {
            briefingContinueButton.interactable = false;
        }

        float startVolume = briefingTapeAudioSource != null ? briefingTapeAudioSource.volume : 0f;
        const float audioFadeDuration = 0.55f;
        const float blackHoldDuration = 0.75f;
        const float gameplayWarmupHoldDuration = 0.90f;
        const float revealDuration = 1.05f;
        float elapsed = 0f;
        PrepareTransitionBlockerForFade(1f);
        BackgroundMusicManager.Instance?.FadeCurrentMusicToSilence(audioFadeDuration + blackHoldDuration);

        while (elapsed < audioFadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / audioFadeDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (briefingCanvasGroup != null)
            {
                briefingCanvasGroup.alpha = 1f;
                briefingCanvasGroup.interactable = false;
                briefingCanvasGroup.blocksRaycasts = true;
            }

            if (briefingTapeAudioSource != null)
            {
                briefingTapeAudioSource.volume = Mathf.Lerp(startVolume, 0f, eased);
            }

            AudioManager.Instance?.SetBriefingCassetteFade(1f - eased);

            yield return null;
        }

        StopBriefingTapeAudio();
        if (briefingTapeAudioSource != null)
        {
            briefingTapeAudioSource.volume = 0.38f;
        }

        if (briefingCanvasGroup != null)
        {
            briefingCanvasGroup.alpha = 1f;
            briefingCanvasGroup.interactable = true;
            briefingCanvasGroup.blocksRaycasts = true;
        }

        briefingPanel?.SetActive(false);

        yield return new WaitForSecondsRealtime(blackHoldDuration);

        briefingContinueAction?.Invoke();
        yield return new WaitForSecondsRealtime(gameplayWarmupHoldDuration);
        yield return FadeOutTransitionBlocker(revealDuration);
        IsBlockingScreenOpen = false;
        isBriefingClosing = false;
        briefingOutroRoutine = null;
    }

    private IEnumerator FadeInReportBriefingThenType()
    {
        if (briefingCanvasGroup != null)
        {
            if (instantBriefingIntro)
            {
                briefingCanvasGroup.alpha = 1f;
                briefingCanvasGroup.interactable = true;
                briefingCanvasGroup.blocksRaycasts = true;
            }
            else
            {
                briefingCanvasGroup.alpha = 0f;
                briefingCanvasGroup.interactable = false;
                briefingCanvasGroup.blocksRaycasts = true;

                const float fadeDuration = 0.55f;
                float elapsed = 0f;
                while (elapsed < fadeDuration)
                {
                    elapsed += Time.unscaledDeltaTime;
                    float t = Mathf.Clamp01(elapsed / fadeDuration);
                    briefingCanvasGroup.alpha = 1f - Mathf.Pow(1f - t, 3f);
                    yield return null;
                }

                briefingCanvasGroup.alpha = 1f;
                briefingCanvasGroup.interactable = true;
            }
        }

        yield return new WaitForSecondsRealtime(instantBriefingIntro ? 0.05f : 0.35f);
        instantBriefingIntro = false;

        if (briefingContinueButton != null)
        {
            briefingContinueButton.interactable = true;
        }

        briefingTypingRoutine = StartCoroutine(TypeBriefingText());
        briefingIntroRoutine = null;
    }

    private IEnumerator TypeBriefingText()
    {
        string visibleText = "";

        for (int i = 0; i < fullBriefingText.Length; i++)
        {
            char character = fullBriefingText[i];
            visibleText += character;
            if (briefingBodyText != null)
            {
                briefingBodyText.text = visibleText + " |";
            }

            yield return new WaitForSecondsRealtime(GetTypewriterDelay(character));
        }

        CompleteBriefingTypewriter();
    }

    private void CompleteBriefingTypewriter()
    {
        if (briefingTypingRoutine != null)
        {
            StopCoroutine(briefingTypingRoutine);
            briefingTypingRoutine = null;
        }

        isBriefingTyping = false;
        briefingSkipClicks = 0;
        if (briefingBodyText != null)
        {
            briefingBodyText.text = fullBriefingText;
        }

        UpdateBriefingContinueButtonLabel();

        if (briefingUsesReportScreen && briefingBackgroundImage != null)
        {
            StartReportNextTransition();
        }
        else
        {
            SetReportNextOverlayAlpha(0f);
        }
    }

    private void PrepareReportPages(string text)
    {
        briefingReportPages.Clear();

        string cleanedText = RemoveRecorderMarkers(text ?? "").Trim();
        string[] explicitPages = cleanedText.Split(new[] { "[PAGE]", "[[PAGE]]", "\f" }, StringSplitOptions.None);
        if (explicitPages.Length > 1)
        {
            foreach (string page in explicitPages)
            {
                string trimmedPage = page.Trim();
                if (!string.IsNullOrWhiteSpace(trimmedPage))
                {
                    briefingReportPages.Add(trimmedPage);
                }
            }
        }
        else
        {
            briefingReportPages.AddRange(AutoPaginateReportText(cleanedText));
        }

        if (briefingReportPages.Count == 0)
        {
            briefingReportPages.Add("");
        }
    }

    private string RemoveRecorderMarkers(string text)
    {
        return text
            .Replace("[Gravação Iniciada — 11:46 PM]", "")
            .Replace("[Gravacao Iniciada - 11:46 PM]", "")
            .Replace("[Fim da gravação.", "")
            .Replace("[Fim da gravação]", "")
            .Replace("[Fim da gravacao.]", "")
            .Replace("[Fim da gravacao]", "");
    }

    private List<string> AutoPaginateReportText(string text)
    {
        var pages = new List<string>();
        var pageLines = new List<string>();
        string[] lines = text.Replace("\r\n", "\n").Split('\n');
        int pageCharacterCount = 0;

        foreach (string rawLine in lines)
        {
            string line = rawLine.TrimEnd();
            int nextCharacterCount = pageCharacterCount + line.Length + 1;
            bool pageIsFull = pageLines.Count >= 10 || (pageLines.Count > 0 && nextCharacterCount > 820);

            if (pageIsFull)
            {
                AddReportPage(pages, pageLines);
                pageCharacterCount = 0;
            }

            pageLines.Add(line);
            pageCharacterCount += line.Length + 1;
        }

        AddReportPage(pages, pageLines);
        return pages;
    }

    private void AddReportPage(List<string> pages, List<string> lines)
    {
        if (lines == null || lines.Count == 0)
        {
            return;
        }

        string page = string.Join("\n", lines).Trim();
        lines.Clear();

        if (!string.IsNullOrWhiteSpace(page))
        {
            pages.Add(page);
        }
    }

    private string GetCurrentReportPageText()
    {
        if (briefingReportPages.Count == 0)
        {
            return "";
        }

        int pageIndex = Mathf.Clamp(briefingReportPageIndex, 0, briefingReportPages.Count - 1);
        return briefingReportPages[pageIndex];
    }

    private bool HasMoreBriefingPages()
    {
        return briefingReportPageIndex < briefingReportPages.Count - 1;
    }

    private void UpdateBriefingContinueButtonLabel()
    {
        TMP_Text buttonLabel = briefingContinueButton != null ? briefingContinueButton.GetComponentInChildren<TMP_Text>(true) : null;
        if (buttonLabel == null)
        {
            return;
        }

        bool hasMorePages = HasMoreBriefingPages();
        buttonLabel.gameObject.SetActive(!briefingUsesReportScreen);
        buttonLabel.text = hasMorePages ? "PRÓXIMO" : "CONTINUAR";
    }

    private string JoinPages(IReadOnlyList<string> pages)
    {
        if (pages == null || pages.Count == 0)
        {
            return "";
        }

        var cleanedPages = new List<string>();
        foreach (string page in pages)
        {
            if (!string.IsNullOrWhiteSpace(page))
            {
                cleanedPages.Add(page.Trim());
            }
        }

        return cleanedPages.Count == 0 ? "" : string.Join("\n\n[PAGE]\n\n", cleanedPages);
    }

    private void AddPages(List<string> target, string text)
    {
        target.Clear();
        string cleanedText = (text ?? "").Trim();
        string[] pages = cleanedText.Split(new[] { "[PAGE]", "[[PAGE]]", "\f" }, StringSplitOptions.None);
        foreach (string page in pages)
        {
            string trimmedPage = page.Trim();
            if (!string.IsNullOrWhiteSpace(trimmedPage))
            {
                target.Add(trimmedPage);
            }
        }

        if (target.Count == 0)
        {
            target.Add("");
        }
    }

    private string GetCurrentConclusionPageText()
    {
        if (conclusionPages.Count == 0)
        {
            return "";
        }

        int pageIndex = Mathf.Clamp(conclusionPageIndex, 0, conclusionPages.Count - 1);
        return conclusionPages[pageIndex];
    }

    private void AdvanceConclusion()
    {
        if (conclusionPageIndex < conclusionPages.Count - 1)
        {
            conclusionPageIndex++;
            if (conclusionBodyText != null)
            {
                conclusionBodyText.text = GetCurrentConclusionPageText();
            }

            UpdateConclusionButtonLabel();
            return;
        }

        conclusionContinueAction?.Invoke();
    }

    private void UpdateConclusionButtonLabel()
    {
        TMP_Text buttonLabel = conclusionContinueButton != null ? conclusionContinueButton.GetComponentInChildren<TMP_Text>(true) : null;
        if (buttonLabel != null)
        {
            buttonLabel.text = conclusionPageIndex < conclusionPages.Count - 1 ? "PRÓXIMO" : conclusionFinalButtonText;
        }
    }

    public void PlayBriefingTextGlitch(float duration = 0.75f)
    {
        if (briefingBodyText == null || !briefingUsesReportScreen || briefingPanel == null || !briefingPanel.activeSelf)
        {
            return;
        }

        if (briefingGlitchRoutine != null)
        {
            StopCoroutine(briefingGlitchRoutine);
        }

        briefingGlitchRoutine = StartCoroutine(GlitchBriefingText(duration));
    }

    private IEnumerator GlitchBriefingText(float duration)
    {
        string originalText = briefingBodyText.text;
        Color originalColor = briefingBodyText.color;
        RectTransform rect = briefingBodyText.GetComponent<RectTransform>();
        Vector2 originalPosition = rect != null ? rect.anchoredPosition : Vector2.zero;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;

            if (briefingBodyText != null)
            {
                briefingBodyText.text = MakeGlitchedText(originalText);
                briefingBodyText.color = Color.Lerp(originalColor, new Color(0.850f, 0.420f, 1f, 1f), UnityEngine.Random.Range(0.25f, 0.72f));
            }

            if (rect != null)
            {
                rect.anchoredPosition = originalPosition + new Vector2(UnityEngine.Random.Range(-4f, 4f), UnityEngine.Random.Range(-2f, 2f));
            }

            yield return new WaitForSecondsRealtime(UnityEngine.Random.Range(0.035f, 0.075f));
        }

        if (briefingBodyText != null)
        {
            briefingBodyText.text = originalText;
            briefingBodyText.color = originalColor;
        }

        if (rect != null)
        {
            rect.anchoredPosition = originalPosition;
        }

        briefingGlitchRoutine = null;
    }

    private string MakeGlitchedText(string source)
    {
        if (string.IsNullOrEmpty(source))
        {
            return source;
        }

        char[] characters = source.ToCharArray();
        int mutationCount = Mathf.Clamp(characters.Length / 22, 1, 14);
        const string noise = "7#_:/|";

        for (int i = 0; i < mutationCount; i++)
        {
            int index = UnityEngine.Random.Range(0, characters.Length);
            if (!char.IsWhiteSpace(characters[index]))
            {
                characters[index] = noise[UnityEngine.Random.Range(0, noise.Length)];
            }
        }

        return new string(characters);
    }

    private float GetTypewriterDelay(char character)
    {
        switch (character)
        {
            case '.':
            case '?':
            case '!':
                return 0.22f;
            case ',':
            case ';':
            case ':':
                return 0.11f;
            case '\n':
                return 0.16f;
            case ' ':
                return 0.035f;
            default:
                return 0.055f;
        }
    }

    private void StartReportNextTransition()
    {
        if (briefingTransitionRoutine != null)
        {
            StopCoroutine(briefingTransitionRoutine);
        }

        briefingTransitionRoutine = StartCoroutine(FadeReportNextOverlay());
    }

    private IEnumerator FadeReportNextOverlay()
    {
        if (briefingNextOverlayImage == null)
        {
            yield break;
        }

        briefingNextOverlayImage.gameObject.SetActive(true);
        float elapsed = 0f;
        const float duration = 0.35f;
        float startAlpha = briefingNextOverlayImage.color.a;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            SetReportNextOverlayAlpha(Mathf.Lerp(startAlpha, 1f, eased));
            yield return null;
        }

        SetReportNextOverlayAlpha(1f);
        briefingTransitionRoutine = null;
    }

    private void SetReportNextOverlayAlpha(float alpha)
    {
        if (briefingNextOverlayImage == null)
        {
            return;
        }

        bool canUseNextOverlay = briefingUsesReportScreen && string.IsNullOrWhiteSpace(activeBriefingBackgroundPath);
        briefingNextOverlayImage.gameObject.SetActive(canUseNextOverlay && (briefingUsesReportScreen || alpha > 0f));
        briefingNextOverlayImage.color = new Color(1f, 1f, 1f, Mathf.Clamp01(alpha));
    }

    private TMP_FontAsset GetReportFontAsset()
    {
        if (reportFontAsset != null)
        {
            return reportFontAsset;
        }

        // Usa o TMP Font Asset importado no projeto, evitando depender de fontes do sistema.
        reportFontAsset = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        if (reportFontAsset != null)
        {
            return reportFontAsset;
        }

        if (TMP_Settings.defaultFontAsset != null)
        {
            reportFontAsset = TMP_Settings.defaultFontAsset;
            return reportFontAsset;
        }

        Font fallbackFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (fallbackFont == null)
        {
            fallbackFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        if (fallbackFont != null)
        {
            reportFontAsset = TMP_FontAsset.CreateFontAsset(fallbackFont);
        }

        return reportFontAsset;
    }

    private void PlayBriefingTapeAudio()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayBriefingCassette();
            BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
            return;
        }

        if (briefingTapeAudioSource == null)
        {
            briefingTapeAudioSource = gameObject.AddComponent<AudioSource>();
            briefingTapeAudioSource.playOnAwake = false;
            briefingTapeAudioSource.loop = true;
            briefingTapeAudioSource.volume = 0.38f;
            briefingTapeAudioSource.spatialBlend = 0f;
        }

        if (briefingTapeLoopClip == null)
        {
            briefingTapeLoopClip = CreateTapeNoiseClip();
        }

        briefingTapeAudioSource.clip = briefingTapeLoopClip;
        briefingTapeAudioSource.volume = 0.38f;
        briefingTapeAudioSource.Play();
        BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
    }

    private void StopBriefingTapeAudio()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopBriefingCassette();
            BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
            return;
        }

        if (briefingTapeAudioSource != null && briefingTapeAudioSource.isPlaying)
        {
            briefingTapeAudioSource.Stop();
        }

        BackgroundMusicManager.Instance?.SetMusicSuppressed(false);
    }

    private AudioClip CreateTapeNoiseClip()
    {
        const int sampleRate = 22050;
        const float duration = 3.2f;
        int sampleCount = Mathf.RoundToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];
        float phase = 0f;
        float voicePhase = 0f;
        float previousNoise = 0f;

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float whiteNoise = UnityEngine.Random.Range(-1f, 1f) * 0.034f;
            previousNoise = Mathf.Lerp(previousNoise, UnityEngine.Random.Range(-1f, 1f), 0.045f);
            phase += 2f * Mathf.PI * 58f / sampleRate;
            float motorHum = Mathf.Sin(phase) * 0.024f;
            float wobble = (Mathf.PerlinNoise(i * 0.0009f, 0.23f) - 0.5f) * 0.020f;
            float speechGate = Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 2.25f * time + 0.4f)), 2.6f);
            float mouthShape = 0.65f + Mathf.PerlinNoise(time * 1.7f, 4.7f) * 0.55f;
            voicePhase += 2f * Mathf.PI * (120f + Mathf.Sin(time * 4.2f) * 18f) / sampleRate;
            float voiceFundamental = Mathf.Sin(voicePhase) * 0.022f;
            float voiceFormantA = Mathf.Sin(voicePhase * 2.20f) * 0.011f;
            float voiceFormantB = Mathf.Sin(voicePhase * 3.65f) * 0.007f;
            float consonants = previousNoise * 0.040f * Mathf.Pow(Mathf.Max(0f, Mathf.Sin(2f * Mathf.PI * 6.2f * time)), 3.0f);
            float muffledVoice = (voiceFundamental + voiceFormantA + voiceFormantB + consonants) * speechGate * mouthShape;
            samples[i] = Mathf.Clamp(whiteNoise + motorHum + wobble + muffledVoice, -1f, 1f);
        }

        AudioClip clip = AudioClip.Create("Generated Cassette Voice Recording Loop", sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private TMP_Text CreateText(string name, Transform parent, Vector2 anchoredPosition, Vector2 size, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = new Color(0.909f, 0.878f, 1f);
        text.outlineWidth = 0.15f;
        text.outlineColor = Color.black;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.lineSpacing = 8f;
        return text;
    }

    private Button CreateButton(string name, Transform parent, string label, Vector2 anchoredPosition, UnityEngine.Events.UnityAction onClick)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(360f, 96f);

        var image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.045f, 0.030f, 0.075f, 0.92f);

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        var labelText = CreateText("Label", buttonObject.transform, Vector2.zero, rect.sizeDelta, 34f, TextAlignmentOptions.Center);
        labelText.text = label;
        labelText.fontStyle = FontStyles.Bold;
        labelText.color = new Color(0.945f, 0.914f, 1f);
        return button;
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
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
    }

    private bool KeyboardEscapePressed()
    {
        return UnityEngine.InputSystem.Keyboard.current != null
            && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame;
    }

    private bool KeyboardEnterPressed()
    {
        UnityEngine.InputSystem.Keyboard keyboard = UnityEngine.InputSystem.Keyboard.current;
        return keyboard != null
            && (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame);
    }

    private void EnsureEventSystem()
    {
        if (UnityEngine.Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
