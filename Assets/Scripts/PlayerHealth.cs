using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private TMP_Text sanityText;
    [SerializeField] private UnityEvent onDeath = new UnityEvent();

    public event Action Died;

    public int MaxHealth => maxHealth;
    public int CurrentHealth { get; private set; }

    private GameManager gameManager;
    private bool isDead;

    private void Awake()
    {
        CurrentHealth = maxHealth;
        gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
    }

    private void Start()
    {
        CreateDefaultUiIfNeeded();
        UpdateUi();
    }

    public void TakeDamage(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        if (gameManager == null)
        {
            gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0, CurrentHealth - amount);
        AudioManager.Instance?.PlayDamageReceived();
        HorrorEffectsManager.Instance?.OnPlayerDamaged();
        UpdateUi();

        if (CurrentHealth <= 0)
        {
            Die();
        }
    }

    public void Heal(int amount)
    {
        if (isDead || amount <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        UpdateUi();
    }

    private void Die()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        Died?.Invoke();
        onDeath?.Invoke();

        if (gameManager == null)
        {
            gameManager = UnityEngine.Object.FindFirstObjectByType<GameManager>();
        }

        gameManager?.GameOver("Player health reached zero.");
    }

    private void UpdateUi()
    {
        if (healthText != null)
        {
            healthText.text = "Vida: " + CurrentHealth + "/" + maxHealth;
        }

        if (sanityText != null)
        {
            sanityText.text = "Sanidade: " + CurrentHealth + "/" + maxHealth;
        }
    }

    private void CreateDefaultUiIfNeeded()
    {
        if (healthText != null && sanityText != null)
        {
            return;
        }

        var canvasObject = new GameObject("Player Health UI");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 80;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        healthText = healthText != null
            ? healthText
            : CreateTmpText("Health Text", canvasObject.transform, new Vector2(28f, -34f), "Vida: --", new Vector2(0f, 1f), TextAlignmentOptions.Left);

        sanityText = sanityText != null
            ? sanityText
            : CreateTmpText("Sanity Text", canvasObject.transform, new Vector2(-28f, -34f), "Sanidade: --", new Vector2(1f, 1f), TextAlignmentOptions.Right);
    }

    private TMP_Text CreateTmpText(string objectName, Transform parent, Vector2 anchoredPosition, string initialText, Vector2 anchor, TextAlignmentOptions alignment)
    {
        var textObject = new GameObject(objectName);
        textObject.transform.SetParent(parent, false);

        var rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = new Vector2(380f, 44f);

        var text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = initialText;
        text.fontSize = 32f;
        text.color = new Color(0.909f, 0.878f, 1f);
        text.alignment = alignment;
        text.outlineWidth = 0.15f;
        text.outlineColor = Color.black;
        return text;
    }
}
