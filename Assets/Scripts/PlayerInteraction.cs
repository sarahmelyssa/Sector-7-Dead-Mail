using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Lida com input direto do jogador: clique/E nos botoes, abrir report,
/// aceitar/rejeitar e rodar a caixa.
/// </summary>
public class PlayerInteraction : MonoBehaviour
{
    [SerializeField] private Camera playerCamera;
    [SerializeField] private float interactionDistance = 7f;
    [SerializeField] private LayerMask interactableMask = ~0;
    [SerializeField] private TMP_Text promptText;
    [SerializeField] private GameObject promptRoot;

    private PackageInteractable currentInteractable;
    private PhysicalButton currentPhysicalButton;
    private GameManager gameManager;

    private void Start()
    {
        interactionDistance = Mathf.Max(interactionDistance, 7f);

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        CreatePromptIfNeeded();
        HidePrompt();
        gameManager = Object.FindFirstObjectByType<GameManager>();
    }

    private void Update()
    {
        if (gameManager == null)
        {
            gameManager = Object.FindFirstObjectByType<GameManager>();
        }

        if (gameManager != null && !gameManager.IsPlaying)
        {
            currentInteractable = null;
            currentPhysicalButton = null;
            HidePrompt();
            return;
        }

        if (UIManager.Instance != null && UIManager.Instance.IsBlockingScreenOpen)
        {
            currentInteractable = null;
            currentPhysicalButton = null;
            HidePrompt();
            return;
        }

        UpdateRaycastTarget();
        HandleInputActions();
    }

    private void HandleInputActions()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;

        InspectionStation station = InspectionStation.Instance;
        if (station == null)
        {
            return;
        }

        if (mouse != null && mouse.leftButton.wasPressedThisFrame)
        {
            if (TryInteractFromMousePosition(mouse.position.ReadValue()) || TryInteractWithTarget())
            {
                return;
            }
        }

        if (keyboard == null)
        {
            return;
        }

        if (keyboard.eKey.wasPressedThisFrame)
        {
            if (TryInteractWithTarget())
            {
                return;
            }

            station.ToggleReport();
        }

        if (keyboard.qKey.wasPressedThisFrame)
        {
            station.RejectPackage();
        }

        if (keyboard.aKey.wasPressedThisFrame || keyboard.leftArrowKey.wasPressedThisFrame)
        {
            station.RotateLeft();
        }

        if (keyboard.dKey.wasPressedThisFrame || keyboard.rightArrowKey.wasPressedThisFrame)
        {
            station.RotateRight();
        }

        if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame)
        {
            station.AcceptPackage();
        }
    }

    private bool TryInteractWithTarget()
    {
        if (currentPhysicalButton != null)
        {
            currentPhysicalButton.Interact();
            return true;
        }

        if (currentInteractable != null)
        {
            currentInteractable.Interact();
            return true;
        }

        return false;
    }

    private bool TryInteractFromMousePosition(Vector2 screenPosition)
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                return false;
            }
        }

        Ray ray = playerCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableMask))
        {
            return false;
        }

        PhysicalButton physicalButton = hit.collider.GetComponentInParent<PhysicalButton>();
        if (physicalButton != null)
        {
            physicalButton.Interact();
            return true;
        }

        PackageInteractable interactable = hit.collider.GetComponentInParent<PackageInteractable>();
        if (interactable != null && interactable.CanInteract)
        {
            interactable.Interact();
            return true;
        }

        return false;
    }

    private void UpdateRaycastTarget()
    {
        // Raycast central para descobrir se o jogador esta a apontar para um botao.
        currentInteractable = null;
        currentPhysicalButton = null;

        if (playerCamera == null)
        {
            playerCamera = Camera.main;
            if (playerCamera == null)
            {
                HidePrompt();
                return;
            }
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, interactionDistance, interactableMask))
        {
            currentPhysicalButton = hit.collider.GetComponentInParent<PhysicalButton>();

            if (currentPhysicalButton == null)
            {
                currentInteractable = hit.collider.GetComponentInParent<PackageInteractable>();
                if (currentInteractable != null && !currentInteractable.CanInteract)
                {
                    currentInteractable = null;
                }
            }
        }

        HidePrompt();
    }

    private void CreatePromptIfNeeded()
    {
        if (promptText != null)
        {
            HidePrompt();
            return;
        }
    }

    private void ShowPrompt(string message)
    {
        HidePrompt();
    }

    private void HidePrompt()
    {
        if (promptRoot != null)
        {
            promptRoot.SetActive(false);
        }
        else if (promptText != null)
        {
            promptText.gameObject.SetActive(false);
        }
    }
}
