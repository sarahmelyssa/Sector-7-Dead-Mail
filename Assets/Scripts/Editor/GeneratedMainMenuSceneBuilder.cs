using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
#endif
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GeneratedMainMenuSceneBuilder
{
    private const string MainMenuScenePath = "Assets/Scenes/MainMenu.unity";
    private const string GameScenePath = "Assets/Scenes/SampleScene.unity";
    private const string GameSceneName = "SampleScene";
    private const string MenuImagePath = "Assets/Resources/MainMenuGenerated/postal_sector7_main_menu_generated_1920x1080.png";
    private const string LampGlowPath = "Assets/Resources/MainMenuGenerated/Effects/lamp_glow_radial.png";
    private const string ButtonGlowPath = "Assets/Resources/MainMenuGenerated/Effects/button_hover_glow.png";
    private const string MusicPath = "Assets/Resources/MainMenuGenerated/Audio/postal_sector7_menu_ambience_loop.wav";

    [InitializeOnLoadMethod]
    private static void BuildWhenReady()
    {
        SetPlayModeStartScene();

        if (Application.isBatchMode || !File.Exists(MenuImagePath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (!File.Exists(MainMenuScenePath))
            {
                BuildMainMenuScene();
            }
            else
            {
                UpgradeMainMenuScene();
            }
        };
    }

    [InitializeOnLoadMethod]
    private static void SetPlayModeStartScene()
    {
        SceneAsset sceneAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainMenuScenePath);
        if (sceneAsset != null && EditorSceneManager.playModeStartScene != sceneAsset)
        {
            EditorSceneManager.playModeStartScene = sceneAsset;
        }
    }

    [MenuItem("Tools/Sector 7/Build Generated Main Menu")]
    public static void BuildMainMenuScene()
    {
        Directory.CreateDirectory("Assets/Scenes");
        PrepareImportedAssets();

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        EditorSceneManager.SetActiveScene(scene);
        scene.name = "MainMenu";

        GameObject root = new GameObject("MainMenuScene");
        CreateMenuCamera(root.transform);
        CreateMenuAmbience(root.transform);

        GameObject canvasObject = new GameObject("Canvas_MainMenu", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasObject.transform.SetParent(root.transform, false);

        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        MainMenuController controller = canvasObject.AddComponent<MainMenuController>();
        CreateFullScreenImage("GeneratedMenuImage", canvasObject.transform, AssetDatabase.LoadAssetAtPath<Sprite>(MenuImagePath), Color.white);
        CreateLampGlow(canvasObject.transform);

        ButtonStackRefs buttons = CreateInvisibleButtons(canvasObject.transform);

        GameObject fadePanel = CreateFullScreenImage("FadePanel", canvasObject.transform, null, Color.black);
        CanvasGroup fadeGroup = fadePanel.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        FadeTransition fadeTransition = fadePanel.AddComponent<FadeTransition>();
        SetObjectReference(fadeTransition, "fadeGroup", fadeGroup);

        SetString(controller, "gameSceneName", GameSceneName);
        SetInt(controller, "gameSceneBuildIndex", 1);
        SetObjectReference(controller, "fadeTransition", fadeTransition);

        UnityEventTools.AddPersistentListener(buttons.Continue.onClick, controller.ContinueGame);
        UnityEventTools.AddPersistentListener(buttons.NewShift.onClick, controller.NewShift);
        UnityEventTools.AddPersistentListener(buttons.Quit.onClick, controller.QuitGame);

        CreateEventSystem(root.transform);

        EditorSceneManager.SaveScene(scene, MainMenuScenePath);
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(MainMenuScenePath, true),
            new EditorBuildSettingsScene(GameScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        if (previousScene.IsValid() && previousScene.isLoaded)
        {
            EditorSceneManager.SetActiveScene(previousScene);
            EditorSceneManager.CloseScene(scene, true);
        }

        Debug.Log("Generated MainMenu scene from the custom image.");
    }

    [MenuItem("Tools/Sector 7/Upgrade Generated Main Menu FX")]
    public static void UpgradeMainMenuScene()
    {
        if (!File.Exists(MainMenuScenePath))
        {
            return;
        }

        PrepareImportedAssets();

        Scene previousScene = SceneManager.GetActiveScene();
        Scene scene = FindLoadedScene(MainMenuScenePath);
        bool openedScene = !scene.IsValid() || !scene.isLoaded;
        if (openedScene)
        {
            scene = EditorSceneManager.OpenScene(MainMenuScenePath, OpenSceneMode.Additive);
        }

        EditorSceneManager.SetActiveScene(scene);

        GameObject root = FindInScene(scene, "MainMenuScene");
        Canvas canvas = FindCanvasInScene(scene, "Canvas_MainMenu");
        if (root == null || canvas == null)
        {
            RestorePreviousScene(previousScene, scene, openedScene);
            return;
        }

        bool changed = false;

        if (FindInScene(scene, "MenuAmbience") == null)
        {
            CreateMenuAmbience(root.transform);
            changed = true;
        }

        if (FindInScene(scene, "LampGlow") == null)
        {
            CreateLampGlow(canvas.transform);
            changed = true;
        }

        changed |= EnsureButtonGlow(scene, "ContinueButton");
        changed |= EnsureButtonGlow(scene, "NewShiftButton");
        changed |= EnsureButtonGlow(scene, "QuitButton");

        if (changed)
        {
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
        }

        RestorePreviousScene(previousScene, scene, openedScene);
    }

    private static void PrepareImportedAssets()
    {
        PrepareSprite(MenuImagePath);
        PrepareSprite(LampGlowPath);
        PrepareSprite(ButtonGlowPath);
        AssetDatabase.ImportAsset(MusicPath, ImportAssetOptions.ForceUpdate);
    }

    private static void PrepareSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            return;
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.SaveAndReimport();
    }

    private static ButtonStackRefs CreateInvisibleButtons(Transform parent)
    {
        GameObject group = CreateRect("ButtonHitboxes", parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);

        return new ButtonStackRefs
        {
            Continue = CreateHitbox(group.transform, "ContinueButton", new Vector2(0.580f, 0.390f), new Vector2(0.920f, 0.530f)),
            NewShift = CreateHitbox(group.transform, "NewShiftButton", new Vector2(0.580f, 0.255f), new Vector2(0.920f, 0.385f)),
            Quit = CreateHitbox(group.transform, "QuitButton", new Vector2(0.580f, 0.115f), new Vector2(0.920f, 0.245f))
        };
    }

    private static Button CreateHitbox(Transform parent, string name, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = CreateRect(name, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.None;
        AddButtonGlow(buttonObject);
        return button;
    }

    private static void AddButtonGlow(GameObject buttonObject)
    {
        if (buttonObject.GetComponent<MenuButtonGlow>() != null)
        {
            return;
        }

        GameObject glowObject = CreateRect("ButtonGlow", buttonObject.transform, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-10f, -8f));
        Image glowImage = glowObject.AddComponent<Image>();
        glowImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ButtonGlowPath);
        glowImage.color = Color.white;
        glowImage.raycastTarget = false;

        CanvasGroup glowGroup = glowObject.AddComponent<CanvasGroup>();
        glowGroup.alpha = 0f;
        glowGroup.blocksRaycasts = false;
        glowGroup.interactable = false;

        MenuButtonGlow glow = buttonObject.AddComponent<MenuButtonGlow>();
        SetObjectReference(glow, "glowGroup", glowGroup);
        SetObjectReference(glow, "targetRect", buttonObject.GetComponent<RectTransform>());
    }

    private static GameObject CreateTrainingPanel(Transform parent, MainMenuController controller)
    {
        GameObject panel = CreatePanel("TrainingPanel", parent, new Vector2(760f, 560f));
        CreateText(panel.transform, "TrainingTitle", "TRAINING TAPE - EMPLOYEE FILE", 32f, FontStyles.Bold, new Vector2(0.08f, 0.78f), new Vector2(0.92f, 0.92f));
        CreateText(panel.transform, "TrainingBody", "Welcome to the night sorting room.\n\nYour application has been accepted.\nYour dependents have been registered.\n\nInspect each package.\nRead the manifest.\nReject anything that does not match.\n\nDo not leave the desk.\nDo not answer sounds from behind you.\nDo not open dead letters.", 23f, FontStyles.Normal, new Vector2(0.08f, 0.22f), new Vector2(0.92f, 0.76f));
        Button close = CreatePanelButton(panel.transform, "CloseTrainingButton", "CLOSE", new Vector2(0.62f, 0.06f), new Vector2(0.92f, 0.16f));
        UnityEventTools.AddPersistentListener(close.onClick, controller.CloseTrainingTape);
        return panel;
    }

    private static GameObject CreateSettingsPanel(Transform parent, MainMenuController controller)
    {
        GameObject panel = CreatePanel("SettingsPanel", parent, new Vector2(600f, 360f));
        CreateText(panel.transform, "SettingsTitle", "SETTINGS", 34f, FontStyles.Bold, new Vector2(0.1f, 0.72f), new Vector2(0.9f, 0.9f));
        CreateText(panel.transform, "SettingsBody", "Volume // placeholder\nSensitivity // placeholder", 23f, FontStyles.Normal, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.66f));
        Button close = CreatePanelButton(panel.transform, "CloseSettingsButton", "CLOSE", new Vector2(0.56f, 0.08f), new Vector2(0.9f, 0.22f));
        UnityEventTools.AddPersistentListener(close.onClick, controller.CloseSettings);
        return panel;
    }

    private static GameObject CreatePanel(string name, Transform parent, Vector2 size)
    {
        GameObject panel = CreateRect(name, parent, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.02f, 0.015f, 0.035f, 0.96f);
        return panel;
    }

    private static Button CreatePanelButton(Transform parent, string name, string label, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject buttonObject = CreateRect(name, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.42f, 0.2f, 0.58f, 0.92f);
        Button button = buttonObject.AddComponent<Button>();
        button.transition = Selectable.Transition.ColorTint;
        CreateText(buttonObject.transform, "Label", label, 21f, FontStyles.Bold, Vector2.zero, Vector2.one).alignment = TextAlignmentOptions.Center;
        return button;
    }

    private static TextMeshProUGUI CreateText(Transform parent, string name, string text, float fontSize, FontStyles style, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject textObject = CreateRect(name, parent, anchorMin, anchorMax, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI tmp = textObject.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = new Color(0.9f, 0.86f, 1f, 1f);
        tmp.alignment = TextAlignmentOptions.Left;
        tmp.textWrappingMode = TextWrappingModes.Normal;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static GameObject CreateFullScreenImage(string name, Transform parent, Sprite sprite, Color color)
    {
        GameObject imageObject = CreateRect(name, parent, Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = imageObject.AddComponent<Image>();
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        image.preserveAspect = false;
        return imageObject;
    }

    private static void CreateLampGlow(Transform parent)
    {
        GameObject glowObject = CreateRect("LampGlow", parent, new Vector2(0.005f, 0.69f), new Vector2(0.25f, 1.08f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        Image image = glowObject.AddComponent<Image>();
        image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LampGlowPath);
        image.color = Color.white;
        image.raycastTarget = false;

        CanvasGroup group = glowObject.AddComponent<CanvasGroup>();
        group.alpha = 0.24f;
        group.blocksRaycasts = false;
        group.interactable = false;

        MenuLampFlicker flicker = glowObject.AddComponent<MenuLampFlicker>();
        SetObjectReference(flicker, "canvasGroup", group);
        MoveAfterSibling(glowObject.transform, parent, "GeneratedMenuImage");
    }

    private static GameObject CreateRect(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 pivot, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);
        RectTransform rect = gameObject.GetComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = sizeDelta;
        rect.localScale = Vector3.one;
        return gameObject;
    }

    private static void CreateMenuCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("MainMenuCamera", typeof(Camera), typeof(AudioListener));
        cameraObject.tag = "MainCamera";
        cameraObject.transform.SetParent(parent, false);
        cameraObject.transform.position = new Vector3(0f, 0f, -10f);

        Camera camera = cameraObject.GetComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.black;
        camera.orthographic = true;
        camera.orthographicSize = 5f;
    }

    private static void CreateMenuAmbience(Transform parent)
    {
        GameObject audioObject = new GameObject("MenuAmbience", typeof(AudioSource));
        audioObject.transform.SetParent(parent, false);

        AudioSource audioSource = audioObject.GetComponent<AudioSource>();
        audioSource.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(MusicPath);
        audioSource.loop = true;
        audioSource.playOnAwake = true;
        audioSource.volume = 0.34f;
        audioSource.spatialBlend = 0f;
    }

    private static void CreateEventSystem(Transform parent)
    {
        GameObject eventSystem = new GameObject("EventSystem", typeof(EventSystem));
        eventSystem.transform.SetParent(parent, false);
#if ENABLE_INPUT_SYSTEM
        InputSystemUIInputModule inputModule = eventSystem.AddComponent<InputSystemUIInputModule>();
        InputActionAsset actions = AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions");
        if (actions != null)
        {
            inputModule.actionsAsset = actions;
        }
        else
        {
            inputModule.AssignDefaultActions();
        }
#else
        eventSystem.AddComponent<StandaloneInputModule>();
#endif
    }

    private static void SetString(Object target, string propertyName, string value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).stringValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Object target, string propertyName, int value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).intValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjectReference(Object target, string propertyName, Object value)
    {
        SerializedObject serializedObject = new SerializedObject(target);
        serializedObject.FindProperty(propertyName).objectReferenceValue = value;
        serializedObject.ApplyModifiedPropertiesWithoutUndo();
    }

    private static bool EnsureButtonGlow(Scene scene, string buttonName)
    {
        GameObject buttonObject = FindInScene(scene, buttonName);
        if (buttonObject == null)
        {
            return false;
        }

        if (buttonObject.GetComponent<MenuButtonGlow>() != null)
        {
            return false;
        }

        AddButtonGlow(buttonObject);
        return true;
    }

    private static Scene FindLoadedScene(string path)
    {
        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (scene.path == path)
            {
                return scene;
            }
        }

        return default;
    }

    private static Canvas FindCanvasInScene(Scene scene, string name)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Canvas[] canvases = rootObject.GetComponentsInChildren<Canvas>(true);
            foreach (Canvas canvas in canvases)
            {
                if (canvas.name == name)
                {
                    return canvas;
                }
            }
        }

        return null;
    }

    private static GameObject FindInScene(Scene scene, string name)
    {
        foreach (GameObject rootObject in scene.GetRootGameObjects())
        {
            Transform found = FindChildRecursive(rootObject.transform, name);
            if (found != null)
            {
                return found.gameObject;
            }
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string name)
    {
        if (root.name == name)
        {
            return root;
        }

        foreach (Transform child in root)
        {
            Transform found = FindChildRecursive(child, name);
            if (found != null)
            {
                return found;
            }
        }

        return null;
    }

    private static void RestorePreviousScene(Scene previousScene, Scene editedScene, bool closeEditedScene)
    {
        if (previousScene.IsValid() && previousScene.isLoaded)
        {
            EditorSceneManager.SetActiveScene(previousScene);
        }

        if (closeEditedScene && editedScene.IsValid() && editedScene.isLoaded)
        {
            EditorSceneManager.CloseScene(editedScene, true);
        }
    }

    private static void MoveAfterSibling(Transform item, Transform parent, string siblingName)
    {
        Transform sibling = parent.Find(siblingName);
        if (sibling != null)
        {
            item.SetSiblingIndex(sibling.GetSiblingIndex() + 1);
        }
    }

    private struct ButtonStackRefs
    {
        public Button Continue;
        public Button NewShift;
        public Button Quit;
    }
}
