using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// HP/ATK ally theo <b>bậc hiếm instance</b> (Common…): bảng cố định, nhân thêm theo catalog so với mốc 100 HP / 15 ATK.
    /// </summary>
    public static class AllyStatScaling
    {
        /// <summary>Mốc “Common” để scale theo từng unit trong catalog.</summary>
        public const float ReferenceCommonHp = 100f;

        public const float ReferenceCommonAtk = 15f;

        /// <summary>Giữ hằng số cũ cho script/editor; combat dùng <see cref="ScaleStats"/>.</summary>
        public const float TierStepMultiplier = 3.5f;

        /// <summary>Bảng base HP/ATK theo bậc (trước khi nhân theo line catalog).</summary>
        public static (float hp, float atk) TableBaseForRarity(Rarity tier)
        {
            switch (ClampAllyTier(tier))
            {
                case Rarity.Common:
                    return (100f, 15f);
                case Rarity.Rare:
                    return (300f, 45f);
                case Rarity.Epic:
                    return (900f, 135f);
                case Rarity.Legendary:
                    return (2700f, 305f);
                default:
                    // Mythic: chưa có spec riêng — một bước ×3 từ Legendary.
                    return (8100f, 915f);
            }
        }

        static Rarity ClampAllyTier(Rarity tier)
        {
            var v = (int)tier;
            if (v < 0)
                return Rarity.Common;
            if (v > (int)Rarity.Mythic)
                return Rarity.Mythic;
            return (Rarity)v;
        }

        /// <summary>Số bậc tier so với rarity ghi trên <see cref="UnitDefinition"/> (tối thiểu 0) — giữ API cũ.</summary>
        public static int TierStepsAboveDefinition(UnitDefinition def, Rarity tier)
        {
            if (def == null)
                return Mathf.Max(0, (int)tier);
            return Mathf.Max(0, (int)tier - (int)def.Rarity);
        }

        public static float TierMultiplier(UnitDefinition def, Rarity tier) =>
            Mathf.Pow(TierStepMultiplier, TierStepsAboveDefinition(def, tier));

        /// <summary>HP/ATK trước meta upgrade / roll: bảng theo tier × tỉ lệ catalog vs 100/15.</summary>
        public static (float hp, float atk) ScaleStats(UnitDefinition def, Rarity tier)
        {
            var (tabHp, tabAtk) = TableBaseForRarity(tier);
            if (def == null)
                return (tabHp, tabAtk);
            var hpLine = def.MaxHitPoints / Mathf.Max(0.01f, ReferenceCommonHp);
            var atkLine = def.Attack / Mathf.Max(0.01f, ReferenceCommonAtk);
            return (tabHp * hpLine, tabAtk * atkLine);
        }

        /// <summary>Khi không có catalog (fallback ally) — dùng bảng theo tier.</summary>
        public static (float hp, float atk) ScaleFallback(float baseHp, float baseAtk, Rarity tier)
        {
            var (tabHp, tabAtk) = TableBaseForRarity(tier);
            var hpLine = baseHp / Mathf.Max(0.01f, ReferenceCommonHp);
            var atkLine = baseAtk / Mathf.Max(0.01f, ReferenceCommonAtk);
            return (tabHp * hpLine, tabAtk * atkLine);
        }
    }
}
