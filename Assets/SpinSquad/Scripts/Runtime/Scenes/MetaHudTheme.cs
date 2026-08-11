using System.Collections.Generic;
using SpinSquad.UI;
using UnityEngine;
using UnityEngine.Events;
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

        /// <summary>Lề ngang thêm (px) cho cụm nút icon góc phải — tránh sát viền máy.</summary>
        public const float TopIconBarExtraInsetX = 14f;

        /// <summary>Lề dọc thêm (px) từ mép trên — tách khỏi status / title.</summary>
        public const float TopIconBarExtraInsetY = 10f;

        /// <summary>Khoảng hở (px) dưới đáy chữ &quot;Homepage&quot; trước hàng Shop / Thư / Set.</summary>
        public const float HomepageStubRowGapBelowTitle = 18f;

        public static float HomepageTitleBottomFromTop => SafeEdgeYTop + 8f + 72f;

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

        /// <summary>Nút icon góc màn (Pause / Cài đặt / hàng meta).</summary>
        public static readonly Color ButtonIconBar = new Color(0.22f, 0.28f, 0.38f, 0.96f);

        /// <summary>Nút stub chưa mở tính năng (ví dụ tốc độ x2).</summary>
        public static readonly Color ButtonStubDisabled = new Color(0.26f, 0.26f, 0.3f, 0.72f);

        public static readonly Color OverlayDim = new Color(0f, 0f, 0f, 0.64f);

        public static readonly Vector2 IconBarButtonSize = new Vector2(88f, 76f);

        public const float IconBarGap = 12f;

        public const int FontTitle = 44;
        public const int FontSection = 22;
        public const int FontBody = 26;
        public const int FontResource = 28;
        public const int FontHint = 21;
        public const int FontButton = 30;
        public const int FontOverlayTitle = 34;
        public const int FontOverlayRow = 22;

        public static readonly Vector2 CtaSize = new Vector2(520f, 96f);
        public static readonly Vector2 CtaHeroSize = new Vector2(560f, 112f);
        public static readonly Vector2 NavArrowSize = new Vector2(92f, 82f);

        /// <summary>Khe giữa mép hai nút (px).</summary>
        public const float ActionRowGap = 24f;

        public const float BottomNavHeight = 132f;
        public const float BottomActionBarHeight = 148f;
        public const float InfoStripHeight = 112f;

        /// <summary>Khoảng tâm–tâm CTA dọc (mép–mép ≥ ActionRowGap).</summary>
        public static float CtaSpacing => CtaSize.y + ActionRowGap;

        public static readonly Vector2 UpgradeCardSize = new Vector2(292f, 168f);
        public const float UpgradeCardRowSpacing = 186f;
        public const float UpgradeCardTopY = -348f;
        public const float UpgradeColumnHeaderY = -268f;
        /// <summary>Khoảng cách tâm cột — dùng với <see cref="UpgradeCardSize"/>.</summary>
        public const float UpgradeColumnSpacingX = 312f;

        public const float FooterBackInsetBottom = 56f;
        public static readonly Vector2 FooterBackButtonSize = new Vector2(120f, 96f);

        public static readonly Color BottomLaneBand = new Color(0.08f, 0.1f, 0.16f, 0.94f);
        public static readonly Color BottomNavTabActive = new Color(0.18f, 0.38f, 0.62f, 0.98f);
        public static readonly Color BottomNavTabIdle = new Color(0.14f, 0.17f, 0.24f, 0.92f);

        static Sprite _whiteSprite;

        public static Sprite WhiteSprite()
        {
            if (_whiteSprite != null)
                return _whiteSprite;
            var tex = Texture2D.whiteTexture;
            _whiteSprite = Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
            return _whiteSprite;
        }

        public static void AddFullScreenBackdrop(Transform canvasRoot, int siblingIndex = 0) =>
            AddFullScreenBackdrop(canvasRoot, null, siblingIndex);

        /// <summary>Nền full màn: sprite từ game_background/ hoặc màu phẳng khi null.</summary>
        public static void AddFullScreenBackdrop(Transform canvasRoot, Sprite backgroundSprite, int siblingIndex = 0)
        {
            var go = new GameObject("Backdrop");
            go.transform.SetParent(canvasRoot, false);
            go.transform.SetSiblingIndex(siblingIndex);
            var img = go.AddComponent<Image>();
            if (backgroundSprite != null)
            {
                img.sprite = backgroundSprite;
                img.color = Color.white;
            }
            else
            {
                img.sprite = WhiteSprite();
                img.color = CanvasBackdrop;
            }

            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }

        public static void AddHeaderStrip(Transform parent, int siblingIndex = 1)
        {
            var go = new GameObject("HeaderStrip");
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(siblingIndex);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = HeaderBand;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 168f);
        }

        /// <summary>Back cố định đáy màn (meta scenes) — luôn trong safe area.</summary>
        public static Button CreateFooterBackButton(Transform parent, Font font, UnityAction onClick)
        {
            var btnGo = new GameObject("BackFooter");
            btnGo.transform.SetParent(parent, false);
            var image = btnGo.AddComponent<Image>();
            image.sprite = WhiteSprite();
            image.color = ButtonBack;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = FontButton;
            text.text = "Back";
            text.alignment = TextAnchor.MiddleCenter;
            text.color = TextPrimary;
            text.raycastTarget = false;
            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = new Vector2(0f, FooterBackInsetBottom);
            rt.sizeDelta = FooterBackButtonSize;

            ApplyHudBackButton(btn);
            return btn;
        }

        public static void ApplyHudBackButton(Button button)
        {
            if (button == null)
                return;
            var image = button.GetComponent<Image>();
            HudUiSprites.ApplyIcon(image, HudUiSprites.CombatBack, ButtonBack);
            var label = button.GetComponentInChildren<Text>();
            HudUiButtonHelper.SyncLabelVisibility(label, HudUiSprites.CombatBack, null);
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

        public static float GetSafeContentBottomInset(bool hasBottomNav) =>
            hasBottomNav ? BottomNavHeight + 16f : FooterBackInsetBottom + FooterBackButtonSize.y;

        /// <summary>Đổi chiều cao UI (px ref 1920) sang world units (camera ortho).</summary>
        public static float ScreenPixelsToWorldHeight(Camera cam, float pixels)
        {
            if (cam == null || !cam.orthographic)
                return 0f;
            return pixels / ReferenceHeight * (cam.orthographicSize * 2f);
        }

        public static float DuelPrepBottomUiWorldHeight(Camera cam) =>
            ScreenPixelsToWorldHeight(cam, BottomActionBarHeight + InfoStripHeight + 32f);

        /// <summary>Panel full-width neo đáy màn (lane UI).</summary>
        public static RectTransform CreateBottomLanePanel(Transform parent, float height, Color bg, string objectName = "BottomLane")
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(1f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, height);
            var img = go.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = bg;
            img.raycastTarget = false;
            return rt;
        }

        /// <summary>Hàng con trong lane; widthFractions phải cộng ≈ 1.</summary>
        public static RectTransform CreateHorizontalRowParent(RectTransform lane, float verticalPad = 10f)
        {
            var go = new GameObject("HorizontalRow");
            go.transform.SetParent(lane, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(SafeEdgeX, verticalPad);
            rt.offsetMax = new Vector2(-SafeEdgeX, -verticalPad);
            return rt;
        }

        public static void LayoutHorizontalRow(
            RectTransform row,
            float gap,
            IReadOnlyList<RectTransform> children,
            IReadOnlyList<float> widthFractions)
        {
            if (children == null || children.Count == 0)
                return;
            if (widthFractions == null || widthFractions.Count != children.Count)
                return;

            var totalWeight = 0f;
            for (var i = 0; i < widthFractions.Count; i++)
                totalWeight += Mathf.Max(0f, widthFractions[i]);
            if (totalWeight <= 0f)
                return;

            // Anchor every slot to a percentage of the live row instead of
            // reading row.rect during construction. At that point SafeAreaFitter
            // may not have applied yet, which previously made the last tab spill
            // beyond the right edge on iPhone-style safe areas.
            var cursor = 0f;
            for (var i = 0; i < children.Count; i++)
            {
                var fraction = Mathf.Max(0f, widthFractions[i]) / totalWeight;
                var start = cursor;
                var end = cursor + fraction;
                var child = children[i];
                child.SetParent(row, false);
                child.anchorMin = new Vector2(start, 0f);
                child.anchorMax = new Vector2(end, 1f);
                child.pivot = new Vector2(0.5f, 0.5f);
                child.offsetMin = new Vector2(i == 0 ? 0f : gap * 0.5f, 0f);
                child.offsetMax = new Vector2(i == children.Count - 1 ? 0f : -gap * 0.5f, 0f);
                cursor = end;
            }
        }
    }
}
