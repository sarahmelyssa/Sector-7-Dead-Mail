using TMPro;
using UnityEngine;

/// <summary>
/// Atualiza o painel fisico da parede: hora, pedidos, timer da caixa,
/// progresso e coracoes de vida.
/// </summary>
public class StationStatusMonitor : MonoBehaviour
{
    [SerializeField] private TMP_Text statusText = null;
    [SerializeField] private TMP_Text counterText = null;
    [SerializeField] private TMP_Text resultText = null;
    [SerializeField] private TMP_Text wallTimerText = null;
    [SerializeField] private TMP_Text wallQuotaText = null;
    [SerializeField] private TMP_Text wallStateText = null;
    [SerializeField] private TMP_Text wallFeedbackText = null;
    [SerializeField] private Renderer[] progressBlocks = null;
    [SerializeField] private Renderer[] lifeHeartRenderers = null;
    [SerializeField] private int lifeHeartPartsPerHeart = 3;
    [SerializeField] private float realSecondsPerGameHour = 60f;

    private GameManager gameManager;
    private PackageConveyor packageConveyor;
    private ShiftTimer shiftTimer;
    private InspectionStation inspectionStation;
    private float analysisTime;

    public void SetTextTargets(TMP_Text status, TMP_Text counter, TMP_Text result)
    {
        statusText = status;
        counterText = counter;
        resultText = result;
    }

    public void SetMachinePanelTargets(TMP_Text timer, TMP_Text quota, TMP_Text state, TMP_Text feedback, Renderer[] blocks, Renderer[] hearts = null, int heartPartsPerHeart = 3)
    {
        wallTimerText = timer;
        wallQuotaText = quota;
        wallStateText = state;
        wallFeedbackText = feedback;
        progressBlocks = blocks;
        lifeHeartRenderers = hearts;
        lifeHeartPartsPerHeart = Mathf.Max(1, heartPartsPerHeart);
    }

    private void OnEnable()
    {
        PackageConveyor.PackageArrivedAtInspection += OnPackageArrived;
        PackageConveyor.PackageLeftInspection += OnPackageLeft;
        DecisionManager.DecisionResolved += OnDecisionResolved;
    }

    private void OnDisable()
    {
        PackageConveyor.PackageArrivedAtInspection -= OnPackageArrived;
        PackageConveyor.PackageLeftInspection -= OnPackageLeft;
        DecisionManager.DecisionResolved -= OnDecisionResolved;
    }

    private void Update()
    {
        ResolveReferences();

        bool isReady = packageConveyor != null && packageConveyor.IsPackageReadyForInspection;
        bool hasPackage = packageConveyor != null && packageConveyor.CurrentPackage != null;

        if (isReady)
        {
            analysisTime += Time.deltaTime;
        }

        UpdateStatusText(isReady, hasPackage);
        UpdateTimerDisplay();
        UpdateBoxTimerDisplay(hasPackage);
        UpdateCounterText();
        UpdateLivesDisplay();
        UpdateResultText();
    }

    private void OnPackageArrived(GameObject packageObject)
    {
        analysisTime = 0f;
    }

    private void OnPackageLeft(GameObject packageObject)
    {
        analysisTime = 0f;
    }

    private void OnDecisionResolved(bool correct)
    {
        ClearDecisionSymbols();
    }

    private void UpdateStatusText(bool isReady, bool hasPackage)
    {
        string legacyState;

        if (gameManager != null && !gameManager.IsPlaying)
        {
            legacyState = "SYSTEM\nLOCKED";
        }
        else if (isReady)
        {
            int seconds = Mathf.FloorToInt(analysisTime);
            legacyState = "READING\n" + (seconds / 60).ToString("00") + ":" + (seconds % 60).ToString("00");
        }
        else if (hasPackage)
        {
            legacyState = "PACKAGE\nTRANSIT";
        }
        else
        {
            legacyState = "WAITING\nNEXT";
        }

        if (statusText == null)
        {
            return;
        }

        statusText.text = legacyState;
    }

    private void UpdateTimerDisplay()
    {
        if (wallTimerText == null)
        {
            return;
        }

        if (shiftTimer == null)
        {
            shiftTimer = Object.FindFirstObjectByType<ShiftTimer>();
        }

        float totalDuration = shiftTimer != null ? Mathf.Max(1f, shiftTimer.TotalShiftDuration) : realSecondsPerGameHour * 6f;
        float remaining = shiftTimer != null ? Mathf.Clamp(shiftTimer.CurrentTime, 0f, totalDuration) : totalDuration;
        float elapsed = Mathf.Max(0f, totalDuration - remaining);
        float progress = Mathf.Clamp01(elapsed / totalDuration);
        int currentHour = progress >= 0.999f
            ? 6
            : Mathf.Clamp(Mathf.FloorToInt(progress * 6f), 0, 5);

        // A hora e simbolica: a noite progride de 00 AM ate 06 AM.
        wallTimerText.text = "HORA " + currentHour.ToString("00") + " AM";
        wallTimerText.color = currentHour >= 5
            ? new Color(0.950f, 0.160f, 0.250f)
            : new Color(0.980f, 0.820f, 0.420f);
    }

    private void UpdateBoxTimerDisplay(bool hasPackage)
    {
        if (wallStateText == null)
        {
            return;
        }

        if (inspectionStation == null)
        {
            inspectionStation = Object.FindFirstObjectByType<InspectionStation>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            wallStateText.text = "CAIXA --";
            wallStateText.color = new Color(0.400f, 0.360f, 0.480f);
            return;
        }

        if (inspectionStation != null && inspectionStation.IsBoxTimerRunning)
        {
            // Mostra o tempo restante da caixa atual no painel da sala.
            int seconds = Mathf.CeilToInt(Mathf.Max(0f, inspectionStation.CurrentBoxTimeRemaining));
            wallStateText.text = "CAIXA " + seconds.ToString("00") + "s";
            wallStateText.color = seconds <= 3
                ? new Color(0.950f, 0.160f, 0.250f)
                : new Color(0.760f, 1.000f, 0.790f);
            return;
        }

        wallStateText.text = hasPackage ? "CAIXA MOVE" : "CAIXA --";
        wallStateText.color = hasPackage
            ? new Color(0.620f, 0.540f, 0.800f)
            : new Color(0.400f, 0.360f, 0.480f);
    }

    private void UpdateCounterText()
    {
        int quota = gameManager != null ? gameManager.quotaAtual : 0;
        int required = gameManager != null ? gameManager.quotaNecessaria : 0;

        if (counterText != null)
        {
            counterText.text = "QUOTA " + quota.ToString("00") + "/" + required.ToString("00")
                + "\nSHIFT";
        }

        if (wallQuotaText != null)
        {
            wallQuotaText.text = "PEDIDOS " + quota.ToString("00") + "/" + required.ToString("00");
        }

        UpdateProgressBlocks(quota, required);
    }

    private void UpdateLivesDisplay()
    {
        // Os coracoes mostram as 3 chances antes do Game Over.
        int wrong = gameManager != null ? gameManager.WrongDecisionCount : 0;
        int maxLives = gameManager != null ? gameManager.MaxWrongDecisions : 3;
        int livesToShow = Mathf.Clamp(maxLives, 1, 3);
        int remainingLives = Mathf.Clamp(livesToShow - wrong, 0, livesToShow);

        if (wallFeedbackText != null)
        {
            wallFeedbackText.richText = true;
            wallFeedbackText.text = BuildHeartDisplay(remainingLives, livesToShow);
            wallFeedbackText.color = Color.white;
        }

        UpdateLifeHeartRenderers(remainingLives, livesToShow);
    }

    private string BuildHeartDisplay(int remainingLives, int livesToShow)
    {
        const string activeHeart = "<color=#ff2748>♥</color>";
        const string lostHeart = "<color=#3b1724>♡</color>";
        string text = "";

        for (int i = 0; i < livesToShow; i++)
        {
            text += i < remainingLives ? activeHeart : lostHeart;
            if (i < livesToShow - 1)
            {
                text += " ";
            }
        }

        return text;
    }

    private void UpdateLifeHeartRenderers(int remainingLives, int livesToShow)
    {
        if (lifeHeartRenderers == null || lifeHeartRenderers.Length == 0)
        {
            return;
        }

        int partsPerHeart = Mathf.Max(1, lifeHeartPartsPerHeart);
        Color activeColor = new Color(1.000f, 0.055f, 0.180f);
        Color lostColor = new Color(0.130f, 0.025f, 0.055f);

        for (int i = 0; i < lifeHeartRenderers.Length; i++)
        {
            Renderer heartRenderer = lifeHeartRenderers[i];
            if (heartRenderer == null)
            {
                continue;
            }

            int heartIndex = i / partsPerHeart;
            bool visible = heartIndex < livesToShow;
            heartRenderer.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            bool isActive = heartIndex < remainingLives;
            ApplyRendererColor(heartRenderer, isActive ? activeColor : lostColor, isActive ? 2.15f : 0.18f);
        }
    }

    private void UpdateResultText()
    {
        ClearDecisionSymbols();
    }

    private void UpdateProgressBlocks(int quota, int required)
    {
        if (progressBlocks == null)
        {
            return;
        }

        int visibleBlocks = Mathf.Clamp(required, 0, progressBlocks.Length);
        for (int i = 0; i < progressBlocks.Length; i++)
        {
            Renderer block = progressBlocks[i];
            if (block == null)
            {
                continue;
            }

            bool visible = i < visibleBlocks;
            block.gameObject.SetActive(visible);
            if (!visible)
            {
                continue;
            }

            bool complete = i < quota;
            Color color = complete
                ? new Color(0.160f, 0.780f, 0.360f)
                : new Color(0.105f, 0.070f, 0.160f);
            float emission = complete ? 1.35f : 0.18f;
            ApplyRendererColor(block, color, emission);
        }
    }

    private void ClearDecisionSymbols()
    {
        if (resultText != null)
        {
            resultText.text = "";
        }

        UpdateLivesDisplay();
    }

    private void ApplyRendererColor(Renderer targetRenderer, Color color, float emission)
    {
        if (targetRenderer == null)
        {
            return;
        }

        Material material = targetRenderer.material;
        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }

        if (material.HasProperty("_EmissionColor"))
        {
            material.SetColor("_EmissionColor", color * emission);
            material.EnableKeyword("_EMISSION");
        }
    }

    private void ResolveReferences()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (packageConveyor == null)
        {
            packageConveyor = Object.FindFirstObjectByType<PackageConveyor>();
        }

        if (shiftTimer == null)
        {
            shiftTimer = Object.FindFirstObjectByType<ShiftTimer>();
        }

        if (inspectionStation == null)
        {
            inspectionStation = Object.FindFirstObjectByType<InspectionStation>();
        }
    }
}
