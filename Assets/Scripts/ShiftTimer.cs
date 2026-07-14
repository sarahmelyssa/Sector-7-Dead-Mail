using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Conta o tempo total do turno. O painel fisico transforma este tempo em
/// horas simbolicas de 00 AM ate 06 AM.
/// </summary>
public class ShiftTimer : MonoBehaviour
{
    [SerializeField] private float totalShiftDuration = 180f;
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private TMP_Text timerText;
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private bool useScreenHud = false;

    public float CurrentTime { get; private set; }
    public float TotalShiftDuration => totalShiftDuration;

    private GameManager gameManager;
    private PackageConveyor packageConveyor;
    private bool shiftEnded;
    private bool timerRunning = true;

    private void Awake()
    {
        CurrentTime = totalShiftDuration;
        gameManager = Object.FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        CreateDefaultUiIfNeeded();
        UpdateTimerText();
    }

    private void Update()
    {
        if (hudRoot != null)
        {
            bool showHud = useScreenHud && (UIManager.Instance == null || !UIManager.Instance.IsBlockingScreenOpen);
            if (hudRoot.activeSelf != showHud)
            {
                hudRoot.SetActive(showHud);
            }
        }

        if (shiftEnded || !timerRunning)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        // O timer so corre durante a gameplay ativa.
        CurrentTime = Mathf.Max(0f, CurrentTime - Time.deltaTime);
        UpdateTimerText();
        UpdateStatusText();

        if (CurrentTime <= 0f)
        {
            EndShift();
        }
    }

    private void EndShift()
    {
        shiftEnded = true;

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        gameManager?.ResolveShiftEnd();
    }

    public void SetTotalShiftDuration(float duration)
    {
        totalShiftDuration = Mathf.Max(1f, duration);
        CurrentTime = totalShiftDuration;
        shiftEnded = false;
        UpdateTimerText();
    }

    public void SetTimerRunning(bool running)
    {
        timerRunning = running;
    }

    private void UpdateTimerText()
    {
        if (timerText == null)
        {
            return;
        }

        int totalSeconds = Mathf.CeilToInt(CurrentTime);
        int minutes = totalSeconds / 60;
        int seconds = totalSeconds % 60;
        timerText.text = minutes.ToString("00") + ":" + seconds.ToString("00");
        timerText.color = CurrentTime <= 15f
            ? new Color(0.950f, 0.160f, 0.250f)
            : new Color(0.820f, 0.690f, 1f);
        UpdateStatusText();
    }

    private void UpdateStatusText()
    {
        if (statusText == null)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (packageConveyor == null)
        {
            packageConveyor = Object.FindFirstObjectByType<PackageConveyor>();
        }

        int quota = gameManager != null ? gameManager.quotaAtual : 0;
        int required = gameManager != null ? gameManager.quotaNecessaria : 0;
        string state = GetInspectionState();

        statusText.text = "QTA " + quota.ToString("00") + "/" + required.ToString("00")
            + "    SHIFT"
            + "    " + state;
    }

    private string GetInspectionState()
    {
        if (gameManager != null && !gameManager.IsPlaying)
        {
            return gameManager.CurrentState.ToString().ToUpperInvariant();
        }

        if (packageConveyor == null)
        {
            return "BOOT";
        }

        if (packageConveyor.IsPackageReadyForInspection)
        {
            return "READY";
        }

        if (packageConveyor.CurrentPackage != null)
        {
            return "TRANSIT";
        }

        return "WAITING";
    }

    private void CreateDefaultUiIfNeeded()
    {
        if (!useScreenHud)
        {
            if (hudRoot != null)
            {
                hudRoot.SetActive(false);
            }

            timerText = null;
            statusText = null;
            return;
        }

        if (timerText != null)
        {
            return;
        }

        var canvasObject = new GameObject("Shift Timer UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        hudRoot = new GameObject("Inspection HUD Bar");
        hudRoot.transform.SetParent(canvasObject.transform, false);
        RectTransform hudRect = hudRoot.AddComponent<RectTransform>();
        hudRect.anchorMin = new Vector2(0f, 1f);
        hudRect.anchorMax = new Vector2(1f, 1f);
        hudRect.pivot = new Vector2(0.5f, 1f);
        hudRect.anchoredPosition = new Vector2(0f, -12f);
        hudRect.sizeDelta = new Vector2(-72f, 50f);

        Image hudImage = hudRoot.AddComponent<Image>();
        hudImage.color = new Color(0.004f, 0.003f, 0.011f, 0.82f);
        Outline hudOutline = hudRoot.AddComponent<Outline>();
        hudOutline.effectColor = new Color(0.420f, 0.140f, 0.740f, 0.34f);
        hudOutline.effectDistance = new Vector2(2f, -2f);

        var textObject = new GameObject("Shift Timer Text");
        textObject.transform.SetParent(hudRoot.transform, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 1f);
        rect.anchorMax = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -25f);
        rect.sizeDelta = new Vector2(180f, 42f);

        timerText = textObject.AddComponent<TextMeshProUGUI>();
        timerText.fontSize = 44f;
        timerText.alignment = TextAlignmentOptions.Center;
        timerText.color = new Color(0.940f, 0.860f, 1f);
        timerText.characterSpacing = 4f;
        timerText.fontStyle = FontStyles.Bold;
        timerText.outlineWidth = 0.12f;
        timerText.outlineColor = Color.black;

        var statusObject = new GameObject("Inspection HUD Status Text");
        statusObject.transform.SetParent(hudRoot.transform, false);
        RectTransform statusRect = statusObject.AddComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(0.5f, 1f);
        statusRect.offsetMin = new Vector2(28f, 4f);
        statusRect.offsetMax = new Vector2(-48f, -4f);

        statusText = statusObject.AddComponent<TextMeshProUGUI>();
        statusText.fontSize = 32f;
        statusText.alignment = TextAlignmentOptions.MidlineLeft;
        statusText.color = new Color(0.900f, 0.820f, 1f, 0.94f);
        statusText.characterSpacing = 0.8f;
        statusText.outlineWidth = 0.10f;
        statusText.outlineColor = Color.black;
    }
}
