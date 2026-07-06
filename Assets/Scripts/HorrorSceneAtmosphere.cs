using UnityEngine;

public class HorrorSceneAtmosphere : MonoBehaviour
{
    [SerializeField] private Color calmAmbient = new Color(0.0025f, 0.0018f, 0.0075f);
    [SerializeField] private Color dangerAmbient = new Color(0.010f, 0.002f, 0.018f);
    [SerializeField] private Color calmFog = new Color(0.006f, 0.003f, 0.013f);
    [SerializeField] private Color dangerFog = new Color(0.024f, 0.003f, 0.020f);
    [SerializeField] private float calmFogDensity = 0.055f;
    [SerializeField] private float dangerFogDensity = 0.082f;
    [SerializeField] private float pulseSpeed = 0.65f;

    private GameManager gameManager;
    private Camera mainCamera;

    private void Awake()
    {
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.fog = true;
        ApplyAtmosphere(0f);
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        int danger = gameManager != null ? gameManager.dangerLevel : 0;
        float dangerT = Mathf.Clamp01(danger / 5f);
        float pulse = (Mathf.Sin(Time.time * pulseSpeed) + 1f) * 0.5f;
        ApplyAtmosphere(Mathf.Clamp01(dangerT + pulse * 0.08f));
    }

    private void ApplyAtmosphere(float intensity)
    {
        RenderSettings.ambientLight = Color.Lerp(calmAmbient, dangerAmbient, intensity);
        RenderSettings.fogColor = Color.Lerp(calmFog, dangerFog, intensity);
        RenderSettings.fogDensity = Mathf.Lerp(calmFogDensity, dangerFogDensity, intensity);

        if (mainCamera != null)
        {
            mainCamera.backgroundColor = Color.Lerp(new Color(0.002f, 0.002f, 0.006f), new Color(0.014f, 0.002f, 0.015f), intensity);
        }
    }
}
