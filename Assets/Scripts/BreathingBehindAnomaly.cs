using UnityEngine;

public class BreathingBehindAnomaly : AnomalyEvent
{
    [SerializeField] private float distortionDuration = 0.65f;

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
        anomalyName = "Breathing Behind Player";
        if (force || duration <= 0f)
        {
            duration = 5.2f;
        }

        // This anomaly follows the rule: hear breathing, turn/check behind you.
        requiresLookingBack = true;
        if (force || sanityDamageIfIgnored <= 0)
        {
            sanityDamageIfIgnored = 1;
        }
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        audioManager?.PlayBreathingBehind();
        HorrorEffectsManager.Instance?.PlayVhsDistortion(distortionDuration, 0.22f);
    }

    public override void OnPlayerLookedBack()
    {
        base.OnPlayerLookedBack();
        if (WasResolved())
        {
            audioManager?.PlayMobDisappears();
            HorrorEffectsManager.Instance?.PlayVhsDistortion(0.22f, 0.18f);
        }
    }
}
