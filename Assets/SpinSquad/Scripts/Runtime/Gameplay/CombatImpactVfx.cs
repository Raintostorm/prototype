using UnityEngine;

namespace SpinSquad.Core
{
    public sealed class CombatImpactVfx : MonoBehaviour
    {
        const float Lifetime = 0.22f;
        static Sprite _sparkSprite;
        static Sprite _slashSprite;

        SpriteRenderer _sr;
        Vector3 _startScale;
        float _age;

        public static void SpawnSpark(Vector3 worldPos, Color color, float scale = 1f)
        {
            Spawn(worldPos, color, scale, SparkSprite(), Random.Range(-18f, 18f));
        }

        public static void SpawnSlash(Vector3 worldPos, Color color, float scale = 1f)
        {
            Spawn(worldPos, color, scale, SlashSprite(), Random.Range(-28f, 28f));
        }

        static void Spawn(Vector3 worldPos, Color color, float scale, Sprite sprite, float angle)
        {
            if (sprite == null)
                return;

            worldPos.z = -0.05f;
            var go = new GameObject("CombatImpactVfx");
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            go.transform.localScale = Vector3.one * Mathf.Max(0.01f, scale);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = 650;

            var vfx = go.AddComponent<CombatImpactVfx>();
            vfx._sr = sr;
            vfx._startScale = go.transform.localScale;
        }

        void Update()
        {
            _age += Time.deltaTime;
            var t = Mathf.Clamp01(_age / Lifetime);
            var easeOut = 1f - (1f - t) * (1f - t);
            transform.localScale = _startScale * Mathf.Lerp(0.72f, 1.42f, easeOut);
            if (_sr != null)
            {
                var c = _sr.color;
                c.a = 1f - t;
                _sr.color = c;
            }

            if (_age >= Lifetime)
                Destroy(gameObject);
        }

        static Sprite SparkSprite()
        {
            if (_sparkSprite != null)
                return _sparkSprite;

            var tex = new Texture2D(64, 64, TextureFormat.RGBA32, false)
            {
                name = "CombatImpactSpark",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Clear(tex);
            DrawLine(tex, 8, 32, 56, 32, Color.white, 6);
            DrawLine(tex, 32, 8, 32, 56, Color.white, 6);
            DrawLine(tex, 15, 15, 49, 49, new Color(1f, 1f, 1f, 0.75f), 4);
            DrawLine(tex, 49, 15, 15, 49, new Color(1f, 1f, 1f, 0.75f), 4);
            DrawCircle(tex, 32, 32, 10, new Color(1f, 1f, 1f, 0.92f));
            tex.Apply();
            _sparkSprite = Sprite.Create(tex, new Rect(0f, 0f, 64f, 64f), new Vector2(0.5f, 0.5f), 100f);
            return _sparkSprite;
        }

        static Sprite SlashSprite()
        {
            if (_slashSprite != null)
                return _slashSprite;

            var tex = new Texture2D(96, 48, TextureFormat.RGBA32, false)
            {
                name = "CombatImpactSlash",
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp
            };
            Clear(tex);
            for (var i = 0; i < 72; i++)
            {
                var x = 12 + i;
                var y = 10 + Mathf.RoundToInt(Mathf.Sin(i / 72f * Mathf.PI) * 26f);
                DrawCircle(tex, x, y, 5, new Color(1f, 1f, 1f, 0.88f));
                DrawCircle(tex, x, y - 4, 3, new Color(1f, 0.82f, 0.28f, 0.55f));
            }
            tex.Apply();
            _slashSprite = Sprite.Create(tex, new Rect(0f, 0f, 96f, 48f), new Vector2(0.5f, 0.5f), 100f);
            return _slashSprite;
        }

        static void DrawLine(Texture2D tex, int x0, int y0, int x1, int y1, Color color, int thickness)
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

        static void DrawCircle(Texture2D tex, int cx, int cy, int radius, Color color)
        {
            var r2 = radius * radius;
            for (var y = -radius; y <= radius; y++)
            for (var x = -radius; x <= radius; x++)
            {
                if (x * x + y * y <= r2)
                    Set(tex, cx + x, cy + y, color);
            }
        }

        static void Clear(Texture2D tex)
        {
            for (var y = 0; y < tex.height; y++)
            for (var x = 0; x < tex.width; x++)
                tex.SetPixel(x, y, Color.clear);
        }

        static void Set(Texture2D tex, int x, int y, Color color)
        {
            if (x < 0 || y < 0 || x >= tex.width || y >= tex.height)
                return;
            tex.SetPixel(x, y, color);
        }
    }
}
