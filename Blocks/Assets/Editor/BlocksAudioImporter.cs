using UnityEditor;
using UnityEngine;

public class BlocksAudioImporter : AssetPostprocessor
{
    void OnPreprocessAudio()
    {
        if(!assetPath.StartsWith("Assets/Resources/Audio/")) return;
        var importer=(AudioImporter)assetImporter;
        importer.forceToMono=!assetPath.Contains("morning_mosaic");
        importer.loadInBackground=true;
        var settings=importer.defaultSampleSettings;
        settings.loadType=assetPath.Contains("morning_mosaic")?AudioClipLoadType.CompressedInMemory:AudioClipLoadType.DecompressOnLoad;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;settings.quality=.65f;
        importer.defaultSampleSettings=settings;
    }
}
