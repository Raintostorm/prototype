using UnityEngine;

namespace SpinSquad.Data
{
    public static class RarityPalette
    {
        static Color Soften(Color c, float toWhite = 0.08f) =>
            Color.Lerp(c, Color.white, toWhite);

        /// <summary>Nền thẻ / panel meta upgrade — cùng tông <see cref="UnitTint"/> nhưng tối hơn cho chữ trắng.</summary>
        public static Color UpgradeCardBackground(Rarity rarity)
        {
            var baseTint = UnitTint(rarity, UnitTeamKind.Ally);
            var dark = Color.Lerp(baseTint, Color.black, 0.42f);
            dark.a = 0.96f;
            return dark;
        }

        /// <summary>Màu sprite unit: ally theo rarity rõ rệt; enemy tông đỏ.</summary>
        public static Color UnitTint(Rarity rarity, UnitTeamKind team)
        {
            if (team == UnitTeamKind.Enemy)
                return EnemyTint(rarity);

            return rarity switch
            {
                Rarity.Common => Soften(new Color(0.55f, 0.62f, 0.72f, 1f)),
                Rarity.Rare => Soften(new Color(0.25f, 0.82f, 0.92f, 1f)),
                Rarity.Epic => Soften(new Color(0.62f, 0.38f, 0.95f, 1f)),
                Rarity.Legendary => Soften(new Color(0.98f, 0.78f, 0.28f, 1f)),
                Rarity.Mythic => Soften(new Color(0.98f, 0.35f, 0.55f, 1f)),
                _ => Soften(new Color(0.55f, 0.62f, 0.72f, 1f))
            };
        }

        static Color EnemyTint(Rarity rarity)
        {
            var deep = new Color(0.92f, 0.1f, 0.08f, 1f);
            var bright = new Color(1f, 0.32f, 0.18f, 1f);
            var t = rarity switch
            {
                Rarity.Common => 0f,
                Rarity.Rare => 0.22f,
                Rarity.Epic => 0.42f,
                Rarity.Legendary => 0.62f,
                Rarity.Mythic => 0.82f,
                _ => 0f
            };
            return Color.Lerp(deep, bright, t);
        }
    }
}
