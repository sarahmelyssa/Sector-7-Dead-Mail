using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ReportPanel : MonoBehaviour
{
    public static ReportPanel Instance { get; private set; }

    [SerializeField] private GameObject panelRoot = null;
    [SerializeField] private Canvas reportCanvas = null;
    [SerializeField] private CanvasGroup canvasGroup = null;
    [SerializeField] private Image dimBackgroundImage = null;
    [SerializeField] private Image reportImageUI = null;
    [SerializeField] private GameObject focusOverlayRoot = null;
    [SerializeField] private CanvasGroup focusOverlayGroup = null;
    [SerializeField] private RectTransform paperRect = null;
    [SerializeField] private TMP_Text titleText = null;
    [SerializeField] private TMP_Text bodyText = null;
    [SerializeField] private float animationDuration = 0.16f;
    [SerializeField] private Vector2 openAnchoredPosition = new Vector2(0f, -8f);
    [SerializeField] private Vector2 hiddenAnchoredPosition = new Vector2(0f, -780f);
    [SerializeField] private Vector2 maxReportSize = new Vector2(690f, 930f);
    [SerializeField] private Button nextPageButton = null;
    [SerializeField] private TMP_Text nextPageButtonLabel = null;

    private PackageData currentData;
    private readonly List<Sprite> currentReportPages = new List<Sprite>();
    private int currentReportPageIndex;
    private Coroutine animationRoutine;
    private Vector3 openScale = Vector3.one;

    public Image ReportImage => reportImageUI;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildDefaultPanelIfNeeded();
        HideImmediate();
    }

    private void OnEnable()
    {
        PackageConveyor.PackageArrivedAtInspection += UpdateFromPackageObject;
        PackageConveyor.PackageLeftInspection += ClearReport;
    }

    private void OnDisable()
    {
        PackageConveyor.PackageArrivedAtInspection -= UpdateFromPackageObject;
        PackageConveyor.PackageLeftInspection -= ClearReport;
    }

    public void ToggleReport()
    {
        Toggle();
    }

    public void Toggle()
    {
        if (currentData == null)
        {
            return;
        }

        if (panelRoot != null && panelRoot.activeSelf)
        {
            Hide();
        }
        else
        {
            Show();
        }
    }

    public void Show()
    {
        if (currentData == null)
        {
            return;
        }

        UpdateText();
        AudioManager.Instance?.PlayReportOpen();
        AudioManager.Instance?.StartReportCassetteLoop();
        Animate(true);
    }

    public void Hide()
    {
        if (panelRoot != null && panelRoot.activeSelf)
        {
            AudioManager.Instance?.PlayReportClose();
        }

        AudioManager.Instance?.StopReportCassetteLoop();
        Animate(false);
    }

    private void UpdateFromPackageObject(GameObject packageObject)
    {
        PackageInteractable package = packageObject != null ? packageObject.GetComponent<PackageInteractable>() : null;
        currentData = package != null ? package.Data : null;
        UpdateText();
    }

    private void ClearReport(GameObject packageObject)
    {
        currentData = null;
        currentReportPages.Clear();
        currentReportPageIndex = 0;
        UpdatePageButton();
        Hide();
    }

    private void UpdateText()
    {
        if (currentData == null)
        {
            return;
        }

        SetReportPages(currentData.GetReportSprites());
    }

    public void SetReport(Sprite sprite)
    {
        currentReportPages.Clear();
        currentReportPageIndex = 0;
        if (sprite != null)
        {
            currentReportPages.Add(sprite);
        }

        ApplyCurrentReportPage();
    }

    public void SetReportPages(IList<Sprite> pages)
    {
        currentReportPages.Clear();
        currentReportPageIndex = 0;

        if (pages != null)
        {
            foreach (Sprite page in pages)
            {
                if (page != null)
                {
                    currentReportPages.Add(page);
                }
            }
        }

        ApplyCurrentReportPage();
    }

    public void GoToNextReportPage()
    {
        if (currentReportPages.Count == 0)
        {
            Hide();
            return;
        }

        if (currentReportPageIndex < currentReportPages.Count - 1)
        {
            currentReportPageIndex++;
            ApplyCurrentReportPage();
            AudioManager.Instance?.PlayReportOpen();
            return;
        }

        Hide();
    }

    private void ApplyCurrentReportPage()
    {
        if (reportImageUI == null)
        {
            return;
        }

        Sprite sprite = currentReportPages.Count > 0
            ? currentReportPages[Mathf.Clamp(currentReportPageIndex, 0, currentReportPages.Count - 1)]
            : null;

        reportImageUI.sprite = sprite;
        reportImageUI.enabled = sprite != null;
        ResizeReportToSprite(sprite);
        UpdatePageButton();

        if (titleText != null)
        {
            titleText.gameObject.SetActive(sprite == null);
            titleText.text = "NO REPORT IMAGE";
        }

        if (bodyText != null)
        {
            bodyText.gameObject.SetActive(false);
            bodyText.text = "";
        }
    }

    private void Animate(bool show)
    {
        if (panelRoot == null)
        {
            return;
        }

        if (animationRoutine != null)
        {
            StopCoroutine(animationRoutine);
        }

        animationRoutine = StartCoroutine(AnimatePanel(show));
    }

    private IEnumerator AnimatePanel(bool show)
    {
        RectTransform movingRect = GetMovingReportRect();
        bool wasActive = panelRoot.activeSelf;

        if (show)
        {
            panelRoot.SetActive(true);

            if (!wasActive && movingRect != null)
            {
                movingRect.anchoredPosition = hiddenAnchoredPosition;
                movingRect.localScale = openScale * 0.94f;
            }
        }

        float elapsed = 0f;
        float startAlpha = canvasGroup != null ? canvasGroup.alpha : (show ? 0f : 1f);
        float targetAlpha = show ? 1f : 0f;
        Vector3 startScale = movingRect != null ? movingRect.localScale : panelRoot.transform.localScale;
        Vector3 targetScale = show ? openScale : openScale * 0.94f;
        Vector2 startPosition = movingRect != null ? movingRect.anchoredPosition : Vector2.zero;
        Vector2 targetPosition = show ? openAnchoredPosition : hiddenAnchoredPosition;

        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / animationDuration);
            float eased = 1f - Mathf.Pow(1f - t, 3f);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, eased);
            }

            if (movingRect != null)
            {
                movingRect.localScale = Vector3.Lerp(startScale, targetScale, eased);
                movingRect.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, eased);
            }

            yield return null;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = targetAlpha;
            canvasGroup.interactable = show;
            canvasGroup.blocksRaycasts = show;
        }

        if (movingRect != null)
        {
            movingRect.localScale = targetScale;
            movingRect.anchoredPosition = targetPosition;
        }

        if (!show)
        {
            panelRoot.SetActive(false);
        }

        animationRoutine = null;
    }

    private void HideImmediate()
    {
        if (panelRoot == null)
        {
            return;
        }

        RectTransform movingRect = GetMovingReportRect();
        openScale = movingRect == null || movingRect.localScale == Vector3.zero ? Vector3.one : movingRect.localScale;

        if (movingRect != null)
        {
            movingRect.localScale = openScale * 0.94f;
            movingRect.anchoredPosition = hiddenAnchoredPosition;
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;
        }

        panelRoot.SetActive(false);
        HideFocusOverlayImmediate();
    }

    private void BuildDefaultPanelIfNeeded()
    {
        if (panelRoot != null)
        {
            reportCanvas = reportCanvas != null ? reportCanvas : panelRoot.GetComponentInParent<Canvas>();
            canvasGroup = canvasGroup != null ? canvasGroup : panelRoot.GetComponent<CanvasGroup>();
            reportImageUI = reportImageUI != null ? reportImageUI : FindReportImageInPanel();
            paperRect = paperRect != null ? paperRect : GetMovingReportRect();
            ApplyLegacyDefaultPlacement();
            ApplyWorldCanvasDefaults();
            EnsurePageButton();
            if (canvasGroup == null)
            {
                canvasGroup = panelRoot.AddComponent<CanvasGroup>();
            }

            ResizeReportToSprite(reportImageUI != null ? reportImageUI.sprite : null);
            HideFocusOverlayImmediate();
            return;
        }

        ApplyLegacyDefaultPlacement();

        var canvasObject = new GameObject("Report Overlay Canvas");
        canvasObject.transform.SetParent(transform, false);
        reportCanvas = canvasObject.AddComponent<Canvas>();
        reportCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        reportCanvas.sortingOrder = 116;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        var canvasRect = canvasObject.GetComponent<RectTransform>();
        canvasRect.anchorMin = Vector2.zero;
        canvasRect.anchorMax = Vector2.one;
        canvasRect.offsetMin = Vector2.zero;
        canvasRect.offsetMax = Vector2.zero;

        panelRoot = new GameObject("Report Overlay");
        panelRoot.transform.SetParent(canvasObject.transform, false);
        var panelRect = panelRoot.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        canvasGroup = panelRoot.AddComponent<CanvasGroup>();

        var dimObject = new GameObject("Report Background Dim");
        dimObject.transform.SetParent(panelRoot.transform, false);
        var dimRect = dimObject.AddComponent<RectTransform>();
        dimRect.anchorMin = Vector2.zero;
        dimRect.anchorMax = Vector2.one;
        dimRect.offsetMin = Vector2.zero;
        dimRect.offsetMax = Vector2.zero;
        dimBackgroundImage = dimObject.AddComponent<Image>();
        dimBackgroundImage.color = new Color(0f, 0f, 0f, 0.78f);
        var dimButton = dimObject.AddComponent<Button>();
        dimButton.transition = Selectable.Transition.None;
        dimButton.onClick.AddListener(Hide);

        var paperObject = new GameObject("Pulled Report Paper");
        paperObject.transform.SetParent(panelRoot.transform, false);
        paperRect = paperObject.AddComponent<RectTransform>();
        paperRect.anchorMin = new Vector2(0.5f, 0.5f);
        paperRect.anchorMax = new Vector2(0.5f, 0.5f);
        paperRect.pivot = new Vector2(0.5f, 0.5f);
        paperRect.anchoredPosition = hiddenAnchoredPosition;
        paperRect.sizeDelta = new Vector2(640f, 930f);

        reportImageUI = paperObject.AddComponent<Image>();
        reportImageUI.preserveAspect = true;
        reportImageUI.color = Color.white;
        reportImageUI.raycastTarget = true;

        var shadow = paperObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0f, 0f, 0f, 0.62f);
        shadow.effectDistance = new Vector2(18f, -18f);

        titleText = CreateText("Report Missing Text", paperObject.transform, new Vector2(42f, 42f), new Vector2(-42f, -42f), 34f, TextAlignmentOptions.Center);
        titleText.color = new Color(0.945f, 0.914f, 1f);
        titleText.gameObject.SetActive(false);

        EnsurePageButton();
    }

    private void ApplyWorldCanvasDefaults()
    {
        if (reportCanvas == null)
        {
            return;
        }

        reportCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        reportCanvas.sortingOrder = 116;

        RectTransform canvasRect = reportCanvas.GetComponent<RectTransform>();
        if (canvasRect != null)
        {
            canvasRect.anchorMin = Vector2.zero;
            canvasRect.anchorMax = Vector2.one;
            canvasRect.offsetMin = Vector2.zero;
            canvasRect.offsetMax = Vector2.zero;
        }
    }

    private TMP_Text CreateText(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax, float fontSize, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(name);
        textObject.transform.SetParent(parent, false);
        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.outlineWidth = 0.15f;
        text.outlineColor = Color.black;
        text.textWrappingMode = TextWrappingModes.Normal;
        return text;
    }

    private Image FindReportImageInPanel()
    {
        Transform reportImageTransform = panelRoot != null ? panelRoot.transform.Find("Report Image") : null;
        if (reportImageTransform != null)
        {
            return reportImageTransform.GetComponent<Image>();
        }

        Image[] images = panelRoot != null ? panelRoot.GetComponentsInChildren<Image>(true) : null;
        if (images == null)
        {
            return null;
        }

        foreach (Image image in images)
        {
            if (image.gameObject != panelRoot)
            {
                return image;
            }
        }

        return null;
    }

    private void ApplyLegacyDefaultPlacement()
    {
        openAnchoredPosition = new Vector2(0f, -8f);
        hiddenAnchoredPosition = new Vector2(0f, -780f);
        maxReportSize = new Vector2(690f, 930f);
    }

    private RectTransform GetMovingReportRect()
    {
        if (paperRect != null)
        {
            return paperRect;
        }

        if (reportImageUI != null)
        {
            return reportImageUI.rectTransform;
        }

        return panelRoot != null ? panelRoot.GetComponent<RectTransform>() : null;
    }

    private void ResizeReportToSprite(Sprite sprite)
    {
        RectTransform targetRect = GetMovingReportRect();
        if (targetRect == null)
        {
            return;
        }

        Vector2 size = maxReportSize;
        if (sprite != null && sprite.rect.height > 0f)
        {
            float aspect = sprite.rect.width / sprite.rect.height;
            float maxAspect = maxReportSize.x / maxReportSize.y;

            size = aspect > maxAspect
                ? new Vector2(maxReportSize.x, maxReportSize.x / aspect)
                : new Vector2(maxReportSize.y * aspect, maxReportSize.y);
        }

        targetRect.sizeDelta = size;
    }

    private void EnsurePageButton()
    {
        if (nextPageButton != null)
        {
            nextPageButtonLabel = nextPageButtonLabel != null ? nextPageButtonLabel : nextPageButton.GetComponentInChildren<TMP_Text>(true);
            UpdatePageButton();
            return;
        }

        Transform buttonParent = paperRect != null ? paperRect.transform : panelRoot != null ? panelRoot.transform : transform;
        var buttonObject = new GameObject("Report Next Page Button");
        buttonObject.transform.SetParent(buttonParent, false);

        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-30f, 28f);
        rect.sizeDelta = new Vector2(176f, 64f);

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.250f, 0.080f, 0.420f, 0.92f);

        var outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.760f, 0.510f, 1f, 0.68f);
        outline.effectDistance = new Vector2(2f, -2f);

        nextPageButton = buttonObject.AddComponent<Button>();
        nextPageButton.targetGraphic = image;
        nextPageButton.onClick.AddListener(GoToNextReportPage);

        nextPageButtonLabel = CreateText("Next Page Label", buttonObject.transform, Vector2.zero, Vector2.zero, 28f, TextAlignmentOptions.Center);
        nextPageButtonLabel.text = "NEXT";
        nextPageButtonLabel.fontStyle = FontStyles.Bold;
        nextPageButtonLabel.color = new Color(0.925f, 0.850f, 1f, 1f);
        nextPageButtonLabel.outlineWidth = 0.12f;

        RectTransform labelRect = nextPageButtonLabel.GetComponent<RectTransform>();
        if (labelRect != null)
        {
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
        }

        UpdatePageButton();
    }

    private void UpdatePageButton()
    {
        if (nextPageButton == null)
        {
            return;
        }

        bool showButton = currentReportPages.Count > 1;
        nextPageButton.gameObject.SetActive(showButton);
        nextPageButton.interactable = showButton;

        if (nextPageButtonLabel != null)
        {
            nextPageButtonLabel.text = currentReportPageIndex < currentReportPages.Count - 1 ? "NEXT" : "CLOSE";
        }
    }

    private void BuildFocusOverlayIfNeeded()
    {
        if (focusOverlayRoot != null)
        {
            focusOverlayGroup = focusOverlayGroup != null ? focusOverlayGroup : focusOverlayRoot.GetComponent<CanvasGroup>();
            HideFocusOverlayImmediate();
            return;
        }

        var canvasObject = new GameObject("Report Focus Overlay");
        canvasObject.transform.SetParent(transform, false);

        Canvas overlayCanvas = canvasObject.AddComponent<Canvas>();
        overlayCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
        overlayCanvas.sortingOrder = 35;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        focusOverlayGroup = canvasObject.AddComponent<CanvasGroup>();
        focusOverlayGroup.blocksRaycasts = false;
        focusOverlayGroup.interactable = false;
        focusOverlayRoot = canvasObject;

        Color shade = new Color(0f, 0f, 0f, 0.74f);
        CreateOverlayBand("Top Shade", canvasObject.transform, new Vector2(0f, 0.78f), Vector2.one, shade);
        CreateOverlayBand("Bottom Shade", canvasObject.transform, Vector2.zero, new Vector2(1f, 0.16f), shade);
        CreateOverlayBand("Left Shade", canvasObject.transform, new Vector2(0f, 0.16f), new Vector2(0.24f, 0.78f), shade);
        CreateOverlayBand("Right Shade", canvasObject.transform, new Vector2(0.76f, 0.16f), new Vector2(1f, 0.78f), shade);

        HideFocusOverlayImmediate();
    }

    private void CreateOverlayBand(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var bandObject = new GameObject(name);
        bandObject.transform.SetParent(parent, false);

        RectTransform rect = bandObject.AddComponent<RectTransform>();
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image image = bandObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
    }

    private void HideFocusOverlayImmediate()
    {
        if (focusOverlayGroup != null)
        {
            focusOverlayGroup.alpha = 0f;
        }

        if (focusOverlayRoot != null)
        {
            focusOverlayRoot.SetActive(false);
        }
    }
}
