#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    /// <summary>
    /// PNG HUD trong Resources cần import đúng kiểu Sprite (Single).
    /// Meta tạo tay có thể để spriteSheet rỗng → Resources.Load&lt;Sprite&gt; null.
    /// </summary>
    public static class HudSpritesImportFixer
    {
        const string HudRoot = "Assets/SpinSquad/Resources/UI/Hud";

        [MenuItem("SpinSquad/UI/Reimport HUD Sprites (fix Resources.Load)")]
        public static void ReimportAllHudSprites()
        {
            if (!AssetDatabase.IsValidFolder(HudRoot))
            {
                Debug.LogError("[HudSprites] Không tìm thấy " + HudRoot);
                return;
            }

            var count = 0;
            foreach (var path in Directory.GetFiles(HudRoot, "*.PNG", SearchOption.AllDirectories))
            {
                var assetPath = path.Replace('\\', '/');
                if (assetPath.EndsWith(".meta"))
                    continue;

                var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
                if (importer == null)
                    continue;

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.filterMode = FilterMode.Bilinear;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                count++;
            }

            SpinSquad.UI.HudUiSprites.ClearCache();
            Debug.Log($"[HudSprites] Reimported {count} PNG under {HudRoot}. Chạy Validate HUD Sprites hoặc Play lại scene.");
        }
    }
}
#endif
