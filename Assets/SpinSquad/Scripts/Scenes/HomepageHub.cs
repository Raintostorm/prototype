using SpinSquad.Data;
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
        Text _resourceText;
        Text _levelText;
        GameObject _levelSelectRoot;
        readonly Button[] _levelPickButtons = new Button[3];

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

            MetaHudTheme.AddFullScreenBackdrop(canvasGo.transform, 0);
            AddHeaderStrip(canvasGo.transform);

            CreateTitle(canvasGo.transform);
            _resourceText = CreateTopLeftResourceText(canvasGo.transform);
            CreateSectionCaption(canvasGo.transform, "LevelNavCaption", "Level", new Vector2(0f, -208f));
            _levelText = CreateInfoText(canvasGo.transform, "LevelText", new Vector2(0f, -252f), MetaHudTheme.FontBody);

            var navLeft = CreateButton(canvasGo.transform, "<", new Vector2(-210f, -332f), MetaHudTheme.ButtonNav, DecreaseLevel);
            navLeft.GetComponent<RectTransform>().sizeDelta = MetaHudTheme.NavArrowSize;
            var navRight = CreateButton(canvasGo.transform, ">", new Vector2(210f, -332f), MetaHudTheme.ButtonNav, IncreaseLevel);
            navRight.GetComponent<RectTransform>().sizeDelta = MetaHudTheme.NavArrowSize;

            CreateButton(canvasGo.transform, "Play", new Vector2(0f, -32f), MetaHudTheme.ButtonPlay, OpenLevelSelect)
                .GetComponent<RectTransform>().sizeDelta = MetaHudTheme.CtaSize;
            CreateButton(canvasGo.transform, "Upgrade", new Vector2(0f, -152f), MetaHudTheme.ButtonUpgrade, LoadUpgrade)
                .GetComponent<RectTransform>().sizeDelta = MetaHudTheme.CtaSize;
            CreateButton(canvasGo.transform, "Treasure", new Vector2(0f, -272f), MetaHudTheme.ButtonTreasure, LoadTreasure)
                .GetComponent<RectTransform>().sizeDelta = MetaHudTheme.CtaSize;
            _levelSelectRoot = BuildLevelSelectOverlay(canvasGo.transform);
            _levelSelectRoot.SetActive(false);
            RefreshMetaTexts();
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
            rt.sizeDelta = new Vector2(0f, 168f);
        }

        void CreateSectionCaption(Transform parent, string objectName, string caption, Vector2 pos)
        {
            var go = new GameObject(objectName);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = MetaHudTheme.FontSection;
            t.text = caption;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = MetaHudTheme.SectionLabel;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(720f, 36f);
        }

        void CreateTitle(Transform parent)
        {
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(parent, false);
            var title = titleGo.AddComponent<Text>();
            title.font = _font;
            title.text = "Homepage";
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
                    img.color = unlocked
                        ? RarityPalette.UpgradeCardBackground((Rarity)i)
                        : new Color(0.28f, 0.28f, 0.32f, 0.92f);
                var txt = btn.GetComponentInChildren<Text>();
                if (txt != null)
                    txt.color = unlocked ? Color.white : new Color(0.62f, 0.62f, 0.65f, 1f);
            }
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
                var btn = CreateButton(
                    content.transform,
                    label,
                    new Vector2(0f, 190f - i * 150f),
                    RarityPalette.UpgradeCardBackground((Rarity)i),
                    () => PickLevelAndPlay(captureLv));
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(580f, 112f);
                var t = btn.GetComponentInChildren<Text>();
                if (t != null)
                    t.fontSize = MetaHudTheme.FontOverlayRow;
                _levelPickButtons[i] = btn;
            }

            var closeBtn = CreateButton(
                content.transform,
                "Đóng",
                new Vector2(0f, -320f),
                MetaHudTheme.ButtonMuted,
                CloseLevelSelect);
            closeBtn.GetComponent<RectTransform>().sizeDelta = new Vector2(320f, 80f);

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
            if (_resourceText != null)
                _resourceText.text = $"Gold: {MetaProgressionStore.Gold}   Key: {MetaProgressionStore.TreasureKeys}";
            if (_levelText != null)
                _levelText.text = $"Level: {MetaProgressionStore.SelectedLevel}/{MetaProgressionStore.UnlockedLevel} (selected/unlocked)";
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

        Text CreateTopLeftResourceText(Transform parent)
        {
            var go = new GameObject("ResourcesText");
            go.transform.SetParent(parent, false);
            var text = go.AddComponent<Text>();
            text.font = _font;
            text.fontSize = MetaHudTheme.FontResource;
            text.alignment = TextAnchor.UpperLeft;
            text.color = MetaHudTheme.TextPrimary;
            text.raycastTarget = false;
            var rt = text.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = new Vector2(MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop);
            rt.sizeDelta = new Vector2(640f, 96f);
            return text;
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
