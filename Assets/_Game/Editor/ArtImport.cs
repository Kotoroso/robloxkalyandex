using UnityEditor;
using UnityEngine;

namespace DragonHeist.EditorTools
{
    /// <summary>
    /// Картинки из Resources/Art (интерфейс, иконки, драконы): без мип-мапов и сжатия, прозрачность без ореолов,
    /// размер не округляется до степени двойки — чтобы кнопки и иконки были чёткими.
    /// </summary>
    public class ArtImport : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').Contains("/Resources/Art/")) return;
            var ti = (TextureImporter)assetImporter;
            ti.textureType = TextureImporterType.Default;
            ti.mipmapEnabled = false;
            ti.alphaIsTransparency = true;
            ti.npotScale = TextureImporterNPOTScale.None;
            ti.wrapMode = TextureWrapMode.Clamp;
            ti.filterMode = FilterMode.Bilinear;
            bool ui = System.IO.Path.GetFileName(assetPath).StartsWith("ui_");
            ti.textureCompression = ui ? TextureImporterCompression.Uncompressed : TextureImporterCompression.Compressed;
            ti.maxTextureSize = ui ? 512 : 1024;
        }
    }
}
