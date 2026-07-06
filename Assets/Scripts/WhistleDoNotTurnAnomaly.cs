using UnityEngine;

public class WhistleDoNotTurnAnomaly : AnomalyEvent
{
    [SerializeField] private int sanityDamageIfPlayerTurns = 1;

    private void Awake()
    {
        ApplyDefaultsIfNeeded();
    }

    private void Reset()
    {
        ApplyDefaultsIfNeeded(true);
    }

    private void ApplyDefaultsIfNeeded(bool force = false)
    {
        anomalyName = "Whistle - Do Not Turn";
        if (force || duration <= 0f)
        {
            duration = 4.5f;
        }

        // Opposite of breathing: this one is safe only if the player keeps facing the desk.
        requiresLookingBack = false;
        sanityDamageIfIgnored = 0;
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        audioManager?.PlayWhistleCue();
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.35f, 0.16f);
    }

    public override void OnPlayerLookedBack()
    {
        if (!isRunning || resolved)
        {
            return;
        }

        if (sanityManager != null && sanityDamageIfPlayerTurns > 0)
        {
            sanityManager.TakeSanityDamage(sanityDamageIfPlayerTurns);
            audioManager?.PlayDamageReceived();
            HorrorEffectsManager.Instance?.OnPlayerDamaged();
        }

        Resolve();
    }

    public override void EndAnomaly()
    {
        Resolve();
        base.EndAnomaly();
    }
}
