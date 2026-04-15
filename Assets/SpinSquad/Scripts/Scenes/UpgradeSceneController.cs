using SpinSquad.Core;
using SpinSquad.Data;
using SpinSquad.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpinSquad.Scenes
{
    public sealed class UpgradeSceneController : MonoBehaviour
    {
        [SerializeField] string homepageSceneName = "Homepage";
        [SerializeField] string detailSceneName = "UpgradeDetail";
        Font _font;
        UnitCatalog _unitCatalog;
        Text _goldText;
        Text[] _cardNameTexts = new Text[MetaProgressionStore.AllyLineCount * MetaProgressionStore.UpgradableRarityCount];
        Image[] _cardPortraits = new Image[MetaProgressionStore.AllyLineCount * MetaProgressionStore.UpgradableRarityCount];
        Text _hintText;

        void Start()
        {
            EnsureEventSystem();
            ResolveFont();
            BuildUi();
            RefreshUi();
        }

        void OnEnable()
        {
            if (_goldText != null)
                RefreshUi();
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void ResolveFont()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            _unitCatalog = Resources.Load<UnitCatalog>("UnitCatalog_Main");
            _unitCatalog?.RebuildIndex();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("UpgradeUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            MetaHudTheme.AddFullScreenBackdrop(canvasGo.transform, 0);
            AddHeaderStrip(canvasGo.transform);

            CreateText(
                canvasGo.transform,
                "Title",
                "Upgrade",
                MetaHudTheme.FontTitle,
                MetaHudTheme.TextPrimary,
                new Vector2(0f, -MetaHudTheme.SafeEdgeYTop - 8f),
                new Vector2(900f, 72f));
            _goldText = CreateTopLeftGoldText(canvasGo.transform);

            for (var line = 0; line < MetaProgressionStore.AllyLineCount; line++)
            {
                var cx = ColumnCenterX(line);
                CreateText(
                    canvasGo.transform,
                    $"ColHeader_{line}",
                    $"Line {line}",
                    MetaHudTheme.FontSection,
                    MetaHudTheme.SectionLabel,
                    new Vector2(cx, MetaHudTheme.UpgradeColumnHeaderY),
                    new Vector2(MetaHudTheme.UpgradeCardSize.x + 8f, 40f));
            }

            for (var step = 0; step < MetaProgressionStore.UpgradableRarityCount; step++)
            {
                var rowCenterY = RowCenterY(step);
                var gridLeft = ColumnCenterX(0) - MetaHudTheme.UpgradeCardSize.x * 0.5f;
                CreateText(
                    canvasGo.transform,
                    $"RowLabel_{step}",
                    ((Rarity)step).ToString(),
                    MetaHudTheme.FontSection,
                    MetaHudTheme.SectionLabel,
                    new Vector2(gridLeft - 10f, rowCenterY),
                    new Vector2(132f, 40f),
                    TextAnchor.MiddleRight);
            }

            for (var line = 0; line < MetaProgressionStore.AllyLineCount; line++)
            {
                for (var step = 0; step < MetaProgressionStore.UpgradableRarityCount; step++)
                {
                    var rarity = (Rarity)step;
                    var idx = line * MetaProgressionStore.UpgradableRarityCount + step;
                    var lineCapture = line;
                    var rarityCapture = rarity;
                    var x = ColumnCenterX(line);
                    var y = MetaHudTheme.UpgradeCardTopY - step * MetaHudTheme.UpgradeCardRowSpacing;
                    CreateUpgradeCard(
                        canvasGo.transform,
                        idx,
                        new Vector2(x, y),
                        rarity,
                        () => OpenDetail(lineCapture, rarityCapture));
                }
            }

            _hintText = CreateText(
                canvasGo.transform,
                "Hint",
                "Tap a card for stats and upgrade.",
                MetaHudTheme.FontHint,
                MetaHudTheme.TextSecondary,
                new Vector2(0f, -1120f),
                new Vector2(980f, 90f));
            CreateButton(canvasGo.transform, "Back", new Vector2(0f, -1268f), MetaHudTheme.ButtonBack, BackToHomepage);
        }

        static float ColumnCenterX(int line)
        {
            var n = MetaProgressionStore.AllyLineCount;
            var dx = MetaHudTheme.UpgradeColumnSpacingX;
            var start = -(n - 1) * 0.5f * dx;
            return start + line * dx;
        }

        static float RowCenterY(int step) =>
            MetaHudTheme.UpgradeCardTopY - step * MetaHudTheme.UpgradeCardRowSpacing - MetaHudTheme.UpgradeCardSize.y * 0.5f;

        static void AddHeaderStrip(Transform parent)
        {
            var go = new GameObject("HeaderStrip");
            go.transform.SetParent(parent, false);
            go.transform.SetSiblingIndex(1);
            var img = go.AddComponent<Image>();
            img.sprite = MetaHudTheme.WhiteSprite();
            img.color = MetaHudTheme.HeaderBand;
            img.raycastTarget = false;
            var rt = img.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(0f, 168f);
        }

        void OpenDetail(int line, Rarity rarity)
        {
            if (line < 0 || line >= MetaProgressionStore.AllyLineCount)
            {
                _hintText.text = "Invalid ally card selection.";
                return;
            }

            MetaUiSelectionContext.SetSelectedUpgrade(line, rarity);
            if (Application.CanStreamedLevelBeLoaded(detailSceneName))
                SceneManager.LoadScene(detailSceneName);
            else
            {
                _hintText.text = $"Cannot load scene: {detailSceneName}";
                Debug.LogWarning("[Upgrade] Cannot load detail scene: " + detailSceneName);
            }
        }

        void RefreshUi()
        {
            _goldText.text = $"Gold: {MetaProgressionStore.Gold}";
            for (var line = 0; line < MetaProgressionStore.AllyLineCount; line++)
            {
                for (var step = 0; step < MetaProgressionStore.UpgradableRarityCount; step++)
                {
                    var rarity = (Rarity)step;
                    var idx = line * MetaProgressionStore.UpgradableRarityCount + step;
                    var id = AllyLineCatalog.UnitIdForLine(line);
                    var allyName = ResolveAllyDisplayName(id);
                    _cardNameTexts[idx].text = allyName;
                    var shape = UnitBodyShape.Square;
                    if (_unitCatalog != null && _unitCatalog.TryGet(id, out var def))
                        shape = def.BodyShape;
                    var port = _cardPortraits[idx];
                    port.sprite = UnitSpriteFactory.GetSprite(shape);
                    port.color = RarityPalette.UnitTint(rarity, UnitTeamKind.Ally);
                }
            }
        }

        string ResolveAllyDisplayName(string unitId)
        {
            if (_unitCatalog != null && _unitCatalog.TryGet(unitId, out var def) && !string.IsNullOrWhiteSpace(def.DisplayName))
                return def.DisplayName;
            return unitId;
        }

        void BackToHomepage()
        {
            if (Application.CanStreamedLevelBeLoaded(homepageSceneName))
                SceneManager.LoadScene(homepageSceneName);
            else
            {
                _hintText.text = $"Cannot load scene: {homepageSceneName}";
                Debug.LogWarning("[Upgrade] Cannot load homepage scene: " + homepageSceneName);
            }
        }

        Text CreateText(
            Transform parent,
            string name,
            string value,
            int size,
            Color color,
            Vector2 pos,
            Vector2 dim,
            TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.text = value;
            t.alignment = alignment;
            t.color = color;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            if (alignment == TextAnchor.MiddleRight || alignment == TextAnchor.UpperRight || alignment == TextAnchor.LowerRight)
                rt.pivot = new Vector2(1f, 0.5f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;
            return t;
        }

        Text CreateTopLeftGoldText(Transform parent)
        {
            var go = new GameObject("Gold");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = MetaHudTheme.FontResource;
            t.alignment = TextAnchor.UpperLeft;
            t.color = MetaHudTheme.TextPrimary;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop);
            rt.sizeDelta = new Vector2(640f, 96f);
            return t;
        }

        void CreateUpgradeCard(Transform parent, int idx, Vector2 pos, Rarity rarity, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject($"UpgradeCard_{idx}");
            btnGo.transform.SetParent(parent, false);
            var bg = btnGo.AddComponent<Image>();
            bg.sprite = MetaHudTheme.WhiteSprite();
            bg.color = RarityPalette.UpgradeCardBackground(rarity);
            MetaHudTheme.ApplyImageOutline(bg);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = MetaHudTheme.UpgradeCardSize;

            var portGo = new GameObject("Portrait");
            portGo.transform.SetParent(btnGo.transform, false);
            var portImg = portGo.AddComponent<Image>();
            portImg.raycastTarget = false;
            portImg.preserveAspect = true;
            var prt = portImg.rectTransform;
            prt.anchorMin = new Vector2(0.1f, 0.34f);
            prt.anchorMax = new Vector2(0.9f, 0.94f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(btnGo.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = _font;
            nameText.fontSize = MetaHudTheme.FontHint;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = MetaHudTheme.TextPrimary;
            nameText.raycastTarget = false;
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;
            nameText.verticalOverflow = VerticalWrapMode.Truncate;
            var nrt = nameText.rectTransform;
            nrt.anchorMin = new Vector2(0.06f, 0.02f);
            nrt.anchorMax = new Vector2(0.94f, 0.32f);
            nrt.offsetMin = Vector2.zero;
            nrt.offsetMax = Vector2.zero;

            _cardPortraits[idx] = portImg;
            _cardNameTexts[idx] = nameText;
        }

        Button CreateButton(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = MetaHudTheme.WhiteSprite();
            image.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = MetaHudTheme.FontButton;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = MetaHudTheme.TextPrimary;
            text.raycastTarget = false;
            var lrt = text.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = MetaHudTheme.CtaSize;
            return btn;
        }
    }
}
