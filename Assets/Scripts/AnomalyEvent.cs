using UnityEngine;

public class AnomalyEvent : MonoBehaviour
{
    [SerializeField] protected string anomalyName = "Anomaly";
    [SerializeField] protected float duration = 5f;
    [SerializeField] protected bool requiresLookingBack;
    [SerializeField] protected int sanityDamageIfIgnored;
    [SerializeField] protected AudioClip startSound = null;
    [SerializeField] protected AudioClip endSound = null;

    protected GameManager gameManager;
    protected ViewSwitcher viewSwitcher;
    protected SanityManager sanityManager;
    protected AudioManager audioManager;
    protected bool isRunning;
    protected bool resolved;

    public string AnomalyName => anomalyName;
    public float Duration => Mathf.Max(0.1f, duration);
    public bool RequiresLookingBack => requiresLookingBack;
    public int SanityDamageIfIgnored => Mathf.Max(0, sanityDamageIfIgnored);
    public bool IsRunning => isRunning;

    public virtual void Configure(GameManager game, ViewSwitcher view, SanityManager sanity, AudioManager audio)
    {
        gameManager = game;
        viewSwitcher = view;
        sanityManager = sanity;
        audioManager = audio;
    }

    public virtual void StartAnomaly()
    {
        isRunning = true;
        resolved = false;
        PlayClip(startSound);
    }

    public virtual void EndAnomaly()
    {
        if (!isRunning)
        {
            return;
        }

        isRunning = false;
        PlayClip(endSound);
    }

    public virtual void OnPlayerLookedBack()
    {
        if (requiresLookingBack)
        {
            resolved = true;
        }
    }

    public virtual bool WasResolved()
    {
        return resolved;
    }

    protected void Resolve()
    {
        resolved = true;
    }

    protected void PlayClip(AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        Camera camera = Camera.main;
        Vector3 position = camera != null ? camera.transform.position : transform.position;
        AudioSource.PlayClipAtPoint(clip, position, 0.8f);
    }
}
