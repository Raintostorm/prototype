using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    public static class HudUiButtonHelper
    {
        public static void ApplySpriteButton(Image image, Sprite sprite, Color fallbackTint, bool preserveAspect = true)
        {
            HudUiSprites.ApplyIcon(image, sprite, fallbackTint);
            if (image != null)
                image.preserveAspect = preserveAspect;
        }

        public static void SyncLabelVisibility(Text label, Sprite sprite, string labelText)
        {
            if (label == null)
                return;
            var show = sprite == null || !string.IsNullOrEmpty(labelText);
            label.gameObject.SetActive(show);
            if (show)
                label.text = labelText;
        }

        public static Button ConfigureButtonColors(Button btn, Color fallbackTint)
        {
            if (btn == null)
                return btn;
            var colors = btn.colors;
            colors.highlightedColor = fallbackTint * 1.12f;
            colors.pressedColor = fallbackTint * 0.82f;
            colors.disabledColor = new Color(fallbackTint.r, fallbackTint.g, fallbackTint.b, 0.55f);
            btn.colors = colors;
            return btn;
        }
    }
}
