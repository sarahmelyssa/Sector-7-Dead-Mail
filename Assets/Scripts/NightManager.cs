using System.Collections.Generic;
using UnityEngine;

public class NightManager : MonoBehaviour
{
    public static NightManager Instance { get; private set; }

    private const string CurrentNightKey = "PackageInspection_CurrentNight";
    private const string UnlockedNightKey = "PackageInspection_UnlockedNight";

    [System.Serializable]
    public class NightSettings
    {
        public int nightNumber = 1;
        public int quotaRequired = 10;
        public float shiftDuration = 180f;
        [Range(0f, 1f)] public float reportErrorChance = 0.05f;
    }

    [SerializeField] private int currentNight = 1;
    [SerializeField] private int unlockedNight = 1;
    [SerializeField] private List<NightSettings> nights = new List<NightSettings>
    {
        new NightSettings { nightNumber = 1, quotaRequired = 10, shiftDuration = 360f, reportErrorChance = 0.18f }
    };

    public int CurrentNight => currentNight;
    public int UnlockedNight => unlockedNight;
    public NightSettings CurrentSettings { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        NormalizeSingleShiftSettings();
        LoadProgress();
        CurrentSettings = GetSettingsForNight(currentNight);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

#if UNITY_INCLUDE_TESTS
    public static void ResetInstanceForTests()
    {
        Instance = null;
    }
#endif

    private void Start()
    {
        ApplyNightSettings();
    }

    public void ApplyNightSettings()
    {
        NormalizeSingleShiftSettings();
        CurrentSettings = GetSettingsForNight(currentNight);

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        gameManager?.SetQuotaRequired(CurrentSettings.quotaRequired);

        ShiftTimer shiftTimer = Object.FindFirstObjectByType<ShiftTimer>();
        shiftTimer?.SetTotalShiftDuration(CurrentSettings.shiftDuration);

        PackageManager packageManager = Object.FindFirstObjectByType<PackageManager>();
        packageManager?.SetReportErrorChance(CurrentSettings.reportErrorChance);
    }

    public void UnlockNextNight()
    {
        currentNight = 1;
        unlockedNight = 1;
        PlayerPrefs.SetInt(CurrentNightKey, 1);
        PlayerPrefs.SetInt(UnlockedNightKey, 1);
        PlayerPrefs.Save();
    }

    public void SetCurrentNight(int night)
    {
        currentNight = 1;
        unlockedNight = 1;
        PlayerPrefs.SetInt(CurrentNightKey, 1);
        PlayerPrefs.SetInt(UnlockedNightKey, 1);
        PlayerPrefs.Save();
        ApplyNightSettings();
    }

    public void ResetProgressToFirstNight()
    {
        currentNight = 1;
        unlockedNight = 1;
        PlayerPrefs.SetInt(CurrentNightKey, currentNight);
        PlayerPrefs.SetInt(UnlockedNightKey, unlockedNight);
        PlayerPrefs.Save();
        ApplyNightSettings();
    }

    private void LoadProgress()
    {
        NormalizeSingleShiftSettings();
        currentNight = 1;
        unlockedNight = 1;
        PlayerPrefs.SetInt(CurrentNightKey, 1);
        PlayerPrefs.SetInt(UnlockedNightKey, 1);
        PlayerPrefs.Save();
    }

    private void NormalizeSingleShiftSettings()
    {
        currentNight = 1;
        unlockedNight = 1;
        nights = new List<NightSettings>
        {
            new NightSettings { nightNumber = 1, quotaRequired = 10, shiftDuration = 360f, reportErrorChance = 0.18f }
        };
    }

    private NightSettings GetSettingsForNight(int night)
    {
        foreach (NightSettings settings in nights)
        {
            if (settings.nightNumber == night)
            {
                return settings;
            }
        }

        return nights.Count > 0 ? nights[0] : new NightSettings();
    }

    private int GetHighestConfiguredNight()
    {
        int highestNight = 1;

        foreach (NightSettings settings in nights)
        {
            highestNight = Mathf.Max(highestNight, settings.nightNumber);
        }

        return highestNight;
    }
}
