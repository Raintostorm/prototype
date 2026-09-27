using SpinSquad.Core;
using SpinSquad.Data;
using SpinSquad.Meta;
using SpinSquad.UI;
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
        MetaResourceHudBar _resourceHud;
        Text[] _cardNameTexts = new Text[MetaProgressionStore.AllyLineCount * MetaProgressionStore.UpgradableRarityCount];
        Image[] _cardPortraits = new Image[MetaProgressionStore.AllyLineCount * MetaProgressionStore.UpgradableRarityCount];
        readonly GameObject[] _cardLockOverlays = new GameObject[MetaProgressionStore.AllyLineCount * MetaProgressionStore.UpgradableRarityCount];
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
            if (_resourceHud != null)
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
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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

            MetaHudTheme.AddFullScreenBackdrop(canvasGo.transform, GameBackgroundUiSprites.Inventory, 0);
            MetaHudTheme.AddHeaderStrip(canvasGo.transform);

            CreateText(
                canvasGo.transform,
                "Title",
                "Upgrade",
                MetaHudTheme.FontTitle,
                MetaHudTheme.TextPrimary,
                new Vector2(0f, -MetaHudTheme.SafeEdgeYTop - 8f),
                new Vector2(900f, 72f));
            _resourceHud = new MetaResourceHudBar();
            _resourceHud.Build(
                canvasGo.transform,
                _font,
                new Vector2(MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop));
            MetaDebugGrantPanel.TryAttach(canvasGo.transform, _font, RefreshUi);

            _hintText = CreateText(
                canvasGo.transform,
                "Hint",
                "Swipe sideways to view every line. Tap a card for stats and upgrade.",
                MetaHudTheme.FontHint,
                MetaHudTheme.TextSecondary,
                new Vector2(0f, -246f),
                new Vector2(980f, 54f));

            var gridViewport = CreateUpgradeScrollView(canvasGo.transform, out var gridContent);

            for (var line = 0; line < MetaProgressionStore.AllyLineCount; line++)
            {
                var cx = ColumnCenterX(line);
                CreateText(
                    gridContent,
                    $"ColHeader_{line}",
                    $"LINE {line}",
                    MetaHudTheme.FontSection,
                    MetaHudTheme.SectionLabel,
                    new Vector2(cx, -14f),
                    new Vector2(MetaHudTheme.UpgradeCardSize.x + 8f, 40f));
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
                    var y = -68f - step * MetaHudTheme.UpgradeCardRowSpacing;
                    CreateUpgradeCard(
                        gridContent,
                        idx,
                        lineCapture,
                        new Vector2(x, y),
                        rarity,
                        () => OpenDetail(lineCapture, rarityCapture));
                }
            }

            Canvas.ForceUpdateCanvases();
            gridViewport.horizontalNormalizedPosition = 0f;
            MetaHudTheme.CreateFooterBackButton(canvasGo.transform, _font, BackToHomepage);
        }

        ScrollRect CreateUpgradeScrollView(Transform parent, out RectTransform content)
        {
            const float horizontalContentPadding = 42f;
            var viewportGo = new GameObject("UpgradeGridViewport");
            viewportGo.transform.SetParent(parent, false);
            var viewport = viewportGo.AddComponent<RectTransform>();
            viewport.anchorMin = viewport.anchorMax = new Vector2(0.5f, 1f);
            viewport.pivot = new Vector2(0.5f, 1f);
            viewport.anchoredPosition = new Vector2(0f, -306f);
            viewport.sizeDelta = new Vector2(1008f, 850f);
            var viewportImage = viewportGo.AddComponent<Image>();
            viewportImage.sprite = MetaHudTheme.WhiteSprite();
            viewportImage.color = new Color(0.02f, 0.07f, 0.12f, 0.18f);
            viewportGo.AddComponent<RectMask2D>();

            var contentGo = new GameObject("ScrollableUpgradeGrid");
            contentGo.transform.SetParent(viewportGo.transform, false);
            content = contentGo.AddComponent<RectTransform>();
            content.anchorMin = content.anchorMax = new Vector2(0.5f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            var width = MetaProgressionStore.AllyLineCount * MetaHudTheme.UpgradeCardSize.x
                + (MetaProgressionStore.AllyLineCount - 1) *
                (MetaHudTheme.UpgradeColumnSpacingX - MetaHudTheme.UpgradeCardSize.x)
                + horizontalContentPadding * 2f;
            content.sizeDelta = new Vector2(width, 830f);
            // RectTransform and ScrollRect both use centred pivots here. Explicitly
            // align the content's left edge with the viewport's left edge so the
            // first column is fully visible on the very first frame.
            content.anchoredPosition = new Vector2(
                Mathf.Max(0f, (width - viewport.sizeDelta.x) * 0.5f),
                0f);

            var scroll = viewportGo.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = content;
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.elasticity = 0.08f;
            scroll.inertia = true;
            scroll.decelerationRate = 0.12f;
            scroll.scrollSensitivity = 38f;

            var barGo = new GameObject("PositionBar");
            barGo.transform.SetParent(parent, false);
            var barImage = barGo.AddComponent<Image>();
            barImage.sprite = MetaHomeUiSprites.RarityTab;
            barImage.type = Image.Type.Sliced;
            barImage.color = new Color(1f, 1f, 1f, 0.68f);
            var barRt = barImage.rectTransform;
            barRt.anchorMin = barRt.anchorMax = new Vector2(0.5f, 1f);
            barRt.pivot = new Vector2(0.5f, 1f);
            barRt.anchoredPosition = new Vector2(0f, -1172f);
            barRt.sizeDelta = new Vector2(420f, 24f);

            var handleGo = new GameObject("Handle");
            handleGo.transform.SetParent(barGo.transform, false);
            var handleImage = handleGo.AddComponent<Image>();
            handleImage.sprite = MetaHomeUiSprites.RarityTabActive;
            handleImage.type = Image.Type.Sliced;
            handleImage.color = Color.white;
            var handleRt = handleImage.rectTransform;
            handleRt.anchorMin = Vector2.zero;
            handleRt.anchorMax = Vector2.one;
            handleRt.offsetMin = new Vector2(3f, 3f);
            handleRt.offsetMax = new Vector2(-3f, -3f);

            var scrollbar = barGo.AddComponent<Scrollbar>();
            scrollbar.handleRect = handleRt;
            scrollbar.targetGraphic = handleImage;
            scrollbar.direction = Scrollbar.Direction.LeftToRight;
            scroll.horizontalScrollbar = scrollbar;
            scroll.horizontalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scroll.horizontalScrollbarSpacing = 0f;
            return scroll;
        }

        static float ColumnCenterX(int line)
        {
            var n = MetaProgressionStore.AllyLineCount;
            var dx = MetaHudTheme.UpgradeColumnSpacingX;
            var start = -(n - 1) * 0.5f * dx;
            return start + line * dx;
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
            _resourceHud?.Refresh();
            for (var line = 0; line < MetaProgressionStore.AllyLineCount; line++)
            {
                for (var step = 0; step < MetaProgressionStore.UpgradableRarityCount; step++)
                {
                    var rarity = (Rarity)step;
                    var idx = line * MetaProgressionStore.UpgradableRarityCount + step;
                    var id = AllyLineCatalog.UnitIdForLine(line, rarity);
                    var allyName = ResolveAllyDisplayName(id);
                    _cardNameTexts[idx].text = allyName;
                    var shape = UnitBodyShape.Square;
                    Sprite resolvedCardSprite = null;
                    if (_unitCatalog != null && _unitCatalog.TryGet(id, out var def))
                    {
                        shape = def.BodyShape;
                        resolvedCardSprite = AllyUpgradePortraitResolver.Resolve(_unitCatalog, line, rarity, def);
                    }
                    var port = _cardPortraits[idx];
                    if (resolvedCardSprite != null)
                    {
                        port.sprite = resolvedCardSprite;
                        port.color = Color.white;
                    }
                    else if (HudUiSprites.LineIcon(line) != null)
                    {
                        port.sprite = HudUiSprites.LineIcon(line);
                        port.color = Color.white;
                    }
                    else
                    {
                        port.sprite = UnitSpriteFactory.GetSprite(shape);
                        port.color = RarityPalette.UnitTint(rarity, UnitTeamKind.Ally);
                    }

                    var lockOverlay = _cardLockOverlays[idx];
                    if (lockOverlay != null)
                    {
                        var status = MetaProgressionStore.GetLineUpgradeStatus(line, rarity);
                        lockOverlay.SetActive(status == LineUpgradeStatus.NeedGold);
                    }
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

        void CreateUpgradeCard(
            Transform parent,
            int idx,
            int line,
            Vector2 pos,
            Rarity rarity,
            UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject($"UpgradeCard_{idx}");
            btnGo.transform.SetParent(parent, false);
            var bg = btnGo.AddComponent<Image>();
            bg.sprite = MetaHomeUiSprites.UpgradeCard;
            bg.type = Image.Type.Sliced;
            bg.color = Color.Lerp(Color.white, RarityPalette.UpgradeCardBackground(rarity), 0.18f);

            MetaHudTheme.ApplyImageOutline(bg);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = MetaHudTheme.UpgradeCardSize;

            var rarityGo = new GameObject("RarityBand");
            rarityGo.transform.SetParent(btnGo.transform, false);
            var rarityBand = rarityGo.AddComponent<Image>();
            rarityBand.sprite = MetaHudTheme.WhiteSprite();
            rarityBand.color = RarityPalette.UnitTint(rarity, UnitTeamKind.Ally);
            rarityBand.raycastTarget = false;
            var rarityRt = rarityBand.rectTransform;
            rarityRt.anchorMin = new Vector2(0.04f, 0.08f);
            rarityRt.anchorMax = new Vector2(0.065f, 0.92f);
            rarityRt.offsetMin = rarityRt.offsetMax = Vector2.zero;

            var lineIconGo = new GameObject("LineIcon");
            lineIconGo.transform.SetParent(btnGo.transform, false);
            var lineIcon = lineIconGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(lineIcon, HudUiSprites.LineIcon(line), MetaHudTheme.TextSecondary);
            lineIcon.raycastTarget = false;
            var lineIconRt = lineIcon.rectTransform;
            lineIconRt.anchorMin = lineIconRt.anchorMax = new Vector2(0.82f, 0.82f);
            lineIconRt.sizeDelta = new Vector2(36f, 36f);

            var portGo = new GameObject("Portrait");
            portGo.transform.SetParent(btnGo.transform, false);
            var portImg = portGo.AddComponent<Image>();
            portImg.raycastTarget = false;
            portImg.preserveAspect = true;
            var prt = portImg.rectTransform;
            prt.anchorMin = new Vector2(0.13f, 0.35f);
            prt.anchorMax = new Vector2(0.87f, 0.88f);
            prt.offsetMin = Vector2.zero;
            prt.offsetMax = Vector2.zero;

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(btnGo.transform, false);
            var nameText = nameGo.AddComponent<Text>();
            nameText.font = _font;
            nameText.fontSize = 20;
            nameText.fontStyle = FontStyle.Bold;
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

            var lockGo = new GameObject("LockOverlay");
            lockGo.transform.SetParent(btnGo.transform, false);
            var lockImg = lockGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(lockImg, HudUiSprites.MetaLock, new Color(0.2f, 0.2f, 0.24f, 0.85f));
            lockImg.raycastTarget = false;
            var lockRt = lockImg.rectTransform;
            lockRt.anchorMin = new Vector2(0.72f, 0.72f);
            lockRt.anchorMax = new Vector2(0.96f, 0.96f);
            lockRt.offsetMin = Vector2.zero;
            lockRt.offsetMax = Vector2.zero;
            lockGo.SetActive(false);
            _cardLockOverlays[idx] = lockGo;

            _cardPortraits[idx] = portImg;
            _cardNameTexts[idx] = nameText;
        }

        Button CreateButton(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            SpinSquadButtonTheme.Apply(image, SpinSquadButtonTheme.FromLabel(label));
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);
            SpinSquadButtonTheme.ApplyInteraction(btn);

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
