using System;
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
    public sealed class TreasureSceneController : MonoBehaviour
    {
        [SerializeField] string homepageSceneName = "Homepage";
        [SerializeField] string detailSceneName = "TreasureDetail";
        Font _font;
        MetaResourceHudBar _resourceHud;
        Text _resultText;
        Rarity _activeTab = Rarity.Common;
        readonly Image[] _tabImages = new Image[4];
        readonly Button[] _tabButtons = new Button[4];
        readonly GameObject[] _cardRoots = new GameObject[10];
        readonly Image[] _cardImages = new Image[10];
        readonly Image[] _cardIconImages = new Image[10];
        readonly Text[] _cardTexts = new Text[10];
        readonly System.Random _rng = new((int)(DateTime.UtcNow.Ticks ^ Guid.NewGuid().GetHashCode()));

        void Start()
        {
            EnsureEventSystem();
            ResolveFont();
            if (MetaUiSelectionContext.TryConsumeTreasureListReturnTab(out var returnTab))
                _activeTab = returnTab;
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
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("TreasureUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            MetaHudTheme.AddFullScreenBackdrop(canvasGo.transform, GameBackgroundUiSprites.Treasure, 0);
            MetaHudTheme.AddHeaderStrip(canvasGo.transform);

            CreateText(
                canvasGo.transform,
                "Title",
                "Treasure",
                MetaHudTheme.FontTitle,
                new Vector2(0f, -MetaHudTheme.SafeEdgeYTop - 8f),
                new Vector2(900f, 72f));

            _resourceHud = new MetaResourceHudBar();
            _resourceHud.Build(
                canvasGo.transform,
                _font,
                new Vector2(MetaHudTheme.SafeEdgeX, -(MetaHudTheme.SafeEdgeYTop + 4f)));
            MetaDebugGrantPanel.TryAttach(canvasGo.transform, _font, RefreshUi);

            const float rollY = -200f;
            var rollBtn = CreateButton(
                canvasGo.transform,
                "Roll (1 Key)",
                new Vector2(0f, rollY),
                new Color(0.48f, 0.34f, 0.17f, 0.96f),
                RollTreasure);
            rollBtn.GetComponent<RectTransform>().sizeDelta = MetaHudTheme.CtaSize;
            var rollImg = rollBtn.GetComponent<Image>();
            HudUiSprites.ApplyIcon(rollImg, HudUiSprites.MetaRewardButton, new Color(0.48f, 0.34f, 0.17f, 0.96f));
            HudUiButtonHelper.SyncLabelVisibility(rollBtn.GetComponentInChildren<Text>(), HudUiSprites.MetaRewardButton, "Roll (1 Key)");

            var gap = MetaHudTheme.ActionRowGap;
            _resultText = CreateText(
                canvasGo.transform,
                "Result",
                "Roll a treasure.",
                MetaHudTheme.FontOverlayRow,
                new Vector2(0f, rollY - MetaHudTheme.CtaSize.y - gap),
                new Vector2(950f, 72f));

            var tabY = rollY - MetaHudTheme.CtaSize.y - gap - 72f - MetaHudTheme.ActionRowGap * 2f;
            var tabXs = new[] { -252f, -84f, 84f, 252f };
            for (var ti = 0; ti < 4; ti++)
            {
                var rarity = (Rarity)ti;
                var cap = rarity;
                var label = rarity switch
                {
                    Rarity.Common => "Common",
                    Rarity.Rare => "Rare",
                    Rarity.Epic => "Epic",
                    _ => "Legendary"
                };
                var btn = CreateTabButton(
                    canvasGo.transform,
                    label,
                    new Vector2(tabXs[ti], tabY),
                    HudUiSprites.TreasureTabSprite(ti),
                    () => SelectTab(cap));
                _tabButtons[ti] = btn;
                _tabImages[ti] = btn.GetComponent<Image>();
                btn.GetComponent<RectTransform>().sizeDelta = new Vector2(156f, 64f);
            }

            CreateText(
                canvasGo.transform,
                "TabHint",
                "Common 10 · Rare 7 · Epic 4 · Legendary 4",
                MetaHudTheme.FontHint,
                new Vector2(0f, tabY - 64f - gap),
                new Vector2(980f, 48f));

            var cardTopY = tabY - 80f - gap;
            var cardRowH = 112f + gap * 0.25f;
            for (var i = 0; i < 10; i++)
            {
                var slot = i;
                var col = i % 2;
                var row = i / 2;
                var x = col == 0 ? -220f : 220f;
                var y = cardTopY - row * cardRowH;
                CreateTreasureCard(canvasGo.transform, slot, new Vector2(x, y), () => OpenDetail(slot));
            }

            MetaHudTheme.CreateFooterBackButton(canvasGo.transform, _font, BackToHomepage);
        }

        void SelectTab(Rarity rarity)
        {
            if (rarity > Rarity.Legendary)
                rarity = Rarity.Legendary;
            _activeTab = rarity;
            RefreshUi();
        }

        void RollTreasure()
        {
            if (!MetaProgressionStore.TrySpendTreasureKey(1))
            {
                _resultText.text = "Not enough Treasure Keys.";
                RefreshUi();
                return;
            }

            var def = TreasureDefinitions.Roll(_rng);
            var owned = MetaProgressionStore.GrantTreasure(def);
            _resultText.text =
                $"Rolled {def.Name} [{def.Rarity}] → Lv {owned.Level}/{MetaProgressionStore.MaxUpgradeLevel}";
            _activeTab = def.Rarity;
            RefreshUi();
        }

        void RefreshUi()
        {
            _resourceHud?.Refresh();
            for (var ti = 0; ti < 4; ti++)
            {
                var selected = (int)_activeTab == ti;
                var tabSprite = HudUiSprites.TreasureTabSprite(ti);
                var fallback = RarityPalette.UnitTint((Rarity)ti, UnitTeamKind.Ally);
                HudUiSprites.ApplyIcon(_tabImages[ti], tabSprite, fallback);
                _tabImages[ti].color = selected ? Color.white : new Color(0.72f, 0.72f, 0.76f, 0.92f);
            }

            var defs = TreasureDefinitions.DefinitionsOrderedForRarity(_activeTab);
            var bg = RarityPalette.UpgradeCardBackground(_activeTab);
            var iconFallback = RarityPalette.UnitTint(_activeTab, UnitTeamKind.Ally);
            for (var i = 0; i < 10; i++)
            {
                var visible = i < defs.Length;
                if (_cardRoots[i] != null)
                    _cardRoots[i].SetActive(visible);
                if (!visible)
                    continue;

                var def = defs[i];
                _cardImages[i].color = bg;
                TreasureUiSprites.ApplyIcon(
                    _cardIconImages[i],
                    TreasureUiSprites.GetIconForDefinition(def),
                    iconFallback);
                var snap = MetaProgressionStore.GetTreasureSnapshot(def.Id);
                var levelLabel = snap.IsOwned
                    ? $"Lv {snap.Level}/{snap.MaxLevel}"
                    : "NOT OWNED";
                _cardTexts[i].text = $"<b>{def.Name}</b>\n<size=20><color=#D0DCE8>{levelLabel}</color></size>";
            }
        }

        void OpenDetail(int slotIndex)
        {
            var defs = TreasureDefinitions.DefinitionsOrderedForRarity(_activeTab);
            if (slotIndex < 0 || slotIndex >= defs.Length)
            {
                _resultText.text = "Invalid treasure card selection.";
                return;
            }

            MetaUiSelectionContext.SetSelectedTreasure(defs[slotIndex].Id, _activeTab);
            if (Application.CanStreamedLevelBeLoaded(detailSceneName))
                SceneManager.LoadScene(detailSceneName);
            else
            {
                _resultText.text = $"Cannot load scene: {detailSceneName}";
                Debug.LogWarning("[Treasure] Cannot load detail scene: " + detailSceneName);
            }
        }

        void BackToHomepage()
        {
            if (Application.CanStreamedLevelBeLoaded(homepageSceneName))
                SceneManager.LoadScene(homepageSceneName);
            else
            {
                _resultText.text = $"Cannot load scene: {homepageSceneName}";
                Debug.LogWarning("[Treasure] Cannot load homepage scene: " + homepageSceneName);
            }
        }

        Text CreateText(Transform parent, string name, string value, int fontSize, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = fontSize;
            t.text = value;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = MetaHudTheme.TextPrimary;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;
            return t;
        }

        Button CreateTabButton(
            Transform parent,
            string label,
            Vector2 pos,
            Sprite tabSprite,
            UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Tab");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            HudUiSprites.ApplyIcon(image, tabSprite, MetaHudTheme.ButtonIconBar);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 22;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            text.gameObject.SetActive(tabSprite == null);
            var lrt = text.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(188f, 72f);
            return btn;
        }

        void CreateTreasureCard(Transform parent, int slotIndex, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject($"TreasureCard_{slotIndex}");
            btnGo.transform.SetParent(parent, false);
            var bg = btnGo.AddComponent<Image>();
            bg.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            bg.color = RarityPalette.UpgradeCardBackground(Rarity.Common);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(340f, 100f);

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(btnGo.transform, false);
            var iconImg = iconGo.AddComponent<Image>();
            iconImg.raycastTarget = false;
            var iconRt = iconImg.rectTransform;
            iconRt.anchorMin = new Vector2(0f, 0.5f);
            iconRt.anchorMax = new Vector2(0f, 0.5f);
            iconRt.pivot = new Vector2(0f, 0.5f);
            iconRt.anchoredPosition = new Vector2(10f, 0f);
            iconRt.sizeDelta = new Vector2(72f, 72f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 19;
            text.text = string.Empty;
            text.alignment = TextAnchor.MiddleLeft;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            var lrt = text.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(88f, 6f);
            lrt.offsetMax = new Vector2(-8f, -6f);

            _cardRoots[slotIndex] = btnGo;
            _cardImages[slotIndex] = bg;
            _cardIconImages[slotIndex] = iconImg;
            _cardTexts[slotIndex] = text;
        }

        Button CreateButton(Transform parent, string label, Vector2 pos, Color color, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Button");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            image.color = color;
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = image;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(go.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 28;
            text.text = label;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
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
            rt.sizeDelta = new Vector2(480f, 90f);
            return btn;
        }
    }
}
