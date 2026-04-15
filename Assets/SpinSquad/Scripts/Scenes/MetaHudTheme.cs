using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.Scenes
{
    /// <summary>Hằng số màu / chữ / khoảng cách dùng chung cho Homepage và Upgrade (runtime uGUI).</summary>
    public static class MetaHudTheme
    {
        public const float ReferenceWidth = 1080f;
        public const float ReferenceHeight = 1920f;

        public const float SafeEdgeX = 36f;
        public const float SafeEdgeYTop = 40f;

        public static readonly Color CanvasBackdrop = new Color(0.06f, 0.09f, 0.16f, 0.92f);
        public static readonly Color HeaderBand = new Color(0.1f, 0.14f, 0.22f, 0.88f);
        public static readonly Color SectionLabel = new Color(0.72f, 0.8f, 0.92f, 0.85f);

        public static readonly Color TextPrimary = Color.white;
        public static readonly Color TextSecondary = new Color(0.82f, 0.88f, 0.96f, 1f);

        public static readonly Color ButtonNav = new Color(0.2f, 0.24f, 0.36f, 0.98f);
        public static readonly Color ButtonPlay = new Color(0.18f, 0.44f, 0.78f, 0.98f);
        public static readonly Color ButtonUpgrade = new Color(0.14f, 0.5f, 0.36f, 0.97f);
        public static readonly Color ButtonTreasure = new Color(0.5f, 0.34f, 0.16f, 0.97f);
        public static readonly Color ButtonBack = new Color(0.2f, 0.28f, 0.4f, 0.96f);
        public static readonly Color ButtonMuted = new Color(0.32f, 0.32f, 0.38f, 0.96f);

        public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.64f);

        public const int FontTitle = 44;
        public const int FontSection = 22;
        public const int FontBody = 26;
        public const int FontResource = 28;
        public const int FontHint = 21;
        public const int FontButton = 30;
        public const int FontOverlayTitle = 34;
        public const int FontOverlayRow = 22;

        public static readonly Vector2 CtaSize = new Vector2(520f, 96f);
        public static readonly Vector2 NavArrowSize = new Vector2(92f, 82f);
        public const float CtaSpacing = 108f;

        public static readonly Vector2 UpgradeCardSize = new Vector2(292f, 168f);
        public const float UpgradeCardRowSpacing = 186f;
        public const float UpgradeCardTopY = -348f;
        public const float UpgradeColumnHeaderY = -268f;
        /// <summary>Khoảng cách tâm cột — dùng với <see cref="UpgradeCardSize"/>.</summary>
        public const float UpgradeColumnSpacingX = 312f;

        static Sprite _whiteSprite;

        public static Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }

        public static void AddFullScreenBackdrop(Transform canvasRoot, int siblingIndex = 0)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(canvasRoot, false);
            go.transform.SetSiblingIndex(siblingIndex);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = CanvasBackdrop;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void ApplyImageOutline(Image img)
        {
            if (img == null)
                return;
            var o = img.gameObject.GetComponent<Outline>();
            if (o == null)
                o = img.gameObject.AddComponent<Outline>();
            o.effectColor = new Color(0f, 0f, 0f, 0.42f);
            o.effectDistance = new Vector2(1.5f, -1.5f);
            o.useGraphicAlpha = true;
        }
    }
}
