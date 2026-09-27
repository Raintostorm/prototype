#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    public sealed class SpinSquadV3TexturePostprocessor : AssetPostprocessor
    {
        const string Root = "Assets/SpinSquad/Resources/UI/SpinSquadV3/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".png"))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
#endif
