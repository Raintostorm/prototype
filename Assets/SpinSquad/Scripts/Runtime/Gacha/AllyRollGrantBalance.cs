using SpinSquad.Core;
using SpinSquad.Data;

namespace SpinSquad.Gacha
{
    /// <summary>HP/ATK ally từ roll — <see cref="AllyStatScaling.ScaleStats"/> (bảng theo tier × tỉ lệ catalog so với 100/15).</summary>
    public static class AllyRollGrantBalance
    {
        public static float ScaledHp(UnitDefinition def, Rarity tier) =>
            AllyStatScaling.ScaleStats(def, tier).hp;

        public static float ScaledAttack(UnitDefinition def, Rarity tier) =>
            AllyStatScaling.ScaleStats(def, tier).atk;
    }
}
