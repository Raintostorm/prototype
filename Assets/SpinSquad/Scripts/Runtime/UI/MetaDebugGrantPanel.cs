using System;
using SpinSquad.Meta;
using SpinSquad.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>
    /// Nút cộng Gold / Treasure Keys để test Upgrade và Roll (chỉ Editor + Development Build).
    /// </summary>
    public sealed class MetaDebugGrantPanel
    {
        public const int GoldGrantAmount = 10_000;
        public const int KeysGrantAmount = 10;

        readonly Action _onGrant;

        MetaDebugGrantPanel(Action onGrant)
        {
            _onGrant = onGrant;
        }

        public static MetaDebugGrantPanel TryAttach(Transform canvasRoot, Font font, Action onGrant)
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            if (canvasRoot == null || font == null)
                return null;
            var panel = new MetaDebugGrantPanel(onGrant);
            panel.Build(canvasRoot, font);
            return panel;
#else
            return null;
#endif
        }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        void Build(Transform canvasRoot, Font font)
        {
            var rootGo = new GameObject("MetaDebugGrantPanel");
            rootGo.transform.SetParent(canvasRoot, false);
            var rootRt = rootGo.AddComponent<RectTransform>();
            rootRt.anchorMin = rootRt.anchorMax = new Vector2(1f, 1f);
            rootRt.pivot = new Vector2(1f, 1f);
            rootRt.anchoredPosition = new Vector2(-MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop - 8f);
            rootRt.sizeDelta = new Vector2(220f, 168f);

            CreateLabel(rootGo.transform, font, "DEV", 0f, 22);
            CreateGrantButton(rootGo.transform, font, $"+{GoldGrantAmount:N0} Gold", -36f, GrantGold);
            CreateGrantButton(rootGo.transform, font, $"+{KeysGrantAmount} Keys", -92f, GrantKeys);
        }

        void GrantGold()
        {
            MetaProgressionStore.AddGold(GoldGrantAmount);
            _onGrant?.Invoke();
            Debug.Log($"[MetaDebug] +{GoldGrantAmount} Gold → {MetaProgressionStore.Gold:N0}");
        }

        void GrantKeys()
        {
            MetaProgressionStore.AddTreasureKeys(KeysGrantAmount);
            _onGrant?.Invoke();
            Debug.Log($"[MetaDebug] +{KeysGrantAmount} Keys → {MetaProgressionStore.TreasureKeys}");
        }

        static void CreateLabel(Transform parent, Font font, string text, float y, int fontSize)
        {
            var go = new GameObject("DevLabel");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = fontSize;
            t.text = text;
            t.alignment = TextAnchor.MiddleRight;
            t.color = new Color(1f, 0.82f, 0.35f, 0.95f);
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(220f, 28f);
        }

        void CreateGrantButton(Transform parent, Font font, string label, float y, Action onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.sprite = MetaHudTheme.WhiteSprite();
            img.color = new Color(0.18f, 0.28f, 0.42f, 0.94f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(() => onClick());

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(0f, y);
            rt.sizeDelta = new Vector2(210f, 48f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = font;
            text.fontSize = 20;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var lrt = text.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;
        }
#endif
    }
}
