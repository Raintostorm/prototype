using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>Full-screen scene backgrounds from game_background/ → Resources/UI/Backgrounds.</summary>
    public static class GameBackgroundUiSprites
    {
        const string Folder = "UI/Backgrounds";

        static Dictionary<string, Sprite> _index;
        static bool _indexBuilt;

        public static Sprite Home => Get("home");
        public static Sprite PreBattle => Get("pre_battle");
        public static Sprite Inventory => Get("inventory");
        public static Sprite Treasure => Get("treasure");

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
            Debug.LogWarning("[GameBackgroundUiSprites] Missing '" + assetName + "'. Run Tools/sync_game_background_from_root.sh");
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
