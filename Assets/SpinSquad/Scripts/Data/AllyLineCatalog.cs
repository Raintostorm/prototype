using System;

namespace SpinSquad.Data
{
    /// <summary>
    /// Ba dòng ally (0–2) dùng chung cho roll, merge, stack — map sang <see cref="UnitDefinition"/> trong catalog.
    /// </summary>
    public static class AllyLineCatalog
    {
        public const int LineCount = 3;
        public const string Line0Id = "unit_slip_slinger";
        public const string Line1Id = "unit_brush_warden";
        public const string Line2Id = "unit_ally_ranged";

        public static int LineIndexFromUnitId(string unitId)
        {
            if (string.IsNullOrEmpty(unitId))
                return 0;
            if (unitId == Line1Id)
                return 1;
            if (unitId == Line2Id)
                return 2;
            return 0;
        }

        public static string UnitIdForLine(int line) =>
            line switch
            {
                1 => Line1Id,
                2 => Line2Id,
                _ => Line0Id
            };

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
