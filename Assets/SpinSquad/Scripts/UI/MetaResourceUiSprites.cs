using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>Meta resource HUD art: Resources/UI/Meta/Resource (sync bar_and_resource/).</summary>
    public static class MetaResourceUiSprites
    {
        const string Folder = "UI/Meta/Resource";

        static Dictionary<string, Sprite> _index;
        static bool _indexBuilt;

        public static Sprite CoinIcon => Get("coin");
        public static Sprite CoinBar => Get("coin_bar");
        public static Sprite KeyIcon => Get("key");
        public static Sprite KeyBar => Get("key_bar");
        public static Sprite EnergyIcon => Get("energy");
        public static Sprite EnergyBar => Get("energy_bar");

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
            var sprites = Resources.LoadAll<Sprite>(Folder);
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
            Debug.LogWarning(
                "[MetaResourceUiSprites] Không load được '" + assetName +
                "'. Chạy Tools/sync_bar_resource_from_root.sh.");
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
