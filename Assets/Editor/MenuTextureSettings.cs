using UnityEditor;
using UnityEngine;

/// <summary>UI images are whole textures, never sprite sheets or scaled NPOT atlases.</summary>
public sealed class MenuTextureSettings : AssetPostprocessor
{
    private void OnPreprocessTexture()
    {
        if (!assetPath.StartsWith("Assets/Resources/Menu/") && assetPath != "Assets/Resources/CrewClimb.png") return;
        var texture = (TextureImporter)assetImporter;
        texture.textureType = TextureImporterType.Default;
        texture.spriteImportMode = SpriteImportMode.None;
        texture.mipmapEnabled = false;
        texture.alphaIsTransparency = assetPath.EndsWith("CrewClimb.png");
        texture.npotScale = TextureImporterNPOTScale.None;
        texture.maxTextureSize = assetPath.EndsWith("Starfield.png") ? 8192 : 4096;
        texture.textureCompression = TextureImporterCompression.Uncompressed;
        texture.filterMode = FilterMode.Bilinear;
        texture.wrapMode = TextureWrapMode.Clamp;
        var settings = texture.GetDefaultPlatformTextureSettings();
        settings.maxTextureSize = texture.maxTextureSize;
        settings.textureCompression = TextureImporterCompression.Uncompressed;
        texture.SetPlatformTextureSettings(settings);
    }

    [MenuItem("Tools/Crew/Refresh Menu Textures")]
    public static void RefreshTextures()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Texture2D", new[] { "Assets/Resources/Menu" }))
            AssetDatabase.ImportAsset(AssetDatabase.GUIDToAssetPath(guid), ImportAssetOptions.ForceUpdate);
    }
}
