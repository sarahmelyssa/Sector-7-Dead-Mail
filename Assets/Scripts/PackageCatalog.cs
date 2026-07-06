using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public static class PackageCatalog
{
    private const string AssetManifestResourcePath = "PackageInspectionAssets/package_manifest_30";

    [System.Serializable]
    private class ManifestWrapper
    {
        public ManifestItem[] items = new ManifestItem[0];
    }

    [System.Serializable]
    private class ManifestItem
    {
        public string id = "";
        public string difficulty = "";
        public string reportImage = "";
        public string boxLabel = "";
        public ManifestSide report = new ManifestSide();
        public ManifestSide box = new ManifestSide();
    }

    [System.Serializable]
    private class ManifestSide
    {
        public string shape = "";
        public string barcode = "";
        public string logo = "";
        public string tapeColor = "";
        public string destination = "";
        public string weight = "";
    }

    public static List<PackageData> CreateAssetPackagesForNight(int night)
    {
        List<PackageData> allPackages = CreateAssetPackages();
        if (allPackages.Count == 0)
        {
            return CreateInitialPackages();
        }

        var allowedDifficulties = GetAllowedDifficultiesForNight(night);
        var filteredPackages = new List<PackageData>();

        foreach (PackageData packageData in allPackages)
        {
            if (allowedDifficulties.Contains(packageData.difficulty))
            {
                filteredPackages.Add(packageData);
            }
        }

        return filteredPackages.Count > 0 ? filteredPackages : allPackages;
    }

    public static List<PackageData> CreateAssetPackages()
    {
        TextAsset manifest = Resources.Load<TextAsset>(AssetManifestResourcePath);
        if (manifest == null || string.IsNullOrWhiteSpace(manifest.text))
        {
            return new List<PackageData>();
        }

        string wrappedJson = "{\"items\":" + manifest.text + "}";
        ManifestWrapper wrapper = JsonUtility.FromJson<ManifestWrapper>(wrappedJson);
        var packages = new List<PackageData>();

        if (wrapper?.items == null)
        {
            return packages;
        }

        foreach (ManifestItem item in wrapper.items)
        {
            PackageData packageData = CreateFromManifestItem(item);
            if (packageData != null)
            {
                packages.Add(packageData);
            }
        }

        return packages;
    }

    public static List<PackageData> CreateInitialPackages()
    {
        return new List<PackageData>
        {
            Create("North Annex", "Sorting Room", 4.2f, "documents", "PKG-124-H", "Manifesto limpo. Papel seco, carimbo de entrada confirmado.", null),
            Create("Basement Office", "Cold Storage", 7.8f, "medical samples", "PKG-305-I", "Amostras seladas. Temperatura dentro do intervalo.", null),
            Create("Unit 04", "Inspection Desk", 2.1f, "office supplies", "PKG-640-K", "Inventario comum. Nenhuma vibracao registrada.", null),
            Create("Night Archive", "Return Cage", 9.4f, "machine parts", "PKG-222-G", "Pecas antigas, mas catalogadas. Sem fuga de oleo.", null),
            Create("North Annex", "Red Corridor", 5.6f, "sealed tools", "PKG-731-L", "Caixa limpa. Etiqueta parcialmente gasta, codigo ainda legivel.", null),

            Create("Basement Office", "Sorting Room", 12.9f, "documents", "PKG-018-J", "Peso declarado no manifesto: 6.1 kg. Balanca local insiste em 12.9 kg.", Reasons("Peso acima do permitido")),
            Create("Unit 04", "Cold Storage", 14.3f, "medical samples", "PKG-441-J", "Gelo seco presente. A caixa afunda ligeiramente a mesa.", Reasons("Peso acima do permitido")),
            Create("North Annex", "Inspection Desk", 18.6f, "machine parts", "PKG-909-R", "Transportador marcou volume pequeno; peso excede limite operacional.", Reasons("Peso acima do permitido")),
            Create("Night Archive", "Return Cage", 13.2f, "office supplies", "PKG-523-K", "Sem som interno. Peso ainda acima do permitido para despacho manual.", Reasons("Peso acima do permitido")),

            Create("Unknown Clerk", "Sorting Room", 3.7f, "documents", "PKG-802-K", "Assinatura do remetente nao consta no turno. A tinta parece recente.", Reasons("Remetente proibido")),
            Create("Red Choir", "Cold Storage", 5.5f, "sealed tools", "PKG-136-K", "Origem rejeitada pelo terminal. O selo esta intacto demais.", Reasons("Remetente proibido")),
            Create("Basement Mouth", "Inspection Desk", 6.4f, "office supplies", "PKG-711-J", "Remetente recusado automaticamente. Manifesto anexado sem autorizacao.", Reasons("Remetente proibido")),
            Create("Unlisted Hand", "Return Cage", 8.0f, "machine parts", "PKG-254-L", "Rota normal, mas origem nao existe na lista de fornecedores.", Reasons("Remetente proibido")),

            Create("North Annex", "Room 000", 4.8f, "documents", "PKG-450-J", "Destino impresso como sala zero. O mapa nao mostra essa sala.", Reasons("Destino inexistente")),
            Create("Unit 04", "Below Receiving", 7.2f, "sealed tools", "PKG-620-I", "Etiqueta de destino aponta para um andar nao registrado.", Reasons("Destino inexistente")),
            Create("Basement Office", "Door Without Number", 2.9f, "office supplies", "PKG-117-J", "O destino foi escrito a mao por cima da etiqueta original.", Reasons("Destino inexistente")),
            Create("Night Archive", "North Annex B13", 6.8f, "documents", "PKG-741-M", "Nenhuma ala B13 cadastrada. A impressora repetiu o endereco duas vezes.", Reasons("Destino inexistente")),

            Create("North Annex", "Cold Storage", 3.1f, "biological culture", "PKG-333-J", "Conteudo biologico sem autorizacao sanitaria. Selo frio ao toque.", Reasons("Tipo de conteudo proibido")),
            Create("Basement Office", "Sorting Room", 4.4f, "preserved tissue", "PKG-552-M", "Material biologico declarado como arquivo clinico antigo.", Reasons("Tipo de conteudo proibido")),
            Create("Unit 04", "Return Cage", 2.6f, "sealed spores", "PKG-281-L", "Pacote nao respira, mas o plastico turva por dentro.", Reasons("Tipo de conteudo proibido")),
            Create("Night Archive", "Inspection Desk", 5.0f, "ritual wax tablets", "PKG-692-Q", "Itens religiosos ficticios sem registro de estudo aprovado.", Reasons("Tipo de conteudo proibido")),
            Create("North Annex", "Red Corridor", 3.9f, "ceremonial thread bundle", "PKG-460-O", "Conteudo ritualistico ficticio. Os fios estao todos numerados.", Reasons("Tipo de conteudo proibido")),

            Create("Basement Office", "Cold Storage", 5.7f, "medical samples", "PKG-814-N", "Relatorio diz 'amostras aquecidas'; destino exige cadeia fria intacta.", Reasons("Relatorio contraditorio")),
            Create("Unit 04", "Sorting Room", 2.4f, "documents", "PKG-639-R", "Manifesto declara documentos secos. Observacao secundaria: umidade interna detectada.", Reasons("Relatorio contraditorio")),
            Create("Night Archive", "Inspection Desk", 6.2f, "machine parts", "PKG-174-M", "Scanner: metal ausente. Manifesto: engrenagens de reposicao.", Reasons("Relatorio contraditorio")),
            Create("North Annex", "Return Cage", 4.6f, "office supplies", "PKG-227-L", "Relatorio confirma caixa vazia, mas inventario lista 14 itens.", Reasons("Relatorio contraditorio")),

            Create("Basement Office", "Red Corridor", 3.5f, "sealed tools", "PKG-456-Z", "Codigo aceito visualmente, mas digito de verificacao nao confere.", Reasons("Codigo de serie invalido")),
            Create("Unit 04", "Cold Storage", 7.1f, "medical samples", "PKG-88A-Q", "Codigo contem letra onde deveria haver numero. Etiqueta sem rasura.", Reasons("Codigo de serie invalido")),
            Create("Night Archive", "Sorting Room", 4.9f, "documents", "PKG-112-A", "Carimbo limpo. Codigo de serie falha no terminal de entrada.", Reasons("Codigo de serie invalido")),

            Create("Red Choir", "Room 000", 15.4f, "ritual glass ampoule", "PKG-71X-B", "Manifesto chama de material didatico. Balanca e destino recusam a caixa.", Reasons("Remetente proibido", "Destino inexistente", "Peso acima do permitido", "Tipo de conteudo proibido", "Codigo de serie invalido"))
        };
    }

    private static PackageData Create(string senderName, string destination, float weight, string contentType, string serialCode, string reportText, List<string> rejectionReasons)
    {
        return new PackageData(senderName, destination, weight, contentType, serialCode, reportText, rejectionReasons ?? new List<string>());
    }

    private static PackageData CreateFromManifestItem(ManifestItem item)
    {
        if (item == null || item.report == null || item.box == null)
        {
            return null;
        }

        var data = new PackageData
        {
            id = item.id,
            difficulty = NormalizeDifficulty(item.difficulty),
            reportImagePath = ToResourcePath(item.reportImage),
            boxLabelTexturePath = ToResourcePath(item.boxLabel),
            reportShape = item.report.shape,
            reportBarcode = item.report.barcode,
            reportLogo = item.report.logo,
            reportTapeColor = item.report.tapeColor,
            reportDestination = item.report.destination,
            reportWeight = item.report.weight,
            boxShape = item.box.shape,
            boxBarcode = item.box.barcode,
            boxLogo = item.box.logo,
            boxTapeColor = item.box.tapeColor,
            boxDestination = item.box.destination,
            boxWeight = item.box.weight,
            remetente = "Sorting Terminal",
            destino = item.box.destination,
            peso = ParseWeight(item.box.weight),
            tipoConteudo = item.box.shape + " / " + item.box.logo,
            codigoSerie = item.box.barcode,
            relatorio = ""
        };

        data.RefreshValidationReasons();
        return data;
    }

    private static HashSet<string> GetAllowedDifficultiesForNight(int night)
    {
        return new HashSet<string> { "easy", "medium", "hard" };
    }

    private static string NormalizeDifficulty(string difficulty)
    {
        return string.IsNullOrWhiteSpace(difficulty) ? "easy" : difficulty.Trim().ToLowerInvariant();
    }

    private static string ToResourcePath(string manifestPath)
    {
        if (string.IsNullOrWhiteSpace(manifestPath))
        {
            return "";
        }

        string path = manifestPath.Replace("\\", "/");
        if (path.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
        {
            path = path.Substring(0, path.Length - 4);
        }

        return "PackageInspectionAssets/" + path;
    }

    private static float ParseWeight(string weight)
    {
        if (string.IsNullOrWhiteSpace(weight))
        {
            return 0f;
        }

        string cleaned = weight.ToUpperInvariant().Replace("KG", "").Trim();
        if (float.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out float parsedWeight))
        {
            return parsedWeight;
        }

        return 0f;
    }

    private static List<string> Reasons(params string[] reasons)
    {
        return new List<string>(reasons);
    }
}
