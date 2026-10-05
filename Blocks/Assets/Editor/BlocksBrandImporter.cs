using UnityEditor;
using UnityEngine;

public class BlocksBrandImporter : AssetPostprocessor
{
    void OnPreprocessTexture()
    {
        if(!assetPath.StartsWith("Assets/Resources/Brand/") && !assetPath.StartsWith("Assets/Art/Brand/")) return;
        var importer=(TextureImporter)assetImporter;
        importer.textureType=TextureImporterType.Default;
        importer.alphaIsTransparency=true;importer.mipmapEnabled=false;
        importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
        importer.npotScale=TextureImporterNPOTScale.None;
        importer.maxTextureSize=assetPath.Contains("/background")?1024:2048;
        importer.textureCompression=assetPath.EndsWith("app-icon.png")?
            TextureImporterCompression.Uncompressed:TextureImporterCompression.CompressedHQ;
    }
}
