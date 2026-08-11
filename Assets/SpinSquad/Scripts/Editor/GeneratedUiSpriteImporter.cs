#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    public sealed class GeneratedUiSpriteImporter : AssetPostprocessor
    {
        const string Root = "Assets/SpinSquad/Resources/UI/GeneratedV2/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root) || !assetPath.EndsWith(".png"))
                return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.spritePixelsPerUnit = 100f;
        }
    }
}
#endif
