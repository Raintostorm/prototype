using System;
using System.Collections.Generic;
using UnityEngine;

namespace SpinSquad.UI
{
    /// <summary>Sprites imported from the approved Spin Squad button kit.</summary>
    public static class ButtonKitSprites
    {
        const string Root = "UI/ButtonKit/";
        static readonly Dictionary<string, Sprite> Cache = new(StringComparer.OrdinalIgnoreCase);

        public static Sprite BattleOn => Get("battle_on");
        public static Sprite BattleOff => Get("battle_off");
        public static Sprite UpgradeOn => Get("upgrade_on");
        public static Sprite UpgradeOff => Get("upgrade_off");
        public static Sprite DangerOn => Get("danger_on");
        public static Sprite DangerOff => Get("danger_off");
        public static Sprite Back => Get("back");
        public static Sprite Ok => Get("ok");

        // Phase-two kit contract. These intentionally return null until the
        // approved sheet is sliced into Resources/UI/ButtonKit; all callers
        // retain their current generated-theme fallback in the meantime.
        public static Sprite Start => Get("start");
        public static Sprite StartPressed => Get("start_on");
        public static Sprite Roll => Get("roll");
        public static Sprite RollPressed => Get("roll_on");
        public static Sprite AddAlly => Get("add_ally");
        public static Sprite AddAllyPressed => Get("add_ally_on");
        public static Sprite BagOpen => Get("bag_open");
        public static Sprite BagOpenPressed => Get("bag_open_on");
        public static Sprite BagClosed => Get("bag_closed");
        public static Sprite BagClosedPressed => Get("bag_closed_on");
        public static Sprite Merge => Get("merge");
        public static Sprite MergePressed => Get("merge_on");
        public static Sprite Sell => Get("sell");
        public static Sprite SellPressed => Get("sell_on");
        public static Sprite Info => Get("info");
        public static Sprite InfoPressed => Get("info_on");
        public static Sprite Close => Get("close");
        public static Sprite ClosePressed => Get("close_on");
        public static Sprite Resume => Get("resume");
        public static Sprite ResumePressed => Get("resume_on");
        public static Sprite Restart => Get("restart");
        public static Sprite RestartPressed => Get("restart_on");

        // Unified navigation set. The artwork intentionally contains no text;
        // scene labels remain live Unity text so localization and resizing stay safe.
        public static Sprite NavigationHome => Get("Navigation/nav_home");
        public static Sprite NavigationUpgrade => Get("Navigation/nav_upgrade");
        public static Sprite NavigationTreasure => Get("Navigation/nav_treasure");
        public static Sprite NavigationSettings => Get("Navigation/nav_settings");
        public static Sprite NavigationShop => Get("Navigation/nav_shop");
        public static Sprite NavigationMail => Get("Navigation/nav_mail");

        public static Sprite Home => Get("home");
        public static Sprite Bag => Get("bag");
        public static Sprite Mail => Get("mail");
        public static Sprite Settings => Get("setting");
        public static Sprite Shop => Get("shop");
        public static Sprite Pause => Get("Pause");
        public static Sprite Coin => Get("coin");
        public static Sprite Key => Get("key");
        public static Sprite Energy => Get("energy");
        public static Sprite HitPoints => Get("HP");
        public static Sprite Attack => Get("ATK");
        public static Sprite Armor => Get("Armor");
        public static Sprite Critical => Get("Crit");
        public static Sprite Healing => Get("Heal");
        public static Sprite Speed => Get("Speed");

        public static Sprite Get(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return null;
            if (Cache.TryGetValue(name, out var cached) && cached != null)
                return cached;

            var sprite = Resources.Load<Sprite>(Root + name);
            if (sprite == null)
            {
                var texture = Resources.Load<Texture2D>(Root + name);
                if (texture != null)
                {
                    sprite = Sprite.Create(texture,
                        new Rect(0f, 0f, texture.width, texture.height),
                        new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect);
                    sprite.name = "ButtonKit_" + name;
                }
            }

            Cache[name] = sprite;
            return sprite;
        }
    }
}
