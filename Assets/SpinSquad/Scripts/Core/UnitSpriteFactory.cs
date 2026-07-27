using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Sprite placeholder theo hình — cùng PPU 16 với ô vuông cũ.</summary>
    public static class UnitSpriteFactory
    {
        const float Ppu = 16f;
        const int Res = 32;

        static Sprite _square;
        static Sprite _circle;
        static Sprite _triangle;

        public static Sprite GetSprite(UnitBodyShape shape)
        {
            return shape switch
            {
                UnitBodyShape.Circle => Circle(),
                UnitBodyShape.Triangle => Triangle(),
                _ => Square()
            };
        }

        public static Sprite Square()
        {
            if (_square != null)
                return _square;
            var tex = Texture2D.whiteTexture;
            _square = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                Ppu);
            return _square;
        }

        static Sprite Circle()
        {
            if (_circle != null)
                return _circle;
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var cx = Res * 0.5f;
            var cy = Res * 0.5f;
            var r = Res * 0.42f;
            for (var y = 0; y < Res; y++)
            {
                for (var x = 0; x < Res; x++)
                {
                    var dx = x - cx;
                    var dy = y - cy;
                    var c = dx * dx + dy * dy <= r * r ? Color.white : Color.clear;
                    tex.SetPixel(x, y, c);
                }
            }

            tex.Apply(false, true);
            _circle = Sprite.Create(tex, new Rect(0, 0, Res, Res), new Vector2(0.5f, 0.5f), Ppu);
            return _circle;
        }

        static Sprite Triangle()
        {
            if (_triangle != null)
                return _triangle;
            var tex = new Texture2D(Res, Res, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            var yTop = Res * 0.82f;
            var yBot = Res * 0.16f;
            var xMid = Res * 0.5f;
            var xLeft = Res * 0.14f;
            var xRight = Res * 0.86f;
            for (var y = 0; y < Res; y++)
            {
                for (var x = 0; x < Res; x++)
                {
                    var inside = PointInTriangle(x, y, xLeft, yBot, xRight, yBot, xMid, yTop);
                    tex.SetPixel(x, y, inside ? Color.white : Color.clear);
                }
            }

            tex.Apply(false, true);
            _triangle = Sprite.Create(tex, new Rect(0, 0, Res, Res), new Vector2(0.5f, 0.5f), Ppu);
            return _triangle;
        }

        static bool PointInTriangle(float px, float py, float x1, float y1, float x2, float y2, float x3, float y3)
        {
            static float Sign(float ax, float ay, float bx, float by, float cx, float cy) =>
                (ax - cx) * (by - cy) - (bx - cx) * (ay - cy);

            var d1 = Sign(px, py, x1, y1, x2, y2);
            var d2 = Sign(px, py, x2, y2, x3, y3);
            var d3 = Sign(px, py, x3, y3, x1, y1);
            var neg = d1 < 0f || d2 < 0f || d3 < 0f;
            var pos = d1 > 0f || d2 > 0f || d3 > 0f;
            return !(neg && pos);
        }
    }
}
