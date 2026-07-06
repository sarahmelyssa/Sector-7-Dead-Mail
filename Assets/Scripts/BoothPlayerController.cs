using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(CapsuleCollider))]
public class BoothPlayerController : MonoBehaviour
{
    [SerializeField] private float lookSensitivity = 0.10f;
    [SerializeField] private float turnSpeed = 9f;

    private Rigidbody playerBody;
    private Camera playerCamera;
    private float pitch;
    private float yawOffset;
    private float baseYaw;
    private bool lookEnabled = true;

    public void Configure(Camera cameraToUse)
    {
        playerCamera = cameraToUse;

        playerCamera.transform.SetParent(transform);
        playerCamera.transform.localPosition = new Vector3(0f, 1.44f, 0f);
        playerCamera.transform.localRotation = Quaternion.Euler(11f, 0f, 0f);
        pitch = 11f;
    }

    public void Configure(Camera cameraToUse, CheckpointGameManager manager)
    {
        Configure(cameraToUse);
    }

    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;
    }

    private void Awake()
    {
        playerBody = GetComponent<Rigidbody>();
        playerBody.isKinematic = true;
        playerBody.interpolation = RigidbodyInterpolation.Interpolate;

        var capsule = GetComponent<CapsuleCollider>();
        capsule.height = 1.7f;
        capsule.radius = 0.25f;
        capsule.center = new Vector3(0f, 0.85f, 0f);
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void Update()
    {
        var keyboard = Keyboard.current;
        var mouse = Mouse.current;

        if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (keyboard != null && Time.timeScale > 0f && keyboard.sKey.wasPressedThisFrame)
        {
            ViewSwitcher.Instance?.ToggleView();
        }

        if (ViewSwitcher.Instance != null && ViewSwitcher.Instance.ControlsCamera(playerCamera))
        {
            return;
        }

        if (!lookEnabled || mouse == null || Cursor.lockState != CursorLockMode.Locked)
        {
            return;
        }

        Vector2 mouseDelta = mouse.delta.ReadValue();
        baseYaw = Mathf.LerpAngle(baseYaw, 0f, turnSpeed * Time.deltaTime);
        yawOffset = Mathf.Clamp(yawOffset + mouseDelta.x * lookSensitivity, -58f, 58f);
        pitch = Mathf.Clamp(pitch - mouseDelta.y * lookSensitivity, -18f, 34f);

        transform.rotation = Quaternion.Euler(0f, baseYaw + yawOffset, 0f);
        if (playerCamera != null)
        {
            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
