using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>HUD sprites under Resources/UI/Hud/Meta and .../Combat (runtime uGUI).</summary>
    public static class HudUiSprites
    {
        const string MetaFolder = "UI/Hud/Meta";
        const string CombatFolder = "UI/Hud/Combat";

        static Dictionary<string, Sprite> _index;
        static bool _indexBuilt;

        public static Sprite MetaSetting => Get("Setting");
        public static Sprite MetaMail => Get("Mail_button");
        public static Sprite MetaBattleButton => Get("Battle_button");
        public static Sprite MetaGreenButton => Get("Green_button");
        public static Sprite MetaChest => Get("Chest");
        public static Sprite MetaRewardButton => Get("Reward_button");
        public static Sprite MetaLock => Get("Lock");
        public static Sprite MetaPrev => Get("Prev");
        public static Sprite MetaNext => Get("Next");
        public static Sprite MetaAllTab => Get("All_tab");
        public static Sprite MetaGearTab => Get("Gear_tab");
        public static Sprite MetaItemTab => Get("Item_tab");
        public static Sprite MetaMaterialTab => Get("Material_tab");
        public static Sprite MetaSelected => Get("Selected");
        public static Sprite MetaNotSelected => Get("Not_selected");
        public static Sprite MetaOn => Get("On");
        public static Sprite MetaOff => Get("Off");
        public static Sprite MetaTurnOff => Get("Turn_off");
        public static Sprite MetaVolumeOn => Get("Volume_on");
        public static Sprite MetaVolumeOff => Get("Volume_off");
        public static Sprite LineWoodIcon => Get("Wood_icon");
        public static Sprite LineFireIcon => Get("Fire_icon");
        public static Sprite LineMetalIcon => Get("Metal_icon");
        public static Sprite LineWaterIcon => Get("Water_icon");
        public static Sprite LineEarthIcon => Get("Earth_icon");

        public static Sprite CombatStopOn => Get("Stop_on");
        public static Sprite CombatStopOff => Get("Stop_off");
        public static Sprite CombatSpeed2Off => Get("Speed_x2_off");
        public static Sprite CombatSpeed2On => Get("Speed_x2_on");
        public static Sprite CombatShop => Get("Shop_icon");
        public static Sprite CombatMerge => Get("Merge");
        public static Sprite CombatSell => Get("Sell");
        public static Sprite CombatBack => Get("Back");
        public static Sprite CombatContinue => Get("continue");
        public static Sprite CombatPause => Get("pause");
        public static Sprite CombatRestart => Get("Restart");
        public static Sprite CombatHomeInBattle => Get("Home_in_battle");
        public static Sprite CombatSceneBackground => Get("Background_combat");
        public static Sprite MetaSceneBackground => Get("Background");
        public static Sprite CombatBlueButton => Get("Blue_button");
        public static Sprite CombatOrangeButton => Get("Orange_button");

        public static Sprite LineIcon(int lineIndex)
        {
            return lineIndex switch
            {
                1 => LineFireIcon,
                2 => LineMetalIcon,
                3 => LineWaterIcon,
                4 => LineEarthIcon,
                _ => LineWoodIcon
            };
        }

        public static Sprite LineCardSprite(int lineIndex)
        {
            return lineIndex switch
            {
                1 => Get("Fire_card"),
                2 => Get("Metal_card"),
                3 => Get("Water_card"),
                4 => Get("Earth_card"),
                _ => Get("Wood_card")
            };
        }

        public static Sprite TreasureTabSprite(int tabIndex)
        {
            return tabIndex switch
            {
                1 => MetaGearTab,
                2 => MetaItemTab,
                3 => MetaMaterialTab,
                _ => MetaAllTab
            };
        }

        public static bool TryGet(string assetName, out Sprite sprite)
        {
            sprite = Get(assetName);
            return sprite != null;
        }

        public static IReadOnlyList<string> AllAssetNames()
        {
            EnsureIndex();
            var list = new List<string>(_index.Count);
            foreach (var key in _index.Keys)
                list.Add(key);
            list.Sort(StringComparer.OrdinalIgnoreCase);
            return list;
        }

#if UNITY_EDITOR
        public static void ClearCache()
        {
            _index = null;
            _indexBuilt = false;
        }
#endif

        public static void ApplyIcon(Image image, Sprite sprite, Color fallbackTint)
        {
            if (image == null)
                return;
            if (sprite != null)
            {
                image.sprite = sprite;
                image.color = Color.white;
                image.preserveAspect = true;
                return;
            }

            image.sprite = Scenes.MetaHudTheme.WhiteSprite();
            image.color = fallbackTint;
            image.preserveAspect = false;
        }

        static void EnsureIndex()
        {
            if (_indexBuilt)
                return;
            _indexBuilt = true;
            _index = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            IndexFolder(MetaFolder);
            IndexFolder(CombatFolder);
        }

        static void IndexFolder(string folder)
        {
            var sprites = Resources.LoadAll<Sprite>(folder);
            for (var i = 0; i < sprites.Length; i++)
            {
                var s = sprites[i];
                if (s == null)
                    continue;
                _index[s.name] = s;
            }
        }

        static Sprite Get(string assetName)
        {
            EnsureIndex();
            if (_index.TryGetValue(assetName, out var cached) && cached != null)
                return cached;

            var metaPath = MetaFolder + "/" + assetName;
            var combatPath = CombatFolder + "/" + assetName;
            var sprite = Resources.Load<Sprite>(metaPath) ?? Resources.Load<Sprite>(combatPath);
            if (sprite != null)
            {
                _index[assetName] = sprite;
                return sprite;
            }

            var obj = Resources.Load(metaPath) ?? Resources.Load(combatPath);
            if (obj is Sprite sp)
            {
                _index[assetName] = sp;
                return sp;
            }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
            Debug.LogWarning(
                "[HudUiSprites] Không load được sprite '" + assetName +
                "'. Chạy SpinSquad → UI → Reimport HUD Sprites.");
#endif
            return null;
        }
    }
}
