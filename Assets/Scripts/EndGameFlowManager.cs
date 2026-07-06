using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class EndGameFlowManager : MonoBehaviour
{
    public static EndGameFlowManager Instance { get; private set; }
    private const string ButtonGlowResourcePath = "MainMenuGenerated/Effects/button_hover_glow";

    [Header("Assets")]
    [SerializeField] private VideoClip finalVideoClip = null;
    [SerializeField] private string winBackgroundResourcePath = "EndGame/win_screen";
    [SerializeField] private string gameOverBackgroundResourcePath = "EndGame/game_over_screen";

    [Header("Video")]
    [SerializeField] private bool playVideoBeforeWinScreen = false;

    [Header("Transitions")]
    [SerializeField] private float gameplayFadeOutDuration = 0.9f;
    [SerializeField] private float videoToScreenFadeDuration = 0.75f;
    [SerializeField] private float finalScreenFadeInDuration = 0.75f;
    [SerializeField] private float videoEndFadeOutDuration = 0.65f;
    [SerializeField] private float videoEndCutBeforeFinish = 0.85f;
    [SerializeField] private float blackHoldAfterVideoDuration = 0.18f;

    [Header("Framing")]
    [SerializeField] private float imageMaxScreenWidth = 0.25f;
    [SerializeField] private float imageMaxScreenHeight = 0.31f;

    private Canvas endCanvas;
    private GameObject videoCanvasRoot;
    private GameObject winCanvasRoot;
    private GameObject gameOverCanvasRoot;
    private GameObject fadeCanvasRoot;
    private CanvasGroup fadeCanvasGroup;
    private VideoPlayer videoPlayer;
    private RawImage videoImage;
    private RenderTexture videoTexture;
    private Coroutine transitionRoutine;
    private bool winScreenPending;
    private bool videoSequenceControlledByCoroutine;
    private bool videoPlaybackFailed;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        LoadDefaultVideoIfNeeded();
        BuildUiIfNeeded();
        HideAll();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnFinalVideoFinished;
            videoPlayer.errorReceived -= OnFinalVideoError;
        }

        if (videoTexture != null)
        {
            videoTexture.Release();
        }
    }

    public void PlayWinSequence()
    {
        PrepareEndScreenState();
        StartTransition(PlayWinSequenceRoutine(false));
    }

    public void PlayWinSequenceWithVideo()
    {
        PrepareEndScreenState();
        StartTransition(PlayWinSequenceRoutine(true));
    }

    public void ShowWinScreen()
    {
        PrepareEndScreenState();
        StartTransition(ShowWinScreenRoutine());
    }

    public void ShowGameOverScreen()
    {
        PrepareEndScreenState();
        StartTransition(ShowGameOverScreenRoutine(0f));
    }

    public void ShowGameOverAfterDelay(float delay)
    {
        StartCoroutine(ShowGameOverAfterDelayRoutine(Mathf.Max(0f, delay)));
    }

    private IEnumerator ShowGameOverAfterDelayRoutine(float delay)
    {
        PrepareEndScreenState();
        yield return new WaitForSecondsRealtime(delay);
        ShowGameOverScreen();
    }

    private IEnumerator PlayWinSequenceRoutine(bool forceVideo)
    {
        yield return FadeTo(1f, gameplayFadeOutDuration);

        if ((forceVideo || playVideoBeforeWinScreen) && finalVideoClip != null && videoPlayer != null)
        {
            yield return PlayFinalVideoThenShowWinRoutine();
            yield break;
        }

        yield return ShowWinImageFromBlackRoutine();
    }

    private IEnumerator ShowWinScreenRoutine()
    {
        yield return FadeTo(1f, videoToScreenFadeDuration);
        yield return ShowWinImageFromBlackRoutine();
    }

    private IEnumerator ShowGameOverScreenRoutine(float delay)
    {
        if (delay > 0f)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        yield return FadeTo(1f, gameplayFadeOutDuration);
        HideContent();
        BackgroundMusicManager.Instance?.PlayBriefingMusic(false);
        gameOverCanvasRoot?.SetActive(true);
        yield return null;
        yield return FadeTo(0f, finalScreenFadeInDuration);
    }

    private void PlayFinalVideo()
    {
        HideContent();
        winScreenPending = true;

        if (videoCanvasRoot != null)
        {
            videoCanvasRoot.SetActive(true);
        }

        videoPlayer.clip = finalVideoClip;
        videoPlayer.isLooping = false;
        videoPlayer.time = 0;
        videoPlayer.Play();
    }

    private IEnumerator PlayFinalVideoThenShowWinRoutine()
    {
        HideContent();
        videoSequenceControlledByCoroutine = true;
        videoPlaybackFailed = false;
        winScreenPending = false;

        if (videoCanvasRoot != null)
        {
            videoCanvasRoot.SetActive(true);
        }

        ClearVideoTexture();

        videoPlayer.Stop();
        videoPlayer.clip = finalVideoClip;
        videoPlayer.isLooping = false;
        videoPlayer.time = 0;
        videoPlayer.Prepare();

        float timeout = 2f;
        while (!videoPlayer.isPrepared && !videoPlaybackFailed && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        if (!videoPlayer.isPrepared || videoPlaybackFailed)
        {
            videoSequenceControlledByCoroutine = false;
            yield return ShowWinImageFromBlackRoutine();
            yield break;
        }

        double videoDuration = GetVideoDuration();
        float endFadeDuration = Mathf.Clamp(videoEndFadeOutDuration, 0.1f, 2f);
        double blackOutTime = Mathf.Max(0.1f, (float)(videoDuration - Mathf.Max(0f, videoEndCutBeforeFinish)));
        double fadeStartTime = Mathf.Max(0.05f, (float)(blackOutTime - endFadeDuration));

        videoPlayer.Play();
        yield return null;
        yield return FadeTo(0f, finalScreenFadeInDuration);

        float playbackElapsed = 0f;
        while (!videoPlaybackFailed && videoPlayer != null && videoPlayer.isPlaying)
        {
            playbackElapsed += Time.unscaledDeltaTime;
            double currentVideoTime = videoPlayer.time > 0.01 ? videoPlayer.time : playbackElapsed;
            if (currentVideoTime >= fadeStartTime)
            {
                break;
            }

            yield return null;
        }

        yield return FadeTo(1f, endFadeDuration);

        if (videoPlayer != null)
        {
            videoPlayer.Stop();
        }

        if (videoCanvasRoot != null)
        {
            videoCanvasRoot.SetActive(false);
        }

        videoSequenceControlledByCoroutine = false;

        float hold = Mathf.Max(0f, blackHoldAfterVideoDuration);
        if (hold > 0f)
        {
            yield return new WaitForSecondsRealtime(hold);
        }

        yield return ShowWinImageFromBlackRoutine();
    }

    private IEnumerator PlayFinalVideoPreparedRoutine()
    {
        HideContent();
        winScreenPending = true;

        if (videoCanvasRoot != null)
        {
            videoCanvasRoot.SetActive(true);
        }

        videoPlayer.Stop();
        videoPlayer.clip = finalVideoClip;
        videoPlayer.isLooping = false;
        videoPlayer.time = 0;
        videoPlayer.Prepare();

        float timeout = 2f;
        while (!videoPlayer.isPrepared && timeout > 0f)
        {
            timeout -= Time.unscaledDeltaTime;
            yield return null;
        }

        videoPlayer.Play();
    }

    private void OnFinalVideoFinished(VideoPlayer source)
    {
        if (videoSequenceControlledByCoroutine)
        {
            return;
        }

        if (!winScreenPending)
        {
            return;
        }

        winScreenPending = false;
        PrepareEndScreenState();
        StartTransition(ShowWinScreenRoutine());
    }

    private void OnFinalVideoError(VideoPlayer source, string message)
    {
        Debug.LogWarning("Final video could not play: " + message);
        videoPlaybackFailed = true;
        if (videoSequenceControlledByCoroutine)
        {
            return;
        }

        winScreenPending = false;
        PrepareEndScreenState();
        StartTransition(ShowWinScreenRoutine());
    }

    private void PrepareEndScreenState()
    {
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        EnsureEventSystem();
        BuildUiIfNeeded();
    }

    private void HideAll()
    {
        HideContent();
        SetFadeAlpha(0f, false);
    }

    private void HideContent()
    {
        if (videoPlayer != null && videoPlayer.isPlaying)
        {
            videoPlayer.Stop();
        }

        winScreenPending = false;
        videoCanvasRoot?.SetActive(false);
        winCanvasRoot?.SetActive(false);
        gameOverCanvasRoot?.SetActive(false);
    }

    private IEnumerator ShowWinImageFromBlackRoutine()
    {
        HideContent();
        BackgroundMusicManager.Instance?.PlayBriefingMusic(false);
        winCanvasRoot?.SetActive(true);
        yield return null;
        yield return FadeTo(0f, finalScreenFadeInDuration);
    }

    private void BuildUiIfNeeded()
    {
        if (endCanvas == null)
        {
            GameObject canvasObject = new GameObject("EndGameCanvas");
            canvasObject.transform.SetParent(transform, false);
            endCanvas = canvasObject.AddComponent<Canvas>();
            endCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            endCanvas.sortingOrder = 240;

            CanvasScaler scaler = canvasObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            canvasObject.AddComponent<GraphicRaycaster>();
        }

        if (videoCanvasRoot == null)
        {
            videoCanvasRoot = BuildVideoCanvas();
        }

        if (winCanvasRoot == null)
        {
            winCanvasRoot = BuildImageScreen("WinCanvas", winBackgroundResourcePath, true);
        }

        if (gameOverCanvasRoot == null)
        {
            gameOverCanvasRoot = BuildImageScreen("GameOverCanvas", gameOverBackgroundResourcePath, false);
        }

        if (fadeCanvasRoot == null)
        {
            fadeCanvasRoot = BuildFadeOverlay();
        }

        fadeCanvasRoot.transform.SetAsLastSibling();
    }

    private GameObject BuildVideoCanvas()
    {
        GameObject root = CreateFullScreenRoot("EndVideoCanvas");

        Image black = root.AddComponent<Image>();
        black.color = Color.black;
        black.raycastTarget = true;

        GameObject videoObject = new GameObject("EndVideoPlayer", typeof(RectTransform));
        videoObject.transform.SetParent(root.transform, false);
        RectTransform videoRect = videoObject.GetComponent<RectTransform>();
        videoRect.anchorMin = new Vector2(0.5f, 0.5f);
        videoRect.anchorMax = new Vector2(0.5f, 0.5f);
        videoRect.pivot = new Vector2(0.5f, 0.5f);
        videoRect.anchoredPosition = Vector2.zero;
        videoRect.sizeDelta = new Vector2(1920f, 1080f);

        videoImage = videoObject.AddComponent<RawImage>();
        videoImage.color = Color.white;
        videoImage.raycastTarget = false;

        videoTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32);
        videoTexture.name = "Runtime_FinalVideoTexture";
        videoImage.texture = videoTexture;

        videoPlayer = videoObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.targetTexture = videoTexture;
        videoPlayer.aspectRatio = VideoAspectRatio.FitInside;
        videoPlayer.timeUpdateMode = VideoTimeUpdateMode.UnscaledGameTime;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.AudioSource;
        videoPlayer.waitForFirstFrame = true;
        videoPlayer.skipOnDrop = true;
        videoPlayer.loopPointReached += OnFinalVideoFinished;
        videoPlayer.errorReceived += OnFinalVideoError;

        AudioSource videoAudio = videoObject.AddComponent<AudioSource>();
        videoAudio.playOnAwake = false;
        videoAudio.spatialBlend = 0f;
        videoPlayer.SetTargetAudioSource(0, videoAudio);

        root.SetActive(false);
        return root;
    }

    private void ClearVideoTexture()
    {
        if (videoTexture == null)
        {
            return;
        }

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = videoTexture;
        GL.Clear(true, true, Color.black);
        RenderTexture.active = previous;
    }

    private double GetVideoDuration()
    {
        if (finalVideoClip != null && finalVideoClip.length > 0.01)
        {
            return finalVideoClip.length;
        }

        return videoPlayer != null && videoPlayer.length > 0.01 ? videoPlayer.length : 1.5;
    }

    private GameObject BuildFadeOverlay()
    {
        GameObject root = CreateFullScreenRoot("EndFadeOverlay");

        Image image = root.AddComponent<Image>();
        image.color = Color.black;
        image.raycastTarget = true;

        fadeCanvasGroup = root.AddComponent<CanvasGroup>();
        fadeCanvasGroup.alpha = 0f;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = false;

        root.SetActive(false);
        return root;
    }

    private GameObject BuildImageScreen(string name, string resourcePath, bool victory)
    {
        GameObject root = CreateFullScreenRoot(name);

        Image background = root.AddComponent<Image>();
        background.color = Color.black;
        background.raycastTarget = true;

        Sprite artSprite = LoadSprite(resourcePath);
        Transform artRoot = CreateCenteredArt(root.transform, victory ? "Win Art" : "GameOver Art", artSprite);

        if (victory)
        {
            CreateNormalizedHotspot(artRoot, "Win Main Menu Hotspot", new Rect(0.155f, 0.550f, 0.690f, 0.170f), GoToMainMenu);
            CreateNormalizedHotspot(artRoot, "Win Quit Hotspot", new Rect(0.155f, 0.748f, 0.690f, 0.176f), QuitGame);
        }
        else
        {
            CreateNormalizedHotspot(artRoot, "GameOver Restart Hotspot", new Rect(0.185f, 0.480f, 0.630f, 0.150f), RestartShift);
            CreateNormalizedHotspot(artRoot, "GameOver Main Menu Hotspot", new Rect(0.185f, 0.656f, 0.630f, 0.150f), GoToMainMenu);
            CreateNormalizedHotspot(artRoot, "GameOver Quit Hotspot", new Rect(0.185f, 0.828f, 0.630f, 0.145f), QuitGame);
        }

        root.SetActive(false);
        return root;
    }

    private Transform CreateCenteredArt(Transform parent, string name, Sprite sprite)
    {
        GameObject artObject = new GameObject(name, typeof(RectTransform));
        artObject.transform.SetParent(parent, false);

        RectTransform rect = artObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = GetFittedArtSize(sprite);

        Image image = artObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = sprite != null ? Color.white : new Color(1f, 1f, 1f, 0f);
        image.preserveAspect = true;
        image.raycastTarget = false;
        return artObject.transform;
    }

    private Vector2 GetFittedArtSize(Sprite sprite)
    {
        Vector2 sourceSize = sprite != null ? sprite.rect.size : new Vector2(1920f, 1080f);
        float aspect = sourceSize.x / Mathf.Max(1f, sourceSize.y);
        float maxWidth = 1920f * Mathf.Clamp(imageMaxScreenWidth, 0.1f, 1f);
        float maxHeight = 1080f * Mathf.Clamp(imageMaxScreenHeight, 0.1f, 1f);

        float width = maxWidth;
        float height = width / aspect;
        if (height > maxHeight)
        {
            height = maxHeight;
            width = height * aspect;
        }

        return new Vector2(width, height);
    }

    private void StartTransition(IEnumerator routine)
    {
        if (transitionRoutine != null)
        {
            StopCoroutine(transitionRoutine);
        }

        transitionRoutine = StartCoroutine(RunTransition(routine));
    }

    private IEnumerator RunTransition(IEnumerator routine)
    {
        yield return routine;
        transitionRoutine = null;
    }

    private IEnumerator FadeTo(float targetAlpha, float duration)
    {
        BuildUiIfNeeded();

        float startAlpha = fadeCanvasGroup != null ? fadeCanvasGroup.alpha : 0f;
        float elapsed = 0f;
        bool blocking = targetAlpha > 0.001f || duration > 0.001f;

        SetFadeAlpha(startAlpha, blocking);

        if (duration <= 0.001f)
        {
            SetFadeAlpha(targetAlpha, targetAlpha > 0.001f);
            yield break;
        }

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetFadeAlpha(Mathf.Lerp(startAlpha, targetAlpha, t), true);
            yield return null;
        }

        SetFadeAlpha(targetAlpha, targetAlpha > 0.001f);
    }

    private void SetFadeAlpha(float alpha, bool blocksRaycasts)
    {
        if (fadeCanvasRoot == null || fadeCanvasGroup == null)
        {
            return;
        }

        alpha = Mathf.Clamp01(alpha);
        fadeCanvasRoot.SetActive(alpha > 0.001f || blocksRaycasts);
        fadeCanvasRoot.transform.SetAsLastSibling();
        fadeCanvasGroup.alpha = alpha;
        fadeCanvasGroup.interactable = false;
        fadeCanvasGroup.blocksRaycasts = blocksRaycasts;
    }

    private GameObject CreateFullScreenRoot(string name)
    {
        GameObject root = new GameObject(name);
        root.transform.SetParent(endCanvas.transform, false);
        StretchToParent(root.AddComponent<RectTransform>());
        return root;
    }

    private Button CreateNormalizedHotspot(Transform parent, string name, Rect normalizedImageRect, UnityEngine.Events.UnityAction action)
    {
        GameObject buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(normalizedImageRect.xMin, 1f - normalizedImageRect.yMax);
        rect.anchorMax = new Vector2(normalizedImageRect.xMax, 1f - normalizedImageRect.yMin);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0.001f);
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        button.onClick.AddListener(action);

        CreateButtonGlow(buttonObject.transform);
        buttonObject.AddComponent<MenuButtonGlow>();
        return button;
    }

    private void CreateButtonGlow(Transform parent)
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

    private void RestartShift()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        NightManager.Instance?.ResetProgressToFirstNight();
        NightStoryManager.PrepareImmediateGameplayRestart();

        Scene activeScene = SceneManager.GetActiveScene();
        if (activeScene.buildIndex >= 0)
        {
            SceneManager.LoadScene(activeScene.buildIndex);
            return;
        }

        SceneManager.LoadScene(activeScene.name);
    }

    private void GoToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (SceneExistsInBuildSettings("MainMenu"))
        {
            SceneManager.LoadScene("MainMenu");
            return;
        }

        UIManager.Instance?.GoToMainMenu();
    }

    private void QuitGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

#if UNITY_EDITOR
        EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
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
            Debug.LogWarning("End screen image was not found at Resources/" + resourcePath + ".");
            return null;
        }

        return Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
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

    private void LoadDefaultVideoIfNeeded()
    {
        if (finalVideoClip != null)
        {
            return;
        }

        finalVideoClip = Resources.Load<VideoClip>("EndGame/final_video");
        if (finalVideoClip != null)
        {
            return;
        }

#if UNITY_EDITOR
        string[] guids = AssetDatabase.FindAssets("t:VideoClip");
        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            string lower = path.ToLowerInvariant();
            if (!lower.Contains("final") && !lower.Contains("ending") && !lower.Contains("win") && !lower.Contains("victory"))
            {
                continue;
            }

            finalVideoClip = AssetDatabase.LoadAssetAtPath<VideoClip>(path);
            if (finalVideoClip != null)
            {
                return;
            }
        }
#endif
    }
}
