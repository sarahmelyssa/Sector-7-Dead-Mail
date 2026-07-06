using TMPro;
using UnityEngine;

public class SanityManager : MonoBehaviour
{
    [SerializeField] private int maxSanity = 3;
    [SerializeField] private int currentSanity = 3;
    [SerializeField] private TMP_Text sanityText = null;
    [SerializeField] private PlayerHealth playerHealth = null;
    [SerializeField] private GameManager gameManager = null;

    public int CurrentSanity => currentSanity;
    public int MaxSanity => maxSanity;

    private void Awake()
    {
        currentSanity = Mathf.Clamp(currentSanity <= 0 ? maxSanity : currentSanity, 0, maxSanity);
        ResolveReferences();
        UpdateUi();
    }

    public void TakeSanityDamage(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        ResolveReferences();

        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        currentSanity = Mathf.Max(0, currentSanity - amount);
        if (playerHealth != null)
        {
            playerHealth.TakeDamage(amount);
        }
        else
        {
            AudioManager.Instance?.PlayDamageReceived();
            HorrorEffectsManager.Instance?.OnPlayerDamaged();
        }

        UpdateUi();

        if (currentSanity <= 0)
        {
            gameManager?.GameOver("A sanidade chegou a zero.");
        }
    }

    public void HealSanity(int amount)
    {
        if (amount <= 0)
        {
            return;
        }

        currentSanity = Mathf.Min(maxSanity, currentSanity + amount);
        UpdateUi();
    }

    private void ResolveReferences()
    {
        if (playerHealth == null)
        {
            playerHealth = Object.FindFirstObjectByType<PlayerHealth>();
        }

        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }
    }

    private void UpdateUi()
    {
        if (sanityText != null)
        {
            sanityText.text = "Sanidade: " + currentSanity + "/" + maxSanity;
        }
    }
}
