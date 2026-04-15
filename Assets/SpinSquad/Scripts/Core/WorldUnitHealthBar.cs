using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Thanh máu world-space độc lập với sprite unit (không bị scale theo model/asset).
    /// </summary>
    [RequireComponent(typeof(CombatHealth))]
    public sealed class WorldUnitHealthBar : MonoBehaviour
    {
        /// <summary>Khoảng cách world phía trên pivot unit — giữ nhỏ hơn ~nửa ô để bar sát unit.</summary>
        [SerializeField] float offsetY = 0.3f;
        [SerializeField] float barWidth = 0.82f;
        [SerializeField] float barHeight = 0.11f;

        CombatHealth _hp;
        AllyInstanceSpec _allySpec;
        EnemyInstanceSpec _enemySpec;
        GameObject _root;
        SpriteRenderer _fillSr;

        static Sprite _squareSprite;

        void Awake()
        {
            _hp = GetComponent<CombatHealth>();
            _allySpec = GetComponent<AllyInstanceSpec>();
            _enemySpec = GetComponent<EnemyInstanceSpec>();
            BuildVisuals();
        }

        void OnDestroy()
        {
            if (_root != null)
                Destroy(_root);
        }

        void LateUpdate()
        {
            if (_root == null || _hp == null)
                return;

            var show = !_hp.IsDead;
            _root.SetActive(show);
            if (!show)
                return;

            _root.transform.position = transform.position + new Vector3(0f, offsetY, 0f);

            var t = _hp.Max > 0.0001f ? Mathf.Clamp01(_hp.Current / _hp.Max) : 0f;
            _fillSr.color = FillTintForFaction();

            _fillSr.transform.localScale = new Vector3(barWidth * t, barHeight, 1f);
            // Pivot sprite 0.5,0.5 — co đối xứng quanh tâm khung (không neo mép trái).
            _fillSr.transform.localPosition = new Vector3(0f, 0f, -0.002f);
        }

        void BuildVisuals()
        {
            _root = new GameObject($"WorldHpBar_{name}");
            _root.transform.position = transform.position + new Vector3(0f, offsetY, 0f);
            _root.transform.rotation = Quaternion.identity;
            _root.transform.localScale = Vector3.one;

            var bgGo = new GameObject("Bg");
            bgGo.transform.SetParent(_root.transform, false);
            var bg = bgGo.AddComponent<SpriteRenderer>();
            bg.sprite = SquareSprite();
            bg.color = new Color(0.04f, 0.04f, 0.07f, 0.95f);
            bg.sortingOrder = 35;
            bg.transform.localScale = new Vector3(barWidth, barHeight, 1f);

            var frameGo = new GameObject("Frame");
            frameGo.transform.SetParent(_root.transform, false);
            var frame = frameGo.AddComponent<SpriteRenderer>();
            frame.sprite = SquareSprite();
            frame.color = new Color(0.95f, 0.95f, 1f, 0.8f);
            frame.sortingOrder = 36;
            frame.transform.localScale = new Vector3(barWidth + 0.03f, barHeight + 0.03f, 1f);
            frame.transform.localPosition = new Vector3(0f, 0f, 0.002f);

            var fillGo = new GameObject("Fill");
            fillGo.transform.SetParent(_root.transform, false);
            _fillSr = fillGo.AddComponent<SpriteRenderer>();
            _fillSr.sprite = SquareSprite();
            _fillSr.sortingOrder = 37;
        }

        Color FillTintForFaction()
        {
            const float a = 0.98f;
            if (_hp.Faction == CombatFaction.Ally)
            {
                if (_allySpec != null)
                {
                    var c = RarityPalette.UnitTint(_allySpec.ResolveLeader().RarityTier, UnitTeamKind.Ally);
                    c = Color.Lerp(c, new Color(0.45f, 1f, 0.55f, 1f), 0.35f);
                    c.a = a;
                    return c;
                }

                return new Color(0.38f, 1f, 0.46f, a);
            }

            if (_enemySpec != null)
            {
                var c = RarityPalette.UnitTint(_enemySpec.ResolveLeader().RarityTier, UnitTeamKind.Enemy);
                c = Color.Lerp(c, new Color(1f, 0.55f, 0.35f, 1f), 0.25f);
                c.a = a;
                return c;
            }

            return new Color(1f, 0.32f, 0.24f, a);
        }

        static Sprite SquareSprite()
        {
            if (_squareSprite != null)
                return _squareSprite;
            var tex = Texture2D.whiteTexture;
            _squareSprite = Sprite.Create(
                tex,
                new Rect(0f, 0f, tex.width, tex.height),
                new Vector2(0.5f, 0.5f),
                16f);
            return _squareSprite;
        }
    }
}
