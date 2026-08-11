using UnityEngine;

namespace SpinSquad.Presentation
{
    /// <summary>Procedural, asset-free depth accents used only by the vertical slice.</summary>
    public static class BattleBackdrop25D
    {
        static Sprite _whiteSprite;
        static Sprite _softOvalSprite;

        public static Sprite SoftOvalSprite
        {
            get
            {
                if (_softOvalSprite == null)
                    _softOvalSprite = CreateSoftOvalSprite();
                return _softOvalSprite;
            }
        }

        public static void Build(Transform parent)
        {
            if (parent == null || parent.Find("DepthAccents25D") != null)
                return;

            var root = new GameObject("DepthAccents25D");
            root.transform.SetParent(parent, false);

            CreateBand(root.transform, "HorizonHaze", new Vector3(0f, 1.55f, 0.1f),
                new Vector3(9f, 1.1f, 1f), new Color(0.18f, 0.34f, 0.55f, 0.13f), -18);
            CreateBand(root.transform, "ArenaFloorTint", new Vector3(0f, -0.15f, 0.08f),
                new Vector3(9f, 2.7f, 1f), new Color(0.025f, 0.06f, 0.11f, 0.18f), -17);
            CreateBand(root.transform, "ForegroundShade", new Vector3(0f, -2.55f, 0.06f),
                new Vector3(9f, 1.35f, 1f), new Color(0.01f, 0.018f, 0.035f, 0.32f), 4);
        }

        static void CreateBand(
            Transform parent,
            string name,
            Vector3 position,
            Vector3 scale,
            Color color,
            int sortingOrder)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.localScale = scale;

            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = WhiteSprite;
            renderer.color = color;
            renderer.sortingOrder = sortingOrder;
        }

        static Sprite WhiteSprite
        {
            get
            {
                if (_whiteSprite != null)
                    return _whiteSprite;

                var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false)
                {
                    name = "Battle25D_White",
                    filterMode = FilterMode.Bilinear,
                    wrapMode = TextureWrapMode.Clamp
                };
                texture.SetPixel(0, 0, Color.white);
                texture.Apply();
                _whiteSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                _whiteSprite.name = "Battle25D_White";
                return _whiteSprite;
            }
        }

        static Sprite CreateSoftOvalSprite()
        {
            const int width = 64;
            const int height = 32;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "Battle25D_SoftOval",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    var nx = (x + 0.5f) / width * 2f - 1f;
                    var ny = (y + 0.5f) / height * 2f - 1f;
                    var distance = Mathf.Sqrt(nx * nx + ny * ny);
                    var alpha = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(distance));
                    texture.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }

            texture.Apply();
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, width, height),
                new Vector2(0.5f, 0.5f),
                64f);
            sprite.name = "Battle25D_SoftOval";
            return sprite;
        }
    }
}
