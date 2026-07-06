using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class PrefabSetupTool
{
    private const string PrefabsPath = "Assets/Prefabs";
    private const string MaterialsPath = "Assets/Materials";
    private const string AutoCreateKey = "PackageInspection_BasePrefabsCreated";

    [InitializeOnLoadMethod]
    private static void CreateBasePrefabsIfMissing()
    {
        if (SessionState.GetBool(AutoCreateKey, false))
        {
            return;
        }

        SessionState.SetBool(AutoCreateKey, true);

        if (!File.Exists(PrefabsPath + "/PackagePrefab.prefab")
            || !File.Exists(PrefabsPath + "/MobPrefab.prefab")
            || !File.Exists(PrefabsPath + "/AnomalyObject.prefab")
            || !File.Exists(PrefabsPath + "/InspectionCanvas.prefab")
            || !File.Exists(PrefabsPath + "/GameOverPanel.prefab")
            || !File.Exists(PrefabsPath + "/VictoryPanel.prefab"))
        {
            CreateBasePrefabs();
        }
    }

    [MenuItem("Tools/Package Inspection/Create Base Prefabs")]
    public static void CreateBasePrefabs()
    {
        EnsureFolders();
        CreatePackagePrefab();
        CreateMobPrefab();
        CreateAnomalyPrefab();
        CreateInspectionCanvasPrefab();
        CreateEndPanelPrefab("GameOverPanel", "GAME OVER", new Color(0.42f, 0.02f, 0.02f, 0.88f));
        CreateEndPanelPrefab("VictoryPanel", "VICTORY", new Color(0.02f, 0.22f, 0.08f, 0.88f));

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Base prefabs created in Assets/Prefabs.");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets", "Prefabs");
        EnsureFolder("Assets", "UI");
        EnsureFolder("Assets", "Materials");
        EnsureFolder("Assets", "Audio");
        EnsureFolder("Assets", "Models");
    }

    private static void CreatePackagePrefab()
    {
        GameObject package = GameObject.CreatePrimitive(PrimitiveType.Cube);
        package.name = "PackagePrefab";
        package.transform.localScale = new Vector3(0.85f, 0.62f, 0.72f);
        package.GetComponent<Renderer>().sharedMaterial = GetMaterial("MAT_PackageCardboard", new Color(0.58f, 0.39f, 0.22f));

        var body = package.AddComponent<Rigidbody>();
        body.isKinematic = true;

        package.AddComponent<InspectablePackage>();
        package.AddComponent<PackageInteractable>();

        SavePrefab(package, "PackagePrefab");
    }

    private static void CreateMobPrefab()
    {
        GameObject mob = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        mob.name = "MobPrefab";
        mob.transform.localScale = new Vector3(0.45f, 1.25f, 0.45f);
        mob.GetComponent<Renderer>().sharedMaterial = GetMaterial("MAT_MobShadow", new Color(0.01f, 0.01f, 0.013f));

        GameObject eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        eye.name = "MobEye";
        eye.transform.SetParent(mob.transform, false);
        eye.transform.localPosition = new Vector3(0f, 0.42f, -0.47f);
        eye.transform.localScale = Vector3.one * 0.22f;
        eye.GetComponent<Renderer>().sharedMaterial = GetMaterial("MAT_MobEye", new Color(1f, 0.03f, 0.02f));
        Object.DestroyImmediate(eye.GetComponent<Collider>());

        SavePrefab(mob, "MobPrefab");
    }

    private static void CreateAnomalyPrefab()
    {
        GameObject anomaly = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        anomaly.name = "AnomalyObject";
        anomaly.transform.localScale = Vector3.one * 0.45f;
        anomaly.GetComponent<Renderer>().sharedMaterial = GetMaterial("MAT_AnomalyRed", new Color(0.9f, 0.02f, 0.03f));
        anomaly.SetActive(false);

        SavePrefab(anomaly, "AnomalyObject");
    }

    private static void CreateInspectionCanvasPrefab()
    {
        GameObject canvasObject = new GameObject("InspectionCanvas");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        canvasObject.AddComponent<GraphicRaycaster>();
        canvasObject.AddComponent<InspectionUI>();

        SavePrefab(canvasObject, "InspectionCanvas");
    }

    private static void CreateEndPanelPrefab(string prefabName, string label, Color panelColor)
    {
        GameObject panel = new GameObject(prefabName);
        var rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        var image = panel.AddComponent<Image>();
        image.color = panelColor;

        GameObject textObject = new GameObject("Title");
        textObject.transform.SetParent(panel.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(720f, 180f);

        var title = textObject.AddComponent<TextMeshProUGUI>();
        title.text = label;
        title.fontSize = 58f;
        title.alignment = TextAlignmentOptions.Center;
        title.color = new Color(0.92f, 0.88f, 0.72f);

        SavePrefab(panel, prefabName);
    }

    private static Material GetMaterial(string materialName, Color color)
    {
        string materialPath = MaterialsPath + "/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
        if (material != null)
        {
            return material;
        }

        material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        AssetDatabase.CreateAsset(material, materialPath);
        return material;
    }

    private static void SavePrefab(GameObject instance, string prefabName)
    {
        string prefabPath = PrefabsPath + "/" + prefabName + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        Object.DestroyImmediate(instance);
    }

    private static void EnsureFolder(string parent, string folder)
    {
        string path = parent + "/" + folder;
        if (!Directory.Exists(path))
        {
            AssetDatabase.CreateFolder(parent, folder);
        }
    }
}
