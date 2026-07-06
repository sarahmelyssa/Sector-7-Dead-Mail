using UnityEngine;

public class BoxWhisperAnomaly : AnomalyEvent
{
    [SerializeField, Range(0f, 1f)] private float whisperVolume = 0.35f;

    private bool playedWhisper;

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
        anomalyName = "Box Whisper";
        if (force || duration <= 0f)
        {
            duration = 4f;
        }
        requiresLookingBack = false;
        sanityDamageIfIgnored = 0;
    }

    private void OnEnable()
    {
        PackageConveyor.PackageArrivedAtInspection += OnPackageArrived;
    }

    private void OnDisable()
    {
        PackageConveyor.PackageArrivedAtInspection -= OnPackageArrived;
    }

    public override void StartAnomaly()
    {
        base.StartAnomaly();
        playedWhisper = false;

        PackageConveyor conveyor = Object.FindFirstObjectByType<PackageConveyor>();
        if (conveyor != null && conveyor.IsPackageReadyForInspection)
        {
            PlayWhisper();
        }
    }

    private void OnPackageArrived(GameObject packageObject)
    {
        if (isRunning)
        {
            PlayWhisper();
        }
    }

    private void PlayWhisper()
    {
        if (playedWhisper)
        {
            return;
        }

        playedWhisper = true;
        HorrorEffectsManager.Instance?.PlayVhsDistortion(0.24f, 0.11f);
        HorrorEffectsManager.Instance?.MaybeShowStrangeText(0.45f);

        if (startSound != null)
        {
            Camera camera = Camera.main;
            Vector3 position = camera != null ? camera.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(startSound, position, whisperVolume);
        }
        else
        {
            audioManager?.PlayRandomKnock(0);
        }

        Resolve();
    }
}
