using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>Battle scene art: Resources/UI/Battle (sync background/).</summary>
    public static class BattleUiSprites
    {
        const string Folder = "UI/Battle";

        static Dictionary<string, Sprite> _index;
        static bool _indexBuilt;

        public static Sprite BackgroundBattle => GetBackgroundBattleForLevel(1);

        public static Sprite PlaceHolderCell => Get("place_holder");
        public static Sprite RollButton => Get("roll");

        /// <summary>Nền prep duel — game_background/pre_battle, rồi battle_1, rồi HUD.</summary>
        public static Sprite GetBackgroundPrep()
        {
            var prep = GameBackgroundUiSprites.PreBattle;
            if (prep != null)
                return prep;
            var battle = Get("background_battle_1");
            if (battle != null)
                return battle;
            if (HudUiSprites.CombatSceneBackground != null)
                return HudUiSprites.CombatSceneBackground;
            return HudUiSprites.MetaSceneBackground;
        }

        /// <summary>Nền in-battle đúng level (1–3); thiếu file thì prep fallback.</summary>
        public static Sprite GetBackgroundBattleForLevel(int level)
        {
            level = Mathf.Clamp(level, 1, 4);
            var exact = Get($"background_battle_{level}");
            if (exact != null)
                return exact;
            return GetBackgroundPrep();
        }

        public static void ApplyIcon(Image image, Sprite sprite, Color fallbackTint)
        {
            HudUiSprites.ApplyIcon(image, sprite, fallbackTint);
        }

        static void EnsureIndex()
        {
            if (_indexBuilt)
                return;
            _indexBuilt = true;
            _index = new Dictionary<string, Sprite>(StringComparer.OrdinalIgnoreCase);
            foreach (var s in Resources.LoadAll<Sprite>(Folder))
            {
                if (s != null)
                    _index[s.name] = s;
            }
        }

        public static Sprite Get(string assetName)
        {
            EnsureIndex();
            if (_index.TryGetValue(assetName, out var cached) && cached != null)
                return cached;

            var path = Folder + "/" + assetName;
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
            Debug.LogWarning("[BattleUiSprites] Missing '" + assetName + "'. Run Tools/sync_background_from_root.sh");
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
