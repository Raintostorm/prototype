using UnityEngine;

namespace SpinSquad.Data
{
    [CreateAssetMenu(menuName = "SpinSquad/Data/Unit Definition", fileName = "NewUnitDefinition")]
    public sealed class UnitDefinition : ScriptableObject
    {
        [SerializeField] private string unitId;
        [SerializeField] private string displayName;
        [SerializeField] private Rarity rarity;
        [SerializeField] private float maxHitPoints = 50f;
        [SerializeField] private float attack = 10f;
        [SerializeField] private UnitTeamKind teamKind = UnitTeamKind.Ally;

        [Header("Visual")]
        [SerializeField] private UnitBodyShape bodyShape = UnitBodyShape.Square;
        [Tooltip("Prefab combat (Spine/world). Dùng Object để Unity 6 serialize đúng prefab asset (GameObject hoặc Prefab).")]
        [SerializeField] private Object battlePrefab;
        [Tooltip("Ưu tiên khi spawn: Resources.Load<GameObject> — path không extension, ví dụ Battle/KnightBattleVisual. Tránh lỗi UnityEngine.Prefab trên field Object.")]
        [SerializeField] private string battlePrefabResourcesPath;
        [SerializeField] private Sprite portraitSprite;
        [SerializeField] private Sprite cardSprite;
        [SerializeField] private Sprite iconSprite;

        [Tooltip("Resources path (no extension) tới RuntimeAnimatorController cho portrait UI. Để trống = không dùng Animator UI (chỉ Image/sprite).")]
        [SerializeField] private string uiAnimatorResourcesPath;

        [Header("Combat style")]
        [Tooltip("Đúng: dừng ở khoảng attackRange, đổi sát thương theo chu kỳ (tầm xa). Sai: lao cận chiến như hiện tại.")]
        [SerializeField] private bool rangedAttack;

        [SerializeField] private float attackRange = 1.65f;
        [SerializeField] private float rangedShotIntervalSeconds = 0.52f;

        [Tooltip("Optional: Resources path (no extension) tới Texture2D cho đạn ranged (ví dụ Vfx/AllyThuyProjectile). Để trống = dùng bolt tròn mặc định.")]
        [SerializeField] private string rangedBoltTextureResourcesPath;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public Rarity Rarity => rarity;
        public float MaxHitPoints => maxHitPoints;
        public float Attack => attack;
        public UnitTeamKind TeamKind => teamKind;
        public UnitBodyShape BodyShape => bodyShape;
        /// <summary>Prefab asset cho battlefield; có thể là root GameObject hoặc asset kiểu Prefab (Unity 6).</summary>
        public Object BattlePrefab => battlePrefab;
        /// <summary>Resources path (no extension) cho battle prefab; khi có giá trị, spawn ưu tiên Resources.Load.</summary>
        public string BattlePrefabResourcesPath => battlePrefabResourcesPath;
        public Sprite PortraitSprite => portraitSprite;
        public Sprite CardSprite => cardSprite;
        public Sprite IconSprite => iconSprite;
        /// <summary>Empty = no UI Animator; portrait uses static sprite only.</summary>
        public string UiAnimatorResourcesPath => uiAnimatorResourcesPath;
        public bool RangedAttack => rangedAttack;
        public float AttackRange => attackRange;
        public float RangedShotIntervalSeconds => rangedShotIntervalSeconds;

        /// <summary>Empty = default ranged bolt (circle) from combat VFX helper.</summary>
        public string RangedBoltTextureResourcesPath => rangedBoltTextureResourcesPath;

        void OnValidate()
        {
            if (bodyShape == UnitBodyShape.Triangle && !rangedAttack)
                rangedAttack = true;
        }

#if UNITY_EDITOR
        public void SetEditorData(string id, string name, Rarity r, float hp, float atk, UnitTeamKind team = UnitTeamKind.Ally)
        {
            unitId = id;
            displayName = name;
            rarity = r;
            maxHitPoints = hp;
            attack = atk;
            teamKind = team;
        }

        public void SetEditorRanged(bool ranged, float range, float intervalSeconds)
        {
            rangedAttack = ranged;
            attackRange = range;
            rangedShotIntervalSeconds = intervalSeconds;
        }

        public void SetEditorVisuals(Sprite portrait, Sprite card, Sprite icon, GameObject battle = null)
        {
            portraitSprite = portrait;
            cardSprite = card;
            iconSprite = icon;
            battlePrefab = battle;
        }
#endif
    }
}
