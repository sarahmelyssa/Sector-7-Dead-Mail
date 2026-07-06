using System.Collections.Generic;
using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PackageDecisionFlowTests
{
    private const string CurrentNightKey = "PackageInspection_CurrentNight";
    private const string UnlockedNightKey = "PackageInspection_UnlockedNight";

    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private bool hadCurrentNight;
    private bool hadUnlockedNight;
    private int savedCurrentNight;
    private int savedUnlockedNight;

    [SetUp]
    public void SetUp()
    {
        hadCurrentNight = PlayerPrefs.HasKey(CurrentNightKey);
        hadUnlockedNight = PlayerPrefs.HasKey(UnlockedNightKey);
        savedCurrentNight = PlayerPrefs.GetInt(CurrentNightKey, 1);
        savedUnlockedNight = PlayerPrefs.GetInt(UnlockedNightKey, 1);
        PlayerPrefs.DeleteKey(CurrentNightKey);
        PlayerPrefs.DeleteKey(UnlockedNightKey);

        NightManager.ResetInstanceForTests();
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        NightManager.ResetInstanceForTests();
    }

    [TearDown]
    public void TearDown()
    {
        Time.timeScale = 1f;
        RestorePlayerPrefs();

        foreach (GameObject createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.DestroyImmediate(createdObject);
            }
        }

        createdObjects.Clear();
    }

    [Test]
    public void PackageWithoutValidationErrorsCanBeAccepted()
    {
        PackageData package = CreateLegacyPackage(new List<string>());

        Assert.That(package.ShouldReject, Is.False);
        AssertDecision(package, accepted: true, expectedCorrect: true);
    }

    [Test]
    public void PackageWithValidationErrorsMustBeRejected()
    {
        PackageData package = CreateLegacyPackage(new List<string>
        {
            "Destino inexistente"
        });

        Assert.That(package.ShouldReject, Is.True);
        AssertDecision(package, accepted: false, expectedCorrect: true);
    }

    [Test]
    public void VisualMismatchRefreshesRejectionReasons()
    {
        var package = new PackageData
        {
            reportShape = "Small Box",
            boxShape = "small box ",
            reportBarcode = "PKG-123-F",
            boxBarcode = "PKG-123-F",
            reportLogo = "North Annex",
            boxLogo = "Night Archive",
            reportTapeColor = "Red",
            boxTapeColor = "red",
            reportDestination = "Cold Storage",
            boxDestination = "Cold Storage",
            reportWeight = "4.0kg",
            boxWeight = "4.0KG"
        };

        package.RefreshValidationReasons();

        Assert.That(package.ShouldReject, Is.True);
        Assert.That(package.rejectionReasons, Is.EquivalentTo(new[] { "Logo mismatch" }));
    }

    [Test]
    public void CorrectDecisionsIncreaseQuotaAndCanTriggerVictory()
    {
        GameManager gameManager = CreateGameManager();
        gameManager.SetQuotaRequired(2);

        bool firstDecision = gameManager.RegisterPackageDecision(CreateLegacyPackage(new List<string>()), accepted: true);
        bool secondDecision = gameManager.RegisterPackageDecision(CreateLegacyPackage(new List<string>()), accepted: true);

        Assert.That(firstDecision, Is.True);
        Assert.That(secondDecision, Is.True);
        Assert.That(gameManager.quotaAtual, Is.EqualTo(2));
        Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.Victory));
    }

    [Test]
    public void WrongDecisionDoesNotIncreaseQuota()
    {
        GameManager gameManager = CreateGameManager();

        bool correctDecision = gameManager.RegisterPackageDecision(CreateLegacyPackage(new List<string>
        {
            "Peso acima do permitido"
        }), accepted: true);

        Assert.That(correctDecision, Is.False);
        Assert.That(gameManager.quotaAtual, Is.EqualTo(0));
        Assert.That(gameManager.dangerLevel, Is.EqualTo(0));
        Assert.That(gameManager.CurrentState, Is.EqualTo(GameState.Playing));
    }

    [Test]
    public void ShiftProgressAlwaysStaysOnSingleNight()
    {
        NightManager nightManager = CreateNightManager();

        nightManager.ResetProgressToFirstNight();
        Assert.That(nightManager.CurrentNight, Is.EqualTo(1));
        Assert.That(nightManager.UnlockedNight, Is.EqualTo(1));

        nightManager.UnlockNextNight();

        Assert.That(nightManager.CurrentNight, Is.EqualTo(1));
        Assert.That(nightManager.UnlockedNight, Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(CurrentNightKey), Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(UnlockedNightKey), Is.EqualTo(1));
    }

    [Test]
    public void CurrentNightCannotLeaveSingleShift()
    {
        NightManager nightManager = CreateNightManager();
        nightManager.UnlockNextNight();

        nightManager.SetCurrentNight(3);

        Assert.That(nightManager.CurrentNight, Is.EqualTo(1));
        Assert.That(PlayerPrefs.GetInt(CurrentNightKey), Is.EqualTo(1));
    }

    [Test]
    public void NightSettingsUseSingleFinalShiftQuota()
    {
        NightManager nightManager = CreateNightManager();

        nightManager.ApplyNightSettings();

        Assert.That(nightManager.CurrentSettings.quotaRequired, Is.EqualTo(10));
        Assert.That(nightManager.CurrentSettings.shiftDuration, Is.EqualTo(360f));

        nightManager.UnlockNextNight();
        nightManager.ApplyNightSettings();

        Assert.That(nightManager.CurrentSettings.quotaRequired, Is.EqualTo(10));
        Assert.That(nightManager.CurrentSettings.shiftDuration, Is.EqualTo(360f));
    }

    private void AssertDecision(PackageData package, bool accepted, bool expectedCorrect)
    {
        GameManager gameManager = CreateGameManager();

        bool correctDecision = gameManager.RegisterPackageDecision(package, accepted);

        Assert.That(correctDecision, Is.EqualTo(expectedCorrect));
    }

    private GameManager CreateGameManager()
    {
        var gameObject = new GameObject("GameManager Test Host");
        createdObjects.Add(gameObject);
        return gameObject.AddComponent<GameManager>();
    }

    private NightManager CreateNightManager()
    {
        var gameObject = new GameObject("NightManager Test Host");
        createdObjects.Add(gameObject);
        return gameObject.AddComponent<NightManager>();
    }

    private void RestorePlayerPrefs()
    {
        if (hadCurrentNight)
        {
            PlayerPrefs.SetInt(CurrentNightKey, savedCurrentNight);
        }
        else
        {
            PlayerPrefs.DeleteKey(CurrentNightKey);
        }

        if (hadUnlockedNight)
        {
            PlayerPrefs.SetInt(UnlockedNightKey, savedUnlockedNight);
        }
        else
        {
            PlayerPrefs.DeleteKey(UnlockedNightKey);
        }

        PlayerPrefs.Save();
    }

    private static PackageData CreateLegacyPackage(List<string> rejectionReasons)
    {
        return new PackageData(
            "North Annex",
            "Sorting Room",
            4.2f,
            "documents",
            "PKG-123-G",
            "Scan limpo. Peso declarado confere. Rota confirmada.",
            rejectionReasons
        );
    }
}
