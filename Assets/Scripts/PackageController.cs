using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(BoxCollider))]
public class PackageController : MonoBehaviour
{
    [SerializeField] private float conveyorSpeed = 2.35f;

    private Rigidbody packageBody;
    private CheckpointGameManager gameManager;
    private Vector3 targetPoint;
    private Vector3 dispatchExit;
    private Vector3 quarantineExit;
    private bool movingToScanner;
    private bool leaving;

    public ParcelProfile Profile { get; private set; }

    public void Configure(CheckpointGameManager manager, ParcelProfile profile, Vector3 scannerPoint, Vector3 approveExit, Vector3 rejectExit)
    {
        gameManager = manager;
        Profile = profile;
        targetPoint = scannerPoint;
        dispatchExit = approveExit;
        quarantineExit = rejectExit;
        movingToScanner = true;
        leaving = false;
    }

    public void Leave(bool dispatched)
    {
        targetPoint = dispatched ? dispatchExit : quarantineExit;
        movingToScanner = false;
        leaving = true;
    }

    private void Awake()
    {
        packageBody = GetComponent<Rigidbody>();
        packageBody.freezeRotation = true;
        packageBody.interpolation = RigidbodyInterpolation.Interpolate;

        var box = GetComponent<BoxCollider>();
        box.size = Vector3.one;
        box.center = Vector3.zero;
    }

    private void FixedUpdate()
    {
        if (!movingToScanner && !leaving)
        {
            return;
        }

        Vector3 flatTarget = new Vector3(targetPoint.x, packageBody.position.y, targetPoint.z);
        Vector3 next = Vector3.MoveTowards(packageBody.position, flatTarget, conveyorSpeed * Time.fixedDeltaTime);
        packageBody.MovePosition(next);

        if (leaving && Vector3.Distance(packageBody.position, flatTarget) < 0.15f)
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!movingToScanner || !other.CompareTag("InspectionZone"))
        {
            return;
        }

        movingToScanner = false;
        gameManager.BeginInspection(this);
    }
}
