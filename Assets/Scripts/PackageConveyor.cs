using System.Collections;
using UnityEngine;

public class PackageConveyor : MonoBehaviour
{
    public static event System.Action<GameObject> PackageArrivedAtInspection;
    public static event System.Action<GameObject> PackageLeftInspection;
    public static event System.Action<GameObject> PackageExited;

    [SerializeField] private Transform entryPoint = null;
    [SerializeField] private Transform inspectionPoint = null;
    [SerializeField] private Transform exitPoint = null;
    [SerializeField] private Transform leftEntryPoint = null;
    [SerializeField] private Transform rightEntryPoint = null;
    [SerializeField] private Transform leftExitPoint = null;
    [SerializeField] private Transform rightExitPoint = null;
    [SerializeField] private GameObject packagePrefab = null;
    [SerializeField] private GameObject currentPackage = null;
    [SerializeField] private float moveSpeed = 1.55f;
    [SerializeField] private float arrivalPauseDuration = 0.16f;

    public bool isMoving { get; private set; }
    public bool IsPackageReadyForInspection { get; private set; }
    public GameObject CurrentPackage => currentPackage;

    private Coroutine moveRoutine;
    private GameManager gameManager;

    private void Awake()
    {
        gameManager = Object.FindFirstObjectByType<GameManager>();
        FindOrCreatePoints();
    }

    public void SetPoints(Transform entry, Transform inspection, Transform exit)
    {
        entryPoint = entry;
        inspectionPoint = inspection;
        exitPoint = exit;
    }

    public void ReceiveNewPackage(GameObject packageObject)
    {
        if (packageObject == null)
        {
            return;
        }

        StopActiveMove();
        currentPackage = packageObject;
        IsPackageReadyForInspection = false;

        if (entryPoint != null)
        {
            currentPackage.transform.position = entryPoint.position;
        }

        SetPackageInteractable(false);
        moveRoutine = StartCoroutine(MovePackageToInspectionPoint());
    }

    public GameObject SpawnPackage(PackageData packageData, PackageManager packageManager)
    {
        if (packageData == null || packageManager == null || currentPackage != null)
        {
            return null;
        }

        SelectRouteForNextPackage();
        Vector3 position = entryPoint != null ? entryPoint.position : transform.position;
        Quaternion rotation = entryPoint != null ? entryPoint.rotation : Quaternion.identity;
        GameObject packageObject = packageManager.CreatePackageObject(position, rotation, packageData, packagePrefab);
        ReceiveNewPackage(packageObject);
        AudioManager.Instance?.PlayPackageGenerated(packageObject.transform.position);
        return packageObject;
    }

    public void SendCurrentPackageToExit()
    {
        if (currentPackage == null || isMoving)
        {
            return;
        }

        PackageLeftInspection?.Invoke(currentPackage);
        AudioManager.Instance?.PlayPackageExiting(currentPackage.transform.position);
        StopActiveMove();
        IsPackageReadyForInspection = false;
        SetPackageInteractable(false);
        moveRoutine = StartCoroutine(MovePackageToExitPoint());
    }

    public void StopConveyor()
    {
        StopActiveMove();
        AudioManager.Instance?.StopConveyorLoop();
        isMoving = false;
        IsPackageReadyForInspection = false;
        SetPackageInteractable(false);
    }

    private IEnumerator MovePackageToInspectionPoint()
    {
        isMoving = true;
        if (currentPackage != null)
        {
            AudioManager.Instance?.StartConveyorLoop(currentPackage.transform.position);
        }
        yield return MoveCurrentPackage(inspectionPoint);
        AudioManager.Instance?.StopConveyorLoop();
        if (arrivalPauseDuration > 0f)
        {
            yield return new WaitForSeconds(arrivalPauseDuration);
        }
        isMoving = false;
        IsPackageReadyForInspection = currentPackage != null;
        SetPackageInteractable(IsPackageReadyForInspection);
        if (IsPackageReadyForInspection)
        {
            AudioManager.Instance?.PlayPackageArrived(currentPackage.transform.position);
            PackageArrivedAtInspection?.Invoke(currentPackage);
        }
        moveRoutine = null;
    }

    private IEnumerator MovePackageToExitPoint()
    {
        isMoving = true;
        if (currentPackage != null)
        {
            AudioManager.Instance?.StartConveyorLoop(currentPackage.transform.position);
        }
        yield return MoveCurrentPackage(exitPoint);
        AudioManager.Instance?.StopConveyorLoop();
        isMoving = false;

        if (currentPackage != null)
        {
            GameObject exitedPackage = currentPackage;
            Destroy(currentPackage);
            currentPackage = null;
            PackageExited?.Invoke(exitedPackage);
        }

        IsPackageReadyForInspection = false;
        moveRoutine = null;
    }

    private IEnumerator MoveCurrentPackage(Transform targetPoint)
    {
        if (currentPackage == null || targetPoint == null)
        {
            yield break;
        }

        while (currentPackage != null && Vector3.Distance(currentPackage.transform.position, targetPoint.position) > 0.025f)
        {
            if (gameManager == null)
            {
                gameManager = Object.FindFirstObjectByType<GameManager>();
            }

            if (gameManager != null && !gameManager.IsPlaying)
            {
                yield break;
            }

            currentPackage.transform.position = Vector3.MoveTowards(
                currentPackage.transform.position,
                targetPoint.position,
                moveSpeed * Time.deltaTime
            );

            AudioManager.Instance?.UpdateConveyorLoopPosition(currentPackage.transform.position);

            yield return null;
        }

        if (currentPackage != null)
        {
            currentPackage.transform.position = targetPoint.position;
        }
    }

    private void SetPackageInteractable(bool canInteract)
    {
        if (currentPackage == null)
        {
            return;
        }

        PackageInteractable interactable = currentPackage.GetComponent<PackageInteractable>();
        if (interactable != null)
        {
            interactable.SetCanInteract(canInteract);
        }
    }

    private void StopActiveMove()
    {
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }

        AudioManager.Instance?.StopConveyorLoop();
    }

    private void FindOrCreatePoints()
    {
        leftEntryPoint = leftEntryPoint != null
            ? leftEntryPoint
            : FindOrCreatePoint(new[] { "ConveyorEntryPoint_Left", "ConveyorEntryPoint" }, "ConveyorEntryPoint_Left", new Vector3(-2.45f, 1.39f, 0.62f));
        rightEntryPoint = rightEntryPoint != null
            ? rightEntryPoint
            : FindOrCreatePoint("ConveyorEntryPoint_Right", new Vector3(2.45f, 1.39f, 0.62f));
        leftExitPoint = leftExitPoint != null
            ? leftExitPoint
            : FindOrCreatePoint("ConveyorExitPoint_Left", new Vector3(-2.72f, 1.39f, 0.62f));
        rightExitPoint = rightExitPoint != null
            ? rightExitPoint
            : FindOrCreatePoint(new[] { "ConveyorExitPoint_Right", "ConveyorExitPoint" }, "ConveyorExitPoint_Right", new Vector3(2.72f, 1.39f, 0.62f));
        inspectionPoint = inspectionPoint != null
            ? inspectionPoint
            : FindOrCreatePoint(new[] { "PackageInspectionCenter", "ActivePackageSpawnPoint", "PackageSpawnPoint" }, "PackageInspectionCenter", new Vector3(0f, 1.39f, 0.62f));
        entryPoint = entryPoint != null ? entryPoint : leftEntryPoint;
        exitPoint = exitPoint != null ? exitPoint : rightExitPoint;
    }

    private void SelectRouteForNextPackage()
    {
        FindOrCreatePoints();

        entryPoint = leftEntryPoint;
        exitPoint = rightExitPoint;
    }

    private Transform FindOrCreatePoint(string pointName, Vector3 fallbackPosition)
    {
        return FindOrCreatePoint(new[] { pointName }, pointName, fallbackPosition);
    }

    private Transform FindOrCreatePoint(string[] pointNames, string createName, Vector3 fallbackPosition)
    {
        GameObject pointObject = null;
        if (pointNames != null)
        {
            foreach (string pointName in pointNames)
            {
                pointObject = GameObject.Find(pointName);
                if (pointObject != null)
                {
                    break;
                }
            }
        }

        if (pointObject == null)
        {
            pointObject = new GameObject(createName);
            pointObject.transform.position = fallbackPosition;
        }

        return pointObject.transform;
    }
}
