using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ponto de entrada da cena jogavel. A sala ja esta montada na Unity, e este
/// script apenas garante que os managers e referencias essenciais existem.
/// </summary>
public static class CheckpointBootstrap
{
    public const string RuntimeRootName = "Sector7_RuntimeRoot";
    public const string RoomRootName = "Runtime_Room";
    public const string InspectionDeskRootName = "Runtime_InspectionDesk";
    public const string PhysicalButtonsRootName = "Runtime_PhysicalButtons";
    public const string ReportAreaRootName = "Runtime_ReportArea";
    public const string UiRootName = "Runtime_UI";
    public const string PackageSystemRootName = "Runtime_PackageSystem";
    public const string MobSystemRootName = "Runtime_MobSystem";

    private static bool subscribedToSceneLoaded;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void SubscribeToSceneLoaded()
    {
        if (subscribedToSceneLoaded)
        {
            return;
        }

        SceneManager.sceneLoaded += OnSceneLoaded;
        subscribedToSceneLoaded = true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Boot()
    {
        BootScene(SceneManager.GetActiveScene());
    }

    private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        BootScene(scene);
    }

    private static void BootScene(Scene scene)
    {
        string sceneName = scene.name.ToLowerInvariant();
        if (sceneName.Contains("menu"))
        {
            return;
        }

        GameObject root = GetOrCreateRoot(RuntimeRootName, null);
        GameObject roomRoot = GetOrCreateRoot(RoomRootName, root.transform);
        GetOrCreateRoot(InspectionDeskRootName, root.transform);
        GetOrCreateRoot(PhysicalButtonsRootName, root.transform);
        GetOrCreateRoot(ReportAreaRootName, root.transform);
        GameObject uiRoot = GetOrCreateRoot(UiRootName, root.transform);
        GameObject packageRoot = GetOrCreateRoot(PackageSystemRootName, root.transform);
        DestroyRuntimeRootIfExists("Runtime_Corridor_BackView");
        DestroyRuntimeRootIfExists("Runtime_Anomalies");
        DestroyRuntimeRootIfExists("Runtime_MobSystem");

        // Managers globais da partida: regras, audio, historia, UI e anomalias.
        AddIfMissing<AudioManager>(root);
        AddIfMissing<GameManager>(root);
        AddIfMissing<NightStoryManager>(root);
        AddIfMissing<NightManager>(root);
        AddIfMissing<HorrorEffectsManager>(root);
        AddIfMissing<AnomalyManager>(root);

        AddIfMissing<GameOverUI>(uiRoot);
        AddIfMissing<UIManager>(uiRoot);
        AddIfMissing<EndGameFlowManager>(uiRoot);
        AddIfMissing<ShiftTimer>(uiRoot);
        AddIfMissing<ReportPanel>(uiRoot);

        AddIfMissing<DecisionManager>(packageRoot);
        AddIfMissing<InspectionStation>(packageRoot);
        AddIfMissing<PackageConveyor>(packageRoot);
        AddIfMissing<PackageRotator>(packageRoot);
        AddIfMissing<PackageManager>(packageRoot);

        AddIfMissing<ViewSwitcher>(packageRoot);
        ConfigureSceneCamera();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        AddIfMissing<RuntimeDebugHotkeys>(root);
#endif
    }

    private static bool AddIfMissing<T>(GameObject root) where T : Component
    {
        // Evita duplicar managers quando a cena ja tem algum configurado.
        if (Object.FindFirstObjectByType<T>() == null)
        {
            root.AddComponent<T>();
            return true;
        }

        return false;
    }

    private static void ConfigureSceneCamera()
    {
        Camera playerCamera = Camera.main;
        if (playerCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            playerCamera = cameraObject.AddComponent<Camera>();
        }

        if (playerCamera.GetComponent<AudioListener>() == null)
        {
            playerCamera.gameObject.AddComponent<AudioListener>();
        }

        // A camera fica sempre pronta para gameplay e para o sistema de som.
        playerCamera.fieldOfView = 56f;
        playerCamera.backgroundColor = new Color(0.01f, 0.011f, 0.014f);

        BoothPlayerController playerController = Object.FindFirstObjectByType<BoothPlayerController>();
        playerController?.Configure(playerCamera);
    }

    private static void DestroyRuntimeRootIfExists(string objectName)
    {
        GameObject root = GameObject.Find(objectName);
        if (root == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Object.Destroy(root);
        }
        else
        {
            Object.DestroyImmediate(root);
        }
    }

    public static GameObject GetOrCreateRoot(string objectName, Transform parent)
    {
        GameObject root = GameObject.Find(objectName);
        if (root == null)
        {
            root = new GameObject(objectName);
        }

        if (parent != null && root.transform.parent != parent)
        {
            root.transform.SetParent(parent, false);
        }

        return root;
    }
}
