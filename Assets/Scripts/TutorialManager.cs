using System;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial UI")]
    public GameObject tutorialPanel;
    public TMP_Text tutorialText;
    public TMP_Text continueText;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip tutorialVoiceClip;

    private const string TutorialSeenKey = "Sector7_TutorialSeen";
    private Action onTutorialClosed;
    private bool tutorialActive;

    private static readonly string TutorialBody =
        "Welcome to Sector 7.\n\n" +
        "Your job is simple:\n" +
        "inspect each package and compare it with the report.\n\n" +
        "Check the shape, barcode, logo, tape, destination and weight.\n\n" +
        "If the package matches the report, ACCEPT it.\n" +
        "If something is wrong, REJECT it.\n\n" +
        "Use A/D to rotate the package.\n" +
        "Press E or click to open the report.\n" +
        "Press ENTER to accept.\n" +
        "Press Q to reject.\n\n" +
        "Do not rush.\n" +
        "Mistakes can cost the shift.";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        BuildDefaultUiIfNeeded();
        HideTutorial();
    }

    private void Update()
    {
        if (tutorialActive && Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            CloseTutorial();
        }
    }

    public bool TryShowNightOneTutorial(Action onClosed)
    {
        return false;
    }

    public void ResetTutorial()
    {
        PlayerPrefs.DeleteKey(TutorialSeenKey);
    }

    private void ShowTutorial()
    {
        tutorialActive = true;
        Time.timeScale = 0f;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(true);
        }

        if (tutorialText != null)
        {
            tutorialText.text = TutorialBody;
        }

        if (continueText != null)
        {
            continueText.text = "Press SPACE to begin";
        }

        if (audioSource != null && tutorialVoiceClip != null)
        {
            audioSource.clip = tutorialVoiceClip;
            audioSource.Play();
        }
    }

    private void CloseTutorial()
    {
        PlayerPrefs.SetInt(TutorialSeenKey, 1);
        PlayerPrefs.Save();

        HideTutorial();
        Time.timeScale = 1f;
        onTutorialClosed?.Invoke();
        onTutorialClosed = null;
    }

    private void HideTutorial()
    {
        tutorialActive = false;

        if (tutorialPanel != null)
        {
            tutorialPanel.SetActive(false);
        }
    }

    private void BuildDefaultUiIfNeeded()
    {
        if (tutorialPanel != null && tutorialText != null && continueText != null)
        {
            audioSource = audioSource != null ? audioSource : GetComponent<AudioSource>();
            return;
        }

        Canvas canvas = CreateCanvas();
        tutorialPanel = CreatePanel(canvas.transform);
        tutorialText = CreateTutorialText(tutorialPanel.transform);
        continueText = CreateContinueText(tutorialPanel.transform);

        audioSource = audioSource != null ? audioSource : gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
    }

    private Canvas CreateCanvas()
    {
        var canvasObject = new GameObject("Tutorial Canvas");
        canvasObject.transform.SetParent(transform, false);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 140;

        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        canvasObject.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private GameObject CreatePanel(Transform parent)
    {
        var panel = new GameObject("Shift Tutorial Panel", typeof(RectTransform));
        panel.transform.SetParent(parent, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(940f, 690f);

        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.010f, 0.006f, 0.020f, 0.94f);

        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.430f, 0.130f, 0.740f, 0.62f);
        outline.effectDistance = new Vector2(3f, -3f);

        return panel;
    }

    private TMP_Text CreateTutorialText(Transform parent)
    {
        TMP_Text text = CreateTextObject("Tutorial Text", parent, new Vector2(76f, 90f), new Vector2(-76f, -74f));
        text.fontSize = 28f;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.color = new Color(0.900f, 0.850f, 1f, 0.95f);
        text.lineSpacing = 3f;
        text.characterSpacing = 0.2f;
        return text;
    }

    private TMP_Text CreateContinueText(Transform parent)
    {
        TMP_Text text = CreateTextObject("Tutorial Continue Text", parent, new Vector2(72f, 28f), new Vector2(-72f, -28f));
        text.fontSize = 25f;
        text.alignment = TextAlignmentOptions.Bottom;
        text.fontStyle = FontStyles.Bold;
        text.color = new Color(0.710f, 0.430f, 1f, 0.98f);
        return text;
    }

    private TMP_Text CreateTextObject(string name, Transform parent, Vector2 offsetMin, Vector2 offsetMax)
    {
        var textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);

        RectTransform rect = textObject.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;

        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.textWrappingMode = TextWrappingModes.Normal;
        text.outlineWidth = 0.08f;
        text.outlineColor = Color.black;
        return text;
    }
}
