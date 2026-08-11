using System.Collections.Generic;
using SpinSquad.Data;

namespace SpinSquad.Gacha
{
    public readonly struct AllyRollGrant
    {
        /// <summary>Dòng ally 0–2 (cùng không gian với merge).</summary>
        public readonly byte AllyLineIndex;
        public readonly Rarity RarityTier;

        public AllyRollGrant(byte allyLineIndex, Rarity rarityTier)
        {
            AllyLineIndex = allyLineIndex;
            RarityTier = rarityTier;
        }
    }

    public readonly struct BuffRollAdd
    {
        public readonly BuffStatKind Stat;
        public readonly float Value;

        /// <summary>Value: HP/Dmg/AtkSpeed = phân số (0.1 = 10%). CritChance = xác suất 0–1 (0.01 = 1%).</summary>
        public BuffRollAdd(BuffStatKind stat, float value)
        {
            Stat = stat;
            Value = value;
        }
    }

    public sealed class RollPayout
    {
        public RollCellKind[] Cells { get; }
        public int CoinAwarded { get; set; }
        public bool Jackpot { get; set; }
        public List<AllyRollGrant> AllyGrants { get; } = new();
        public List<BuffRollAdd> BuffAdds { get; } = new();
        public string SummaryLine { get; set; } = string.Empty;

        public RollPayout(RollCellKind[] cells)
        {
            Cells = cells;
        }
    }
}
