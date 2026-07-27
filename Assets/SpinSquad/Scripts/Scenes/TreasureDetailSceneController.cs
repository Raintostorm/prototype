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
    public sealed class TreasureDetailSceneController : MonoBehaviour
    {
        [SerializeField] string listSceneName = "Treasure";
        Font _font;
        TreasureDefinition _def;
        bool _hasValidSelection;
        MetaResourceHudBar _resourceHud;
        Image _portraitImage;
        Image _typeBadgeImage;
        Text _detailText;
        Text _statusText;

        void Start()
        {
            _hasValidSelection = ResolveSelectedDefinition(out _def);
            EnsureEventSystem();
            ResolveFont();
            BuildUi();
            RefreshUi();
        }

        void OnEnable()
        {
            if (_detailText != null)
                RefreshUi();
        }

        static bool ResolveSelectedDefinition(out TreasureDefinition definition)
        {
            if (!MetaUiSelectionContext.TryConsumeSelectedTreasure(out var id) || string.IsNullOrWhiteSpace(id))
            {
                definition = default;
                return false;
            }

            definition = TreasureDefinitions.GetById(id);
            return definition.IsValid;
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
            var canvasGo = new GameObject("TreasureDetailUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();

            MetaHudTheme.AddFullScreenBackdrop(canvasGo.transform, GameBackgroundUiSprites.Treasure, 0);
            MetaHudTheme.AddHeaderStrip(canvasGo.transform);

            CreateText(canvasGo.transform, "Title", "Treasure Detail", 40, new Vector2(0f, -MetaHudTheme.SafeEdgeYTop - 8f), new Vector2(980f, 72f));
            _resourceHud = new MetaResourceHudBar();
            _resourceHud.Build(
                canvasGo.transform,
                _font,
                new Vector2(MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop));
            MetaDebugGrantPanel.TryAttach(canvasGo.transform, _font, RefreshUi);

            var tableGo = new GameObject("TreasureDetailTable");
            tableGo.transform.SetParent(canvasGo.transform, false);
            var tableRt = tableGo.AddComponent<RectTransform>();
            tableRt.anchorMin = tableRt.anchorMax = new Vector2(0.5f, 1f);
            tableRt.pivot = new Vector2(0.5f, 1f);
            tableRt.anchoredPosition = new Vector2(0f, -200f);
            tableRt.sizeDelta = new Vector2(1000f, 1180f);
            var tableImg = tableGo.AddComponent<Image>();
            TreasureUiSprites.ApplyIcon(tableImg, TreasureUiSprites.TableBackground, new Color(0.1f, 0.12f, 0.18f, 0.92f));
            tableImg.raycastTarget = false;

            if (_hasValidSelection)
            {
                var portraitGo = new GameObject("Portrait");
                portraitGo.transform.SetParent(tableGo.transform, false);
                _portraitImage = portraitGo.AddComponent<Image>();
                _portraitImage.raycastTarget = false;
                var portraitRt = _portraitImage.rectTransform;
                portraitRt.anchorMin = portraitRt.anchorMax = new Vector2(0.5f, 1f);
                portraitRt.pivot = new Vector2(0.5f, 1f);
                portraitRt.anchoredPosition = new Vector2(0f, -72f);
                portraitRt.sizeDelta = new Vector2(240f, 240f);
                TreasureUiSprites.ApplyIcon(
                    _portraitImage,
                    TreasureUiSprites.GetIconForDefinition(_def),
                    RarityPalette.UnitTint(_def.Rarity, UnitTeamKind.Ally));

                var typeGo = new GameObject("EffectTypeBadge");
                typeGo.transform.SetParent(portraitGo.transform, false);
                _typeBadgeImage = typeGo.AddComponent<Image>();
                _typeBadgeImage.raycastTarget = false;
                var typeRt = _typeBadgeImage.rectTransform;
                typeRt.anchorMin = typeRt.anchorMax = new Vector2(1f, 0f);
                typeRt.pivot = new Vector2(1f, 0f);
                typeRt.anchoredPosition = new Vector2(10f, -10f);
                typeRt.sizeDelta = new Vector2(64f, 64f);
                TreasureUiSprites.ApplyIcon(
                    _typeBadgeImage,
                    TreasureUiSprites.EffectTypeIcon(_def.EffectKind),
                    new Color(0.35f, 0.42f, 0.55f, 0.95f));
            }

            _detailText = CreateText(tableGo.transform, "Detail", string.Empty, 26, new Vector2(0f, -340f), new Vector2(900f, 560f));
            _detailText.supportRichText = true;
            _detailText.alignment = TextAnchor.UpperLeft;
            _statusText = CreateText(tableGo.transform, "Status", string.Empty, 24, new Vector2(0f, -920f), new Vector2(900f, 120f));
            CreateText(
                tableGo.transform,
                "Hint",
                "Lên cấp bằng cách roll trùng treasure ở màn Treasure.",
                22,
                new Vector2(0f, -1040f),
                new Vector2(900f, 80f));
            MetaHudTheme.CreateFooterBackButton(canvasGo.transform, _font, BackToList);
        }

        void RefreshUi()
        {
            _resourceHud?.Refresh();
            if (_hasValidSelection && _portraitImage != null)
            {
                TreasureUiSprites.ApplyIcon(
                    _portraitImage,
                    TreasureUiSprites.GetIconForDefinition(_def),
                    RarityPalette.UnitTint(_def.Rarity, UnitTeamKind.Ally));
                if (_typeBadgeImage != null)
                    TreasureUiSprites.ApplyIcon(
                        _typeBadgeImage,
                        TreasureUiSprites.EffectTypeIcon(_def.EffectKind),
                        new Color(0.35f, 0.42f, 0.55f, 0.95f));
            }

            if (!_hasValidSelection)
            {
                _detailText.text = "Invalid selection context.\nOpen this page from the Treasure card list.";
                _statusText.text = "Status: InvalidSelection";
                return;
            }

            var snap = MetaProgressionStore.GetTreasureSnapshot(_def.Id);
            var currentLevel = snap.IsOwned ? Mathf.Max(1, snap.Level) : 1;
            var currentValue = TreasureValueAtLevel(_def, currentLevel);
            var nextLevel = Mathf.Min(MetaProgressionStore.MaxUpgradeLevel, currentLevel + 1);
            var nextValue = TreasureValueAtLevel(_def, nextLevel);

            _detailText.text =
                $"<size=30><b>{_def.Name}</b></size>\n" +
                $"<color=#C5D8EC>Bậc {_def.Rarity}  ·  {FormatEffectName(_def.EffectKind)}</color>\n\n" +
                $"<color=#B0C8DC><b>Gợi ý</b></color> {FormatStrategyHint(_def.EffectKind)}\n\n" +
                $"Level: {(snap.IsOwned ? $"{snap.Level}/{snap.MaxLevel}" : "Not owned")}\n" +
                $"<color=#FFFFFF>Hiện tại: {FormatEffectValue(_def.EffectKind, currentValue)}</color>\n" +
                $"<color=#7CE89A>Cấp kế: {FormatEffectValue(_def.EffectKind, nextValue)}</color>\n";

            _statusText.text = snap.Status switch
            {
                TreasureUpgradeStatus.NotOwned => "Status: NotOwned",
                TreasureUpgradeStatus.MaxLevel => "Status: MaxLevel",
                TreasureUpgradeStatus.OwnedCanLevelUp =>
                    $"Status: CanLevelUp ({snap.DuplicateShards}/{snap.RequiredDuplicates} duplicates ready)",
                _ => $"Status: NeedDuplicates ({snap.MissingDuplicates} more)"
            };
        }

        static float TreasureValueAtLevel(TreasureDefinition def, int level)
        {
            level = Mathf.Clamp(level, 1, MetaProgressionStore.MaxUpgradeLevel);
            var levelScale = 1f + (level - 1) * 0.4f;
            return def.BaseValue * levelScale;
        }

        static string FormatEffectName(TreasureEffectKind effectKind)
        {
            return effectKind switch
            {
                TreasureEffectKind.AllyHpPct => "Ally HP%",
                TreasureEffectKind.AllyDmgPct => "Ally ATK%",
                TreasureEffectKind.AllyAtkSpeedPct => "Ally ASPD%",
                TreasureEffectKind.AllyCritChanceFlat => "Ally Crit%",
                TreasureEffectKind.StartRollCoinBonus => "Start Roll Coins",
                _ => "Wave Roll Coins"
            };
        }

        static string FormatStrategyHint(TreasureEffectKind effectKind)
        {
            return effectKind switch
            {
                TreasureEffectKind.AllyHpPct => "Ổn định frontline, wave dài.",
                TreasureEffectKind.AllyDmgPct => "Tăng DPS tổng, hợp burst / clear nhanh.",
                TreasureEffectKind.AllyAtkSpeedPct => "Tối ưu đơn vị ranged / DPS theo thời gian.",
                TreasureEffectKind.AllyCritChanceFlat => "May rủi cao — nhân với buff crit trong trận.",
                TreasureEffectKind.StartRollCoinBonus => "Mở đầu trận dư coin roll — linh hoạt đội hình sớm.",
                _ => "Sau mỗi wave thắng — dài hơi, economy roll."
            };
        }

        static string FormatEffectValue(TreasureEffectKind effectKind, float value)
        {
            return effectKind switch
            {
                TreasureEffectKind.StartRollCoinBonus => $"+{Mathf.RoundToInt(value)}",
                TreasureEffectKind.WaveRollCoinBonus => $"+{Mathf.RoundToInt(value)}",
                _ => $"+{value * 100f:0.##}%"
            };
        }

        void BackToList()
        {
            if (Application.CanStreamedLevelBeLoaded(listSceneName))
                SceneManager.LoadScene(listSceneName);
            else
            {
                _statusText.text = $"Cannot load scene: {listSceneName}";
                Debug.LogWarning("[TreasureDetail] Cannot load list scene: " + listSceneName);
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
