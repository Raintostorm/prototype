using System;
using System.Collections.Generic;
using SpinSquad.Data;
using UnityEngine;

namespace SpinSquad.Meta
{
    public enum TreasureEffectKind
    {
        AllyHpPct = 0,
        AllyDmgPct = 1,
        AllyAtkSpeedPct = 2,
        AllyCritChanceFlat = 3,
        StartRollCoinBonus = 4,
        WaveRollCoinBonus = 5
    }

    public readonly struct TreasureDefinition
    {
        public readonly string Id;
        public readonly string Name;
        public readonly Rarity Rarity;
        public readonly TreasureEffectKind EffectKind;
        public readonly float BaseValue;

        public TreasureDefinition(string id, string name, Rarity rarity, TreasureEffectKind effectKind, float baseValue)
        {
            Id = id;
            Name = name;
            Rarity = rarity;
            EffectKind = effectKind;
            BaseValue = baseValue;
        }

        public bool IsValid => !string.IsNullOrWhiteSpace(Id);
    }

    public static class TreasureDefinitions
    {
        /// <summary>
        /// Thứ tự khai báo quan trọng: <see cref="BuildDefinitions"/> chạy khi gán <see cref="AllDefinitions"/>,
        /// nên mọi mảng/hằng dùng trong build phải khai báo *trước* <see cref="AllDefinitions"/>.
        /// </summary>
        static readonly int[] RarityWeights = { 58, 28, 11, 3 };

        static readonly string[][] TierNames =
        {
            new[]
            {
                "Moss Pebble", "Dust Coil", "Seed Wick", "Twig Token", "Clay Marker",
                "Fog Strand", "Reed Clip", "Ash Fleck", "Grit Knot", "Dew Shell"
            },
            new[]
            {
                "Amber Clip", "Tide Loop", "Glim Shard", "Coral Pin", "Brass Gear",
                "Wind Key", "Salt Sigil", "Fume Coil", "Mica Bolt", "Verd Coil"
            },
            new[]
            {
                "Sun Fragment", "Void Spark", "Rune Prism", "Storm Core", "Frost Seal",
                "Ember Lens", "Star Knot", "Tide Crown", "Iron Halo", "Sky Stamp"
            },
            new[]
            {
                "Aurora Heart", "Eclipse Crest", "Genesis Coil", "Meridian Core", "Apogee Seal",
                "Xylem Crown", "Zenith Band", "Oblivion Key", "Astral Forge", "Tempest Nexus"
            }
        };

        static readonly float[] TierStrength = { 1f, 2.35f, 5.2f, 10.5f };

        static readonly string[] TierIdPrefix = { "c", "r", "e", "l" };

        static readonly TreasureDefinition[] AllDefinitions = BuildDefinitions();

        public static IReadOnlyList<TreasureDefinition> All => AllDefinitions;

        public static TreasureDefinition[] DefinitionsOrderedForRarity(Rarity rarity)
        {
            var r = ClampTreasureRarity(rarity);
            var tmp = new List<TreasureDefinition>(12);
            foreach (var d in AllDefinitions)
            {
                if (d.Rarity == r)
                    tmp.Add(d);
            }

            tmp.Sort((a, b) => string.CompareOrdinal(a.Id, b.Id));
            return tmp.ToArray();
        }

        public static TreasureDefinition GetById(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
                return default;
            foreach (var def in AllDefinitions)
            {
                if (def.Id == id)
                    return def;
            }

            return default;
        }

        public static TreasureDefinition Roll(System.Random rng)
        {
            if (rng == null)
                throw new ArgumentNullException(nameof(rng));

            var rarity = RollRarity(rng);
            var pool = new List<TreasureDefinition>(12);
            foreach (var def in AllDefinitions)
            {
                if (def.Rarity == rarity)
                    pool.Add(def);
            }

            if (pool.Count == 0)
                return AllDefinitions[0];
            return pool[rng.Next(0, pool.Count)];
        }

        public static int RequiredShardsForNextLevel(int currentLevel)
        {
            var lv = Math.Max(1, currentLevel);
            return 1 + (lv / 2);
        }

        static TreasureDefinition[] BuildDefinitions()
        {
            var list = new List<TreasureDefinition>(44);
            for (var tier = 0; tier < 4; tier++)
            {
                var rarity = (Rarity)tier;
                var m = TierStrength[tier];
                var prefix = TierIdPrefix[tier];
                for (var i = 0; i < 10; i++)
                {
                    var kind = (TreasureEffectKind)(i % 6);
                    var bv = BaseValueFor(kind, m, i);
                    var id = $"tr_{prefix}_{i + 1:00}";
                    var name = TierNames[tier][i];
                    list.Add(new TreasureDefinition(id, name, rarity, kind, bv));
                }
            }

            return list.ToArray();
        }

        static float BaseValueFor(TreasureEffectKind kind, float tierMul, int slot)
        {
            var s = 1f + slot * 0.055f;
            switch (kind)
            {
                case TreasureEffectKind.AllyHpPct:
                    return 0.0125f * tierMul * s;
                case TreasureEffectKind.AllyDmgPct:
                    return 0.0095f * tierMul * s;
                case TreasureEffectKind.AllyAtkSpeedPct:
                    return 0.0065f * tierMul * s;
                case TreasureEffectKind.AllyCritChanceFlat:
                    return 0.0018f * tierMul * s;
                case TreasureEffectKind.StartRollCoinBonus:
                    return Mathf.Round(3f + tierMul * 12f + slot * 1.2f);
                default:
                    return Mathf.Round(2f + tierMul * 9f + slot);
            }
        }

        static Rarity RollRarity(System.Random rng)
        {
            var total = 0;
            for (var i = 0; i < RarityWeights.Length; i++)
                total += RarityWeights[i];

            var pick = rng.Next(0, total);
            var cursor = 0;
            for (var i = 0; i < RarityWeights.Length; i++)
            {
                cursor += RarityWeights[i];
                if (pick < cursor)
                {
                    return i switch
                    {
                        0 => Rarity.Common,
                        1 => Rarity.Rare,
                        2 => Rarity.Epic,
                        _ => Rarity.Legendary
                    };
                }
            }

            return Rarity.Common;
        }

        static Rarity ClampTreasureRarity(Rarity rarity)
        {
            var v = (int)rarity;
            if (v < 0)
                return Rarity.Common;
            if (v > (int)Rarity.Legendary)
                return Rarity.Legendary;
            return (Rarity)v;
        }
    }
}
