using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuController : MonoBehaviour
{
    private const string AmbienceResourcePath = "MainMenuGenerated/Audio/postal_sector7_menu_ambience_loop";
    private const string LampGlowResourcePath = "MainMenuGenerated/Effects/lamp_glow_radial";
    private const string ButtonGlowResourcePath = "MainMenuGenerated/Effects/button_hover_glow";
    private const string CurrentNightKey = "PackageInspection_CurrentNight";
    private const string UnlockedNightKey = "PackageInspection_UnlockedNight";

    [SerializeField] private string gameSceneName = "SampleScene";
    [SerializeField] private int gameSceneBuildIndex = 1;
    [SerializeField] private GameObject trainingPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private FadeTransition fadeTransition;
    [SerializeField] private bool createRuntimeFx = true;
    [SerializeField] private bool routeImageButtonClicks = true;
    [SerializeField] private float musicVolume = 0.34f;

    private static readonly Rect ContinueImageRect = new Rect(0.580f, 0.390f, 0.340f, 0.140f);
    private static readonly Rect NewShiftImageRect = new Rect(0.580f, 0.255f, 0.340f, 0.130f);
    private static readonly Rect QuitImageRect = new Rect(0.580f, 0.115f, 0.340f, 0.130f);

    private MenuSettingsPanel settingsMenu;
    private Button continueButton;
    private bool isLoadingShift;

    private void Start()
    {
        ConfigureGeneratedButtonLayout();
        SetupSettingsMenu();
        SetupContinueButton();

        if (!createRuntimeFx)
        {
            return;
        }

        CreateMenuAmbience();
        CreateLampGlow();
        CreateButtonGlows();
    }

    private void Update()
    {
        if (!routeImageButtonClicks || isLoadingShift || IsPanelOpen())
        {
            return;
        }

        if (!TryGetPointerPress(out Vector2 pointerPosition) || Screen.width <= 0 || Screen.height <= 0)
        {
            return;
        }

        Vector2 normalizedPosition = new Vector2(pointerPosition.x / Screen.width, pointerPosition.y / Screen.height);
        if (ContinueImageRect.Contains(normalizedPosition))
        {
            ContinueGame();
        }
        else if (NewShiftImageRect.Contains(normalizedPosition))
        {
            NewShift();
        }
        else if (QuitImageRect.Contains(normalizedPosition))
        {
            QuitGame();
        }
    }

    public void ContinueGame()
    {
        if (isLoadingShift || !HasSavedProgress())
        {
            return;
        }

        isLoadingShift = true;
        LoadSavedProgressForContinue();
        NightStoryManager.MarkMainMenuCompletedForSession();
        UIManager.BlockNextStorySceneUntilBriefing();
        BackgroundMusicManager.Instance?.KeepBriefingMusicForNextScene();
        LoadGameScene();
    }

    public void NewShift()
    {
        if (isLoadingShift)
        {
            return;
        }

        isLoadingShift = true;
        ResetProgressToFirstNight();
        NightStoryManager.MarkMainMenuCompletedForSession();
        UIManager.BlockNextStorySceneUntilBriefing();
        BackgroundMusicManager.Instance?.KeepBriefingMusicForNextScene();
        LoadGameScene();
    }

    private void LoadGameScene()
    {
        if (fadeTransition != null)
        {
            if (gameSceneBuildIndex >= 0 && gameSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
            {
                fadeTransition.FadeToScene(gameSceneBuildIndex);
            }
            else
            {
                fadeTransition.FadeToScene(gameSceneName);
            }

            return;
        }

        Time.timeScale = 1f;
        if (gameSceneBuildIndex >= 0 && gameSceneBuildIndex < SceneManager.sceneCountInBuildSettings)
        {
            SceneManager.LoadScene(gameSceneBuildIndex);
            return;
        }

        SceneManager.LoadScene(gameSceneName);
    }

    public void OpenTrainingTape()
    {
        SetPanel(trainingPanel, true);
    }

    public void CloseTrainingTape()
    {
        SetPanel(trainingPanel, false);
    }

    public void OpenSettings()
    {
        if (settingsMenu != null)
        {
            settingsMenu.Show();
            return;
        }

        SetPanel(settingsPanel, true);
    }

    public void CloseSettings()
    {
        if (settingsMenu != null)
        {
            settingsMenu.Hide();
            return;
        }

        SetPanel(settingsPanel, false);
    }

    public void QuitGame()
    {
#if UNITY_EDITOR
        Debug.Log("Quit Game");
#else
        Application.Quit();
#endif
    }

    private static void SetPanel(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void CreateMenuAmbience()
    {
        if (BackgroundMusicManager.Instance != null)
        {
            BackgroundMusicManager.Instance.PlayBriefingMusic(false);
            return;
        }

        Transform root = transform.root;
        if (FindChildRecursive(root, "MenuAmbience") != null || FindChildRecursive(root, "RuntimeMenuAmbience") != null)
        {
            return;
        }

        AudioClip clip = Resources.Load<AudioClip>(AmbienceResourcePath);
        if (clip == null)
        {
            Debug.LogWarning("Main menu ambience clip was not found.");
            return;
        }

        GameObject audioObject = new GameObject("RuntimeMenuAmbience");
        audioObject.transform.SetParent(root, false);

        AudioSource audioSource = audioObject.AddComponent<AudioSource>();
        audioSource.clip = clip;
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = musicVolume;
        audioSource.spatialBlend = 0f;
        audioSource.Play();
    }

    private void CreateLampGlow()
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null || canvas.GetComponentInChildren<MenuLampFlicker>(true) != null)
        {
            return;
        }

        Sprite sprite = Resources.Load<Sprite>(LampGlowResourcePath);
        GameObject glowObject = CreateRect("RuntimeLampGlow", canvas.transform, new Vector2(0.005f, 0.69f), new Vector2(0.25f, 1.08f), Vector2.zero);
        Image image = glowObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = Color.white;
        image.raycastTarget = false;

        CanvasGroup group = glowObject.AddComponent<CanvasGroup>();
        group.alpha = 0.24f;
        group.blocksRaycasts = false;
        group.interactable = false;

        glowObject.AddComponent<MenuLampFlicker>();

        Transform background = canvas.transform.Find("GeneratedMenuImage");
        if (background != null)
        {
            glowObject.transform.SetSiblingIndex(background.GetSiblingIndex() + 1);
        }
    }

    private void CreateButtonGlows()
    {
        Sprite sprite = Resources.Load<Sprite>(ButtonGlowResourcePath);
        string[] buttonNames =
        {
            "ContinueButton",
            "NewShiftButton",
            "QuitButton"
        };

        foreach (string buttonName in buttonNames)
        {
            Transform buttonTransform = FindChildRecursive(transform, buttonName);
            if (buttonTransform == null || buttonTransform.GetComponent<MenuButtonGlow>() != null)
            {
                continue;
            }

            GameObject glowObject = CreateRect("RuntimeButtonGlow", buttonTransform, Vector2.zero, Vector2.one, new Vector2(-10f, -8f));
            Image image = glowObject.AddComponent<Image>();
            image.sprite = sprite;
            image.color = Color.white;
            image.raycastTarget = false;

            CanvasGroup group = glowObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;

            buttonTransform.gameObject.AddComponent<MenuButtonGlow>();
        }
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        return gameObject;
    }

    private static Transform FindChildRecursive(Transform root, string childName)
    {
        if (root.name == childName)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildRecursive(child, childName);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private bool IsPanelOpen()
    {
        return (trainingPanel != null && trainingPanel.activeSelf) || (settingsPanel != null && settingsPanel.activeSelf);
    }

    private void ConfigureGeneratedButtonLayout()
    {
        SetButtonRect("ContinueButton", ContinueImageRect);
        SetButtonRect("NewShiftButton", NewShiftImageRect);
        SetButtonRect("QuitButton", QuitImageRect);
        SetButtonActive("TrainingTapeButton", false);
        SetButtonActive("SettingsButton", false);
        SetPanel(trainingPanel, false);
        SetPanel(settingsPanel, false);
    }

    private void SetButtonRect(string buttonName, Rect normalizedRect)
    {
        Transform buttonTransform = FindChildRecursive(transform, buttonName);
        RectTransform rectTransform = buttonTransform != null ? buttonTransform.GetComponent<RectTransform>() : null;
        if (rectTransform == null)
        {
            return;
        }

        rectTransform.anchorMin = new Vector2(normalizedRect.xMin, normalizedRect.yMin);
        rectTransform.anchorMax = new Vector2(normalizedRect.xMax, normalizedRect.yMax);
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;
        buttonTransform.gameObject.SetActive(true);
    }

    private void SetButtonActive(string buttonName, bool active)
    {
        Transform buttonTransform = FindChildRecursive(transform, buttonName);
        if (buttonTransform != null)
        {
            buttonTransform.gameObject.SetActive(active);
        }
    }

    private void SetupSettingsMenu()
    {
        if (settingsPanel == null)
        {
            return;
        }

        settingsMenu = settingsPanel.GetComponent<MenuSettingsPanel>();
        if (settingsMenu == null)
        {
            settingsMenu = settingsPanel.AddComponent<MenuSettingsPanel>();
        }

        settingsMenu.Initialize(this);
    }

    private void SetupContinueButton()
    {
        Transform buttonTransform = FindChildRecursive(transform, "ContinueButton");
        continueButton = buttonTransform != null ? buttonTransform.GetComponent<Button>() : null;
        if (continueButton != null)
        {
            continueButton.interactable = HasSavedProgress();
        }
    }

    private static bool TryGetPointerPress(out Vector2 pointerPosition)
    {
#if ENABLE_INPUT_SYSTEM
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            pointerPosition = Mouse.current.position.ReadValue();
            return true;
        }
#else
        if (Input.GetMouseButtonDown(0))
        {
            pointerPosition = Input.mousePosition;
            return true;
        }
#endif

        pointerPosition = Vector2.zero;
        return false;
    }

    private static void ResetProgressToFirstNight()
    {
        PlayerPrefs.SetInt(CurrentNightKey, 1);
        PlayerPrefs.SetInt(UnlockedNightKey, 1);
        PlayerPrefs.Save();
    }

    private static bool HasSavedProgress()
    {
        return false;
    }

    private static void LoadSavedProgressForContinue()
    {
        PlayerPrefs.SetInt(UnlockedNightKey, 1);
        PlayerPrefs.SetInt(CurrentNightKey, 1);
        PlayerPrefs.Save();
    }
}
