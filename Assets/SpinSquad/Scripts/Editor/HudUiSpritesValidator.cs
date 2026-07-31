#if UNITY_EDITOR
using System.IO;
using SpinSquad.UI;
using UnityEditor;
using UnityEngine;

namespace SpinSquad.EditorTools
{
    public static class HudUiSpritesValidator
    {
        const string HudRoot = "Assets/SpinSquad/Resources/UI/Hud";

        [MenuItem("SpinSquad/UI/Validate HUD Sprites (Resources)", false, 1)]
        public static void ValidateWiredSeven()
        {
            HudUiSprites.ClearCache();
            var ok = 0;
            var fail = 0;
            Check("Meta Setting", HudUiSprites.MetaSetting, ref ok, ref fail);
            Check("Meta Mail", HudUiSprites.MetaMail, ref ok, ref fail);
            Check("Combat Stop", HudUiSprites.CombatStopOn, ref ok, ref fail);
            Check("Combat Speed2 off", HudUiSprites.CombatSpeed2Off, ref ok, ref fail);
            Check("Combat Shop", HudUiSprites.CombatShop, ref ok, ref fail);
            Check("Combat Merge", HudUiSprites.CombatMerge, ref ok, ref fail);
            Check("Combat Sell", HudUiSprites.CombatSell, ref ok, ref fail);
            Check("Generated primary button", GeneratedUiSprites.PrimaryButton, ref ok, ref fail);
            Debug.Log($"[HudUiSprites] Validate wired assets: {ok} OK, {fail} missing.");
        }

        [MenuItem("SpinSquad/UI/Validate All HUD Sprites (68)", false, 3)]
        public static void ValidateAll()
        {
            HudUiSprites.ClearCache();
            if (!Directory.Exists(HudRoot))
            {
                Debug.LogError("[HudUiSprites] Missing " + HudRoot);
                return;
            }

            var pngs = Directory.GetFiles(HudRoot, "*.PNG", SearchOption.AllDirectories);
            var ok = 0;
            var fail = 0;
            foreach (var abs in pngs)
            {
                var name = Path.GetFileNameWithoutExtension(abs);
                if (HudUiSprites.TryGet(name, out var sprite) && sprite != null)
                {
                    ok++;
                    continue;
                }

                fail++;
                Debug.LogWarning("[HudUiSprites] MISSING — " + name);
            }

            Debug.Log($"[HudUiSprites] Validate All: {ok}/{pngs.Length} OK, {fail} missing. Reimport HUD Sprites nếu thiếu.");
        }

        static void Check(string label, Sprite sprite, ref int ok, ref int fail)
        {
            if (sprite != null)
            {
                ok++;
                Debug.Log($"[HudUiSprites] OK — {label} ({sprite.name})");
            }
            else
            {
                fail++;
                Debug.LogWarning($"[HudUiSprites] MISSING — {label}");
            }
        }
    }
}
#endif
