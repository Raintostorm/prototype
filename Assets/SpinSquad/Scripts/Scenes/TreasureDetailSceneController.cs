using SpinSquad.Data;
using SpinSquad.Meta;
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

            CreateText(canvasGo.transform, "Title", $"Treasure Detail", 40, new Vector2(0f, -120f), new Vector2(980f, 100f));
            if (_hasValidSelection)
            {
                var accentGo = new GameObject("RarityAccent");
                accentGo.transform.SetParent(canvasGo.transform, false);
                var accentImg = accentGo.AddComponent<Image>();
                accentImg.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
                accentImg.color = RarityPalette.UnitTint(_def.Rarity, UnitTeamKind.Ally);
                accentImg.raycastTarget = false;
                var accentRt = accentImg.rectTransform;
                accentRt.anchorMin = accentRt.anchorMax = new Vector2(0.5f, 1f);
                accentRt.pivot = new Vector2(0.5f, 1f);
                accentRt.anchoredPosition = new Vector2(0f, -168f);
                accentRt.sizeDelta = new Vector2(920f, 12f);
            }

            _detailText = CreateText(canvasGo.transform, "Detail", string.Empty, 26, new Vector2(0f, -470f), new Vector2(980f, 520f));
            _detailText.supportRichText = true;
            _detailText.alignment = TextAnchor.UpperLeft;
            _statusText = CreateText(canvasGo.transform, "Status", string.Empty, 24, new Vector2(0f, -900f), new Vector2(980f, 130f));
            CreateText(canvasGo.transform, "Hint", "Upgrade treasure level by rolling duplicates in Treasure scene.", 22, new Vector2(0f, -1020f), new Vector2(980f, 100f));
            CreateButton(canvasGo.transform, "Back", new Vector2(0f, -1180f), new Color(0.2f, 0.28f, 0.4f, 0.95f), BackToList);
        }

        void RefreshUi()
        {
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
