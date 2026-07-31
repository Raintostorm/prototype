#if UNITY_EDITOR
using UnityEditor;

namespace SpinSquad.Editor
{
    /// <summary>Keep local animal-kit placeholder UI PNGs importable through Resources.Load&lt;Sprite&gt;.</summary>
    public sealed class AnimalKitPrototypeSpriteImporter : AssetPostprocessor
    {
        const string LocalAnimalKitUiPath = "Assets/SpinSquad/Resources/UI/AnimalKitLocal/";
        const string LocalAnimalKitBattlePath = "Assets/SpinSquad/Resources/Battle/AnimalKitLocal/";
        const string LocalAnimalKitFramesPath = "Assets/SpinSquad/Resources/Battle/AnimalKitFrames/";

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(LocalAnimalKitUiPath, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(LocalAnimalKitBattlePath, System.StringComparison.Ordinal) &&
                !assetPath.StartsWith(LocalAnimalKitFramesPath, System.StringComparison.Ordinal))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = UnityEngine.FilterMode.Bilinear;
            importer.spritePixelsPerUnit = 100f;
        }
    }
}
#endif
