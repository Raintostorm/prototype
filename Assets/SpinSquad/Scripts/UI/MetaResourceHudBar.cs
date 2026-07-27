using SpinSquad.Meta;
using SpinSquad.Scenes;
using UnityEngine;
using UnityEngine.UI;

namespace SpinSquad.UI
{
    /// <summary>Gold / Keys / Energy bar for the meta screens.</summary>
    public sealed class MetaResourceHudBar
    {
        const float RowWidth = 300f;
        const float RowHeight = 64f;
        const float RowGap = 10f;
        const float ValueLeftPad = 86f;

        Text _goldValue;
        Text _keysValue;
        Text _energyValue;

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

            _goldValue = CreateRow(rootGo.transform, font, "GoldRow", MetaHomeUiSprites.CoinIcon, 0f);
            _keysValue = CreateRow(
                rootGo.transform,
                font,
                "KeysRow",
                MetaHomeUiSprites.KeyIcon,
                -(RowHeight + RowGap));
            _energyValue = CreateRow(
                rootGo.transform,
                font,
                "EnergyRow",
                MetaHomeUiSprites.EnergyIcon,
                -(RowHeight + RowGap) * 2f);
        }

        static Text CreateRow(
            Transform parent,
            Font font,
            string rowName,
            Sprite iconSprite,
            float yOffset)
        {
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
            iconRt.anchoredPosition = new Vector2(12f, 0f);
            iconRt.sizeDelta = new Vector2(56f, 56f);

            var valueGo = new GameObject("Value");
            valueGo.transform.SetParent(rowGo.transform, false);
            var value = valueGo.AddComponent<Text>();
            value.font = font;
            value.fontSize = 26;
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
        }
    }
}
