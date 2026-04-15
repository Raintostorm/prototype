using System;
using SpinSquad.Data;
using SpinSquad.Meta;
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
        Text _resourceText;
        Text _resultText;
        Rarity _activeTab = Rarity.Common;
        readonly Image[] _tabImages = new Image[4];
        readonly Button[] _tabButtons = new Button[4];
        readonly Image[] _cardImages = new Image[10];
        readonly Text[] _cardTexts = new Text[10];
        readonly System.Random _rng = new((int)(DateTime.UtcNow.Ticks ^ Guid.NewGuid().GetHashCode()));

        void Start()
        {
            EnsureEventSystem();
            ResolveFont();
            BuildUi();
            RefreshUi();
        }

        void OnEnable()
        {
            if (_resourceText != null)
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
            canvasGo.AddComponent<GraphicRaycaster>();

            CreateText(canvasGo.transform, "Title", "Treasure", 42, new Vector2(0f, -120f), new Vector2(900f, 100f));
            _resourceText = CreateText(canvasGo.transform, "Keys", string.Empty, 28, new Vector2(0f, -210f), new Vector2(900f, 80f));
            CreateButton(canvasGo.transform, "Roll (1 Key)", new Vector2(0f, -300f), new Color(0.48f, 0.34f, 0.17f, 0.96f), RollTreasure);

            _resultText = CreateText(canvasGo.transform, "Result", "Roll a treasure.", 24, new Vector2(0f, -400f), new Vector2(950f, 90f));

            var tabXs = new[] { -330f, -110f, 110f, 330f };
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
                var btn = CreateTabButton(canvasGo.transform, label, new Vector2(tabXs[ti], -498f), () => SelectTab(cap));
                _tabButtons[ti] = btn;
                _tabImages[ti] = btn.GetComponent<Image>();
                var trt = btn.GetComponent<RectTransform>();
                trt.sizeDelta = new Vector2(188f, 56f);
                var txt = btn.GetComponentInChildren<Text>();
                txt.fontSize = 22;
            }

            CreateText(
                canvasGo.transform,
                "TabHint",
                "Chọn bậc hiếm — mỗi bậc có 10 treasure. Bậc cao cho buff mạnh hơn.",
                20,
                new Vector2(0f, -568f),
                new Vector2(980f, 70f));

            for (var i = 0; i < 10; i++)
            {
                var slot = i;
                var col = i % 2;
                var row = i / 2;
                var x = col == 0 ? -230f : 230f;
                var y = -648f - row * 118f;
                CreateTreasureCard(canvasGo.transform, slot, new Vector2(x, y), () => OpenDetail(slot));
            }

            CreateButton(canvasGo.transform, "Back", new Vector2(0f, -1290f), new Color(0.2f, 0.28f, 0.4f, 0.95f), BackToHomepage);
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
            _resourceText.text = $"Treasure Keys: {MetaProgressionStore.TreasureKeys}";

            for (var ti = 0; ti < 4; ti++)
            {
                var selected = (int)_activeTab == ti;
                var tint = RarityPalette.UnitTint((Rarity)ti, UnitTeamKind.Ally);
                _tabImages[ti].color = selected
                    ? Color.Lerp(tint, Color.white, 0.22f)
                    : Color.Lerp(tint, Color.black, 0.38f);
            }

            var defs = TreasureDefinitions.DefinitionsOrderedForRarity(_activeTab);
            var bg = RarityPalette.UpgradeCardBackground(_activeTab);
            for (var i = 0; i < 10; i++)
            {
                _cardImages[i].color = bg;
                var def = defs[i];
                var snap = MetaProgressionStore.GetTreasureSnapshot(def.Id);
                var levelLabel = snap.IsOwned ? $"{snap.Level}/{snap.MaxLevel}" : $"0/{MetaProgressionStore.MaxUpgradeLevel}";
                _cardTexts[i].text =
                    $"<b>{def.Name}</b>\n" +
                    $"<color=#D8E6F4>{ShortEffect(def.EffectKind)}</color>  ·  Lv {levelLabel}\n" +
                    $"<size=18>{FormatTreasureStatus(snap)}</size>";
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

            MetaUiSelectionContext.SetSelectedTreasure(defs[slotIndex].Id);
            if (Application.CanStreamedLevelBeLoaded(detailSceneName))
                SceneManager.LoadScene(detailSceneName);
            else
            {
                _resultText.text = $"Cannot load scene: {detailSceneName}";
                Debug.LogWarning("[Treasure] Cannot load detail scene: " + detailSceneName);
            }
        }

        static string ShortEffect(TreasureEffectKind k) =>
            k switch
            {
                TreasureEffectKind.AllyHpPct => "HP%",
                TreasureEffectKind.AllyDmgPct => "ATK%",
                TreasureEffectKind.AllyAtkSpeedPct => "ASPD%",
                TreasureEffectKind.AllyCritChanceFlat => "Crit",
                TreasureEffectKind.StartRollCoinBonus => "Start coin",
                _ => "Wave coin"
            };

        static string FormatTreasureStatus(TreasureUpgradeSnapshot snap)
        {
            return snap.Status switch
            {
                TreasureUpgradeStatus.NotOwned => "Chưa sở hữu",
                TreasureUpgradeStatus.MaxLevel => "Max level",
                TreasureUpgradeStatus.OwnedCanLevelUp => "Có thể lên cấp",
                _ => $"Cần thêm {snap.MissingDuplicates} bản trùng"
            };
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

        Text CreateText(Transform parent, string name, string value, int size, Vector2 pos, Vector2 dim)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = _font;
            t.fontSize = size;
            t.text = value;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = dim;
            return t;
        }

        Button CreateTabButton(Transform parent, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject(label + "Tab");
            go.transform.SetParent(parent, false);
            var image = go.AddComponent<Image>();
            image.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            image.color = Color.gray;
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
            rt.sizeDelta = new Vector2(188f, 56f);
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
            rt.sizeDelta = new Vector2(340f, 108f);

            var textGo = new GameObject("Label");
            textGo.transform.SetParent(btnGo.transform, false);
            var text = textGo.AddComponent<Text>();
            text.font = _font;
            text.fontSize = 19;
            text.text = string.Empty;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.supportRichText = true;
            text.raycastTarget = false;
            var lrt = text.rectTransform;
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8f, 6f);
            lrt.offsetMax = new Vector2(-8f, -6f);

            _cardImages[slotIndex] = bg;
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
