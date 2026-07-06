using UnityEngine;

public class DecisionManager : MonoBehaviour
{
    public static event System.Action<bool> DecisionResolved;

    [SerializeField] private GameManager gameManager = null;

    private void Awake()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }
    }

    public bool SubmitDecision(PackageData packageData, bool accepted)
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || !gameManager.IsPlaying || packageData == null)
        {
            return false;
        }

        if (accepted)
        {
            AudioManager.Instance?.PlayAcceptButton();
        }
        else
        {
            AudioManager.Instance?.PlayRejectButton();
        }

        bool correct = gameManager.RegisterPackageDecision(packageData, accepted);
        if (correct)
        {
            AudioManager.Instance?.PlayCorrectResponse();
        }
        else
        {
            AudioManager.Instance?.PlayWrongResponse();
        }

        DecisionResolved?.Invoke(correct);
        return correct;
    }

    public bool SubmitTimeout(PackageData packageData)
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager == null || !gameManager.IsPlaying || packageData == null)
        {
            return false;
        }

        bool forcedWrongDecision = packageData.ShouldReject;
        bool correct = gameManager.RegisterPackageDecision(packageData, forcedWrongDecision);
        AudioManager.Instance?.PlayWrongResponse();
        DecisionResolved?.Invoke(false);
        return correct;
    }
}
