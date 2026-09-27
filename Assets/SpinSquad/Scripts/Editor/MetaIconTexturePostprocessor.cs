using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    /// <summary>
    /// Removes only the near-white area connected to an icon texture's outer edge.
    /// Interior white details remain intact.
    /// </summary>
    public sealed class MetaIconTexturePostprocessor : AssetPostprocessor
    {
        const string IconFolder = "Assets/SpinSquad/Resources/UI/MetaIcons/";
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(IconFolder, System.StringComparison.Ordinal))
                return;

            var importer = (TextureImporter)assetImporter;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = true;
            importer.isReadable = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
        }

        void OnPostprocessTexture(Texture2D texture)
        {
            if (!assetPath.StartsWith(IconFolder, System.StringComparison.Ordinal))
                return;

            var width = texture.width;
            var height = texture.height;
            var pixels = texture.GetPixels32();
            var queued = new bool[pixels.Length];
            var queue = new Queue<int>();

            void Enqueue(int x, int y)
            {
                if (x < 0 || x >= width || y < 0 || y >= height)
                    return;

                var index = y * width + x;
                if (queued[index] || !IsBackground(pixels[index]))
                    return;

                queued[index] = true;
                queue.Enqueue(index);
            }

            for (var x = 0; x < width; x++)
            {
                Enqueue(x, 0);
                Enqueue(x, height - 1);
            }
            for (var y = 0; y < height; y++)
            {
                Enqueue(0, y);
                Enqueue(width - 1, y);
            }

            while (queue.Count > 0)
            {
                var index = queue.Dequeue();
                var x = index % width;
                var y = index / width;
                var color = pixels[index];
                color.a = 0;
                pixels[index] = color;

                Enqueue(x - 1, y);
                Enqueue(x + 1, y);
                Enqueue(x, y - 1);
                Enqueue(x, y + 1);
            }

            texture.SetPixels32(pixels);
            texture.Apply(false, false);
        }

        static bool IsBackground(Color32 color)
        {
            return color.r >= 224 && color.g >= 224 && color.b >= 224;
        }
    }
}
