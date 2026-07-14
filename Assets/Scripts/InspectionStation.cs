using System.Collections;
using UnityEngine;

/// <summary>
/// Coordena a bancada de inspecao: recebe caixas, inicia o temporizador,
/// bloqueia/desbloqueia botoes e envia a decisao para o GameManager.
/// </summary>
public class InspectionStation : MonoBehaviour
{
    public static InspectionStation Instance { get; private set; }

    [Header("References")]
    [SerializeField] private PackageManager packageManager = null;
    [SerializeField] private PackageConveyor packageConveyor = null;
    [SerializeField] private PackageRotator packageRotator = null;
    [SerializeField] private ReportPanel reportPanel = null;
    [SerializeField] private DecisionManager decisionManager = null;
    [SerializeField] private GameManager gameManager = null;

    [Header("Flow")]
    [SerializeField] private bool startAutomatically = true;

    [Header("Evaluation Timer")]
    [SerializeField] private float boxEvaluationTime = 60f;
    [SerializeField] private float minimumBoxEvaluationTime = 30f;

    private const float MaxBoxEvaluationTime = 60f;
    private const float MinBoxEvaluationTime = 30f;

    private PackageInteractable currentPackage;
    private bool actionsLocked = true;
    private bool stationStopped;
    private Coroutine packageFlowRoutine;

    public float BoxEvaluationTime => boxEvaluationTime;
    public float CurrentBoxTimeRemaining { get; private set; }
    public bool IsBoxTimerRunning { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (boxEvaluationTime <= 0f)
        {
            boxEvaluationTime = MaxBoxEvaluationTime;
        }

        boxEvaluationTime = Mathf.Clamp(boxEvaluationTime, MinBoxEvaluationTime, MaxBoxEvaluationTime);
        if (boxEvaluationTime < MaxBoxEvaluationTime)
        {
            boxEvaluationTime = MaxBoxEvaluationTime;
        }

        minimumBoxEvaluationTime = Mathf.Clamp(minimumBoxEvaluationTime, MinBoxEvaluationTime, boxEvaluationTime);

        ResolveReferences();
    }

    private void OnEnable()
    {
        PackageConveyor.PackageArrivedAtInspection += OnPackageArrivedAtInspection;
        PackageConveyor.PackageLeftInspection += OnPackageLeftInspection;
        PackageConveyor.PackageExited += OnPackageExited;
    }

    private void OnDisable()
    {
        PackageConveyor.PackageArrivedAtInspection -= OnPackageArrivedAtInspection;
        PackageConveyor.PackageLeftInspection -= OnPackageLeftInspection;
        PackageConveyor.PackageExited -= OnPackageExited;
    }

    private void Start()
    {
        ResolveReferences();

        if (NightStoryManager.Instance != null && NightStoryManager.Instance.ShouldDelayGameplayStart())
        {
            return;
        }

        if (startAutomatically)
        {
            StartNight();
        }
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying && !stationStopped)
        {
            StopStation();
            return;
        }

        UpdateBoxEvaluationTimer();
    }

    public void StartNight()
    {
        ResolveReferences();

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        stationStopped = false;
        actionsLocked = true;
        // A primeira caixa so e pedida quando a historia/tutorial ja terminou.
        RequestNextPackage();
    }

    public void RotateLeft()
    {
        if (!CanUseStationActions())
        {
            return;
        }

        packageRotator?.RotateLeft();
    }

    public void RotateRight()
    {
        if (!CanUseStationActions())
        {
            return;
        }

        packageRotator?.RotateRight();
    }

    public void ToggleReport()
    {
        if (!CanUseStationActions())
        {
            return;
        }

        reportPanel?.ToggleReport();
    }

    public void AcceptPackage()
    {
        ProcessDecision(true);
    }

    public void RejectPackage()
    {
        ProcessDecision(false);
    }

    public void SendCurrentPackageToExit()
    {
        if (packageConveyor == null || packageConveyor.CurrentPackage == null)
        {
            return;
        }

        // Ao decidir, fecha o report, para o timer e envia a caixa para a esteira.
        actionsLocked = true;
        reportPanel?.Hide();
        packageConveyor.SendCurrentPackageToExit();
    }

    public void StopStation()
    {
        stationStopped = true;
        actionsLocked = true;
        currentPackage = null;
        CurrentBoxTimeRemaining = 0f;
        IsBoxTimerRunning = false;
        reportPanel?.Hide();

        if (packageFlowRoutine != null)
        {
            StopCoroutine(packageFlowRoutine);
            packageFlowRoutine = null;
        }

        packageConveyor?.StopConveyor();
    }

    private void ProcessDecision(bool accepted)
    {
        if (!CanUseStationActions())
        {
            return;
        }

        actionsLocked = true;
        IsBoxTimerRunning = false;
        CurrentBoxTimeRemaining = 0f;
        reportPanel?.Hide();
        decisionManager?.SubmitDecision(currentPackage.Data, accepted);

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        packageConveyor?.SendCurrentPackageToExit();
    }

    private void RequestNextPackage()
    {
        if (stationStopped || packageManager == null || packageConveyor == null)
        {
            return;
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        if (packageConveyor.CurrentPackage != null)
        {
            return;
        }

        actionsLocked = true;
        currentPackage = null;
        CurrentBoxTimeRemaining = 0f;
        IsBoxTimerRunning = false;
        PackageData data = packageManager.GetNextPackageData();
        packageConveyor.SpawnPackage(data, packageManager);
    }

    private void OnPackageArrivedAtInspection(GameObject packageObject)
    {
        if (stationStopped || packageObject == null)
        {
            return;
        }

        // Quando a caixa para no centro, o report e carregado e o timer comeca.
        currentPackage = packageObject.GetComponent<PackageInteractable>();
        if (currentPackage != null)
        {
            reportPanel?.SetReportPages(currentPackage.Data.GetReportSprites());
        }
        else
        {
            reportPanel?.SetReport(null);
        }
        actionsLocked = currentPackage == null;
        if (currentPackage != null)
        {
            CurrentBoxTimeRemaining = GetCurrentEvaluationTime();
            IsBoxTimerRunning = true;
        }
    }

    private void OnPackageLeftInspection(GameObject packageObject)
    {
        currentPackage = null;
        actionsLocked = true;
        CurrentBoxTimeRemaining = 0f;
        IsBoxTimerRunning = false;
    }

    private void OnPackageExited(GameObject packageObject)
    {
        if (packageFlowRoutine != null)
        {
            StopCoroutine(packageFlowRoutine);
        }

        packageFlowRoutine = StartCoroutine(SpawnNextPackageAfterDelay());
    }

    private IEnumerator SpawnNextPackageAfterDelay()
    {
        float delay = packageManager != null ? packageManager.NextPackageDelay : 1f;
        yield return new WaitForSeconds(delay);
        packageFlowRoutine = null;

        if (gameManager != null && gameManager.IsPlaying)
        {
            RequestNextPackage();
        }
    }

    private void UpdateBoxEvaluationTimer()
    {
        if (!IsBoxTimerRunning)
        {
            return;
        }

        if (!CanUseStationActions())
        {
            return;
        }

        // Se o jogador demorar demais, a caixa conta como erro automaticamente.
        CurrentBoxTimeRemaining = Mathf.Max(0f, CurrentBoxTimeRemaining - Time.deltaTime);
        if (CurrentBoxTimeRemaining <= 0f)
        {
            HandleBoxEvaluationTimeout();
        }
    }

    private void HandleBoxEvaluationTimeout()
    {
        if (!CanUseStationActions())
        {
            return;
        }

        IsBoxTimerRunning = false;
        CurrentBoxTimeRemaining = 0f;
        actionsLocked = true;
        reportPanel?.Hide();
        decisionManager?.SubmitTimeout(currentPackage.Data);

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        packageConveyor?.SendCurrentPackageToExit();
    }

    private bool CanUseStationActions()
    {
        ResolveReferences();

        if (actionsLocked || stationStopped)
        {
            return false;
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return false;
        }

        if (packageConveyor == null || !packageConveyor.IsPackageReadyForInspection)
        {
            return false;
        }

        return currentPackage != null && currentPackage.CanInteract;
    }

    private float GetCurrentEvaluationTime()
    {
        ResolveReferences();

        float startTime = Mathf.Max(1f, boxEvaluationTime);
        float endTime = Mathf.Clamp(minimumBoxEvaluationTime, 1f, startTime);
        if (gameManager == null || gameManager.quotaNecessaria <= 1)
        {
            return startTime;
        }

        float progress = Mathf.Clamp01(gameManager.quotaAtual / Mathf.Max(1f, gameManager.quotaNecessaria - 1f));
        return Mathf.Lerp(startTime, endTime, progress);
    }

    private void ResolveReferences()
    {
        if (packageManager == null)
        {
            packageManager = Object.FindFirstObjectByType<PackageManager>();
        }

        if (packageConveyor == null)
        {
            packageConveyor = Object.FindFirstObjectByType<PackageConveyor>();
        }

        if (packageRotator == null)
        {
            packageRotator = Object.FindFirstObjectByType<PackageRotator>();
        }

        if (reportPanel == null)
        {
            reportPanel = Object.FindFirstObjectByType<ReportPanel>();
        }

        if (decisionManager == null)
        {
            decisionManager = Object.FindFirstObjectByType<DecisionManager>();
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }
    }
}
