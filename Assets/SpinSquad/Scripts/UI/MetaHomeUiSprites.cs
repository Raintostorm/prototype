using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpinSquad.UI
{
    public static class MetaHomeUiSprites
    {
        const int IconSize = 128;
        const int ButtonWidth = 320;
        const int ButtonHeight = 128;
        const int PanelWidth = 320;
        const int PanelHeight = 192;

        static readonly Dictionary<string, Sprite> Cache = new Dictionary<string, Sprite>(StringComparer.Ordinal);

        public static Sprite CtaButton => RoundedButton("cta", ButtonWidth, ButtonHeight,
            new Color32(21, 73, 128, 255), new Color32(38, 130, 223, 255), new Color32(255, 206, 92, 255), 30);

        public static Sprite BlueButton => RoundedButton("blue", ButtonWidth, ButtonHeight,
            new Color32(20, 70, 126, 255), new Color32(34, 118, 211, 255), new Color32(111, 199, 255, 255), 26);

        public static Sprite GreenButton => RoundedButton("green", ButtonWidth, ButtonHeight,
            new Color32(20, 93, 78, 255), new Color32(35, 170, 111, 255), new Color32(113, 245, 169, 255), 26);

        public static Sprite GoldButton => RoundedButton("gold", ButtonWidth, ButtonHeight,
            new Color32(109, 63, 23, 255), new Color32(202, 136, 42, 255), new Color32(255, 215, 94, 255), 26);

        public static Sprite DarkButton => RoundedButton("dark", ButtonWidth, ButtonHeight,
            new Color32(18, 25, 42, 246), new Color32(45, 59, 87, 246), new Color32(126, 162, 210, 255), 26);

        public static Sprite SmallIconButton => RoundedButton("small-icon", 128, 112,
            new Color32(18, 31, 50, 245), new Color32(54, 75, 104, 245), new Color32(105, 188, 255, 255), 24);

        public static Sprite NavTab => RoundedButton("nav-idle", 192, 112,
            new Color32(18, 24, 38, 236), new Color32(31, 42, 64, 242), new Color32(68, 92, 125, 255), 16);

        public static Sprite NavTabActive => RoundedButton("nav-active", 192, 112,
            new Color32(22, 68, 116, 248), new Color32(36, 116, 196, 250), new Color32(255, 206, 88, 255), 16);

        public static Sprite CampaignPanel => RoundedPanel("campaign-panel", PanelWidth, PanelHeight,
            new Color32(5, 18, 34, 214), new Color32(41, 127, 205, 110), new Color32(255, 214, 105, 190), 24);

        public static Sprite HomeIcon => Icon("home", DrawHome);
        public static Sprite UpgradeIcon => Icon("upgrade", DrawUpgrade);
        public static Sprite TreasureIcon => Icon("treasure", DrawTreasure);
        public static Sprite SettingsIcon => Icon("settings", DrawSettings);
        public static Sprite SwordIcon => Icon("sword", DrawSword);
        public static Sprite MailIcon => Icon("mail", DrawMail);
        public static Sprite ShopIcon => Icon("shop", DrawShop);
        public static Sprite CoinIcon => Icon("coin", DrawCoin);
        public static Sprite KeyIcon => Icon("key", DrawKey);
        public static Sprite EnergyIcon => Icon("energy", DrawEnergy);
        public static Sprite PrevIcon => Icon("prev", (p, s) => DrawArrow(p, s, false));
        public static Sprite NextIcon => Icon("next", (p, s) => DrawArrow(p, s, true));
        public static Sprite CloseIcon => Icon("close", DrawClose);

        public static Sprite ResourcePill => RoundedButton("resource-pill", 260, 58,
            new Color32(9, 23, 43, 210), new Color32(21, 54, 88, 230), new Color32(247, 194, 75, 255), 20);

        static Sprite RoundedButton(string key, int width, int height, Color32 bottom, Color32 top, Color32 border, int radius)
        {
            return GetOrCreate(key, width, height, tex =>
            {
                Clear(tex);
                for (var y = 0; y < height; y++)
                {
                    var t = y / (float)(height - 1);
                    var fill = Color32.Lerp(bottom, top, t);
                    for (var x = 0; x < width; x++)
                    {
                        if (!InsideRoundedRect(x, y, width, height, radius))
                            continue;
                        tex.SetPixel(x, y, fill);
                    }
                }

                StrokeRounded(tex, width, height, radius, border, 5);
                StrokeRounded(tex, width - 12, height - 12, Mathf.Max(2, radius - 6), new Color32(255, 255, 255, 42), 2, 6, 6);
                FillRect(tex, 24, height - 26, width - 48, 8, new Color32(255, 255, 255, 40));
            }, new Vector4(radius, radius, radius, radius));
        }

        static Sprite RoundedPanel(string key, int width, int height, Color32 fill, Color32 inner, Color32 border, int radius)
        {
            return GetOrCreate(key, width, height, tex =>
            {
                Clear(tex);
                for (var y = 0; y < height; y++)
                {
                    for (var x = 0; x < width; x++)
                    {
                        if (InsideRoundedRect(x, y, width, height, radius))
                            tex.SetPixel(x, y, fill);
                    }
                }

                FillRect(tex, 12, height - 42, width - 24, 24, inner);
                StrokeRounded(tex, width, height, radius, border, 4);
                StrokeRounded(tex, width - 18, height - 18, Mathf.Max(2, radius - 9), new Color32(255, 255, 255, 28), 2, 9, 9);
            }, new Vector4(radius, radius, radius, radius));
        }

        static Sprite Icon(string key, Action<Texture2D, int> draw)
        {
            return GetOrCreate("icon-" + key, IconSize, IconSize, tex =>
            {
                Clear(tex);
                draw(tex, IconSize);
            }, Vector4.zero);
        }

        static Sprite GetOrCreate(string key, int width, int height, Action<Texture2D> draw, Vector4 border)
        {
            if (Cache.TryGetValue(key, out var sprite) && sprite != null)
                return sprite;

            var tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
            {
                name = "MetaHomeUI_" + key,
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            draw(tex);
            tex.Apply();

            sprite = Sprite.Create(tex, new Rect(0f, 0f, width, height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
            sprite.name = tex.name;
            Cache[key] = sprite;
            return sprite;
        }

        static void DrawHome(Texture2D tex, int s)
        {
            var main = new Color32(100, 204, 255, 255);
            var glow = new Color32(255, 218, 95, 230);
            DrawLine(tex, 28, 64, 64, 98, glow, 10);
            DrawLine(tex, 64, 98, 100, 64, glow, 10);
            DrawLine(tex, 38, 60, 38, 30, main, 10);
            DrawLine(tex, 90, 60, 90, 30, main, 10);
            DrawLine(tex, 38, 30, 90, 30, main, 10);
            FillRect(tex, 57, 30, 14, 28, new Color32(20, 58, 100, 255));
        }

        static void DrawUpgrade(Texture2D tex, int s)
        {
            var main = new Color32(122, 245, 156, 255);
            DrawLine(tex, 64, 24, 64, 96, main, 12);
            DrawLine(tex, 64, 96, 38, 70, main, 12);
            DrawLine(tex, 64, 96, 90, 70, main, 12);
            DrawLine(tex, 36, 38, 92, 38, new Color32(255, 225, 100, 245), 10);
        }

        static void DrawTreasure(Texture2D tex, int s)
        {
            var gold = new Color32(255, 190, 64, 255);
            var dark = new Color32(105, 61, 28, 255);
            FillRect(tex, 28, 34, 72, 48, gold);
            FillRect(tex, 24, 58, 80, 16, new Color32(161, 89, 32, 255));
            StrokeRect(tex, 28, 34, 72, 48, dark, 5);
            DrawCircle(tex, 64, 58, 9, new Color32(48, 72, 118, 255));
        }

        static void DrawSettings(Texture2D tex, int s)
        {
            var c = new Color32(204, 222, 244, 255);
            DrawCircle(tex, 64, 64, 31, c, false, 8);
            DrawCircle(tex, 64, 64, 10, c);
            for (var i = 0; i < 8; i++)
            {
                var a = i * Mathf.PI * 0.25f;
                var x1 = 64 + Mathf.RoundToInt(Mathf.Cos(a) * 34f);
                var y1 = 64 + Mathf.RoundToInt(Mathf.Sin(a) * 34f);
                var x2 = 64 + Mathf.RoundToInt(Mathf.Cos(a) * 47f);
                var y2 = 64 + Mathf.RoundToInt(Mathf.Sin(a) * 47f);
                DrawLine(tex, x1, y1, x2, y2, c, 8);
            }
        }

        static void DrawSword(Texture2D tex, int s)
        {
            var blade = new Color32(230, 244, 255, 255);
            var gold = new Color32(255, 204, 74, 255);
            DrawLine(tex, 34, 28, 88, 96, blade, 11);
            DrawLine(tex, 44, 26, 94, 86, new Color32(99, 193, 255, 230), 4);
            DrawLine(tex, 32, 54, 58, 34, gold, 9);
            DrawLine(tex, 27, 25, 42, 40, new Color32(45, 72, 118, 255), 11);
        }

        static void DrawMail(Texture2D tex, int s)
        {
            var c = new Color32(255, 218, 102, 255);
            FillRect(tex, 26, 38, 76, 52, new Color32(36, 93, 141, 255));
            StrokeRect(tex, 26, 38, 76, 52, c, 5);
            DrawLine(tex, 29, 87, 64, 60, c, 5);
            DrawLine(tex, 99, 87, 64, 60, c, 5);
        }

        static void DrawShop(Texture2D tex, int s)
        {
            var c = new Color32(255, 216, 93, 255);
            FillRect(tex, 30, 43, 68, 43, new Color32(31, 98, 150, 255));
            StrokeRect(tex, 30, 43, 68, 43, c, 5);
            DrawLine(tex, 36, 88, 92, 88, c, 8);
            DrawLine(tex, 44, 88, 36, 68, c, 7);
            DrawLine(tex, 84, 88, 92, 68, c, 7);
        }

        static void DrawCoin(Texture2D tex, int s)
        {
            DrawCircle(tex, 64, 64, 39, new Color32(139, 78, 24, 255));
            DrawCircle(tex, 64, 64, 34, new Color32(255, 190, 50, 255));
            DrawCircle(tex, 64, 64, 24, new Color32(255, 224, 105, 255));
            DrawLine(tex, 54, 79, 74, 79, new Color32(129, 77, 24, 245), 6);
            DrawLine(tex, 64, 48, 64, 78, new Color32(129, 77, 24, 245), 7);
        }

        static void DrawKey(Texture2D tex, int s)
        {
            var c = new Color32(255, 214, 87, 255);
            DrawCircle(tex, 45, 73, 18, c, false, 8);
            DrawLine(tex, 58, 62, 94, 26, c, 10);
            DrawLine(tex, 82, 38, 98, 38, c, 8);
            DrawLine(tex, 73, 47, 83, 57, c, 7);
        }

        static void DrawEnergy(Texture2D tex, int s)
        {
            var c = new Color32(255, 232, 75, 255);
            DrawLine(tex, 70, 18, 42, 69, c, 16);
            DrawLine(tex, 42, 69, 67, 69, c, 16);
            DrawLine(tex, 67, 69, 51, 110, c, 16);
            DrawLine(tex, 53, 110, 91, 54, c, 16);
            DrawLine(tex, 91, 54, 65, 54, c, 16);
        }

        static void DrawArrow(Texture2D tex, int s, bool right)
        {
            var c = new Color32(255, 228, 115, 255);
            if (right)
            {
                DrawLine(tex, 42, 64, 88, 64, c, 12);
                DrawLine(tex, 88, 64, 66, 42, c, 12);
                DrawLine(tex, 88, 64, 66, 86, c, 12);
            }
            else
            {
                DrawLine(tex, 86, 64, 40, 64, c, 12);
                DrawLine(tex, 40, 64, 62, 42, c, 12);
                DrawLine(tex, 40, 64, 62, 86, c, 12);
            }
        }

        static void DrawClose(Texture2D tex, int s)
        {
            var c = new Color32(229, 238, 248, 255);
            DrawLine(tex, 40, 40, 88, 88, c, 12);
            DrawLine(tex, 88, 40, 40, 88, c, 12);
        }

        static bool InsideRoundedRect(int x, int y, int w, int h, int r)
        {
            var px = x < r ? r : x >= w - r ? w - r - 1 : x;
            var py = y < r ? r : y >= h - r ? h - r - 1 : y;
            var dx = x - px;
            var dy = y - py;
            return dx * dx + dy * dy <= r * r;
        }

        static void StrokeRounded(Texture2D tex, int w, int h, int r, Color32 color, int thickness, int ox = 0, int oy = 0)
        {
            for (var y = 0; y < h; y++)
            for (var x = 0; x < w; x++)
            {
                if (!InsideRoundedRect(x, y, w, h, r))
                    continue;
                if (x >= thickness && y >= thickness && x < w - thickness && y < h - thickness &&
                    InsideRoundedRect(x - thickness, y - thickness, w - thickness * 2, h - thickness * 2, Mathf.Max(1, r - thickness)))
                    continue;
                Set(tex, x + ox, y + oy, color);
            }
        }

        static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color32 color, int thickness)
        {
            var dx = Mathf.Abs(x1 - x0);
            var sx = x0 < x1 ? 1 : -1;
            var dy = -Mathf.Abs(y1 - y0);
            var sy = y0 < y1 ? 1 : -1;
            var err = dx + dy;
            while (true)
            {
                DrawCircle(tex, x0, y0, Mathf.Max(1, thickness / 2), color);
                if (x0 == x1 && y0 == y1)
                    break;
                var e2 = 2 * err;
                if (e2 >= dy)
                {
                    err += dy;
                    x0 += sx;
                }
                if (e2 <= dx)
                {
                    err += dx;
                    y0 += sy;
                }
            }
        }

        static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color32 color, bool filled = true, int thickness = 1)
        {
            var r2 = radius * radius;
            var inner = Mathf.Max(0, radius - thickness);
            var inner2 = inner * inner;
            for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
            {
                var d = x * x + y * y;
                if (filled ? d <= r2 : d <= r2 && d >= inner2)
                    Set(tex, cx + x, cy + y, color);
            }
        }

        static void FillRect(Texture2D tex, int x, int y, int w, int h, Color32 color)
        {
            for (var yy = y; yy < y + h; yy++)
            for (var xx = x; xx < x + w; xx++)
                Set(tex, xx, yy, color);
        }

        static void StrokeRect(Texture2D tex, int x, int y, int w, int h, Color32 color, int thickness)
        {
            FillRect(tex, x, y, w, thickness, color);
            FillRect(tex, x, y + h - thickness, w, thickness, color);
            FillRect(tex, x, y, thickness, h, color);
            FillRect(tex, x + w - thickness, y, thickness, h, color);
        }

        static void Clear(Texture2D tex)
        {
            for (var y = 0; y < tex.height; y++)
            for (var x = 0; x < tex.width; x++)
                tex.SetPixel(x, y, Color.clear);
        }

        static void Set(Texture2D tex, int x, int y, Color32 color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
                return;
            tex.SetPixel(x, y, color);
        }
    }
}
