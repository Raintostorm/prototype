using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Đạn ranged ngắn (sprite tròn bay) — màu cố định theo phe, không theo rarity.</summary>
    public static class RangedShotVfx
    {
        static readonly Color AllyBoltColor = new Color(0.42f, 0.94f, 1f, 1f);
        static readonly Color EnemyBoltColor = new Color(1f, 0.4f, 0.14f, 1f);

        public static void Spawn(Vector3 fromWorld, Vector3 toWorld, CombatFaction shooterFaction)
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
            bolt.Init(a, b, shooterFaction == CombatFaction.Ally ? AllyBoltColor : EnemyBoltColor);
        }
    }

    sealed class RangedShotBolt : MonoBehaviour
    {
        Vector3 _from;
        Vector3 _to;
        Color _color;
        float _age;
        const float Duration = 0.11f;
        SpriteRenderer _sr;

        public void Init(Vector3 from, Vector3 to, Color color)
        {
            _from = from;
            _to = to;
            _color = color;

            _sr = gameObject.AddComponent<SpriteRenderer>();
            _sr.sprite = UnitSpriteFactory.GetSprite(UnitBodyShape.Circle);
            _sr.color = color;
            _sr.sortingOrder = 472;

            transform.position = from;
            const float s = 0.17f;
            transform.localScale = new Vector3(s, s, 1f);

            var ang = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0f, 0f, ang);
        }

        void Update()
        {
            _age += Time.deltaTime;
            var u = Duration > 0.0001f ? Mathf.Clamp01(_age / Duration) : 1f;
            transform.position = Vector3.Lerp(_from, _to, u);
            var c = _color;
            c.a = 1f - u * u;
            _sr.color = c;
            if (_age >= Duration)
                Destroy(gameObject);
        }
    }
}
