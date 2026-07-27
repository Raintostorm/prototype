using System.Collections.Generic;
using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Đạn ranged ngắn — mặc định sprite tròn + màu phe; có thể override texture từ Resources.</summary>
    public static class RangedShotVfx
    {
        static readonly Color AllyBoltColor = new Color(0.42f, 0.94f, 1f, 1f);
        static readonly Color EnemyBoltColor = new Color(1f, 0.4f, 0.14f, 1f);

        static readonly Dictionary<string, Sprite> BoltSpriteCache = new();

        /// <summary>Texture2D trong Resources (path không extension), readable — cache <see cref="Sprite"/> full rect.</summary>
        public static Sprite GetOrCreateBoltSprite(string resourcesPath)
        {
            if (string.IsNullOrWhiteSpace(resourcesPath))
                return null;
            var key = resourcesPath.Trim();
            if (BoltSpriteCache.TryGetValue(key, out var cached) && cached != null)
                return cached;
            var tex = Resources.Load<Texture2D>(key);
            if (tex == null)
                return null;
            var sp = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            BoltSpriteCache[key] = sp;
            return sp;
        }

        public static void Spawn(Vector3 fromWorld, Vector3 toWorld, CombatFaction shooterFaction, Sprite boltSpriteOverride = null)
        {
            var dir = (Vector2)(toWorld - fromWorld);
            if (dir.sqrMagnitude < 0.0001f)
                dir = Vector2.right;
            dir.Normalize();

            var a = (Vector3)((Vector2)fromWorld + dir * 0.12f);
            var b = (Vector3)Vector2.Lerp(fromWorld, toWorld, 0.94f);
            a.z = b.z = -0.02f;

            var go = new GameObject("RangedShotBolt");
            var bolt = go.AddComponent<RangedShotBolt>();
            var tint = shooterFaction == CombatFaction.Ally ? AllyBoltColor : EnemyBoltColor;
            bolt.Init(a, b, tint, boltSpriteOverride);
        }
    }

    sealed class RangedShotBolt : MonoBehaviour
    {
        Vector3 _from;
        Vector3 _to;
        Color _baseColor;
        float _age;
        const float Duration = 0.32f;
        SpriteRenderer _sr;

        public void Init(Vector3 from, Vector3 to, Color factionTint, Sprite spriteOverride)
        {
            _from = from;
            _to = to;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            if (spriteOverride != null)
            {
                _sr.sprite = spriteOverride;
                _baseColor = Color.white;
            }
            else
            {
                _sr.sprite = UnitSpriteFactory.GetSprite(UnitBodyShape.Circle);
                _baseColor = factionTint;
            }

            _sr.color = _baseColor;
            _sr.sortingOrder = 472;

            transform.position = from;
            var s = spriteOverride != null ? 0.22f : 0.17f;
            transform.localScale = new Vector3(s, s, 1f);

            var ang = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        void Update()
        {
            _age += Time.deltaTime;
            var u = Duration > 0.0001f ? Mathf.Clamp01(_age / Duration) : 1f;
            transform.position = Vector3.Lerp(_from, _to, u);
            var c = _baseColor;
            c.a = _baseColor.a * (1f - u * u);
            _sr.color = c;
            if (_age >= Duration)
                Destroy(gameObject);
        }
    }
}
