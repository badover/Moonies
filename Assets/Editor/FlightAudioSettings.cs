using UnityEditor;
using UnityEngine;

/// <summary>Stream the full-length soundtrack instead of retaining decoded music in memory.</summary>
public sealed class FlightAudioSettings : AssetPostprocessor
{
    private void OnPreprocessAudio()
    {
        if(assetPath!="Assets/Resources/Audio/MissionMusic.ogg")return;
        var importer=(AudioImporter)assetImporter;
        var settings=importer.defaultSampleSettings;
        settings.loadType=AudioClipLoadType.Streaming;
        settings.compressionFormat=AudioCompressionFormat.Vorbis;
        settings.quality=.8f;
        importer.defaultSampleSettings=settings;
        importer.forceToMono=false;importer.loadInBackground=true;
    }
}
