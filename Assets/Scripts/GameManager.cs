using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public enum GameState
{
    Playing,
    Victory,
    GameOver
}

public class GameManager : MonoBehaviour
{
    public int quotaNecessaria = 3;
    [SerializeField] private int maxWrongDecisions = 3;

    public int quotaAtual { get; private set; }
    public int dangerLevel => 0;
    public GameState CurrentState { get; private set; } = GameState.Playing;
    public bool IsPlaying => CurrentState == GameState.Playing;
    public bool isGameOver => CurrentState == GameState.GameOver;
    public int WrongDecisionCount => wrongDecisionCount;
    public int MaxWrongDecisions => Mathf.Max(1, maxWrongDecisions);

    private GameOverUI gameOverUI;
    private GameObject endScreen;
    private Text endScreenText;
    private int wrongDecisionCount;

    private void Awake()
    {
        gameOverUI = Object.FindFirstObjectByType<GameOverUI>();
        if (gameOverUI == null)
        {
            gameOverUI = gameObject.AddComponent<GameOverUI>();
        }

        BuildEndScreen();
    }

    private void Update()
    {
        if (!IsPlaying && Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
        {
            RestartGame();
        }
    }

    public bool RegisterPackageDecision(PackageData packageData, bool accepted)
    {
        if (!IsPlaying)
        {
            return false;
        }

        bool correctDecision = accepted != packageData.ShouldReject;

        if (correctDecision)
        {
            quotaAtual++;
            if (quotaAtual >= quotaNecessaria)
            {
                WinGame();
            }
        }
        else
        {
            wrongDecisionCount++;
            if (wrongDecisionCount >= Mathf.Max(1, maxWrongDecisions))
            {
                GameOver("Erros demais foram registrados.");
            }
        }

        return correctDecision;
    }

    public void AddDanger()
    {
        // Danger/anomaly gameplay has been removed. Keep this as a no-op for old debug/UI callers.
    }

    public void WinGame()
    {
        if (!IsPlaying)
        {
            return;
        }

        CurrentState = GameState.Victory;
        StopGameplaySystems();
        AudioManager.Instance?.PlayVictory();

        EndGameFlowManager endFlow = EndGameFlowManager.Instance != null ? EndGameFlowManager.Instance : Object.FindFirstObjectByType<EndGameFlowManager>();
        if (endFlow != null)
        {
            endFlow.PlayWinSequenceWithVideo();
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictory();
        }
        else
        {
            ShowEndScreen("FIM DO TURNO\n\nVocê sobreviveu ao turno.\n\nPressione R para reiniciar.");
        }
    }

    public void DebugForceWinSequence()
    {
        CurrentState = GameState.Victory;
        StopGameplaySystems();
        AudioManager.Instance?.PlayVictory();

        EndGameFlowManager endFlow = EndGameFlowManager.Instance != null ? EndGameFlowManager.Instance : Object.FindFirstObjectByType<EndGameFlowManager>();
        if (endFlow != null)
        {
            endFlow.PlayWinSequenceWithVideo();
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowVictory();
        }
        else
        {
            ShowEndScreen("FIM DO TURNO\n\nVocê sobreviveu ao turno.\n\nPressione R para reiniciar.");
        }
    }

    public void DebugJumpToOnePackageBeforeVictory()
    {
        if (!IsPlaying)
        {
            return;
        }

        quotaAtual = Mathf.Max(0, quotaNecessaria - 1);
    }

    public void ResolveShiftEnd()
    {
        if (!IsPlaying)
        {
            return;
        }

        if (quotaAtual >= quotaNecessaria)
        {
            WinGame();
        }
        else
        {
            GameOver("Shift timer expired before quota was met.");
        }
    }

    public void GameOver()
    {
        GameOver("O turno falhou.");
    }

    public void GameOver(string reason)
    {
        if (!IsPlaying)
        {
            return;
        }

        CurrentState = GameState.GameOver;
        StopGameplaySystems();
        AudioManager.Instance?.PlayGameOver();

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Time.timeScale = 0f;

        if (gameOverUI == null)
        {
            gameOverUI = Object.FindFirstObjectByType<GameOverUI>();
        }

        EndGameFlowManager endFlow = EndGameFlowManager.Instance != null ? EndGameFlowManager.Instance : Object.FindFirstObjectByType<EndGameFlowManager>();
        if (endFlow != null)
        {
            endFlow.ShowGameOverScreen();
        }
        else if (gameOverUI != null)
        {
            gameOverUI.Show(reason);
        }
        else if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowGameOver();
        }
        else
        {
            ShowEndScreen("SHIFT FAILED\n\n" + reason + "\n\nPress R to restart.");
        }
    }

    public void RestartGame()
    {
        Time.timeScale = 1f;
        NightStoryManager.PrepareImmediateGameplayRestart();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void SetQuotaRequired(int quotaRequired)
    {
        quotaNecessaria = Mathf.Max(1, quotaRequired);
    }

    private void BuildEndScreen()
    {
        var canvasObject = new GameObject("Game End Screen");
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        endScreen = new GameObject("End Screen Panel");
        endScreen.transform.SetParent(canvasObject.transform, false);
        var panelRect = endScreen.AddComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = Vector2.zero;
        panelRect.offsetMax = Vector2.zero;

        var image = endScreen.AddComponent<Image>();
        image.color = new Color(0f, 0f, 0f, 0.82f);

        var textObject = new GameObject("End Screen Text");
        textObject.transform.SetParent(endScreen.transform, false);
        var textRect = textObject.AddComponent<RectTransform>();
        textRect.anchorMin = new Vector2(0.5f, 0.5f);
        textRect.anchorMax = new Vector2(0.5f, 0.5f);
        textRect.anchoredPosition = Vector2.zero;
        textRect.sizeDelta = new Vector2(760f, 360f);

        endScreenText = textObject.AddComponent<Text>();
        endScreenText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        endScreenText.fontSize = 46;
        endScreenText.alignment = TextAnchor.MiddleCenter;
        endScreenText.color = new Color(0.92f, 0.88f, 0.72f);

        endScreen.SetActive(false);
    }

    private void ShowEndScreen(string message)
    {
        if (endScreenText != null)
        {
            endScreenText.text = message;
        }

        if (endScreen != null)
        {
            endScreen.SetActive(true);
        }
    }

    private void StopGameplaySystems()
    {
        InspectionStation.Instance?.StopStation();
        PackageManager.Instance?.StopPackageFlow();
        InspectionUI.Instance?.CloseInspection();

        ShiftTimer shiftTimer = Object.FindFirstObjectByType<ShiftTimer>();
        shiftTimer?.SetTimerRunning(false);
    }
}
