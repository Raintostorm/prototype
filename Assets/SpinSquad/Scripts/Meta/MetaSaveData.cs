using System;
using System.Collections.Generic;
using SpinSquad.Data;

namespace SpinSquad.Meta
{
    public enum LineUpgradeStatus
    {
        CanUpgrade = 0,
        NeedGold = 1,
        MaxLevel = 2
    }

    public enum TreasureUpgradeStatus
    {
        NotOwned = 0,
        OwnedNeedDuplicates = 1,
        OwnedCanLevelUp = 2,
        MaxLevel = 3
    }

    [Serializable]
    public sealed class OwnedTreasureData
    {
        public string TreasureId;
        public Rarity Rarity;
        public int Level = 1;
        public int DuplicateShards;
    }

    [Serializable]
    public sealed class MetaSaveData
    {
        public int Version = 3;
        public int UnlockedLevel = 1;
        public int SelectedLevel = 1;
        public int Gold;
        public int TreasureKeys;
        /// <summary>Stub stamina — hiển thị HUD; gameplay spend chưa nối.</summary>
        public int Energy = 100;
        public int MaxEnergy = 100;
        // Legacy line-only upgrade data (kept for migration).
        public int[] LineUpgradeLevels = new int[5];
        // Current upgrade storage: 5 lines x 4 rarities (Common..Legendary) = 20 entries.
        public int[] LineRarityUpgradeLevels = new int[20];
        public List<OwnedTreasureData> Treasures = new();
    }

    public readonly struct LineUpgradeSnapshot
    {
        public readonly int LineIndex;
        public readonly Rarity RarityTier;
        public readonly int Level;
        public readonly int MaxLevel;
        public readonly int Cost;
        public readonly int MissingGold;
        public readonly LineUpgradeStatus Status;
        public readonly float FlatHpPct;
        public readonly float FlatDmgPct;
        public readonly float FlatAtkSpeedPct;
        public readonly float PercentHpPct;
        public readonly float PercentDmgPct;
        public readonly float PercentAtkSpeedPct;
        public readonly float TotalHpPct;
        public readonly float TotalDmgPct;
        public readonly float TotalAtkSpeedPct;
        public readonly float NextTotalHpPct;
        public readonly float NextTotalDmgPct;
        public readonly float NextTotalAtkSpeedPct;

        public LineUpgradeSnapshot(
            int lineIndex,
            Rarity rarityTier,
            int level,
            int maxLevel,
            int cost,
            int missingGold,
            LineUpgradeStatus status,
            float flatHpPct,
            float flatDmgPct,
            float flatAtkSpeedPct,
            float percentHpPct,
            float percentDmgPct,
            float percentAtkSpeedPct,
            float totalHpPct,
            float totalDmgPct,
            float totalAtkSpeedPct,
            float nextTotalHpPct,
            float nextTotalDmgPct,
            float nextTotalAtkSpeedPct)
        {
            LineIndex = lineIndex;
            RarityTier = rarityTier;
            Level = level;
            MaxLevel = maxLevel;
            Cost = cost;
            MissingGold = missingGold;
            Status = status;
            FlatHpPct = flatHpPct;
            FlatDmgPct = flatDmgPct;
            FlatAtkSpeedPct = flatAtkSpeedPct;
            PercentHpPct = percentHpPct;
            PercentDmgPct = percentDmgPct;
            PercentAtkSpeedPct = percentAtkSpeedPct;
            TotalHpPct = totalHpPct;
            TotalDmgPct = totalDmgPct;
            TotalAtkSpeedPct = totalAtkSpeedPct;
            NextTotalHpPct = nextTotalHpPct;
            NextTotalDmgPct = nextTotalDmgPct;
            NextTotalAtkSpeedPct = nextTotalAtkSpeedPct;
        }
    }

    public readonly struct TreasureUpgradeSnapshot
    {
        public readonly bool IsOwned;
        public readonly int Level;
        public readonly int MaxLevel;
        public readonly int DuplicateShards;
        public readonly int RequiredDuplicates;
        public readonly int MissingDuplicates;
        public readonly TreasureUpgradeStatus Status;

        public TreasureUpgradeSnapshot(
            bool isOwned,
            int level,
            int maxLevel,
            int duplicateShards,
            int requiredDuplicates,
            int missingDuplicates,
            TreasureUpgradeStatus status)
        {
            IsOwned = isOwned;
            Level = level;
            MaxLevel = maxLevel;
            DuplicateShards = duplicateShards;
            RequiredDuplicates = requiredDuplicates;
            MissingDuplicates = missingDuplicates;
            Status = status;
        }
    }

    public readonly struct TreasurePassiveTotals
    {
        public readonly float AllyHpPct;
        public readonly float AllyDmgPct;
        public readonly float AllyAtkSpeedPct;
        public readonly float AllyCritChanceFlat;
        public readonly int StartRollCoinBonus;
        public readonly int WaveRollCoinBonus;

        public TreasurePassiveTotals(
            float allyHpPct,
            float allyDmgPct,
            float allyAtkSpeedPct,
            float allyCritChanceFlat,
            int startRollCoinBonus,
            int waveRollCoinBonus)
        {
            AllyHpPct = allyHpPct;
            AllyDmgPct = allyDmgPct;
            AllyAtkSpeedPct = allyAtkSpeedPct;
            AllyCritChanceFlat = allyCritChanceFlat;
            StartRollCoinBonus = startRollCoinBonus;
            WaveRollCoinBonus = waveRollCoinBonus;
        }
    }
}
