using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gera os dados das encomendas. Pode usar um catalogo inicial controlado ou
/// criar caixas aleatorias, sempre aumentando a dificuldade ao longo da noite.
/// </summary>
public class PackageManager : MonoBehaviour
{
    public static PackageManager Instance { get; private set; }

    [Header("Generation")]
    [SerializeField] private GameObject packagePrefab = null;
    [SerializeField] private Transform packageSpawnPoint = null;
    [SerializeField] private float nextPackageDelay = 1.65f;
    [SerializeField] private float reportErrorChance = 0.05f;
    [SerializeField] private bool useInitialPackageCatalog = true;
    [SerializeField] private List<PackageData> packageDataList = new List<PackageData>();

    private const float MaximumAllowedWeight = 12f;

    private readonly string[] validSenders =
    {
        "North Annex",
        "Basement Office",
        "Unit 04",
        "Night Archive"
    };

    private readonly string[] prohibitedSenders =
    {
        "Unknown Clerk",
        "Basement Mouth",
        "Red Choir",
        "Unlisted Hand"
    };

    private readonly string[] validDestinations =
    {
        "Sorting Room",
        "Cold Storage",
        "Records Office",
        "Inspection Desk",
        "Return Cage"
    };

    private readonly string[] invalidDestinations =
    {
        "Room 000",
        "North Annex B13",
        "Below Receiving",
        "Door Without Number"
    };

    private readonly string[] allowedContents =
    {
        "documents",
        "machine parts",
        "sealed tools",
        "medical samples",
        "office supplies"
    };

    private readonly string[] prohibitedContents =
    {
        "unsealed sample",
        "wet cables",
        "unmarked vial",
        "moving cloth",
        "warm metal box"
    };

    private Text statusText;
    private GameManager gameManager;
    private List<PackageData> initialPackageDeck;
    private int deckNight = -1;

    public GameObject PackagePrefab => packagePrefab;
    public float NextPackageDelay => nextPackageDelay;

    private void Awake()
    {
        Instance = this;
        gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager == null)
        {
            gameManager = gameObject.AddComponent<GameManager>();
        }

        FindOrCreatePackageSpawnPoint();
        BuildStatusUi();
    }

    private void Update()
    {
        UpdateStatusText();

        if (gameManager != null && !gameManager.IsPlaying)
        {
            StopPackageFlow();
            return;
        }
    }

    private void BuildStatusUi()
    {
        statusText = null;
    }

    private void FindOrCreatePackageSpawnPoint()
    {
        if (packageSpawnPoint != null)
        {
            return;
        }

        GameObject existingSpawnPoint = GameObject.Find("PackageInspectionCenter");
        if (existingSpawnPoint == null)
        {
            existingSpawnPoint = GameObject.Find("ActivePackageSpawnPoint");
        }

        if (existingSpawnPoint == null)
        {
            existingSpawnPoint = GameObject.Find("PackageSpawnPoint");
        }

        if (existingSpawnPoint != null)
        {
            packageSpawnPoint = existingSpawnPoint.transform;
            return;
        }

        GameObject spawnObject = new GameObject("PackageInspectionCenter");
        spawnObject.transform.position = new Vector3(0f, 1.39f, 0.62f);
        spawnObject.transform.rotation = Quaternion.identity;
        packageSpawnPoint = spawnObject.transform;
    }

    public void OnPackageDecisionComplete(PackageInteractable package)
    {
        InspectionStation.Instance?.SendCurrentPackageToExit();
    }

    public void OnCurrentPackageExitedConveyor()
    {
    }

    public void SetReportErrorChance(float chance)
    {
        reportErrorChance = Mathf.Clamp01(chance);
    }

    public void StopPackageFlow()
    {
    }

    public PackageData GetNextPackageData()
    {
        if (!useInitialPackageCatalog)
        {
            return CreateRandomPackageData();
        }

        // A primeira noite usa um baralho de caixas para controlar o ritmo da dificuldade.
        int currentNight = 1;
        if (initialPackageDeck == null || deckNight != currentNight)
        {
            deckNight = currentNight;
            initialPackageDeck = CreatePackageDeckForNight(currentNight);
            Shuffle(initialPackageDeck);
        }

        if (initialPackageDeck.Count == 0)
        {
            return CreateRandomPackageData();
        }

        int selectedIndex = PickProgressivePackageIndex(initialPackageDeck);
        PackageData data = initialPackageDeck[selectedIndex];
        initialPackageDeck.RemoveAt(selectedIndex);
        return data;
    }

    private PackageData CreateRandomPackageData()
    {
        float activeReportErrorChance = NightManager.Instance?.CurrentSettings != null
            ? NightManager.Instance.CurrentSettings.reportErrorChance
            : reportErrorChance;
        float difficulty = GetDifficultyProgress();

        // Quanto mais perto da quota final, maior a chance de dados suspeitos.
        bool invalidWeight = Random.value < Mathf.Lerp(0.08f, 0.22f, difficulty);
        bool invalidDestination = Random.value < Mathf.Lerp(0.07f, 0.21f, difficulty);
        bool invalidSender = Random.value < Mathf.Lerp(0.06f, 0.20f, difficulty);
        bool invalidContent = Random.value < Mathf.Lerp(0.08f, 0.22f, difficulty);
        bool invalidSerial = Random.value < Mathf.Lerp(0.07f, 0.23f, difficulty);
        bool contradictoryReport = Random.value < Mathf.Clamp01(Mathf.Lerp(activeReportErrorChance * 0.45f, activeReportErrorChance * 1.55f, difficulty));

        string sender = Pick(invalidSender ? prohibitedSenders : validSenders);
        string destination = Pick(invalidDestination ? invalidDestinations : validDestinations);
        string contentType = Pick(invalidContent ? prohibitedContents : allowedContents);
        float weight = Mathf.Round(Random.Range(invalidWeight ? 12.1f : 0.8f, invalidWeight ? 19f : 11.8f) * 10f) / 10f;
        string serialCode = GenerateSerialCode(!invalidSerial);
        string report = CreateReport(contentType, contradictoryReport);

        var packageData = new PackageData(
            sender,
            destination,
            weight,
            contentType,
            serialCode,
            report,
            new List<string>()
        );

        packageData.rejectionReasons = ValidatePackage(packageData);
        return packageData;
    }

    private int PickProgressivePackageIndex(List<PackageData> deck)
    {
        if (deck == null || deck.Count <= 1)
        {
            return 0;
        }

        // Evita caixas dificeis cedo demais, deixando as primeiras mais didaticas.
        float progress = GetDifficultyProgress();
        int fallbackIndex = deck.Count - 1;
        for (int i = deck.Count - 1; i >= 0; i--)
        {
            PackageData packageData = deck[i];
            if (packageData == null || IsDifficultyAllowedAtProgress(packageData.difficulty, progress))
            {
                return i;
            }
        }

        return fallbackIndex;
    }

    private bool IsDifficultyAllowedAtProgress(string difficulty, float progress)
    {
        string normalized = string.IsNullOrWhiteSpace(difficulty) ? "easy" : difficulty.Trim().ToLowerInvariant();
        if (progress < 0.25f)
        {
            return normalized == "easy";
        }

        if (progress < 0.65f)
        {
            return normalized == "easy" || normalized == "medium";
        }

        return normalized == "easy" || normalized == "medium" || normalized == "hard";
    }

    private float GetDifficultyProgress()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || gameManager.quotaNecessaria <= 1)
        {
            return 0f;
        }

        return Mathf.Clamp01(gameManager.quotaAtual / Mathf.Max(1f, gameManager.quotaNecessaria - 1f));
    }

    private List<string> ValidatePackage(PackageData data)
    {
        var reasons = new List<string>();

        if (data.peso > MaximumAllowedWeight)
        {
            reasons.Add("Peso acima do permitido");
        }

        if (!Contains(validDestinations, data.destino))
        {
            reasons.Add("Destino inexistente");
        }

        if (Contains(prohibitedSenders, data.remetente))
        {
            reasons.Add("Remetente proibido");
        }

        if (Contains(prohibitedContents, data.tipoConteudo))
        {
            reasons.Add("Tipo de conteudo proibido");
        }

        if (IsContradictoryReport(data))
        {
            reasons.Add("Relatorio contraditorio");
        }

        if (!IsValidSerialCode(data.codigoSerie))
        {
            reasons.Add("Codigo de serie invalido");
        }

        return reasons;
    }

    private string CreateReport(string contentType, bool contradictory)
    {
        if (contradictory)
        {
            string falseContent = contentType == "documents" ? "machine parts" : "documents";
            return "Scan limpo, mas manifesto secundario lista " + falseContent + ". Operador marcou: divergencia.";
        }

        return Pick(new[]
        {
            "Scan limpo. Peso declarado confere. Rota confirmada.",
            "Selo intacto. Sem som interno detectado. Manifesto consistente.",
            "Etiqueta legivel. Registro aprovado pelo terminal de entrada.",
            "Conteudo declarado corresponde ao manifesto primario."
        });
    }

    private bool IsContradictoryReport(PackageData data)
    {
        string report = data.relatorio.ToLowerInvariant();
        return report.Contains("divergencia")
            || report.Contains("nao corresponde")
            || report.Contains("secundario");
    }

    private string GenerateSerialCode(bool valid)
    {
        int first = Random.Range(0, 10);
        int second = Random.Range(0, 10);
        int third = Random.Range(0, 10);
        char check = GetSerialCheckCharacter(first, second, third);

        if (!valid)
        {
            check = check == 'Z' ? 'A' : (char)(check + 1);
        }

        return "PKG-" + first + second + third + "-" + check;
    }

    private bool IsValidSerialCode(string serialCode)
    {
        if (string.IsNullOrWhiteSpace(serialCode) || serialCode.Length != 9)
        {
            return false;
        }

        if (!serialCode.StartsWith("PKG-") || serialCode[7] != '-')
        {
            return false;
        }

        if (!char.IsDigit(serialCode[4]) || !char.IsDigit(serialCode[5]) || !char.IsDigit(serialCode[6]))
        {
            return false;
        }

        int first = serialCode[4] - '0';
        int second = serialCode[5] - '0';
        int third = serialCode[6] - '0';
        return serialCode[8] == GetSerialCheckCharacter(first, second, third);
    }

    private char GetSerialCheckCharacter(int first, int second, int third)
    {
        return (char)('A' + (first + second + third) % 26);
    }

    private bool Contains(string[] options, string value)
    {
        foreach (string option in options)
        {
            if (option == value)
            {
                return true;
            }
        }

        return false;
    }

    public GameObject CreatePackageObject(Vector3 position, Quaternion rotation, PackageData data)
    {
        return CreatePackageObject(position, rotation, data, null);
    }

    public GameObject CreatePackageObject(Vector3 position, Quaternion rotation, PackageData data, GameObject packagePrefabOverride)
    {
        if (data != null)
        {
            data.RefreshValidationReasons();
        }

        GameObject prefabToUse = packagePrefabOverride != null ? packagePrefabOverride : packagePrefab;
        GameObject packageObject = prefabToUse != null
            ? Instantiate(prefabToUse, position, rotation)
            : CreateDefaultPackagePrefabObject(position, rotation);

        Transform packageRoot = GetRuntimePackageRoot();
        if (packageRoot != null)
        {
            packageObject.transform.SetParent(packageRoot, true);
        }

        packageObject.transform.position = position;
        packageObject.transform.rotation = rotation;
        packageObject.tag = "Package";

        EnsurePackageRootCollider(packageObject);

        var body = packageObject.GetComponent<Rigidbody>();
        if (body == null)
        {
            body = packageObject.AddComponent<Rigidbody>();
        }
        body.isKinematic = true;

        var package = packageObject.GetComponent<InspectablePackage>();
        if (package == null)
        {
            package = packageObject.AddComponent<InspectablePackage>();
        }
        package.SetData(data);

        var interactable = packageObject.GetComponent<PackageInteractable>();
        if (interactable == null)
        {
            interactable = packageObject.AddComponent<PackageInteractable>();
        }
        interactable.SetData(data);

        PackageVisual packageVisual = packageObject.GetComponentInChildren<PackageVisual>();
        if (packageVisual == null)
        {
            packageVisual = packageObject.AddComponent<PackageVisual>();
        }
        packageVisual.ApplyPackageData(data);

        return packageObject;
    }

    private Transform GetRuntimePackageRoot()
    {
        GameObject root = GameObject.Find(CheckpointBootstrap.PackageSystemRootName);
        return root != null ? root.transform : null;
    }

    private void UpdateStatusText()
    {
        if (statusText == null || gameManager == null)
        {
            return;
        }

        bool showStatus = UIManager.Instance == null || !UIManager.Instance.IsBlockingScreenOpen;
        if (statusText.gameObject.activeSelf != showStatus)
        {
            statusText.gameObject.SetActive(showStatus);
        }

        if (!showStatus)
        {
            return;
        }

        statusText.text = "Quota: " + gameManager.quotaAtual + "/" + gameManager.quotaNecessaria
            + "  |  Turno: Final"
            + "  |  Estado: " + gameManager.CurrentState;
    }

    private string Pick(string[] options)
    {
        return options[Random.Range(0, options.Length)];
    }

    private void Shuffle(List<PackageData> packages)
    {
        for (int i = 0; i < packages.Count; i++)
        {
            int randomIndex = Random.Range(i, packages.Count);
            PackageData temporary = packages[i];
            packages[i] = packages[randomIndex];
            packages[randomIndex] = temporary;
        }
    }

    private List<PackageData> CreatePackageDeckForNight(int currentNight)
    {
        if (packageDataList != null && packageDataList.Count > 0)
        {
            var filteredPackages = new List<PackageData>();
            foreach (PackageData packageData in packageDataList)
            {
                if (packageData == null)
                {
                    continue;
                }

                string difficulty = string.IsNullOrWhiteSpace(packageData.difficulty)
                    ? ""
                    : packageData.difficulty.Trim().ToLowerInvariant();

                if (string.IsNullOrWhiteSpace(difficulty) || IsDifficultyAllowedForShift(difficulty))
                {
                    filteredPackages.Add(packageData);
                }
            }

            return filteredPackages.Count > 0 ? filteredPackages : new List<PackageData>(packageDataList);
        }

        return PackageCatalog.CreateAssetPackagesForNight(currentNight);
    }

    private bool IsDifficultyAllowedForShift(string difficulty)
    {
        return difficulty == "easy" || difficulty == "medium" || difficulty == "hard";
    }

    private GameObject CreateBox(string name, Vector3 position, Vector3 scale, Color color)
    {
        GameObject box = GameObject.CreatePrimitive(PrimitiveType.Cube);
        box.name = name;
        box.transform.position = position;
        box.transform.localScale = scale;

        var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
        material.color = color;
        box.GetComponent<Renderer>().sharedMaterial = material;
        return box;
    }

    private GameObject CreateDefaultPackagePrefabObject(Vector3 position, Quaternion rotation)
    {
        var root = new GameObject("PackagePrefab");
        root.transform.position = position;
        root.transform.rotation = rotation;

        GameObject boxModel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        boxModel.name = "BoxModel";
        boxModel.transform.SetParent(root.transform, false);
        boxModel.transform.localScale = new Vector3(1.05f, 0.70f, 0.86f);
        Renderer boxRenderer = boxModel.GetComponent<Renderer>();
        ApplyMaterial(boxRenderer, new Color(0.58f, 0.40f, 0.24f));
        boxRenderer.receiveShadows = false;
        boxRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        GameObject tapeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tapeObject.name = "TapeObject";
        tapeObject.transform.SetParent(root.transform, false);
        tapeObject.transform.localPosition = new Vector3(0f, 0.02f, -0.006f);
        tapeObject.transform.localScale = new Vector3(0.15f, 0.73f, 0.885f);

        GameObject labelQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
        labelQuad.name = "LabelQuad";
        labelQuad.transform.SetParent(root.transform, false);
        labelQuad.transform.localPosition = new Vector3(0f, 0.05f, -0.446f);
        labelQuad.transform.localRotation = Quaternion.identity;
        labelQuad.transform.localScale = new Vector3(0.60f, 0.38f, 1f);

        Destroy(labelQuad.GetComponent<Collider>());

        var visual = root.AddComponent<PackageVisual>();
        visual.SetRenderers(labelQuad.GetComponent<Renderer>(), tapeObject.GetComponent<Renderer>());

        return root;
    }

    private void EnsurePackageRootCollider(GameObject packageObject)
    {
        if (packageObject.GetComponent<Collider>() != null)
        {
            return;
        }

        BoxCollider collider = packageObject.AddComponent<BoxCollider>();
        collider.center = Vector3.zero;
        collider.size = new Vector3(1.12f, 0.78f, 0.94f);
    }

    private void ApplyMaterial(Renderer targetRenderer, Color color)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else
        {
            material.color = color;
        }

        targetRenderer.sharedMaterial = material;
    }
}
