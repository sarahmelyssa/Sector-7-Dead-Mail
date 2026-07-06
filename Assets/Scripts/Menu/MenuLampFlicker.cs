using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class MenuLampFlicker : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float baseAlpha = 0.26f;
    [SerializeField] private float pulseAmount = 0.08f;
    [SerializeField] private float pulseSpeed = 2.4f;
    [SerializeField] private float flickerChancePerSecond = 0.65f;
    [SerializeField] private Vector2 flickerAlphaRange = new Vector2(0.08f, 0.22f);
    [SerializeField] private Vector2 flickerDurationRange = new Vector2(0.025f, 0.09f);

    private float flickerUntil;

    private void Awake()
    {
        canvasGroup = canvasGroup != null ? canvasGroup : GetComponent<CanvasGroup>();
        canvasGroup.blocksRaycasts = false;
        canvasGroup.interactable = false;
    }

    private void Update()
    {
        if (Time.unscaledTime < flickerUntil)
        {
            canvasGroup.alpha = Random.Range(flickerAlphaRange.x, flickerAlphaRange.y);
            return;
        }

        float pulse = Mathf.Sin(Time.unscaledTime * pulseSpeed) * pulseAmount;
        canvasGroup.alpha = Mathf.Clamp01(baseAlpha + pulse);

        if (Random.value < flickerChancePerSecond * Time.unscaledDeltaTime)
        {
            flickerUntil = Time.unscaledTime + Random.Range(flickerDurationRange.x, flickerDurationRange.y);
        }
    }
}
