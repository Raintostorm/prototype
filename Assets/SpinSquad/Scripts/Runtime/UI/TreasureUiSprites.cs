using System;
using System.Collections.Generic;
using SpinSquad.Data;
using SpinSquad.Meta;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>Treasure UI sprites under Resources/UI/Treasure/Icons (sync from repo treasures/).</summary>
    public static class TreasureUiSprites
    {
        const string IconsFolder = "UI/Treasure/Icons";

        static Dictionary<string, Sprite> _index;
        static bool _indexBuilt;

        public static Sprite TableBackground => Get("table_1");

        public static Sprite GetIcon(Rarity rarity, int oneBasedIndex)
        {
            if (oneBasedIndex < 1)
                return null;
            var prefix = RarityFilePrefix(rarity);
            if (string.IsNullOrEmpty(prefix))
                return null;
            return Get($"{prefix}_{oneBasedIndex}");
        }

        public static Sprite GetIconForDefinition(TreasureDefinition def)
        {
            if (!def.IsValid || !TryParseTreasureId(def.Id, out var rarity, out var slot))
                return null;
            return GetIcon(rarity, slot);
        }

        public static Sprite GetIconForId(string treasureId)
        {
            if (!TryParseTreasureId(treasureId, out var rarity, out var slot))
                return null;
            return GetIcon(rarity, slot);
        }

        public static Sprite EffectTypeIcon(TreasureEffectKind kind)
        {
            var slot = kind switch
            {
                TreasureEffectKind.AllyHpPct => 1,
                TreasureEffectKind.AllyDmgPct => 1,
                TreasureEffectKind.AllyAtkSpeedPct => 2,
                TreasureEffectKind.AllyCritChanceFlat => 2,
                TreasureEffectKind.StartRollCoinBonus => 3,
                _ => 3
            };
            return Get($"type_{slot}");
        }

        public static void ApplyIcon(Image image, Sprite sprite, Color fallbackTint)
        {
            HudUiSprites.ApplyIcon(image, sprite, fallbackTint);
        }

        public static bool TryParseTreasureId(string treasureId, out Rarity rarity, out int oneBasedSlot)
        {
            rarity = Rarity.Common;
            oneBasedSlot = 0;
            if (string.IsNullOrWhiteSpace(treasureId))
                return false;

            var parts = treasureId.Split('_');
            if (parts.Length < 3 || parts[0] != "tr")
                return false;

            rarity = parts[1] switch
            {
                "c" => Rarity.Common,
                "r" => Rarity.Rare,
                "e" => Rarity.Epic,
                "l" => Rarity.Legendary,
                _ => Rarity.Common
            };
            if (!int.TryParse(parts[2], out var slot))
                return false;
            oneBasedSlot = slot;
            return slot >= 1;
        }

        static string RarityFilePrefix(Rarity rarity) =>
            rarity switch
            {
                Rarity.Common => "c",
                Rarity.Rare => "r",
                Rarity.Epic => "e",
                Rarity.Legendary => "l",
                _ => string.Empty
            };

        static void EnsureIndex()
        {
            if (_indexBuilt)
                return;
            _indexBuilt = true;
            _index = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            var sprites = Resources.LoadAll<Sprite>(IconsFolder);
            for (var i = 0; i < sprites.Length; i++)
            {
                var s = sprites[i];
                if (s != null)
                    _index[s.name] = s;
            }
        }

        static Sprite Get(string assetName)
        {
            EnsureIndex();
            if (_index.TryGetValue(assetName, out var cached) && cached != null)
                return cached;

            var path = IconsFolder + "/" + assetName;
            var sprite = Resources.Load<Sprite>(path);
            if (sprite != null)
            {
                _index[assetName] = sprite;
                return sprite;
            }

            var obj = Resources.Load(path);
            if (obj is Sprite sp)
            {
                _index[assetName] = sp;
                return sp;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "[TreasureUiSprites] Không load được '" + assetName +
                "'. Chạy Tools/sync_treasures_from_root.sh và Reimport Icons.");
#endif
            return null;
        }

#if UNITY_EDITOR
        public static void ClearCache()
        {
            _index = null;
            _indexBuilt = false;
        }
#endif
    }
}
