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

        [Header("Combat style")]
        [Tooltip("Đúng: dừng ở khoảng attackRange, đổi sát thương theo chu kỳ (tầm xa). Sai: lao cận chiến như hiện tại.")]
        [SerializeField] private bool rangedAttack;

        [SerializeField] private float attackRange = 1.65f;
        [SerializeField] private float rangedShotIntervalSeconds = 0.52f;

        public string UnitId => unitId;
        public string DisplayName => displayName;
        public Rarity Rarity => rarity;
        public float MaxHitPoints => maxHitPoints;
        public float Attack => attack;
        public UnitTeamKind TeamKind => teamKind;
        public UnitBodyShape BodyShape => bodyShape;
        public bool RangedAttack => rangedAttack;
        public float AttackRange => attackRange;
        public float RangedShotIntervalSeconds => rangedShotIntervalSeconds;

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
#endif
    }
}
