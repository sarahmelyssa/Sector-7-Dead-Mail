using UnityEngine;

public class DoorKnockAnomaly : AnomalyEvent
{
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
        anomalyName = "Door Knock";
        if (force || duration <= 0f)
        {
            duration = 5f;
        }
        requiresLookingBack = true;
        if (force || sanityDamageIfIgnored <= 0)
        {
            sanityDamageIfIgnored = 1;
        }
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        int danger = gameManager != null ? gameManager.dangerLevel : 0;
        audioManager?.PlayRandomKnock(danger);
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.22f, 0.10f + danger * 0.025f);
    }

    public override void OnPlayerLookedBack()
    {
        base.OnPlayerLookedBack();
        if (WasResolved())
        {
            audioManager?.PlayRandomKnock(0);
            HorrorEffectsManager.Instance?.PlayVhsDistortion(0.18f, 0.12f);
        }
    }
}
