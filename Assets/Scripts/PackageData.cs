using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Guarda todos os dados de uma encomenda: o que aparece na caixa, o que aparece
/// no relatorio e a regra que decide se deve ser aceite ou rejeitada.
/// </summary>
[Serializable]
public class PackageData
{
    [Header("Asset Pack")]
    public string id;
    public string difficulty;
    public Sprite reportImage;
    public List<Sprite> reportPages = new List<Sprite>();
    public Texture2D boxLabelTexture;
    public string reportImagePath;
    public List<string> reportPagePaths = new List<string>();
    public string boxLabelTexturePath;

    [Header("Report Rules")]
    public string reportShape;
    public string reportBarcode;
    public string reportLogo;
    public string reportTapeColor;
    public string reportDestination;
    public string reportWeight;

    [Header("Box Visuals")]
    public string boxShape;
    public string boxBarcode;
    public string boxLogo;
    public string boxTapeColor;
    public string boxDestination;
    public string boxWeight;

    [Header("Legacy Text Data")]
    public string remetente;
    public string destino;
    public float peso;
    public string tipoConteudo;
    public string codigoSerie;
    public string relatorio;

    [NonSerialized] public List<string> rejectionReasons = new List<string>();

    private Sprite loadedReportImage;
    private List<Sprite> loadedReportPages;
    private Texture2D loadedBoxLabelTexture;

    // Uma caixa deve ser rejeitada quando algum dado visual nao bate com o report.
    public bool ShouldReject => CalculateShouldReject();
    public bool isDangerous => ShouldReject;
    public bool shouldReject => ShouldReject;
    public string senderName { get => remetente; set => remetente = value; }
    public string destination { get => destino; set => destino = value; }
    public float weight { get => peso; set => peso = value; }
    public string contentType { get => tipoConteudo; set => tipoConteudo = value; }
    public string serialCode { get => codigoSerie; set => codigoSerie = value; }
    public string reportText { get => relatorio; set => relatorio = value; }

    public PackageData()
    {
    }

    public PackageData(string remetente, string destino, float peso, string tipoConteudo, string codigoSerie, string relatorio, List<string> rejectionReasons)
    {
        this.remetente = remetente;
        this.destino = destino;
        this.peso = peso;
        this.tipoConteudo = tipoConteudo;
        this.codigoSerie = codigoSerie;
        this.relatorio = relatorio;
        this.rejectionReasons = rejectionReasons ?? new List<string>();
    }

    public Sprite GetReportSprite()
    {
        if (reportImage != null)
        {
            return reportImage;
        }

        if (loadedReportImage != null)
        {
            return loadedReportImage;
        }

        string normalizedPath = NormalizeResourcePath(reportImagePath);
        loadedReportImage = Resources.Load<Sprite>(normalizedPath);
        if (loadedReportImage != null)
        {
            return loadedReportImage;
        }

        Texture2D texture = LoadTexture(normalizedPath);
        if (texture == null)
        {
            return null;
        }

        loadedReportImage = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        loadedReportImage.name = string.IsNullOrWhiteSpace(id) ? texture.name : id + "_report_sprite";
        return loadedReportImage;
    }

    public List<Sprite> GetReportSprites()
    {
        var pages = new List<Sprite>();

        if (reportPages != null)
        {
            foreach (Sprite page in reportPages)
            {
                if (page != null)
                {
                    pages.Add(page);
                }
            }
        }

        if (reportPagePaths != null && reportPagePaths.Count > 0)
        {
            if (loadedReportPages == null)
            {
                loadedReportPages = new List<Sprite>();
                for (int i = 0; i < reportPagePaths.Count; i++)
                {
                    Sprite loadedPage = LoadReportSpriteFromPath(reportPagePaths[i], "_report_page_" + (i + 1));
                    if (loadedPage != null)
                    {
                        loadedReportPages.Add(loadedPage);
                    }
                }
            }

            pages.AddRange(loadedReportPages);
        }

        if (pages.Count == 0)
        {
            Sprite singlePage = GetReportSprite();
            if (singlePage != null)
            {
                pages.Add(singlePage);
            }
        }

        return pages;
    }

    public Texture2D GetBoxLabelTexture()
    {
        if (boxLabelTexture != null)
        {
            return boxLabelTexture;
        }

        if (loadedBoxLabelTexture == null)
        {
            loadedBoxLabelTexture = LoadTexture(boxLabelTexturePath);
        }

        return loadedBoxLabelTexture;
    }

    public void RefreshValidationReasons()
    {
        if (!HasVisualValidationData())
        {
            rejectionReasons = rejectionReasons ?? new List<string>();
            return;
        }

        rejectionReasons = new List<string>();

        AddMismatchReason("Shape", boxShape, reportShape);
        AddMismatchReason("Barcode", boxBarcode, reportBarcode);
        AddMismatchReason("Logo", boxLogo, reportLogo);
        AddMismatchReason("Tape color", boxTapeColor, reportTapeColor);
        AddMismatchReason("Destination", boxDestination, reportDestination);
        AddMismatchReason("Weight", boxWeight, reportWeight);
    }

    private bool CalculateShouldReject()
    {
        if (HasVisualValidationData())
        {
            return !Matches(boxShape, reportShape)
                || !Matches(boxBarcode, reportBarcode)
                || !Matches(boxLogo, reportLogo)
                || !Matches(boxTapeColor, reportTapeColor)
                || !Matches(boxDestination, reportDestination)
                || !Matches(boxWeight, reportWeight);
        }

        return rejectionReasons != null && rejectionReasons.Count > 0;
    }

    private bool HasVisualValidationData()
    {
        return !string.IsNullOrWhiteSpace(reportShape)
            || !string.IsNullOrWhiteSpace(reportBarcode)
            || !string.IsNullOrWhiteSpace(reportLogo)
            || !string.IsNullOrWhiteSpace(reportTapeColor)
            || !string.IsNullOrWhiteSpace(reportDestination)
            || !string.IsNullOrWhiteSpace(reportWeight)
            || !string.IsNullOrWhiteSpace(boxShape)
            || !string.IsNullOrWhiteSpace(boxBarcode)
            || !string.IsNullOrWhiteSpace(boxLogo)
            || !string.IsNullOrWhiteSpace(boxTapeColor)
            || !string.IsNullOrWhiteSpace(boxDestination)
            || !string.IsNullOrWhiteSpace(boxWeight);
    }

    private void AddMismatchReason(string label, string boxValue, string reportValue)
    {
        if (!Matches(boxValue, reportValue))
        {
            rejectionReasons.Add(label + " mismatch");
        }
    }

    private bool Matches(string left, string right)
    {
        return string.Equals(Clean(left), Clean(right), StringComparison.OrdinalIgnoreCase);
    }

    private string Clean(string value)
    {
        return string.IsNullOrWhiteSpace(value) ? "" : value.Trim();
    }

    private Texture2D LoadTexture(string resourcePath)
    {
        if (string.IsNullOrWhiteSpace(resourcePath))
        {
            return null;
        }

        return Resources.Load<Texture2D>(NormalizeResourcePath(resourcePath));
    }

    private Sprite LoadReportSpriteFromPath(string path, string suffix)
    {
        string normalizedPath = NormalizeResourcePath(path);
        Sprite sprite = Resources.Load<Sprite>(normalizedPath);
        if (sprite != null)
        {
            return sprite;
        }

        Texture2D texture = LoadTexture(normalizedPath);
        if (texture == null)
        {
            return null;
        }

        Sprite generatedSprite = Sprite.Create(
            texture,
            new Rect(0f, 0f, texture.width, texture.height),
            new Vector2(0.5f, 0.5f),
            100f
        );

        generatedSprite.name = (string.IsNullOrWhiteSpace(id) ? texture.name : id) + suffix;
        return generatedSprite;
    }

    private string NormalizeResourcePath(string path)
    {
        string normalized = path.Replace("\\", "/");
        if (normalized.StartsWith("Assets/Resources/"))
        {
            normalized = normalized.Substring("Assets/Resources/".Length);
        }

        if (normalized.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
        {
            normalized = normalized.Substring(0, normalized.Length - 4);
        }

        return normalized;
    }
}
