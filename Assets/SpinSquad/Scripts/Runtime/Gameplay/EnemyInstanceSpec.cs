using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>Runtime enemy trên lưới: catalog, rarity, chỉ số, stack tối đa 3/ô (cùng unit + rarity).</summary>
    public sealed class EnemyInstanceSpec : MonoBehaviour
    {
        [SerializeField] string catalogUnitId;
        [SerializeField] Rarity rarityTier;
        [SerializeField] float combatMaxHitPoints = 50f;
        [SerializeField] float combatAttack = 10f;
        [SerializeField] UnitBodyShape bodyShape = UnitBodyShape.Square;
        [SerializeField] bool stackLeader = true;
        [SerializeField] EnemyInstanceSpec stackLeaderSpec;

        public string CatalogUnitId => catalogUnitId;
        public Rarity RarityTier => rarityTier;
        public float CombatMaxHitPoints => combatMaxHitPoints;
        public float CombatAttack => combatAttack;
        public UnitBodyShape BodyShape => bodyShape;
        public bool StackLeader => stackLeader;
        public EnemyInstanceSpec StackLeaderSpec => stackLeaderSpec;

        public void InitFromDefinition(UnitDefinition def, Rarity tier, float maxHp, float attack)
        {
            if (def == null)
            {
                catalogUnitId = string.Empty;
                rarityTier = tier;
                bodyShape = UnitBodyShape.Square;
            }
            else
            {
                catalogUnitId = def.UnitId;
                rarityTier = tier;
                bodyShape = def.BodyShape;
            }

            combatMaxHitPoints = maxHp;
            combatAttack = attack;
            stackLeader = true;
            stackLeaderSpec = null;
        }

        public void SetStackFollower(EnemyInstanceSpec leader)
        {
            stackLeader = false;
            stackLeaderSpec = leader;
        }

        public void ApplyMergeUpgrade(Rarity newRarity, float newMaxHp, float newAttack)
        {
            rarityTier = newRarity;
            combatMaxHitPoints = newMaxHp;
            combatAttack = newAttack;
            stackLeader = true;
            stackLeaderSpec = null;
        }

        public EnemyInstanceSpec ResolveLeader() => stackLeaderSpec != null ? stackLeaderSpec : this;
    }
}
