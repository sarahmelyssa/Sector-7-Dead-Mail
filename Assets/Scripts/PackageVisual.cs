using UnityEngine;

public class PackageVisual : MonoBehaviour
{
    [Header("Gameplay Room Package Materials")]
    [Tooltip("Tune this if the active box is not warm/yellow enough under Package_OverheadSpotlight.")]
    [SerializeField] private Color defaultCardboardColor = new Color(0.66f, 0.46f, 0.235f);
    [SerializeField] private Color fallbackLabelColor = new Color(0.78f, 0.69f, 0.52f);
    [SerializeField] private Renderer bodyRenderer = null;
    [SerializeField] private Renderer labelRenderer = null;
    [SerializeField] private Renderer tapeRenderer = null;
    [SerializeField] private Material bodyMaterialTemplate = null;
    [SerializeField] private Material labelMaterialTemplate = null;
    [SerializeField] private Material tapeMaterialTemplate = null;

    private Material bodyMaterialInstance;
    private Material labelMaterialInstance;
    private Material tapeMaterialInstance;

    private void Awake()
    {
        ResolveRenderers();
    }

    public void SetRenderers(Renderer label, Renderer tape)
    {
        labelRenderer = label;
        tapeRenderer = tape;
    }

    public void ApplyPackageData(PackageData data)
    {
        ResolveRenderers();
        EnsureVisualParts();
        ApplyBody(data);
        ApplyLabel(data != null ? data.GetBoxLabelTexture() : null);
        ApplyTapeColor(data != null ? data.boxTapeColor : "");
    }

    private void ResolveRenderers()
    {
        if (bodyRenderer == null)
        {
            Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
            foreach (Renderer renderer in renderers)
            {
                if (renderer == null)
                {
                    continue;
                }

                if (renderer == labelRenderer || renderer == tapeRenderer)
                {
                    continue;
                }

                string normalized = renderer.gameObject.name.ToLowerInvariant();
                if (normalized.Contains("label") || normalized.Contains("tape"))
                {
                    continue;
                }

                bodyRenderer = renderer;
                break;
            }
        }

        if (labelRenderer == null)
        {
            Transform label = transform.Find("LabelQuad");
            labelRenderer = label != null ? label.GetComponent<Renderer>() : null;
        }

        if (tapeRenderer == null)
        {
            Transform tape = transform.Find("TapeObject");
            tapeRenderer = tape != null ? tape.GetComponent<Renderer>() : null;
        }
    }

    private void EnsureVisualParts()
    {
        if (tapeRenderer == null)
        {
            GameObject tapeObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tapeObject.name = "TapeObject";
            tapeObject.transform.SetParent(transform, false);
            tapeObject.transform.localPosition = new Vector3(0f, 0.025f, -0.006f);
            tapeObject.transform.localScale = new Vector3(0.16f, 1.05f, 1.04f);
            Collider tapeCollider = tapeObject.GetComponent<Collider>();
            if (tapeCollider != null)
            {
                Destroy(tapeCollider);
            }
            tapeRenderer = tapeObject.GetComponent<Renderer>();
        }

        if (labelRenderer == null)
        {
            GameObject labelQuad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            labelQuad.name = "LabelQuad";
            labelQuad.transform.SetParent(transform, false);
            labelQuad.transform.localPosition = new Vector3(0f, 0.035f, -0.525f);
            labelQuad.transform.localRotation = Quaternion.identity;
            labelQuad.transform.localScale = new Vector3(0.64f, 0.40f, 1f);
            Collider labelCollider = labelQuad.GetComponent<Collider>();
            if (labelCollider != null)
            {
                Destroy(labelCollider);
            }
            labelRenderer = labelQuad.GetComponent<Renderer>();
        }
    }

    private void ApplyBody(PackageData data)
    {
        if (bodyRenderer == null)
        {
            return;
        }

        if (bodyMaterialInstance == null)
        {
            bodyMaterialInstance = CreateMaterial(bodyMaterialTemplate, false);
            bodyRenderer.sharedMaterial = bodyMaterialInstance;
        }

        bodyRenderer.receiveShadows = false;
        bodyRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetMaterialColor(bodyMaterialInstance, GetBodyColor(data));
    }

    private void ApplyLabel(Texture2D labelTexture)
    {
        if (labelRenderer == null)
        {
            return;
        }

        if (labelMaterialInstance == null)
        {
            labelMaterialInstance = CreateMaterial(labelMaterialTemplate, true);
            labelRenderer.sharedMaterial = labelMaterialInstance;
        }

        labelRenderer.receiveShadows = false;
        labelRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        PushGeneratedLabelAwayFromBoxFace();

        if (labelTexture != null)
        {
            SetMaterialTexture(labelMaterialInstance, labelTexture);
            SetMaterialColor(labelMaterialInstance, Color.white);
        }
        else
        {
            SetMaterialColor(labelMaterialInstance, fallbackLabelColor);
        }
    }

    private void ApplyTapeColor(string colorName)
    {
        if (tapeRenderer == null)
        {
            return;
        }

        if (tapeMaterialInstance == null)
        {
            tapeMaterialInstance = CreateMaterial(tapeMaterialTemplate, false);
            tapeRenderer.sharedMaterial = tapeMaterialInstance;
        }

        tapeRenderer.receiveShadows = true;
        tapeRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        SetMaterialColor(tapeMaterialInstance, GetTapeColor(colorName));
    }

    private void PushGeneratedLabelAwayFromBoxFace()
    {
        if (labelRenderer == null || labelRenderer.gameObject.name != "LabelQuad")
        {
            return;
        }

        Vector3 localPosition = labelRenderer.transform.localPosition;
        if (localPosition.z > -0.446f)
        {
            localPosition.z = -0.446f;
            labelRenderer.transform.localPosition = localPosition;
        }
    }

    private Material CreateMaterial(Material template, bool unlit)
    {
        if (template != null)
        {
            return new Material(template);
        }

        Shader shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        var material = new Material(shader);
        if (material.HasProperty("_Cull"))
        {
            material.SetFloat("_Cull", 0f);
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

    private void SetMaterialTexture(Material material, Texture texture)
    {
        if (material == null)
        {
            return;
        }

        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
        }

        if (material.HasProperty("_MainTex"))
        {
            material.SetTexture("_MainTex", texture);
        }

        material.mainTexture = texture;
    }

    private Color GetBodyColor(PackageData data)
    {
        string shape = (data != null ? data.boxShape : "").Trim().ToLowerInvariant();
        string logo = (data != null ? data.boxLogo : "").Trim().ToLowerInvariant();

        Color baseColor = defaultCardboardColor;
        if (shape.Contains("tube") || shape.Contains("cylinder"))
        {
            baseColor = Color.Lerp(defaultCardboardColor, new Color(0.48f, 0.36f, 0.230f), 0.45f);
        }
        else if (shape.Contains("crate") || shape.Contains("large"))
        {
            baseColor = Color.Lerp(defaultCardboardColor, new Color(0.45f, 0.29f, 0.175f), 0.50f);
        }
        else if (shape.Contains("flat") || shape.Contains("envelope"))
        {
            baseColor = Color.Lerp(defaultCardboardColor, new Color(0.75f, 0.62f, 0.395f), 0.42f);
        }
        else if (shape.Contains("small"))
        {
            baseColor = Color.Lerp(defaultCardboardColor, new Color(0.70f, 0.48f, 0.255f), 0.35f);
        }

        if (logo.Contains("red"))
        {
            baseColor = Color.Lerp(baseColor, new Color(0.54f, 0.210f, 0.200f), 0.22f);
        }
        else if (logo.Contains("blue") || logo.Contains("gear"))
        {
            baseColor = Color.Lerp(baseColor, new Color(0.250f, 0.280f, 0.520f), 0.20f);
        }
        else if (logo.Contains("green") || logo.Contains("annex"))
        {
            baseColor = Color.Lerp(baseColor, new Color(0.200f, 0.420f, 0.260f), 0.18f);
        }

        return baseColor;
    }

    private Color GetTapeColor(string colorName)
    {
        switch ((colorName ?? "").Trim().ToLowerInvariant())
        {
            case "red":
                return new Color(0.560f, 0.040f, 0.075f);
            case "blue":
                return new Color(0.145f, 0.155f, 0.560f);
            case "green":
                return new Color(0.070f, 0.420f, 0.210f);
            case "white":
                return new Color(0.760f, 0.720f, 0.620f);
            case "black":
                return new Color(0.015f, 0.015f, 0.018f);
            case "yellow":
                return new Color(0.680f, 0.540f, 0.180f);
            case "purple":
                return new Color(0.460f, 0.150f, 0.690f);
            default:
                return new Color(0.35f, 0.28f, 0.21f);
        }
    }
}
