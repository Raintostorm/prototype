using SpinSquad.Meta;
using SpinSquad.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>Gold / Keys / Energy bar for the meta screens.</summary>
    public sealed class MetaResourceHudBar
    {
        const float RowWidth = 270f;
        const float RowHeight = 54f;
        const float RowGap = 8f;
        const float ValueLeftPad = 70f;

        Text _goldValue;
        Text _keysValue;
        Text _energyValue;
        Image _energyFill;

        public void Build(Transform parent, Font font, Vector2 topLeftAnchoredPosition)
        {
            var rootGo = new GameObject("MetaResourceHud");
            rootGo.transform.SetParent(parent, false);
            var rootRt = rootGo.AddComponent<RectTransform>();
            rootRt.anchorMin = new Vector2(0f, 1f);
            rootRt.anchorMax = new Vector2(0f, 1f);
            rootRt.pivot = new Vector2(0f, 1f);
            rootRt.anchoredPosition = topLeftAnchoredPosition;
            rootRt.sizeDelta = new Vector2(RowWidth, RowHeight * 3f + RowGap * 2f);

            _goldValue = CreateRow(rootGo.transform, font, "GoldRow", MetaHomeUiSprites.CoinIcon, 0f, false, out _);
            _keysValue = CreateRow(
                rootGo.transform,
                font,
                "KeysRow",
                MetaHomeUiSprites.KeyIcon,
                -(RowHeight + RowGap),
                false,
                out _);
            _energyValue = CreateRow(
                rootGo.transform,
                font,
                "EnergyRow",
                MetaHomeUiSprites.EnergyIcon,
                -(RowHeight + RowGap) * 2f,
                true,
                out _energyFill);
        }

        static Text CreateRow(
            Transform parent,
            Font font,
            string rowName,
            Sprite iconSprite,
            float yOffset,
            bool showProgress,
            out Image progressFill)
        {
            progressFill = null;
            var rowGo = new GameObject(rowName);
            rowGo.transform.SetParent(parent, false);
            var rowRt = rowGo.AddComponent<RectTransform>();
            rowRt.anchorMin = new Vector2(0f, 1f);
            rowRt.anchorMax = new Vector2(0f, 1f);
            rowRt.pivot = new Vector2(0f, 1f);
            rowRt.anchoredPosition = new Vector2(0f, yOffset);
            rowRt.sizeDelta = new Vector2(RowWidth, RowHeight);

            var barGo = new GameObject("Bar");
            barGo.transform.SetParent(rowGo.transform, false);
            var barImg = barGo.AddComponent<Image>();
            barImg.sprite = MetaHomeUiSprites.ResourcePill;
            barImg.type = Image.Type.Sliced;
            barImg.color = Color.white;
            barImg.raycastTarget = false;
            var barRt = barImg.rectTransform;
            barRt.anchorMin = Vector2.zero;
            barRt.anchorMax = Vector2.one;
            barRt.offsetMin = Vector2.zero;
            barRt.offsetMax = Vector2.zero;

            if (showProgress)
            {
                var fillGo = new GameObject("ProgressFill");
                fillGo.transform.SetParent(barGo.transform, false);
                progressFill = fillGo.AddComponent<Image>();
                progressFill.sprite = MetaHudTheme.WhiteSprite();
                progressFill.type = Image.Type.Filled;
                progressFill.fillMethod = Image.FillMethod.Horizontal;
                progressFill.fillOrigin = (int)Image.OriginHorizontal.Left;
                progressFill.fillAmount = 1f;
                progressFill.color = new Color(0.12f, 0.7f, 1f, 0.72f);
                progressFill.raycastTarget = false;
                var fillRt = progressFill.rectTransform;
                fillRt.anchorMin = Vector2.zero;
                fillRt.anchorMax = Vector2.one;
                fillRt.offsetMin = new Vector2(7f, 7f);
                fillRt.offsetMax = new Vector2(-7f, -7f);
            }

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(rowGo.transform, false);
            var icon = iconGo.AddComponent<Image>();
            icon.sprite = iconSprite;
            icon.color = Color.white;
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(7f, 0f);
            iconRt.sizeDelta = new Vector2(52f, 52f);

            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(rowGo.transform, false);
            var value = valueGo.AddComponent<Text>();
            value.font = font;
            value.fontSize = 23;
            value.fontStyle = FontStyle.Bold;
            value.alignment = TextAnchor.MiddleRight;
            value.color = MetaHudTheme.TextPrimary;
            value.raycastTarget = false;
            var valueRt = value.rectTransform;
            valueRt.anchorMin = Vector2.zero;
            valueRt.anchorMax = Vector2.one;
            valueRt.offsetMin = new Vector2(ValueLeftPad, 0f);
            valueRt.offsetMax = new Vector2(-12f, 0f);
            return value;
        }

        public void Refresh()
        {
            if (_goldValue != null)
                _goldValue.text = MetaProgressionStore.Gold.ToString("N0");
            if (_keysValue != null)
                _keysValue.text = MetaProgressionStore.TreasureKeys.ToString();
            if (_energyValue != null)
                _energyValue.text = $"{MetaProgressionStore.Energy}/{MetaProgressionStore.MaxEnergy}";
            if (_energyFill != null)
                _energyFill.fillAmount = Mathf.Clamp01(
                    MetaProgressionStore.Energy / (float)Mathf.Max(1, MetaProgressionStore.MaxEnergy));
        }
    }
}
