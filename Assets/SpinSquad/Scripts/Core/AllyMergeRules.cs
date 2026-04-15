using SpinSquad.Data;

namespace SpinSquad.Core
{
    public static class AllyMergeRules
    {
        /// <summary>Enemy merge 3→1 (giữ cân cũ).</summary>
        public const float HpMultiplier = 1.78f;
        public const float AttackMultiplier = 1.52f;

        /// <summary>Hằng số cũ cho merge ally tại chỗ — hiện merge ally spawn mới dùng <see cref="AllyStatScaling.ScaleStats"/>; giữ cho script/editor tham chiếu.</summary>
        public const float AllyMergeHpMultiplier = 3f;
        public const float AllyMergeAttackMultiplier = 3f;

        public const int MaxStackPerCell = 3;

        public static Rarity NextRarity(Rarity current)
        {
            if (current >= Rarity.Mythic)
                return current;
            return (Rarity)((int)current + 1);
        }

        /// <summary>Merge 3→1 rarity kế: tới Epic gồm; Legendary trở lên không merge.</summary>
        public static bool CanMerge(Rarity current) => current < Rarity.Legendary;

        /// <summary>3 Mythic cùng unit — ghép bằng công thức Combine (khác merge).</summary>
        public static bool CanCombine(Rarity current) => current == Rarity.Mythic;
    }
}
