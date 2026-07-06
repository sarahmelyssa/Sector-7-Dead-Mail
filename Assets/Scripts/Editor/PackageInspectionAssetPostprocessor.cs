#if UNITY_EDITOR
using UnityEditor;

public class PackageInspectionAssetPostprocessor : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        string normalizedPath = assetPath.Replace("\\", "/");
        if (!normalizedPath.Contains("Assets/Resources/PackageInspectionAssets/"))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;

        if (normalizedPath.Contains("/reports_png/"))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
        }
        else if (normalizedPath.Contains("/box_labels_png/"))
        {
            importer.textureType = TextureImporterType.Default;
        }
    }
}
#endif
