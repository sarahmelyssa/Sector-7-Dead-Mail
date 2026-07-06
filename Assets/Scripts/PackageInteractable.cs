using UnityEngine;

[RequireComponent(typeof(Collider))]
public class PackageInteractable : MonoBehaviour
{
    [SerializeField] private PackageData data;
    [SerializeField] private bool canInteract = true;

    public PackageData Data => data;
    public bool CanInteract => canInteract;

    public void SetData(PackageData packageData)
    {
        data = packageData;
    }

    public void SetCanInteract(bool enabled)
    {
        canInteract = enabled;
    }

    public void Interact()
    {
        if (!canInteract)
        {
            return;
        }

        GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
        if (gameManager != null && !gameManager.IsPlaying)
        {
            return;
        }

        InspectionStation.Instance?.ToggleReport();
    }
}
