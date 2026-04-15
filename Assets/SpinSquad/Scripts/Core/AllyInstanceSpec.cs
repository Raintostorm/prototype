using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Trạng thái runtime một ally trên lưới: catalog, độ hiếm hiện tại, chỉ số combat, stack.</summary>
    public sealed class AllyInstanceSpec : MonoBehaviour
    {
        [Tooltip("Dòng ally 0–2 (roll / merge / stack thống nhất).")]
        [SerializeField] int allyLineIndex;

        [SerializeField] string catalogUnitId;
        [SerializeField] Rarity rarityTier;
        [SerializeField] float combatMaxHitPoints = 50f;
        [SerializeField] float combatAttack = 10f;
        [SerializeField] UnitBodyShape bodyShape = UnitBodyShape.Square;
        [SerializeField] bool stackLeader = true;
        [SerializeField] AllyInstanceSpec stackLeaderSpec;

        public int AllyLineIndex => allyLineIndex;

        public string CatalogUnitId => catalogUnitId;
        public Rarity RarityTier => rarityTier;
        public float CombatMaxHitPoints => combatMaxHitPoints;
        public float CombatAttack => combatAttack;
        public UnitBodyShape BodyShape => bodyShape;
        public bool StackLeader => stackLeader;
        public AllyInstanceSpec StackLeaderSpec => stackLeaderSpec;

        /// <param name="allyLineOverride">0–2 ép dòng; âm = suy từ <see cref="UnitDefinition.UnitId"/>.</param>
        public void InitFromDefinition(UnitDefinition def, Rarity tier, float maxHp, float attack, int allyLineOverride = -1)
        {
            if (def == null)
            {
                catalogUnitId = string.Empty;
                rarityTier = tier;
                bodyShape = UnitBodyShape.Square;
                allyLineIndex = allyLineOverride >= 0 && allyLineOverride <= 2 ? allyLineOverride : 0;
            }
            else
            {
                catalogUnitId = def.UnitId;
                rarityTier = tier;
                bodyShape = def.BodyShape;
                allyLineIndex = allyLineOverride >= 0 && allyLineOverride <= 2
                    ? allyLineOverride
                    : AllyLineCatalog.LineIndexFromUnitId(def.UnitId);
            }

            combatMaxHitPoints = maxHp;
            combatAttack = attack;
            stackLeader = true;
            stackLeaderSpec = null;
        }

        public void SetStackFollower(AllyInstanceSpec leader)
        {
            stackLeader = false;
            stackLeaderSpec = leader;
        }

        public void SetStackLeader()
        {
            stackLeader = true;
            stackLeaderSpec = null;
        }

        public void ApplyMergeUpgrade(Rarity newRarity, float newMaxHp, float newAttack)
        {
            rarityTier = newRarity;
            combatMaxHitPoints = newMaxHp;
            combatAttack = newAttack;
            SetStackLeader();
        }

        public AllyInstanceSpec ResolveLeader() => stackLeaderSpec != null ? stackLeaderSpec : this;
    }
}
