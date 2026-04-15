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
        const float CardRowSpacing = 182f;
        const float CardTopY = -332f;
        static readonly Vector2 CardSize = new Vector2(300f, 170f);

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
            canvasGo.AddComponent<GraphicRaycaster>();

            CreateText(canvasGo.transform, "Title", "Upgrade", 42, new Vector2(0f, -120f), new Vector2(900f, 100f));
            _goldText = CreateText(canvasGo.transform, "Gold", string.Empty, 28, new Vector2(0f, -210f), new Vector2(900f, 80f));

            for (var line = 0; line < MetaProgressionStore.AllyLineCount; line++)
            {
                for (var step = 0; step < MetaProgressionStore.UpgradableRarityCount; step++)
                {
                    var rarity = (Rarity)step;
                    var idx = line * MetaProgressionStore.UpgradableRarityCount + step;
                    var lineCapture = line;
                    var rarityCapture = rarity;
                    var x = -320f + line * 320f;
                    var y = CardTopY - step * CardRowSpacing;
                    CreateUpgradeCard(
                        canvasGo.transform,
                        idx,
                        new Vector2(x, y),
                        rarity,
                        () => OpenDetail(lineCapture, rarityCapture));
                }
            }

            _hintText = CreateText(canvasGo.transform, "Hint", "Tap a card for stats and upgrade.", 22, new Vector2(0f, -1105f), new Vector2(980f, 90f));
            CreateButton(canvasGo.transform, "Back", new Vector2(0f, -1245f), new Color(0.2f, 0.28f, 0.4f, 0.95f), BackToHomepage);
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

        void CreateUpgradeCard(Transform parent, int idx, Vector2 pos, Rarity rarity, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject($"UpgradeCard_{idx}");
            btnGo.transform.SetParent(parent, false);
            var bg = btnGo.AddComponent<Image>();
            bg.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            bg.color = RarityPalette.UpgradeCardBackground(rarity);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = bg;
            btn.onClick.AddListener(onClick);
            var rt = btnGo.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = CardSize;

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
            nameText.fontSize = 21;
            nameText.alignment = TextAnchor.MiddleCenter;
            nameText.color = Color.white;
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
