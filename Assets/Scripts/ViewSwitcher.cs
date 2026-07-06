using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

public class ViewSwitcher : MonoBehaviour
{
    public static ViewSwitcher Instance { get; private set; }
    public static event System.Action<bool> ViewTurnStarted;
    public static event System.Action<bool> ViewChanged;

    [SerializeField] private Transform frontView = null;
    [SerializeField] private Transform backView = null;
    [SerializeField] private Camera playerCamera = null;
    [SerializeField] private float switchSpeed = 6f;
    [SerializeField] private float turnDuration = 0.85f;
    [SerializeField] private float lookSensitivity = 0.24f;
    [SerializeField] private float maxYawOffset = 18f;
    [SerializeField] private float minPitchOffset = -8f;
    [SerializeField] private float maxPitchOffset = 10f;
    private bool lookingBack;
    private bool isTurning;
    private float yawOffset;
    private float pitchOffset;

    public bool IsLookingBack => lookingBack;
    public bool IsTurning => isTurning;
    public bool ControlsCamera(Camera cameraToCheck) => playerCamera != null && playerCamera == cameraToCheck;

    private void Awake()
    {
        Instance = this;
        ResolveReferences();
    }

    private void Start()
    {
        ResolveReferences();
        SnapToCurrentView();
    }

    private void Update()
    {
        ResolveReferences();

        Transform targetView = GetCurrentView();
        if (playerCamera == null || targetView == null)
        {
            return;
        }

        if (isTurning)
        {
            return;
        }

        UpdateMouseLook();

        float t = 1f - Mathf.Exp(-switchSpeed * Time.deltaTime);
        Quaternion targetRotation = targetView.rotation * Quaternion.Euler(pitchOffset, yawOffset, 0f);
        playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetView.position, t);
        playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetRotation, t);
    }

    public void ToggleView()
    {
        if (isTurning)
        {
            return;
        }

        SetLookingBack(!lookingBack);
    }

    public void SetLookingBack(bool shouldLookBack)
    {
        if (isTurning)
        {
            return;
        }

        if (shouldLookBack && backView == null)
        {
            ResolveReferences();
        }

        bool targetLookingBack = shouldLookBack && backView != null;
        if (targetLookingBack == lookingBack)
        {
            return;
        }

        yawOffset = 0f;
        pitchOffset = 0f;
        AudioManager.Instance?.PlayLookTurn(targetLookingBack);
        ViewTurnStarted?.Invoke(targetLookingBack);
        StartCoroutine(TurnToView(targetLookingBack));
    }

    public void SetViews(Transform front, Transform back)
    {
        frontView = front;
        backView = back;
        SnapToCurrentView();
    }

    public void SetCamera(Camera cameraToUse)
    {
        playerCamera = cameraToUse;
        SnapToCurrentView();
    }

    private void ResolveReferences()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (frontView == null)
        {
            GameObject front = GameObject.Find("FrontView");
            frontView = front != null ? front.transform : null;
        }

        if (backView == null)
        {
            GameObject back = GameObject.Find("BackView");
            backView = back != null ? back.transform : null;
        }
    }

    private void SnapToCurrentView()
    {
        Transform targetView = GetCurrentView();
        if (playerCamera == null || targetView == null)
        {
            return;
        }

        playerCamera.transform.position = targetView.position;
        playerCamera.transform.rotation = targetView.rotation * Quaternion.Euler(pitchOffset, yawOffset, 0f);
    }

    private Transform GetCurrentView()
    {
        return lookingBack && backView != null ? backView : frontView;
    }

    private IEnumerator TurnToView(bool targetLookingBack)
    {
        Transform targetView = targetLookingBack && backView != null ? backView : frontView;
        if (playerCamera == null || targetView == null)
        {
            lookingBack = targetLookingBack;
            SnapToCurrentView();
            ViewChanged?.Invoke(lookingBack);
            yield break;
        }

        isTurning = true;
        Vector3 startPosition = playerCamera.transform.position;
        Quaternion startRotation = playerCamera.transform.rotation;
        Vector3 targetPosition = targetView.position;
        Quaternion targetRotation = targetView.rotation;
        float duration = Mathf.Clamp(turnDuration, 0.8f, 1.5f);
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float eased = t * t * (3f - 2f * t);
            playerCamera.transform.position = Vector3.Lerp(startPosition, targetPosition, eased);
            playerCamera.transform.rotation = Quaternion.Slerp(startRotation, targetRotation, eased);
            yield return null;
        }

        lookingBack = targetLookingBack;
        playerCamera.transform.position = targetPosition;
        playerCamera.transform.rotation = targetRotation;
        isTurning = false;
        ViewChanged?.Invoke(lookingBack);
    }

    private void UpdateMouseLook()
    {
        Mouse mouse = Mouse.current;
        if (mouse == null || (Cursor.lockState != CursorLockMode.Locked && !mouse.rightButton.isPressed))
        {
            return;
        }

        Vector2 delta = mouse.delta.ReadValue();
        yawOffset = Mathf.Clamp(yawOffset + delta.x * lookSensitivity, -maxYawOffset, maxYawOffset);
        pitchOffset = Mathf.Clamp(pitchOffset - delta.y * lookSensitivity, minPitchOffset, maxPitchOffset);
    }
}
