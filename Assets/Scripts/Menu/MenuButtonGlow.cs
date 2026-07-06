using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
public class MenuButtonGlow : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISelectHandler, IDeselectHandler
{
    [SerializeField] private CanvasGroup glowGroup;
    [SerializeField] private RectTransform targetRect;
    [SerializeField] private float idleAlpha;
    [SerializeField] private float hoverAlpha = 0.34f;
    [SerializeField] private float disabledAlpha;
    [SerializeField] private float hoverScale = 1.018f;
    [SerializeField] private float tweenTime = 0.11f;
    [SerializeField] private bool playHoverSound = true;
    [SerializeField] private bool playClickSound = true;

    private Button button;
    private Vector3 baseScale;
    private Coroutine tween;
    private static float lastHoverSoundTime;
    private static AudioSource fallbackUiSource;
    private static AudioClip fallbackHoverClip;
    private static AudioClip fallbackClickClip;

    private void Awake()
    {
        button = GetComponent<Button>();
        targetRect = targetRect != null ? targetRect : GetComponent<RectTransform>();
        glowGroup = glowGroup != null ? glowGroup : GetComponentInChildren<CanvasGroup>();
        baseScale = targetRect.localScale;
        SetGlowAlpha(IsInteractable() ? idleAlpha : disabledAlpha);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        ShowHover();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        HideHover();
    }

    public void OnSelect(BaseEventData eventData)
    {
        ShowHover();
    }

    public void OnDeselect(BaseEventData eventData)
    {
        HideHover();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (IsInteractable())
        {
            TryPlayClickSound();
            StartTween(hoverAlpha, baseScale * 1.03f);
        }
    }

    private void ShowHover()
    {
        if (IsInteractable())
        {
            TryPlayHoverSound();
            StartTween(hoverAlpha, baseScale * hoverScale);
        }
    }

    private void HideHover()
    {
        StartTween(IsInteractable() ? idleAlpha : disabledAlpha, baseScale);
    }

    private void StartTween(float alpha, Vector3 scale)
    {
        if (!isActiveAndEnabled || !gameObject.activeInHierarchy)
        {
            SetGlowAlpha(alpha);
            if (targetRect != null)
            {
                targetRect.localScale = scale;
            }
            tween = null;
            return;
        }

        if (tween != null)
        {
            StopCoroutine(tween);
        }

        tween = StartCoroutine(TweenRoutine(alpha, scale));
    }

    private IEnumerator TweenRoutine(float alpha, Vector3 scale)
    {
        float startAlpha = glowGroup != null ? glowGroup.alpha : 0f;
        Vector3 startScale = targetRect.localScale;
        float elapsed = 0f;

        while (elapsed < tweenTime)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / tweenTime);
            float eased = 1f - Mathf.Pow(1f - t, 3f);
            SetGlowAlpha(Mathf.Lerp(startAlpha, alpha, eased));
            targetRect.localScale = Vector3.Lerp(startScale, scale, eased);
            yield return null;
        }

        SetGlowAlpha(alpha);
        targetRect.localScale = scale;
        tween = null;
    }

    private bool IsInteractable()
    {
        return button == null || button.interactable;
    }

    private void TryPlayHoverSound()
    {
        if (!playHoverSound || Time.unscaledTime - lastHoverSoundTime < 0.08f)
        {
            return;
        }

        lastHoverSoundTime = Time.unscaledTime;
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUiHover();
            return;
        }

        PlayFallbackUiSound(true);
    }

    private void TryPlayClickSound()
    {
        if (!playClickSound)
        {
            return;
        }

        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.PlayUiClick();
            return;
        }

        PlayFallbackUiSound(false);
    }

    private static void PlayFallbackUiSound(bool hover)
    {
        if (fallbackUiSource == null)
        {
            GameObject sourceObject = new GameObject("RuntimeFallbackUiAudio");
            DontDestroyOnLoad(sourceObject);
            fallbackUiSource = sourceObject.AddComponent<AudioSource>();
            fallbackUiSource.playOnAwake = false;
            fallbackUiSource.spatialBlend = 0f;
            fallbackUiSource.volume = 0.55f;
        }

        AudioClip clip = hover
            ? fallbackHoverClip != null ? fallbackHoverClip : fallbackHoverClip = LoadUiClip("ui_button_hover") ?? CreateFallbackUiClip("Generated UI Hover", 620f, 0.055f, 0.10f)
            : fallbackClickClip != null ? fallbackClickClip : fallbackClickClip = LoadSfxClip("sfx_button_press_custom") ?? LoadUiClip("ui_button_click") ?? CreateFallbackUiClip("Generated UI Click", 1040f, 0.075f, 0.22f);

        if (clip != null)
        {
            fallbackUiSource.PlayOneShot(clip, hover ? 0.36f : 0.58f);
        }
    }

    private static AudioClip LoadUiClip(string clipName)
    {
        AudioClip resourceClip = Resources.Load<AudioClip>("Audio/UI/" + clipName);
        if (resourceClip != null)
        {
            return resourceClip;
        }

#if UNITY_EDITOR
        const string folderPath = "Assets/Audio/UI";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
        {
            return null;
        }

        string[] guids = UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == clipName)
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
#endif

        return null;
    }

    private static AudioClip LoadSfxClip(string clipName)
    {
        AudioClip resourceClip = Resources.Load<AudioClip>("Audio/SFX/" + clipName);
        if (resourceClip != null)
        {
            return resourceClip;
        }

#if UNITY_EDITOR
        const string folderPath = "Assets/Audio/SFX";
        if (!UnityEditor.AssetDatabase.IsValidFolder(folderPath))
        {
            return null;
        }

        string[] guids = UnityEditor.AssetDatabase.FindAssets(clipName + " t:AudioClip", new[] { folderPath });
        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            if (System.IO.Path.GetFileNameWithoutExtension(path) == clipName)
            {
                return UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>(path);
            }
        }
#endif

        return null;
    }

    private static AudioClip CreateFallbackUiClip(string clipName, float frequency, float duration, float level)
    {
        const int sampleRate = 44100;
        int sampleCount = Mathf.CeilToInt(sampleRate * duration);
        float[] samples = new float[sampleCount];

        for (int i = 0; i < sampleCount; i++)
        {
            float time = i / (float)sampleRate;
            float t = time / Mathf.Max(0.001f, duration);
            float envelope = Mathf.Exp(-t * 5.5f) * Mathf.Sin(Mathf.Clamp01(t) * Mathf.PI);
            samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * level;
        }

        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, sampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private void SetGlowAlpha(float alpha)
    {
        if (glowGroup != null)
        {
            glowGroup.alpha = alpha;
        }
    }
}
