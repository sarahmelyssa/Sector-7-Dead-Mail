using System.Collections;
using UnityEngine;

public enum ButtonType
{
    Accept,
    Reject,
    RotateLeft,
    RotateRight,
    ToggleReport
}

/// <summary>
/// Representa os botoes fisicos da bancada. Cada botao chama uma acao da
/// InspectionStation e toca/animacao de clique.
/// </summary>
[RequireComponent(typeof(Collider))]
public class PhysicalButton : MonoBehaviour
{
    [SerializeField] private ButtonType buttonType;
    [SerializeField] private InspectionStation inspectionStation = null;
    [SerializeField] private Transform buttonTop = null;
    [SerializeField] private Renderer buttonRenderer = null;
    [SerializeField] private Renderer iconRenderer = null;
    [SerializeField] private Texture normalTexture = null;
    [SerializeField] private Texture pressedTexture = null;
    [SerializeField] private float pressDepth = 0.025f;
    [SerializeField] private float pressDuration = 0.12f;
    [SerializeField] private bool animatePress = true;

    public string PromptText => "Pressiona E - " + GetActionLabel();

    private Vector3 originalLocalPosition;
    private Coroutine pressRoutine;
    private Material buttonMaterial;
    private Material iconMaterial;

    public void Configure(ButtonType type, InspectionStation station)
    {
        buttonType = type;
        inspectionStation = station;
        ResolveVisualReferences();
        LoadDefaultTexturesIfNeeded();
        ApplyDefaultVisuals();
    }

    private void Awake()
    {
        if (inspectionStation == null)
        {
            inspectionStation = Object.FindFirstObjectByType<InspectionStation>();
        }

        ResolveVisualReferences();
        LoadDefaultTexturesIfNeeded();
        originalLocalPosition = buttonTop.localPosition;
        ApplyDefaultVisuals();
    }

    public void Interact()
    {
        if (pressRoutine != null)
        {
            return;
        }

        if (inspectionStation == null)
        {
            inspectionStation = Object.FindFirstObjectByType<InspectionStation>();
        }

        if (inspectionStation == null)
        {
            return;
        }

        if (animatePress)
        {
            pressRoutine = StartCoroutine(AnimateButtonPress());
        }
        else
        {
            SetPressedVisual(false);
        }

        AudioManager.Instance?.PlayButtonClick();

        // Um unico componente serve para aceitar, rejeitar, rodar e abrir report.
        switch (buttonType)
        {
            case ButtonType.Accept:
                inspectionStation.AcceptPackage();
                break;
            case ButtonType.Reject:
                inspectionStation.RejectPackage();
                break;
            case ButtonType.RotateLeft:
                inspectionStation.RotateLeft();
                break;
            case ButtonType.RotateRight:
                inspectionStation.RotateRight();
                break;
            case ButtonType.ToggleReport:
                inspectionStation.ToggleReport();
                break;
        }
    }

    private IEnumerator AnimateButtonPress()
    {
        SetPressedVisual(true);
        Vector3 pressedPosition = originalLocalPosition + Vector3.down * pressDepth;
        yield return MoveButtonTop(pressedPosition, pressDuration);
        yield return MoveButtonTop(originalLocalPosition, pressDuration);
        SetPressedVisual(false);
        pressRoutine = null;
    }

    private IEnumerator MoveButtonTop(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = buttonTop.localPosition;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            buttonTop.localPosition = Vector3.Lerp(startPosition, targetPosition, elapsed / duration);
            yield return null;
        }

        buttonTop.localPosition = targetPosition;
    }

    private void ResolveVisualReferences()
    {
        Transform top = transform.Find("ButtonTop");
        if (top != null)
        {
            buttonTop = top;
        }

        if (buttonTop == null)
        {
            buttonTop = transform;
        }

        if (buttonRenderer == null && buttonTop != null)
        {
            buttonRenderer = buttonTop.GetComponent<Renderer>();
        }

        if (buttonRenderer == null)
        {
            buttonRenderer = GetComponentInChildren<Renderer>();
        }

        if (iconRenderer == null)
        {
            Transform icon = transform.Find("ButtonIconPlane");
            iconRenderer = icon != null ? icon.GetComponent<Renderer>() : null;
        }
    }

    private void LoadDefaultTexturesIfNeeded()
    {
        if (normalTexture == null)
        {
            normalTexture = Resources.Load<Texture2D>("InspectionStationButtons/button_sprites_png/" + GetTextureName(false));
        }

        if (pressedTexture == null)
        {
            pressedTexture = Resources.Load<Texture2D>("InspectionStationButtons/button_sprites_png/" + GetTextureName(true));
        }
    }

    private void ApplyDefaultVisuals()
    {
        ApplyButtonColor();
        SetPressedVisual(false);
    }

    private void ApplyButtonColor()
    {
        if (buttonRenderer == null)
        {
            return;
        }

        buttonMaterial = CreateMaterial(false);
        SetMaterialColor(buttonMaterial, GetButtonColor());
        buttonRenderer.sharedMaterial = buttonMaterial;
    }

    private void SetPressedVisual(bool pressed)
    {
        Texture texture = pressed ? pressedTexture : normalTexture;
        if (texture == null || iconRenderer == null)
        {
            return;
        }

        Renderer targetRenderer = iconRenderer;
        if (targetRenderer == null)
        {
            return;
        }

        if (iconMaterial == null)
        {
            iconMaterial = CreateMaterial(true);
            targetRenderer.sharedMaterial = iconMaterial;
        }

        iconMaterial.mainTexture = texture;
        SetMaterialColor(iconMaterial, Color.white);
    }

    private Material CreateMaterial(bool transparent)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (transparent)
        {
            material.renderQueue = 3000;
            if (material.HasProperty("_Surface"))
            {
                material.SetFloat("_Surface", 1f);
            }

            if (material.HasProperty("_SrcBlend"))
            {
                material.SetFloat("_SrcBlend", (float)UnityEngine.Rendering.BlendMode.SrcAlpha);
            }

            if (material.HasProperty("_DstBlend"))
            {
                material.SetFloat("_DstBlend", (float)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }

            if (material.HasProperty("_ZWrite"))
            {
                material.SetFloat("_ZWrite", 0f);
            }

            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        }

        return material;
    }

    private void SetMaterialColor(Material material, Color color)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseColor"))
        {
            material.SetColor("_BaseColor", color);
        }
        else if (material.HasProperty("_Color"))
        {
            material.SetColor("_Color", color);
        }
    }

    private Color GetButtonColor()
    {
        switch (buttonType)
        {
            case ButtonType.Accept:
                return new Color(0.045f, 0.255f, 0.135f);
            case ButtonType.Reject:
                return new Color(0.330f, 0.045f, 0.070f);
            case ButtonType.RotateLeft:
            case ButtonType.RotateRight:
                return new Color(0.105f, 0.130f, 0.360f);
            case ButtonType.ToggleReport:
                return new Color(0.245f, 0.135f, 0.460f);
            default:
                return Color.gray;
        }
    }

    private string GetActionLabel()
    {
        switch (buttonType)
        {
            case ButtonType.Accept:
                return "aceitar";
            case ButtonType.Reject:
                return "rejeitar";
            case ButtonType.RotateLeft:
                return "rodar esquerda";
            case ButtonType.RotateRight:
                return "rodar direita";
            case ButtonType.ToggleReport:
                return "relatorio";
            default:
                return "interagir";
        }
    }

    private string GetTextureName(bool pressed)
    {
        string state = pressed ? "pressed" : "normal";
        switch (buttonType)
        {
            case ButtonType.Accept:
                return "btn_accept_" + state;
            case ButtonType.Reject:
                return "btn_reject_" + state;
            case ButtonType.RotateLeft:
                return "btn_rotate_left_" + state;
            case ButtonType.RotateRight:
                return "btn_rotate_right_" + state;
            case ButtonType.ToggleReport:
                return "btn_report_" + state;
            default:
                return "";
        }
    }
}
