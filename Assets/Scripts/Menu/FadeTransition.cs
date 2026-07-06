using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class FadeTransition : MonoBehaviour
{
    [SerializeField] private CanvasGroup fadeGroup;
    [SerializeField] private float duration = 0.55f;

    private bool isFading;

    private void Awake()
    {
        if (fadeGroup != null)
        {
            fadeGroup.alpha = 0f;
            fadeGroup.blocksRaycasts = false;
        }
    }

    public void FadeToScene(string sceneName)
    {
        if (!isFading && !string.IsNullOrWhiteSpace(sceneName))
        {
            StartCoroutine(FadeRoutine(() => SceneManager.LoadScene(sceneName)));
        }
    }

    public void FadeToScene(int buildIndex)
    {
        if (!isFading && buildIndex >= 0 && buildIndex < SceneManager.sceneCountInBuildSettings)
        {
            StartCoroutine(FadeRoutine(() => SceneManager.LoadScene(buildIndex)));
        }
    }

    private IEnumerator FadeRoutine(System.Action loadScene)
    {
        isFading = true;
        Time.timeScale = 1f;

        if (fadeGroup != null)
        {
            fadeGroup.blocksRaycasts = true;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                fadeGroup.alpha = Mathf.Clamp01(elapsed / duration);
                yield return null;
            }
        }

        loadScene?.Invoke();
    }
}
