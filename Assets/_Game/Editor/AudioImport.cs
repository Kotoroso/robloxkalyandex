using UnityEditor;
using UnityEngine;

namespace DragonHeist.EditorTools
{
    /// <summary>Настройки импорта звука: моно, сжатие, музыка — стриминг. Экономит память на телефонах.</summary>
    public class AudioImport : AssetPostprocessor
    {
        void OnPreprocessAudio()
        {
            if (!assetPath.Contains("_Game/Resources/Audio")) return;
            var imp = (AudioImporter)assetImporter;
            imp.forceToMono = true;
            var s = imp.defaultSampleSettings;
            bool isMusic = assetPath.EndsWith("music.ogg");
            s.compressionFormat = AudioCompressionFormat.Vorbis;
            s.quality = isMusic ? 0.45f : 0.6f;
            s.loadType = isMusic ? AudioClipLoadType.CompressedInMemory : AudioClipLoadType.DecompressOnLoad;
            s.sampleRateSetting = AudioSampleRateSetting.OptimizeSampleRate;
            imp.defaultSampleSettings = s;
        }
    }
}
