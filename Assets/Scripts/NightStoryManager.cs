using System.Collections.Generic;
using UnityEngine;

public class NightStoryManager : MonoBehaviour
{
    public static NightStoryManager Instance { get; private set; }

    [System.Serializable]
    public class NightStoryData
    {
        public int nightNumber = 1;
        public string briefingTitle;

        [Header("Report art")]
        public string briefingBackgroundResourcePath;
        public string completionBackgroundResourcePath;

        // Edit story reports here. Each item in these lists becomes one page in the report UI.
        [TextArea(4, 10)] public List<string> briefingPages = new List<string>();
        [TextArea(3, 8)] public List<string> completionPages = new List<string>();
        [TextArea(3, 8)] public List<string> failurePages = new List<string>();

        [Header("Fallback single-page text")]
        [TextArea(4, 10)] public string briefingText;
        [TextArea(3, 8)] public string completionText;
        [TextArea(3, 8)] public string failureText;
        public int nightQuota = 5;
        public float shiftDuration = 180f;
        public string difficulty = "Easy";

        public IReadOnlyList<string> GetBriefingPages()
        {
            return GetPagesOrFallback(briefingPages, briefingText);
        }

        public IReadOnlyList<string> GetCompletionPages()
        {
            return GetPagesOrFallback(completionPages, completionText);
        }

        public IReadOnlyList<string> GetFailurePages()
        {
            return GetPagesOrFallback(failurePages, failureText);
        }

        private IReadOnlyList<string> GetPagesOrFallback(List<string> pages, string fallback)
        {
            if (pages != null && pages.Count > 0)
            {
                return pages;
            }

            return new List<string> { fallback ?? "" };
        }
    }

    [SerializeField] private int currentNight = 1;
    [SerializeField] private bool showMainMenuOnFirstLoad = true;
    [SerializeField] private bool newGameStartsAtNightOne = true;
    [SerializeField] private bool showBriefingOnStart = true;
    [SerializeField] private List<NightStoryData> nights = new List<NightStoryData>
    {
        new NightStoryData
        {
            nightNumber = 1,
            briefingTitle = "FITA DE BRIEFING - TURNO FINAL",
            briefingPages = new List<string>
            {
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nOlá, funcionário.\nBem-vindo ao Setor Postal 7.\n\nEste é o seu turno completo na sala de armazenamento.\nDas 00:00 às 06:00, a estação pertence a você.\n\nNão peça transferência durante o expediente.",
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nAvalie cada encomenda e compare com o relatório.\n\nConfira FORMATO, CÓDIGO DE BARRAS, LOGOTIPO, FITA, DESTINO e PESO.\n\nAbra o relatório com E ou clique.\nUse A/D para girar a caixa.",
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nSe tudo estiver correto, pressione ENTER para APROVAR.\nSe houver qualquer diferença, pressione Q para REJEITAR.\n\nCada caixa pode chegar a 1 minuto.\nConforme o turno avança, o tempo diminui até 20 segundos.\nO painel da parede mostra hora, pedidos, erros e tempo restante.",
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nMantenha atenção nos sons ao redor.\nSe ouvir uma batida atrás de você, pressione S e olhe para o corredor.\n\nQuando estiver virado para trás, pressione F e procure com a lanterna.\n\nSe encontrar os olhos, mantenha a luz neles até a anomalia fugir."
            },
            completionPages = new List<string>
            {
                "FIM DO TURNO\n\n00:00 - 06:00\nPedidos processados.\nRegistro fechado.\n\nVocê sobreviveu ao turno."
            },
            failurePages = new List<string>
            {
                "TURNO FALHOU\n\nSeu arquivo não foi fechado.\nO Setor Postal 7 guardará o restante do procedimento."
            },
            briefingText = "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n\nBem-vindo ao Setor Postal 7. Avalie encomendas, compare com o relatório, use E ou clique para abrir o relatório, A/D para girar, ENTER para aprovar e Q para rejeitar. Cada caixa pode chegar a 1 minuto, diminuindo até 20 segundos conforme o turno avança. Se ouvir uma batida atrás de você, pressione S. Quando estiver olhando para o corredor, pressione F e procure com a lanterna até a anomalia fugir.",
            completionText = "FIM DO TURNO\n\n00:00 - 06:00\nPedidos processados. Registro fechado. Você sobreviveu ao turno.",
            failureText = "TURNO FALHOU\n\nSeu arquivo não foi fechado. O Setor Postal 7 guardará o restante do procedimento.",
            nightQuota = 10,
            shiftDuration = 360f,
            difficulty = "Final"
        }
    };

    public int CurrentNight => currentNight;
    public bool IsBriefingOpen { get; private set; }

    private NightStoryData activeNight;
    private bool nightStarted;
    private bool waitingForMainMenu;
    private static bool hasStartedFromMainMenuThisSession;
    private static bool skipBriefingOnNextGameplayLoad;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        NormalizeSingleShiftData();
    }

    private void NormalizeSingleShiftData()
    {
        var finalShift = new NightStoryData
        {
            nightNumber = 1,
            briefingTitle = "FITA DE BRIEFING - TURNO FINAL",
            briefingPages = new List<string>
            {
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nOlá, funcionário.\nBem-vindo ao Setor Postal 7.\n\nEste é o seu turno completo na sala de armazenamento.\nDas 00:00 às 06:00, a estação pertence a você.\n\nNão peça transferência durante o expediente.",
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nAvalie cada encomenda e compare com o relatório.\n\nConfira FORMATO, CÓDIGO DE BARRAS, LOGOTIPO, FITA, DESTINO e PESO.\n\nAbra o relatório com E ou clique.\nUse A/D para girar a caixa.",
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nSe tudo estiver correto, pressione ENTER para APROVAR.\nSe houver qualquer diferença, pressione Q para REJEITAR.\n\nCada caixa pode chegar a 1 minuto.\nConforme o turno avança, o tempo diminui até 20 segundos.\nO painel da parede mostra hora, pedidos, erros e tempo restante.",
                "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n____________________________\n\nMantenha atenção nos sons ao redor.\nSe ouvir uma batida atrás de você, pressione S e olhe para o corredor.\n\nQuando estiver virado para trás, pressione F e procure com a lanterna.\n\nSe encontrar os olhos, mantenha a luz neles até a anomalia fugir."
            },
            completionPages = new List<string>
            {
                "FIM DO TURNO\n\n00:00 - 06:00\nPedidos processados.\nRegistro fechado.\n\nVocê sobreviveu ao turno."
            },
            failurePages = new List<string>
            {
                "TURNO FALHOU\n\nSeu arquivo não foi fechado.\nO Setor Postal 7 guardará o restante do procedimento."
            },
            briefingText = "FITA DE BRIEFING - TURNO FINAL\nLOCUTOR - FUNCIONÁRIO #64\n\nBem-vindo ao Setor Postal 7. Avalie encomendas, compare com o relatório, use E ou clique para abrir o relatório, A/D para girar, ENTER para aprovar e Q para rejeitar. Cada caixa pode chegar a 1 minuto, diminuindo até 20 segundos conforme o turno avança. Se ouvir uma batida atrás de você, pressione S. Quando estiver olhando para o corredor, pressione F e procure com a lanterna até a anomalia fugir.",
            completionText = "FIM DO TURNO\n\n00:00 - 06:00\nPedidos processados. Registro fechado. Você sobreviveu ao turno.",
            failureText = "TURNO FALHOU\n\nSeu arquivo não foi fechado. O Setor Postal 7 guardará o restante do procedimento.",
            nightQuota = 10,
            shiftDuration = 360f,
            difficulty = "Final"
        };

        nights = new List<NightStoryData> { finalShift };
        currentNight = 1;
    }

    private void Start()
    {
        currentNight = 1;

        if (skipBriefingOnNextGameplayLoad)
        {
            skipBriefingOnNextGameplayLoad = false;
            hasStartedFromMainMenuThisSession = true;
            waitingForMainMenu = false;
            activeNight = GetNightData(currentNight);
            ApplyNightSettings(activeNight);
            BackgroundMusicManager.Instance?.PlayGameplayMusic(true);
            BeginGameplayAfterTutorial();
            return;
        }

        if (showMainMenuOnFirstLoad && !hasStartedFromMainMenuThisSession)
        {
            waitingForMainMenu = true;
            nightStarted = false;
            SetGameplayWaiting(true);

            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowMainMenu(StartNewGameFromMenu);
                return;
            }
        }

        if (showBriefingOnStart)
        {
            StartNight(currentNight);
        }
    }

    public void StartNewGameFromMenu()
    {
        MarkMainMenuCompletedForSession();
        waitingForMainMenu = false;

        if (newGameStartsAtNightOne)
        {
            currentNight = 1;
            NightManager.Instance?.ResetProgressToFirstNight();
        }

        StartNight(1);
    }

    public static void MarkMainMenuCompletedForSession()
    {
        hasStartedFromMainMenuThisSession = true;
    }

    public static void PrepareImmediateGameplayRestart()
    {
        hasStartedFromMainMenuThisSession = true;
        skipBriefingOnNextGameplayLoad = true;
    }

    public void StartNight(int night)
    {
        currentNight = 1;
        activeNight = GetNightData(currentNight);
        nightStarted = false;
        IsBriefingOpen = true;

        ApplyNightSettings(activeNight);
        SetGameplayWaiting(true);
        BackgroundMusicManager.Instance?.PlayBriefingMusic();

        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowBriefing(activeNight.briefingTitle, activeNight.GetBriefingPages(), BeginGameplay, activeNight.briefingBackgroundResourcePath);
        }
        else
        {
            BeginGameplay();
        }
    }

    public void CompleteNight()
    {
        NightStoryData data = activeNight ?? GetNightData(currentNight);
        IsBriefingOpen = false;
        SetGameplayWaiting(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowConclusion("FIM DO TURNO", data.GetCompletionPages(), "MENU PRINCIPAL", ContinueAfterConclusion, data.completionBackgroundResourcePath);
        }
    }

    public void FailNight()
    {
        NightStoryData data = activeNight ?? GetNightData(currentNight);
        IsBriefingOpen = false;
        SetGameplayWaiting(true);
        if (UIManager.Instance != null)
        {
            UIManager.Instance.ShowConclusion("TURNO FALHOU", data.GetFailurePages(), "MENU PRINCIPAL", ContinueAfterConclusion);
        }
    }

    public void ContinueAfterConclusion()
    {
        Time.timeScale = 1f;
        if (UIManager.Instance != null)
        {
            UIManager.Instance.GoToMainMenu();
            return;
        }

        UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");
    }

    public bool ShouldDelayGameplayStart()
    {
        return waitingForMainMenu || (showBriefingOnStart && !nightStarted);
    }

    private void BeginGameplay()
    {
        BeginGameplayAfterTutorial();
    }

    private void BeginGameplayAfterTutorial()
    {
        IsBriefingOpen = false;
        nightStarted = true;
        Time.timeScale = 1f;
        SetGameplayWaiting(false);
        AudioManager.Instance?.PlayDoorCreak();
        BackgroundMusicManager.Instance?.PlayGameplayMusic();
        InspectionStation.Instance?.StartNight();
    }

    private void ApplyNightSettings(NightStoryData data)
    {
        NightManager.Instance?.SetCurrentNight(1);

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        gameManager?.SetQuotaRequired(data.nightQuota);

        ShiftTimer timer = Object.FindFirstObjectByType<ShiftTimer>();
        timer?.SetTotalShiftDuration(data.shiftDuration);

        PackageManager packageManager = Object.FindFirstObjectByType<PackageManager>();
        if (packageManager != null)
        {
            packageManager.SetReportErrorChance(GetReportErrorChance(data.difficulty));
        }
    }

    private float GetReportErrorChance(string difficulty)
    {
        string normalized = string.IsNullOrWhiteSpace(difficulty) ? "easy" : difficulty.Trim().ToLowerInvariant();
        if (normalized == "final")
        {
            return 0.18f;
        }

        if (normalized == "hard")
        {
            return 0.36f;
        }

        if (normalized == "medium")
        {
            return 0.24f;
        }

        return 0.05f;
    }

    private void SetGameplayWaiting(bool waiting)
    {
        ShiftTimer timer = Object.FindFirstObjectByType<ShiftTimer>();
        timer?.SetTimerRunning(!waiting);
        if (waiting)
        {
            InspectionStation.Instance?.StopStation();
        }
    }

    private NightStoryData GetNightData(int night)
    {
        foreach (NightStoryData data in nights)
        {
            if (data.nightNumber == night)
            {
                return data;
            }
        }

        return nights.Count > 0 ? nights[0] : new NightStoryData();
    }
}
