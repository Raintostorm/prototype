using System;

namespace SpinSquad.Data
{
    /// <summary>
    /// Năm dòng ally (0–4) map sang <see cref="UnitDefinition"/> trong catalog — ngũ hành starter: Mộc/Hỏa/Kim/Thủy + Knight (Thổ).
    /// </summary>
    public static class AllyLineCatalog
    {
        public const int LineCount = 5;

        public const string Line0Id = "ally_moc";
        public const string Line1Id = "ally_hoa";
        public const string Line2Id = "ally_kim";
        public const string Line3Id = "ally_thuy";
        public const string Line4Id = "common_melee";

        public static int LineIndexFromUnitId(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return 0;
            if (unitId == Line0Id || unitId.StartsWith("unit_l0_"))
                return 0;
            if (unitId == Line1Id)
                return 1;
            if (unitId == Line2Id || unitId.StartsWith("unit_l2_"))
                return 2;
            if (unitId == Line3Id)
                return 3;
            if (unitId == Line4Id || unitId.StartsWith("unit_l1_"))
                return 4;
            if (unitId == "unit_slip_slinger")
                return 0;
            if (unitId == "unit_iron_guard")
                return 4;
            if (unitId == "unit_ally_ranged")
                return 3;
            if (unitId == "unit_brush_warden")
                return 1;
            return 0;
        }

        public static string UnitIdForLine(int line, Rarity rarity)
        {
            _ = rarity;
            return ClampLineIndex(line) switch
            {
                1 => Line1Id,
                2 => Line2Id,
                3 => Line3Id,
                4 => Line4Id,
                _ => Line0Id
            };
        }

        public static int ClampLineIndex(int line)
        {
            if (line < 0)
                return 0;
            if (line >= LineCount)
                return LineCount - 1;
            return line;
        }

        public static int PickRandomLineIndex(Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));
            return rng.Next(0, LineCount);
        }
    }
}
