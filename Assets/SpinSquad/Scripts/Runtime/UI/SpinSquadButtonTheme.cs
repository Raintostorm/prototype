using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    public enum SpinSquadButtonRole { Primary, Confirm, Reward, Secondary, Danger, Icon }

    /// <summary>One project-owned button language shared by runtime-built screens.</summary>
    public static class SpinSquadButtonTheme
    {
        public static void Apply(Image image, SpinSquadButtonRole role)
        {
            if (image == null) return;
            image.sprite = role switch
            {
                SpinSquadButtonRole.Confirm => MetaHomeUiSprites.GreenButton,
                SpinSquadButtonRole.Reward => MetaHomeUiSprites.GoldButton,
                SpinSquadButtonRole.Secondary => MetaHomeUiSprites.DarkButton,
                SpinSquadButtonRole.Danger => MetaHomeUiSprites.RedButton,
                SpinSquadButtonRole.Icon => MetaHomeUiSprites.SmallIconButton,
                _ => MetaHomeUiSprites.BlueButton
            };
            image.type = Image.Type.Sliced;
            image.preserveAspect = false;
            image.color = Color.white;
        }

        public static SpinSquadButtonRole FromLabel(string label)
        {
            var key = (label ?? string.Empty).Trim().ToLowerInvariant();
            if (ContainsAny(key, "delete", "remove", "sell", "quit", "restart")) return SpinSquadButtonRole.Danger;
            if (ContainsAny(key, "add", "start", "resume", "apply", "confirm")) return SpinSquadButtonRole.Confirm;
            if (ContainsAny(key, "reward", "claim", "treasure", "upgrade")) return SpinSquadButtonRole.Reward;
            if (ContainsAny(key, "back", "close", "cancel", "bag", "settings", "pause", "speed")) return SpinSquadButtonRole.Secondary;
            return SpinSquadButtonRole.Primary;
        }

        public static SpinSquadButtonRole FromColor(Color color)
        {
            if (color.r > color.g * 1.35f && color.r > color.b * 1.12f) return SpinSquadButtonRole.Danger;
            if (color.g > color.r * 1.2f && color.g > color.b * 1.05f) return SpinSquadButtonRole.Confirm;
            if (color.r > 0.38f && color.g > 0.25f && color.b < 0.24f) return SpinSquadButtonRole.Reward;
            if (color.maxColorComponent < 0.42f) return SpinSquadButtonRole.Secondary;
            return SpinSquadButtonRole.Primary;
        }

        static bool ContainsAny(string value, params string[] needles)
        {
            foreach (var needle in needles) if (value.Contains(needle)) return true;
            return false;
        }
    }
}
