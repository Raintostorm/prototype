using System;
using System.Collections.Generic;
using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Meta
{
    public static class MetaProgressionStore
    {
        const string SaveKey = "SpinSquad_MetaProgression_v1";
        const float LineHpPerLevel = 0.015f;
        const float LineDmgPerLevel = 0.0125f;
        const float LineAtkSpeedPerLevel = 0.008f;
        const float PercentPerRarityStep = 0.002f; // +0.2 percentage point each rarity step.
        static MetaSaveData _cache;

        public static int MaxLevels => 3;

        /// <summary>Gold khi xóa sạch một campaign level (hết 10 wave).</summary>
        public static int LevelCompleteGoldReward(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevels);
            return level switch
            {
                1 => 2500,
                2 => 5000,
                3 => 7500,
                _ => 2500
            };
        }

        /// <summary>Key rương khi hoàn thành một campaign level (mỗi level).</summary>
        public static int LevelCompleteTreasureKeyReward() => 5;

        public static int AllyLineCount => 5;
        public static int UpgradableRarityCount => 4; // Common..Legendary
        public static Rarity DefaultUpgradeRarity => Rarity.Common;
        public static Rarity MaxUpgradableRarity => Rarity.Legendary;
        public static int MaxUpgradeLevel => 50;
        public static int DefaultMaxEnergy => 100;

        public static int UnlockedLevel
        {
            get => Data.UnlockedLevel;
            set
            {
                Data.UnlockedLevel = Mathf.Clamp(value, 1, MaxLevels);
                if (Data.SelectedLevel > Data.UnlockedLevel)
                    Data.SelectedLevel = Data.UnlockedLevel;
                Save();
            }
        }

        public static int SelectedLevel
        {
            get => Mathf.Clamp(Data.SelectedLevel, 1, Mathf.Max(1, UnlockedLevel));
            set
            {
                Data.SelectedLevel = Mathf.Clamp(value, 1, Mathf.Max(1, UnlockedLevel));
                Save();
            }
        }

        public static int Gold
        {
            get => Data.Gold;
            set
            {
                Data.Gold = Mathf.Max(0, value);
                Save();
            }
        }

        public static int TreasureKeys
        {
            get => Data.TreasureKeys;
            set
            {
                Data.TreasureKeys = Mathf.Max(0, value);
                Save();
            }
        }

        public static int Energy
        {
            get => Data.Energy;
            set
            {
                Data.Energy = Mathf.Clamp(value, 0, MaxEnergy);
                Save();
            }
        }

        public static int MaxEnergy
        {
            get => Mathf.Max(1, Data.MaxEnergy);
            set
            {
                Data.MaxEnergy = Mathf.Max(1, value);
                Data.Energy = Mathf.Clamp(Data.Energy, 0, Data.MaxEnergy);
                Save();
            }
        }

        static MetaSaveData Data
        {
            get
            {
                if (_cache != null)
                    return _cache;

                var raw = PlayerPrefs.GetString(SaveKey, string.Empty);
                if (string.IsNullOrEmpty(raw))
                    _cache = CreateDefault();
                else
                {
                    try
                    {
                        _cache = JsonUtility.FromJson<MetaSaveData>(raw) ?? CreateDefault();
                    }
                    catch
                    {
                        _cache = CreateDefault();
                    }
                }

                Normalize(_cache);
                return _cache;
            }
        }

        public static void Save()
        {
            Normalize(Data);
            var json = JsonUtility.ToJson(Data);
            PlayerPrefs.SetString(SaveKey, json);
            PlayerPrefs.Save();
        }

        public static void ResetAll()
        {
            _cache = CreateDefault();
            Save();
        }

        public static void AddGold(int amount)
        {
            if (amount <= 0)
                return;
            Data.Gold += amount;
            Save();
        }

        public static bool TrySpendGold(int amount)
        {
            if (amount <= 0)
                return true;
            if (Data.Gold < amount)
                return false;
            Data.Gold -= amount;
            Save();
            return true;
        }

        public static void AddTreasureKeys(int amount)
        {
            if (amount <= 0)
                return;
            Data.TreasureKeys += amount;
            Save();
        }

        public static bool TrySpendTreasureKey(int amount = 1)
        {
            if (amount <= 0)
                return true;
            if (Data.TreasureKeys < amount)
                return false;
            Data.TreasureKeys -= amount;
            Save();
            return true;
        }

        public static int GetUpgradeIndex(int line, Rarity rarity)
        {
            var safeLine = Mathf.Clamp(line, 0, AllyLineCount - 1);
            var step = RarityStep(rarity);
            return safeLine * UpgradableRarityCount + step;
        }

        public static int GetUpgradeLevel(int line, Rarity rarity)
        {
            var idx = GetUpgradeIndex(line, rarity);
            return Data.LineRarityUpgradeLevels[idx];
        }

        public static int GetLineUpgradeLevel(int line) => GetUpgradeLevel(line, DefaultUpgradeRarity);

        public static int GetUpgradeCost(int line, Rarity rarity)
        {
            var lv = GetUpgradeLevel(line, rarity);
            if (lv >= MaxUpgradeLevel)
                return 0;
            return 60 + (26 * lv) + 8 * (lv / 5);
        }

        public static int GetLineUpgradeCost(int line) => GetUpgradeCost(line, DefaultUpgradeRarity);

        public static bool CanUpgradeLine(int line, Rarity rarity) =>
            GetLineUpgradeStatus(line, rarity) == LineUpgradeStatus.CanUpgrade;

        public static bool CanUpgradeLine(int line) => CanUpgradeLine(line, DefaultUpgradeRarity);

        public static LineUpgradeStatus GetLineUpgradeStatus(int line, Rarity rarity)
        {
            var lv = GetUpgradeLevel(line, rarity);
            if (lv >= MaxUpgradeLevel)
                return LineUpgradeStatus.MaxLevel;
            var cost = GetUpgradeCost(line, rarity);
            return Data.Gold >= cost ? LineUpgradeStatus.CanUpgrade : LineUpgradeStatus.NeedGold;
        }

        public static LineUpgradeStatus GetLineUpgradeStatus(int line) =>
            GetLineUpgradeStatus(line, DefaultUpgradeRarity);

        public static LineUpgradeSnapshot GetLineUpgradeSnapshot(int line, Rarity rarity)
        {
            line = Mathf.Clamp(line, 0, AllyLineCount - 1);
            rarity = ClampUpgradableRarity(rarity);
            var lv = GetUpgradeLevel(line, rarity);
            var status = GetLineUpgradeStatus(line, rarity);
            var cost = GetUpgradeCost(line, rarity);
            var missingGold = Mathf.Max(0, cost - Data.Gold);
            var step = RarityStep(rarity);

            var flatHp = ComputeLineFlatPct(lv, LineHpPerLevel, step);
            var flatDmg = ComputeLineFlatPct(lv, LineDmgPerLevel, step);
            var flatAsp = ComputeLineFlatPct(lv, LineAtkSpeedPerLevel, step);
            var pctHp = ComputeAmplification(lv, 0.65f, step);
            var pctDmg = ComputeAmplification(lv, 0.55f, step);
            var pctAsp = ComputeAmplification(lv, 0.45f, step);
            var totalHp = TotalBuffPct(flatHp, pctHp);
            var totalDmg = TotalBuffPct(flatDmg, pctDmg);
            var totalAsp = TotalBuffPct(flatAsp, pctAsp);

            var nextLv = Mathf.Min(MaxUpgradeLevel, lv + 1);
            var nextTotalHp = ComputeLineHpPct(nextLv, step);
            var nextTotalDmg = ComputeLineDmgPct(nextLv, step);
            var nextTotalAsp = ComputeLineAtkSpeedPct(nextLv, step);
            return new LineUpgradeSnapshot(
                line,
                rarity,
                lv,
                MaxUpgradeLevel,
                cost,
                missingGold,
                status,
                flatHp,
                flatDmg,
                flatAsp,
                pctHp,
                pctDmg,
                pctAsp,
                totalHp,
                totalDmg,
                totalAsp,
                nextTotalHp,
                nextTotalDmg,
                nextTotalAsp);
        }

        public static LineUpgradeSnapshot GetLineUpgradeSnapshot(int line) =>
            GetLineUpgradeSnapshot(line, DefaultUpgradeRarity);

        public static bool TryUpgradeLine(int line, Rarity rarity)
        {
            var idx = GetUpgradeIndex(line, rarity);
            if (Data.LineRarityUpgradeLevels[idx] >= MaxUpgradeLevel)
                return false;
            var cost = GetUpgradeCost(line, rarity);
            if (!TrySpendGold(cost))
                return false;
            Data.LineRarityUpgradeLevels[idx] = Mathf.Clamp(Data.LineRarityUpgradeLevels[idx] + 1, 0, MaxUpgradeLevel);
            Save();
            return true;
        }

        public static bool TryUpgradeLine(int line) => TryUpgradeLine(line, DefaultUpgradeRarity);

        public static void UnlockLevel(int level)
        {
            level = Mathf.Clamp(level, 1, MaxLevels);
            if (level <= Data.UnlockedLevel)
                return;
            Data.UnlockedLevel = level;
            Save();
        }

        public static float GetLineUpgradeHpPct(int line, Rarity rarity)
        {
            var step = RarityStep(rarity);
            return ComputeLineHpPct(GetUpgradeLevel(line, rarity), step);
        }

        public static float GetLineUpgradeDmgPct(int line, Rarity rarity)
        {
            var step = RarityStep(rarity);
            return ComputeLineDmgPct(GetUpgradeLevel(line, rarity), step);
        }

        public static float GetLineUpgradeAtkSpeedPct(int line, Rarity rarity)
        {
            var step = RarityStep(rarity);
            return ComputeLineAtkSpeedPct(GetUpgradeLevel(line, rarity), step);
        }

        public static float GetLineUpgradeHpPct(int line) => GetLineUpgradeHpPct(line, DefaultUpgradeRarity);

        public static float GetLineUpgradeDmgPct(int line) => GetLineUpgradeDmgPct(line, DefaultUpgradeRarity);

        public static float GetLineUpgradeAtkSpeedPct(int line) => GetLineUpgradeAtkSpeedPct(line, DefaultUpgradeRarity);

        public static IReadOnlyList<OwnedTreasureData> GetOwnedTreasures() => Data.Treasures;

        public static OwnedTreasureData GetOwnedTreasure(string treasureId)
        {
            if (string.IsNullOrWhiteSpace(treasureId))
                return null;
            foreach (var t in Data.Treasures)
            {
                if (t != null && t.TreasureId == treasureId)
                    return t;
            }

            return null;
        }

        public static TreasureUpgradeSnapshot GetTreasureSnapshot(string treasureId)
        {
            var owned = GetOwnedTreasure(treasureId);
            if (owned == null)
                return new TreasureUpgradeSnapshot(false, 0, MaxUpgradeLevel, 0, 0, 0, TreasureUpgradeStatus.NotOwned);

            if (owned.Level >= MaxUpgradeLevel)
                return new TreasureUpgradeSnapshot(true, MaxUpgradeLevel, MaxUpgradeLevel, 0, 0, 0, TreasureUpgradeStatus.MaxLevel);

            var required = TreasureDefinitions.RequiredShardsForNextLevel(owned.Level);
            var missing = Mathf.Max(0, required - owned.DuplicateShards);
            var status = owned.DuplicateShards >= required
                ? TreasureUpgradeStatus.OwnedCanLevelUp
                : TreasureUpgradeStatus.OwnedNeedDuplicates;
            return new TreasureUpgradeSnapshot(
                true,
                owned.Level,
                MaxUpgradeLevel,
                owned.DuplicateShards,
                required,
                missing,
                status);
        }

        public static OwnedTreasureData GrantTreasure(TreasureDefinition def)
        {
            var id = def.Id;
            OwnedTreasureData owned = null;
            foreach (var t in Data.Treasures)
            {
                if (t.TreasureId == id)
                {
                    owned = t;
                    break;
                }
            }

            if (owned == null)
            {
                owned = new OwnedTreasureData
                {
                    TreasureId = id,
                    Rarity = def.Rarity,
                    Level = 1,
                    DuplicateShards = 0
                };
                Data.Treasures.Add(owned);
            }
            else
            {
                if (owned.Level >= MaxUpgradeLevel)
                {
                    Save();
                    return owned;
                }

                owned.DuplicateShards++;
                while (owned.Level < MaxUpgradeLevel &&
                       owned.DuplicateShards >= TreasureDefinitions.RequiredShardsForNextLevel(owned.Level))
                {
                    owned.DuplicateShards -= TreasureDefinitions.RequiredShardsForNextLevel(owned.Level);
                    owned.Level++;
                }

                if (owned.Level >= MaxUpgradeLevel)
                    owned.DuplicateShards = 0;
            }

            Save();
            return owned;
        }

        public static TreasurePassiveTotals GetTreasurePassiveTotals()
        {
            var hp = 0f;
            var dmg = 0f;
            var asp = 0f;
            var crit = 0f;
            var startCoins = 0;
            var waveCoins = 0;
            foreach (var t in Data.Treasures)
            {
                var def = TreasureDefinitions.GetById(t.TreasureId);
                if (!def.IsValid)
                    continue;
                var levelScale = 1f + (t.Level - 1) * 0.4f;
                var value = def.BaseValue * levelScale;
                switch (def.EffectKind)
                {
                    case TreasureEffectKind.AllyHpPct:
                        hp += value;
                        break;
                    case TreasureEffectKind.AllyDmgPct:
                        dmg += value;
                        break;
                    case TreasureEffectKind.AllyAtkSpeedPct:
                        asp += value;
                        break;
                    case TreasureEffectKind.AllyCritChanceFlat:
                        crit += value;
                        break;
                    case TreasureEffectKind.StartRollCoinBonus:
                        startCoins += Mathf.RoundToInt(value);
                        break;
                    case TreasureEffectKind.WaveRollCoinBonus:
                        waveCoins += Mathf.RoundToInt(value);
                        break;
                }
            }

            return new TreasurePassiveTotals(hp, dmg, asp, crit, startCoins, waveCoins);
        }

        static MetaSaveData CreateDefault()
        {
            return new MetaSaveData
            {
                Version = 3,
                UnlockedLevel = 1,
                SelectedLevel = 1,
                Gold = 0,
                TreasureKeys = 0,
                Energy = DefaultMaxEnergy,
                MaxEnergy = DefaultMaxEnergy,
                LineUpgradeLevels = new int[AllyLineCount],
                LineRarityUpgradeLevels = new int[AllyLineCount * UpgradableRarityCount],
                Treasures = new List<OwnedTreasureData>()
            };
        }

        static void Normalize(MetaSaveData data)
        {
            if (data == null)
                return;
            data.UnlockedLevel = Mathf.Clamp(data.UnlockedLevel, 1, MaxLevels);
            data.SelectedLevel = Mathf.Clamp(data.SelectedLevel, 1, data.UnlockedLevel);
            data.Gold = Mathf.Max(0, data.Gold);
            data.TreasureKeys = Mathf.Max(0, data.TreasureKeys);
            if (data.MaxEnergy < 1)
                data.MaxEnergy = DefaultMaxEnergy;
            if (data.Version < 3)
            {
                if (data.Energy <= 0)
                    data.Energy = data.MaxEnergy > 0 ? data.MaxEnergy : DefaultMaxEnergy;
                data.Version = 3;
            }

            data.Energy = Mathf.Clamp(data.Energy, 0, data.MaxEnergy);
            if (data.LineUpgradeLevels == null)
                data.LineUpgradeLevels = new int[AllyLineCount];
            else if (data.LineUpgradeLevels.Length != AllyLineCount)
            {
                var old = data.LineUpgradeLevels;
                data.LineUpgradeLevels = new int[AllyLineCount];
                var n = Mathf.Min(old.Length, AllyLineCount);
                for (var i = 0; i < n; i++)
                    data.LineUpgradeLevels[i] = old[i];
            }

            var expectedRarityLen = AllyLineCount * UpgradableRarityCount;
            if (data.LineRarityUpgradeLevels == null)
                data.LineRarityUpgradeLevels = new int[expectedRarityLen];
            else if (data.LineRarityUpgradeLevels.Length != expectedRarityLen)
            {
                var oldR = data.LineRarityUpgradeLevels;
                data.LineRarityUpgradeLevels = new int[expectedRarityLen];
                var n = Mathf.Min(oldR.Length, expectedRarityLen);
                for (var i = 0; i < n; i++)
                    data.LineRarityUpgradeLevels[i] = oldR[i];
            }

            if (data.Version < 2)
            {
                for (var line = 0; line < AllyLineCount; line++)
                {
                    var legacy = Mathf.Clamp(data.LineUpgradeLevels[line], 0, MaxUpgradeLevel);
                    var commonIdx = line * UpgradableRarityCount;
                    data.LineRarityUpgradeLevels[commonIdx] =
                        Mathf.Max(data.LineRarityUpgradeLevels[commonIdx], legacy);
                }

                data.Version = 2;
            }

            for (var i = 0; i < data.LineRarityUpgradeLevels.Length; i++)
                data.LineRarityUpgradeLevels[i] = Mathf.Clamp(data.LineRarityUpgradeLevels[i], 0, MaxUpgradeLevel);

            if (data.Treasures == null)
                data.Treasures = new List<OwnedTreasureData>();
            for (var i = data.Treasures.Count - 1; i >= 0; i--)
            {
                var t = data.Treasures[i];
                if (t == null || string.IsNullOrEmpty(t.TreasureId))
                {
                    data.Treasures.RemoveAt(i);
                    continue;
                }

                if (!TreasureDefinitions.GetById(t.TreasureId).IsValid)
                {
                    data.Treasures.RemoveAt(i);
                    continue;
                }

                t.Level = Mathf.Max(1, t.Level);
                t.Level = Mathf.Min(MaxUpgradeLevel, t.Level);
                t.DuplicateShards = Mathf.Max(0, t.DuplicateShards);
                if (t.Level >= MaxUpgradeLevel)
                    t.DuplicateShards = 0;
            }
        }

        static float ComputeLineHpPct(int level, int rarityStep) =>
            ComputeLinePct(level, LineHpPerLevel, 0.65f, rarityStep);

        static float ComputeLineDmgPct(int level, int rarityStep) =>
            ComputeLinePct(level, LineDmgPerLevel, 0.55f, rarityStep);

        static float ComputeLineAtkSpeedPct(int level, int rarityStep) =>
            ComputeLinePct(level, LineAtkSpeedPerLevel, 0.45f, rarityStep);

        public static float GetLineFixedHpPerLevelPct() => LineHpPerLevel;

        public static float GetLineFixedDmgPerLevelPct() => LineDmgPerLevel;

        public static float GetLineFixedAtkSpeedPerLevelPct() => LineAtkSpeedPerLevel;

        public static float GetLineFlatHpBuffPct(int line, Rarity rarity)
        {
            var step = RarityStep(rarity);
            return ComputeLineFlatPct(GetUpgradeLevel(line, rarity), LineHpPerLevel, step);
        }

        public static float GetLineFlatDmgBuffPct(int line, Rarity rarity)
        {
            var step = RarityStep(rarity);
            return ComputeLineFlatPct(GetUpgradeLevel(line, rarity), LineDmgPerLevel, step);
        }

        public static float GetLineFlatAtkSpeedBuffPct(int line, Rarity rarity)
        {
            var step = RarityStep(rarity);
            return ComputeLineFlatPct(GetUpgradeLevel(line, rarity), LineAtkSpeedPerLevel, step);
        }

        public static float GetLineFlatHpBuffPct(int line) => GetLineFlatHpBuffPct(line, DefaultUpgradeRarity);

        public static float GetLineFlatDmgBuffPct(int line) => GetLineFlatDmgBuffPct(line, DefaultUpgradeRarity);

        public static float GetLineFlatAtkSpeedBuffPct(int line) => GetLineFlatAtkSpeedBuffPct(line, DefaultUpgradeRarity);

        public static float GetLineHpAmplificationBonusPct(int level, Rarity rarity) =>
            ComputeAmplification(level, 0.65f, RarityStep(rarity));

        public static float GetLineDmgAmplificationBonusPct(int level, Rarity rarity) =>
            ComputeAmplification(level, 0.55f, RarityStep(rarity));

        public static float GetLineAtkSpeedAmplificationBonusPct(int level, Rarity rarity) =>
            ComputeAmplification(level, 0.45f, RarityStep(rarity));

        public static float GetLineHpAmplificationBonusPct(int level) =>
            GetLineHpAmplificationBonusPct(level, DefaultUpgradeRarity);

        public static float GetLineDmgAmplificationBonusPct(int level) =>
            GetLineDmgAmplificationBonusPct(level, DefaultUpgradeRarity);

        public static float GetLineAtkSpeedAmplificationBonusPct(int level) =>
            GetLineAtkSpeedAmplificationBonusPct(level, DefaultUpgradeRarity);

        static float ComputeLinePct(int level, float perLevel, float maxBonus, int rarityStep)
        {
            level = Mathf.Clamp(level, 0, MaxUpgradeLevel);
            var flat = ComputeLineFlatPct(level, perLevel, rarityStep);
            var bonus = ComputeAmplification(level, maxBonus, rarityStep);
            return TotalBuffPct(flat, bonus);
        }

        static float TotalBuffPct(float flatPct, float percentPct)
        {
            return ((1f + flatPct) * (1f + percentPct)) - 1f;
        }

        static float ComputeAmplification(int level, float maxBonus, int rarityStep)
        {
            level = Mathf.Clamp(level, 0, MaxUpgradeLevel);
            var progress = level / (float)MaxUpgradeLevel;
            return Mathf.Lerp(0f, maxBonus, progress) + PercentPerRarityStep * rarityStep;
        }

        static float ComputeLineFlatPct(int level, float perLevel, int rarityStep)
        {
            level = Mathf.Clamp(level, 0, MaxUpgradeLevel);
            var rarityFactor = Mathf.Pow(4f, rarityStep);
            return level * perLevel * rarityFactor;
        }

        static Rarity ClampUpgradableRarity(Rarity rarity)
        {
            var step = Mathf.Clamp((int)rarity, 0, UpgradableRarityCount - 1);
            return (Rarity)step;
        }

        static int RarityStep(Rarity rarity)
        {
            return Mathf.Clamp((int)rarity, 0, UpgradableRarityCount - 1);
        }
    }
}
