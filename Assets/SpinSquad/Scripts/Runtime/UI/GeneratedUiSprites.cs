using System.Collections.Generic;
using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>Project-owned generated UI art, converted to reusable runtime sprites.</summary>
    public static class GeneratedUiSprites
    {
        const string Root = "UI/GeneratedV2/";
        static readonly Dictionary<string, Sprite> Cache = new();

        public static Sprite PrimaryButton => Get("button_primary", 0.12f, 0.31f);
        public static Sprite SecondaryButton => Get("button_secondary", 0.12f, 0.31f);
        public static Sprite DisabledButton => Get("button_disabled", 0.12f, 0.31f);
        public static Sprite IconPrimary => Get("button_icon_primary", 0.28f, 0.28f);
        public static Sprite IconSelected => Get("button_icon_selected", 0.28f, 0.28f);
        public static Sprite IconDark => Get("button_icon_dark", 0.28f, 0.28f);
        public static Sprite DangerButton => Get("button_danger", 0.28f, 0.28f);
        public static Sprite DangerAltButton => Get("button_danger_alt", 0.28f, 0.28f);
        public static Sprite IconDisabled => Get("button_icon_disabled", 0.28f, 0.28f);

        public static void ClearCache()
        {
            foreach (var pair in Cache)
                if (pair.Value != null)
                {
                    if (Application.isPlaying)
                        Object.Destroy(pair.Value);
                    else
                        Object.DestroyImmediate(pair.Value);
                }
            Cache.Clear();
        }

        static Sprite Get(string assetName, float borderRatioX, float borderRatioY)
        {
            if (Cache.TryGetValue(assetName, out var cached) && cached != null)
                return cached;

            var texture = Resources.Load<Texture2D>(Root + assetName);
            if (texture == null)
                return null;

            var borderX = Mathf.Round(texture.width * borderRatioX);
            var borderY = Mathf.Round(texture.height * borderRatioY);
            var sprite = Sprite.Create(
                texture,
                new Rect(0f, 0f, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f,
                0,
                SpriteMeshType.FullRect,
                new Vector4(borderX, borderY, borderX, borderY));
            sprite.name = "GeneratedUI_" + assetName;
            Cache[assetName] = sprite;
            return sprite;
        }
    }
}
