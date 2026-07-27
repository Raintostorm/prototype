using System.Collections.Generic;
using SpinSquad.Data;
using SpinSquad.UI;
using SpinSquad.Meta;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpinSquad.Scenes
{
    /// <summary>
    /// Trang chủ: vào campaign / upgrade / treasure.
    /// Roll gacha nằm trong trận (<see cref="Core.DuelDirector"/>).
    /// </summary>
    public sealed class HomepageHub : MonoBehaviour
    {
        [SerializeField] string campaignSceneName = "SampleScene";
        [SerializeField] string upgradeSceneName = "Upgrade";
        [SerializeField] string treasureSceneName = "Treasure";

        Font _font;
        MetaResourceHudBar _resourceHud;
        Text _levelText;
        GameObject _levelSelectRoot;
        readonly Button[] _levelPickButtons = new Button[3];
        readonly Image[] _bottomNavTabImages = new Image[4];

        void Start()
        {
            TearDownStaleRuntimeUi();
            EnsureEventSystem();
            ResolveUiFont();
            BuildUi();
        }

        void OnEnable()
        {
            RefreshMetaTexts();
        }

        static void TearDownStaleRuntimeUi()
        {
            var stale = GameObject.Find("HomepageUI");
            if (stale != null)
                Destroy(stale);
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void ResolveUiFont()
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            if (_font == null)
            {
                try
                {
                    _font = Font.CreateDynamicFontFromOSFont(new[] { "Arial", "Helvetica", "Liberation Sans" }, 32);
                }
                catch
                {
                    // ignored
                }
            }

            if (_font == null)
                Debug.LogError("[HomepageHub] Không lấy được font — chữ UI có thể không hiện. Kiểm tra Unity 6 / Resources built-in font.");
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("HomepageUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 50;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            MetaHudTheme.AddFullScreenBackdrop(canvasGo.transform, GameBackgroundUiSprites.Home, 0);
            AddHeaderStrip(canvasGo.transform);
            var safeRoot = CreateSafeAreaRoot(canvasGo.transform);

            CreateTitle(safeRoot);
            _resourceHud = new MetaResourceHudBar();
            _resourceHud.Build(
                safeRoot,
                _font,
                new Vector2(MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop));
            BuildMetaStubTopRow(safeRoot);
            BuildCampaignSelector(safeRoot);
            BuildPlayHeroButton(safeRoot);
            BuildBottomNav(safeRoot);
            _levelSelectRoot = BuildLevelSelectOverlay(canvasGo.transform);
            _levelSelectRoot.SetActive(false);
            RefreshMetaTexts();
        }

        static RectTransform CreateSafeAreaRoot(Transform parent)
        {
            var go = new GameObject("SafeArea");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            go.AddComponent<SafeAreaFitter>();
            return rt;
        }

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
            rt.sizeDelta = new Vector2(0f, 300f);
        }

        void BuildCampaignSelector(Transform parent)
        {
            var cardGo = new GameObject("CampaignSelector");
            cardGo.transform.SetParent(parent, false);
            var card = cardGo.AddComponent<Image>();
            card.sprite = MetaHomeUiSprites.CampaignPanel;
            card.color = Color.white;
            card.type = Image.Type.Sliced;
            card.raycastTarget = false;

            var cardRt = card.rectTransform;
            cardRt.anchorMin = cardRt.anchorMax = new Vector2(0.5f, 0.56f);
            cardRt.pivot = new Vector2(0.5f, 0.5f);
            cardRt.anchoredPosition = new Vector2(0f, 36f);
            cardRt.sizeDelta = new Vector2(790f, 270f);

            var captionGo = new GameObject("CampaignCaption");
            captionGo.transform.SetParent(cardGo.transform, false);
            var caption = captionGo.AddComponent<Text>();
            caption.font = _font;
            caption.fontSize = MetaHudTheme.FontSection;
            caption.text = "CAMPAIGN";
            caption.alignment = TextAnchor.MiddleCenter;
            caption.color = MetaHudTheme.SectionLabel;
            caption.raycastTarget = false;
            var captionRt = caption.rectTransform;
            captionRt.anchorMin = captionRt.anchorMax = new Vector2(0.5f, 1f);
            captionRt.pivot = new Vector2(0.5f, 1f);
            captionRt.anchoredPosition = new Vector2(0f, -24f);
            captionRt.sizeDelta = new Vector2(460f, 40f);

            _levelText = CreateInfoText(cardGo.transform, "LevelText", Vector2.zero, 34);
            var levelRt = _levelText.rectTransform;
            levelRt.anchorMin = levelRt.anchorMax = new Vector2(0.5f, 0.5f);
            levelRt.pivot = new Vector2(0.5f, 0.5f);
            levelRt.anchoredPosition = new Vector2(0f, -2f);
            levelRt.sizeDelta = new Vector2(470f, 118f);

            var navLeft = CreatePolishedIconButton(cardGo.transform, "Prev", MetaHomeUiSprites.PrevIcon, new Vector2(-314f, -4f), DecreaseLevel);
            navLeft.GetComponent<RectTransform>().sizeDelta = new Vector2(104f, 104f);

            var navRight = CreatePolishedIconButton(cardGo.transform, "Next", MetaHomeUiSprites.NextIcon, new Vector2(314f, -4f), IncreaseLevel);
            navRight.GetComponent<RectTransform>().sizeDelta = new Vector2(104f, 104f);
        }

        void CreateTitle(Transform parent)
        {
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(parent, false);
            var title = titleGo.AddComponent<Text>();
            title.font = _font;
            title.text = "SPIN SQUAD";
            title.fontSize = MetaHudTheme.FontTitle;
            title.alignment = TextAnchor.MiddleCenter;
            title.color = MetaHudTheme.TextPrimary;
            title.raycastTarget = false;
            var titleRt = title.rectTransform;
            titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
            titleRt.pivot = new Vector2(0.5f, 1f);
            titleRt.anchoredPosition = new Vector2(0f, -MetaHudTheme.SafeEdgeYTop - 8f);
            titleRt.sizeDelta = new Vector2(900f, 72f);
        }

        void BuildPlayHeroButton(Transform parent)
        {
            var btn = CreateHudCtaButton(
                parent,
                "BATTLE",
                Vector2.zero,
                MetaHudTheme.ButtonPlay,
                MetaHomeUiSprites.SwordIcon,
                OpenLevelSelect);
            var rt = btn.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            rt.sizeDelta = new Vector2(620f, 124f);
            rt.anchoredPosition = new Vector2(0f, MetaHudTheme.BottomNavHeight + 44f);
        }

        void BuildBottomNav(Transform parent)
        {
            var lane = MetaHudTheme.CreateBottomLanePanel(
                parent, MetaHudTheme.BottomNavHeight, MetaHudTheme.BottomLaneBand, "BottomNav");
            var row = MetaHudTheme.CreateHorizontalRowParent(lane, 12f);

            var home = CreateBottomNavTab(row, "Home", MetaHomeUiSprites.HomeIcon, OnBottomNavHome);
            var upgrade = CreateBottomNavTab(row, "Upgrade", MetaHomeUiSprites.UpgradeIcon, LoadUpgrade);
            var treasure = CreateBottomNavTab(row, "Treasure", MetaHomeUiSprites.TreasureIcon, LoadTreasure);
            var settings = CreateBottomNavTab(
                row,
                "Settings",
                MetaHomeUiSprites.SettingsIcon,
                () => StubHudFeedback.LogComingSoon("settings"));

            var rts = new List<RectTransform> { home, upgrade, treasure, settings };
            var fracs = new List<float> { 0.25f, 0.25f, 0.25f, 0.25f };
            MetaHudTheme.LayoutHorizontalRow(row, MetaHudTheme.ActionRowGap, rts, fracs);
            SetBottomNavActive(0);
        }

        void OnBottomNavHome()
        {
            RefreshMetaTexts();
        }

        RectTransform CreateBottomNavTab(
            RectTransform row,
            string label,
            Sprite icon,
            UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(label + "NavTab");
            var background = btnGo.AddComponent<Image>();
            background.sprite = MetaHomeUiSprites.NavTab;
            background.type = Image.Type.Sliced;
            background.color = Color.white;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = background;
            btn.onClick.AddListener(onClick);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            var iconImage = iconGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(iconImage, icon, Color.white);
            iconImage.raycastTarget = false;
            iconImage.preserveAspect = true;
            var iconRt = iconImage.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 1f);
            iconRt.pivot = new Vector2(0.5f, 1f);
            iconRt.anchoredPosition = new Vector2(0f, -8f);
            iconRt.sizeDelta = new Vector2(66f, 66f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = MetaHudTheme.FontHint;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = MetaHudTheme.TextSecondary;
            text.raycastTarget = false;
            var labelRt = text.rectTransform;
            labelRt.anchorMin = new Vector2(0f, 0f);
            labelRt.anchorMax = new Vector2(1f, 0f);
            labelRt.pivot = new Vector2(0.5f, 0f);
            labelRt.anchoredPosition = new Vector2(0f, 8f);
            labelRt.sizeDelta = new Vector2(0f, 34f);

            var idx = label switch
            {
                "Home" => 0,
                "Upgrade" => 1,
                "Treasure" => 2,
                "Settings" => 3,
                _ => -1
            };
            if (idx >= 0)
                _bottomNavTabImages[idx] = background;

            return btnGo.GetComponent<RectTransform>();
        }

        void SetBottomNavActive(int index)
        {
            for (var i = 0; i < _bottomNavTabImages.Length; i++)
            {
                var img = _bottomNavTabImages[i];
                if (img == null)
                    continue;
                img.sprite = i == index ? MetaHomeUiSprites.NavTabActive : MetaHomeUiSprites.NavTab;
                img.color = Color.white;
            }
        }

        void OpenLevelSelect()
        {
            RefreshLevelPickButtons();
            if (_levelSelectRoot != null)
                _levelSelectRoot.SetActive(true);
        }

        void CloseLevelSelect()
        {
            if (_levelSelectRoot != null)
                _levelSelectRoot.SetActive(false);
        }

        void PickLevelAndPlay(int level)
        {
            if (level < 1 || level > MetaProgressionStore.MaxLevels)
                return;
            if (level > MetaProgressionStore.UnlockedLevel)
                return;
            MetaProgressionStore.SelectedLevel = level;
            CloseLevelSelect();
            LoadCampaign();
        }

        void RefreshLevelPickButtons()
        {
            for (var i = 0; i < _levelPickButtons.Length; i++)
            {
                var btn = _levelPickButtons[i];
                if (btn == null)
                    continue;
                var lv = i + 1;
                var unlocked = lv <= MetaProgressionStore.UnlockedLevel;
                btn.interactable = unlocked;
                var img = btn.GetComponent<Image>();
                if (img != null)
                {
                    img.sprite = unlocked ? LevelButtonSprite(i) : MetaHomeUiSprites.DarkButton;
                    img.color = Color.white;
                }
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                    txt.color = unlocked ? Color.white : new Color(0.62f, 0.62f, 0.65f, 1f);
            }
        }

        static Sprite LevelButtonSprite(int index)
        {
            return index switch
            {
                1 => MetaHomeUiSprites.GreenButton,
                2 => MetaHomeUiSprites.GoldButton,
                _ => MetaHomeUiSprites.BlueButton
            };
        }

        GameObject BuildLevelSelectOverlay(Transform canvasTransform)
        {
            var root = new GameObject("LevelSelectOverlay");
            root.transform.SetParent(canvasTransform, false);
            var rootRt = root.AddComponent<RectTransform>();
            rootRt.anchorMin = Vector2.zero;
            rootRt.anchorMax = Vector2.one;
            rootRt.offsetMin = Vector2.zero;
            rootRt.offsetMax = Vector2.zero;

            var dimGo = new GameObject("Dim");
            dimGo.transform.SetParent(root.transform, false);
            var dimRt = dimGo.AddComponent<RectTransform>();
            dimRt.anchorMin = Vector2.zero;
            dimRt.anchorMax = Vector2.one;
            dimRt.offsetMin = Vector2.zero;
            dimRt.offsetMax = Vector2.zero;
            var dimImg = dimGo.AddComponent<Image>();
            dimImg.sprite = MetaHudTheme.WhiteSprite();
            dimImg.color = MetaHudTheme.OverlayDim;
            dimImg.raycastTarget = true;
            var dimBtn = dimGo.AddComponent<Button>();
            dimBtn.targetGraphic = dimImg;
            dimBtn.onClick.AddListener(CloseLevelSelect);

            var content = new GameObject("LevelSelectContent");
            content.transform.SetParent(root.transform, false);
            var crt = content.AddComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = new Vector2(0.5f, 0.5f);
            crt.pivot = new Vector2(0.5f, 0.5f);
            crt.anchoredPosition = Vector2.zero;
            crt.sizeDelta = new Vector2(720f, 980f);

            var titleGo = new GameObject("PickTitle");
            titleGo.transform.SetParent(content.transform, false);
            var pickTitle = titleGo.AddComponent<Text>();
            pickTitle.font = _font;
            pickTitle.text = "Chọn level";
            pickTitle.fontSize = MetaHudTheme.FontOverlayTitle;
            pickTitle.alignment = TextAnchor.MiddleCenter;
            pickTitle.color = MetaHudTheme.TextPrimary;
            pickTitle.raycastTarget = false;
            var pickTitleRt = pickTitle.rectTransform;
            pickTitleRt.anchorMin = pickTitleRt.anchorMax = new Vector2(0.5f, 0.5f);
            pickTitleRt.pivot = new Vector2(0.5f, 0.5f);
            pickTitleRt.anchoredPosition = new Vector2(0f, 380f);
            pickTitleRt.sizeDelta = new Vector2(680f, 56f);

            for (var i = 0; i < 3; i++)
            {
                var captureLv = i + 1;
                var g = MetaProgressionStore.LevelCompleteGoldReward(captureLv);
                var k = MetaProgressionStore.LevelCompleteTreasureKeyReward();
                var label = $"Level {captureLv}\n+{g} Gold   +{k} Keys";
                var btn = CreateHudCtaButton(
                    content.transform,
                    label,
                    new Vector2(0f, 190f - i * 150f),
                    RarityPalette.UpgradeCardBackground((Rarity)i),
                    MetaHomeUiSprites.SwordIcon,
                    () => PickLevelAndPlay(captureLv));
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(520f, MetaHudTheme.CtaSize.y);
                var t = btn.GetComponentInChildren<Text>();
                if (t != null)
                    t.fontSize = MetaHudTheme.FontOverlayRow;
                _levelPickButtons[i] = btn;
            }

            var closeBtn = CreateHudCtaButton(
                content.transform,
                "",
                new Vector2(0f, -320f),
                MetaHudTheme.ButtonMuted,
                MetaHomeUiSprites.CloseIcon,
                CloseLevelSelect);
            closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(120f, 120f);

            return root;
        }

        void LoadCampaign()
        {
            MetaProgressionStore.SelectedLevel = Mathf.Clamp(MetaProgressionStore.SelectedLevel, 1, MetaProgressionStore.UnlockedLevel);
            if (!Application.CanStreamedLevelBeLoaded(campaignSceneName))
            {
                Debug.LogWarning("[Homepage] Không load được scene: " + campaignSceneName);
                return;
            }

            SceneManager.LoadScene(campaignSceneName);
        }

        void LoadUpgrade()
        {
            if (!Application.CanStreamedLevelBeLoaded(upgradeSceneName))
            {
                Debug.LogWarning("[Homepage] Không load được scene: " + upgradeSceneName);
                return;
            }

            SceneManager.LoadScene(upgradeSceneName);
        }

        void LoadTreasure()
        {
            if (!Application.CanStreamedLevelBeLoaded(treasureSceneName))
            {
                Debug.LogWarning("[Homepage] Không load được scene: " + treasureSceneName);
                return;
            }

            SceneManager.LoadScene(treasureSceneName);
        }

        void IncreaseLevel()
        {
            MetaProgressionStore.SelectedLevel = Mathf.Min(MetaProgressionStore.SelectedLevel + 1, MetaProgressionStore.UnlockedLevel);
            RefreshMetaTexts();
        }

        void DecreaseLevel()
        {
            MetaProgressionStore.SelectedLevel = Mathf.Max(1, MetaProgressionStore.SelectedLevel - 1);
            RefreshMetaTexts();
        }

        void RefreshMetaTexts()
        {
            _resourceHud?.Refresh();
            if (_levelText != null)
                _levelText.text =
                    $"LEVEL {MetaProgressionStore.SelectedLevel}\n" +
                    $"Unlocked: {MetaProgressionStore.UnlockedLevel}/{MetaProgressionStore.MaxLevels}";
        }

        Text CreateInfoText(Transform parent, string objectName, Vector2 pos, int fontSize)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = fontSize;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = MetaHudTheme.TextSecondary;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(900f, 90f);
            return text;
        }

        void BuildMetaStubTopRow(Transform canvasParent)
        {
            var w = MetaHudTheme.IconBarButtonSize.x;
            var h = MetaHudTheme.IconBarButtonSize.y;
            var gap = MetaHudTheme.IconBarGap;
            var insetX = MetaHudTheme.SafeEdgeX + MetaHudTheme.TopIconBarExtraInsetX;
            var px = -insetX - w * 0.5f;
            var rowTop = MetaHudTheme.HomepageTitleBottomFromTop + 38f;
            var py = -rowTop - h * 0.5f;

            CreateMetaTopBarButton(canvasParent, "Mail", "home_mail_stub", new Vector2(px, py), () => StubHudFeedback.LogComingSoon("mail"), MetaHomeUiSprites.MailIcon);
            px -= w + gap;
            CreateMetaTopBarButton(canvasParent, "Shop", "home_shop_stub", new Vector2(px, py), () => StubHudFeedback.LogComingSoon("shop"), MetaHomeUiSprites.ShopIcon);
        }

        void CreateMetaTopBarButton(
            Transform canvasParent,
            string label,
            string hierarchyName,
            Vector2 anchoredTopRight,
            UnityEngine.Events.UnityAction onClick,
            Sprite iconSprite = null)
        {
            var btnGo = new GameObject(hierarchyName);
            btnGo.transform.SetParent(canvasParent, false);

            var image = btnGo.AddComponent<Image>();
            image.sprite = MetaHomeUiSprites.SmallIconButton;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = image;
            var colors = btn.colors;
            colors.highlightedColor = MetaHudTheme.ButtonIconBar * 1.12f;
            colors.pressedColor = MetaHudTheme.ButtonIconBar * 0.82f;
            btn.colors = colors;
            btn.onClick.AddListener(onClick);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            var icon = iconGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(icon, iconSprite, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(58f, 58f);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = MetaHudTheme.FontHint;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = MetaHudTheme.TextPrimary;
            text.raycastTarget = false;
            labelGo.SetActive(iconSprite == null && !string.IsNullOrEmpty(label));

            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 1f);
            btnRt.pivot = new Vector2(1f, 1f);
            btnRt.anchoredPosition = anchoredTopRight;
            btnRt.sizeDelta = MetaHudTheme.IconBarButtonSize;
        }

        Button CreateHudCtaButton(
            Transform parent,
            string label,
            Vector2 anchoredPos,
            Color fallbackBg,
            Sprite hudSprite,
            UnityEngine.Events.UnityAction onClick)
        {
            var btn = CreateButton(parent, label, anchoredPos, fallbackBg, onClick);
            var rt = btn.GetComponent<RectTransform>();
            rt.sizeDelta = MetaHudTheme.CtaSize;
            var image = btn.GetComponent<Image>();
            image.sprite = string.Equals(label, "BATTLE", System.StringComparison.Ordinal)
                ? MetaHomeUiSprites.CtaButton
                : MetaHomeUiSprites.BlueButton;
            image.type = Image.Type.Sliced;
            image.color = Color.white;

            if (hudSprite != null)
            {
                var iconGo = new GameObject("Icon");
                iconGo.transform.SetParent(btn.transform, false);
                var icon = iconGo.AddComponent<Image>();
                HudUiSprites.ApplyIcon(icon, hudSprite, Color.white);
                icon.raycastTarget = false;
                icon.preserveAspect = true;
                var iconRt = icon.rectTransform;
                iconRt.anchorMin = iconRt.anchorMax = string.IsNullOrEmpty(label)
                    ? new Vector2(0.5f, 0.5f)
                    : new Vector2(0f, 0.5f);
                iconRt.pivot = string.IsNullOrEmpty(label)
                    ? new Vector2(0.5f, 0.5f)
                    : new Vector2(0f, 0.5f);
                iconRt.anchoredPosition = string.IsNullOrEmpty(label)
                    ? Vector2.zero
                    : new Vector2(48f, 0f);
                iconRt.sizeDelta = string.IsNullOrEmpty(label) ? new Vector2(62f, 62f) : new Vector2(72f, 72f);
            }

            var text = btn.GetComponentInChildren<Text>();
            HudUiButtonHelper.SyncLabelVisibility(text, null, label);
            if (text != null)
            {
                text.fontStyle = FontStyle.Bold;
                var labelRt = text.rectTransform;
                labelRt.offsetMin = string.IsNullOrEmpty(label) ? Vector2.zero : new Vector2(112f, 0f);
                labelRt.offsetMax = string.IsNullOrEmpty(label) ? Vector2.zero : new Vector2(-28f, 0f);
            }
            return btn;
        }

        Button CreatePolishedIconButton(
            Transform parent,
            string hierarchyName,
            Sprite iconSprite,
            Vector2 anchoredPos,
            UnityEngine.Events.UnityAction onClick)
        {
            var btn = CreateButton(parent, "", anchoredPos, MetaHudTheme.ButtonNav, onClick);
            btn.gameObject.name = hierarchyName + "Button";
            var image = btn.GetComponent<Image>();
            image.sprite = MetaHomeUiSprites.SmallIconButton;
            image.type = Image.Type.Sliced;
            image.color = Color.white;
            var label = btn.GetComponentInChildren<Text>();
            if (label != null)
                label.gameObject.SetActive(false);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btn.transform, false);
            var icon = iconGo.AddComponent<Image>();
            HudUiSprites.ApplyIcon(icon, iconSprite, Color.white);
            icon.raycastTarget = false;
            icon.preserveAspect = true;
            var iconRt = icon.rectTransform;
            iconRt.anchorMin = iconRt.anchorMax = new Vector2(0.5f, 0.5f);
            iconRt.pivot = new Vector2(0.5f, 0.5f);
            iconRt.anchoredPosition = Vector2.zero;
            iconRt.sizeDelta = new Vector2(64f, 64f);
            return btn;
        }

        Button CreateButton(Transform parent, string label, Vector2 anchoredPos, Color bg, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(label + "Button");
            btnGo.transform.SetParent(parent, false);

            var image = btnGo.AddComponent<Image>();
            image.sprite = MetaHudTheme.WhiteSprite();
            image.color = bg;

            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = _font;
            text.text = label;
            text.fontSize = MetaHudTheme.FontButton;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = MetaHudTheme.TextPrimary;
            text.raycastTarget = false;

            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 0.5f);
            btnRt.pivot = new Vector2(0.5f, 0.5f);
            btnRt.anchoredPosition = anchoredPos;
            btnRt.sizeDelta = MetaHudTheme.CtaSize;

            return btn;
        }
    }
}
