using System.Text;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

public class InspectionUI : MonoBehaviour
{
    public static InspectionUI Instance { get; private set; }

    private static PackageInteractable currentPackage;

    [SerializeField] private bool enableLegacyScreenUi = false;

    private Font uiFont;
    private Text infoText;
    private Button acceptButton;
    private Button rejectButton;
    private GameManager gameManager;
    private bool isOpen;

    private void Awake()
    {
        Instance = this;
        if (!enableLegacyScreenUi)
        {
            enabled = false;
            return;
        }

        gameManager = Object.FindFirstObjectByType<GameManager>();
        BuildUi();
        SetDecisionButtonsEnabled(false);
        ShowIdle();
    }

    public static void SetCurrentPackage(PackageInteractable package)
    {
        currentPackage = package;
    }

    public static void OpenCurrentPackage()
    {
        if (Instance == null || !Instance.enableLegacyScreenUi)
        {
            return;
        }

        Instance.OpenPackage(currentPackage);
    }

    private void OpenPackage(PackageInteractable package)
    {
        if (package == null)
        {
            ShowIdle();
            return;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            CloseInspection();
            return;
        }

        PackageData data = package.Data;
        var builder = new StringBuilder();
        builder.AppendLine("PACKAGE REPORT");
        builder.AppendLine();
        builder.AppendLine("Remetente: " + data.remetente);
        builder.AppendLine("Destino: " + data.destino);
        builder.AppendLine("Peso: " + data.peso.ToString("0.0") + " kg");
        builder.AppendLine("Tipo de conteudo: " + data.tipoConteudo);
        builder.AppendLine("Codigo serie: " + data.codigoSerie);
        builder.AppendLine("Relatorio: " + data.relatorio);
        infoText.text = builder.ToString();
        isOpen = true;
        SetDecisionButtonsEnabled(true);
    }

    public void AcceptCurrentPackage()
    {
        ChooseDecision(true);
    }

    public void RejectCurrentPackage()
    {
        ChooseDecision(false);
    }

    public void ToggleCurrentPackageReport()
    {
        if (isOpen)
        {
            CloseInspection();
        }
        else
        {
            OpenCurrentPackage();
        }
    }

    private void ChooseDecision(bool accepted)
    {
        if (currentPackage == null)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            CloseInspection();
            return;
        }

        PackageData data = currentPackage.Data;
        if (accepted)
        {
            AudioManager.Instance?.PlayAcceptButton();
        }
        else
        {
            AudioManager.Instance?.PlayRejectButton();
        }

        bool correct = gameManager.RegisterPackageDecision(data, accepted);
        if (correct)
        {
            AudioManager.Instance?.PlayCorrectResponse();
        }
        else
        {
            AudioManager.Instance?.PlayWrongResponse();
        }

        string action = accepted ? "aceite" : "rejeitado";
        string result = correct ? "Decisao correta." : "Decisao errada.";

        infoText.text = "PACKAGE REPORT\n\n"
            + "Pacote " + action + ".\n"
            + result + "\n\n"
            + "Regra: caixas com falhas de validacao devem ser rejeitadas.";

        PackageInteractable decidedPackage = currentPackage;
        currentPackage = null;
        SetDecisionButtonsEnabled(false);
        PackageManager.Instance?.OnPackageDecisionComplete(decidedPackage);
    }

    public void CloseInspection()
    {
        currentPackage = null;
        isOpen = false;
        SetDecisionButtonsEnabled(false);
        if (enableLegacyScreenUi && infoText != null)
        {
            ShowIdle();
        }
    }

    private void ShowIdle()
    {
        isOpen = false;
        infoText.text = "PACKAGE REPORT\n\nOlha para uma caixa e pressiona E para inspecionar.";
    }

    private void BuildUi()
    {
        uiFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (uiFont == null)
        {
            uiFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        EnsureEventSystem();

        var canvasObject = new GameObject("Inspection UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var panel = new GameObject("Inspection Panel");
        panel.transform.SetParent(canvasObject.transform, false);
        var panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f);
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 250f);
        panelRect.sizeDelta = new Vector2(820f, 370f);
        panel.AddComponent<Image>().color = new Color(0.035f, 0.026f, 0.052f, 0.90f);

        infoText = CreateText("Package Info", panel.transform, new Vector2(24f, 104f), new Vector2(-24f, -20f), 44, TextAnchor.UpperLeft, new Color(0.909f, 0.878f, 1f));
        acceptButton = CreateDecisionButton("Aceitar Button", panel.transform, "Aceitar", new Vector2(-135f, 48f), new Color(0.12f, 0.45f, 0.20f));
        rejectButton = CreateDecisionButton("Rejeitar Button", panel.transform, "Rejeitar", new Vector2(135f, 48f), new Color(0.55f, 0.12f, 0.10f));
        acceptButton.onClick.AddListener(() => ChooseDecision(true));
        rejectButton.onClick.AddListener(() => ChooseDecision(false));
    }

    private Text CreateText(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, int fontSize, TextAnchor anchor, Color color)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var text = textObject.AddComponent<Text>();
        text.font = uiFont;
        text.fontSize = fontSize;
        text.alignment = anchor;
        text.color = color;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        var outline = textObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);
        return text;
    }

    private Button CreateDecisionButton(string name, Transform parent, string label, Vector2 anchoredPosition, Color color)
    {
        var buttonObject = new GameObject(name);
        buttonObject.transform.SetParent(parent, false);
        var rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(220f, 56f);

        var image = buttonObject.AddComponent<Image>();
        image.color = color;

        var button = buttonObject.AddComponent<Button>();
        button.targetGraphic = image;

        var labelObject = new GameObject("Label");
        labelObject.transform.SetParent(buttonObject.transform, false);
        var labelRect = labelObject.AddComponent<RectTransform>();
        labelRect.anchorMin = Vector2.zero;
        labelRect.anchorMax = Vector2.one;
        labelRect.offsetMin = Vector2.zero;
        labelRect.offsetMax = Vector2.zero;

        var labelText = labelObject.AddComponent<Text>();
        labelText.font = uiFont;
        labelText.text = label;
        labelText.fontSize = 42;
        labelText.fontStyle = FontStyle.Bold;
        labelText.alignment = TextAnchor.MiddleCenter;
        labelText.color = Color.white;
        var outline = labelObject.AddComponent<Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        return button;
    }

    private void SetDecisionButtonsEnabled(bool enabled)
    {
        if (acceptButton != null)
        {
            acceptButton.interactable = enabled;
        }

        if (rejectButton != null)
        {
            rejectButton.interactable = enabled;
        }
    }

    private void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() != null)
        {
            return;
        }

        var eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<InputSystemUIInputModule>();
    }
}
