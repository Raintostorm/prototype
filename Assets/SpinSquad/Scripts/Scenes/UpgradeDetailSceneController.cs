using System.Text;
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
    public sealed class UpgradeDetailSceneController : MonoBehaviour
    {
        [SerializeField] string listSceneName = "Upgrade";
        Font _font;
        UnitCatalog _unitCatalog;
        int _lineIndex;
        Rarity _rarityTier;
        bool _hasValidSelection;
        MetaResourceHudBar _resourceHud;
        Text _detailText;
        Text _statusText;
        Button _upgradeButton;
        Image _portraitImage;
        AllyUiAnimatorBridge _portraitAnimatorBridge;
        bool _playedRevealOnce;

        void Start()
        {
            _hasValidSelection =
                MetaUiSelectionContext.TryConsumeSelectedUpgrade(out var selectedLine, out var selectedRarity) &&
                selectedLine >= 0 && selectedLine < MetaProgressionStore.AllyLineCount;
            _lineIndex = _hasValidSelection ? selectedLine : 0;
            _rarityTier = _hasValidSelection ? selectedRarity : Rarity.Common;
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
            var canvasGo = new GameObject("UpgradeDetailUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 90;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();

            var accentGo = new GameObject("RarityAccent");
            accentGo.transform.SetParent(canvasGo.transform, false);
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
            accentImg.color = RarityPalette.UnitTint(_rarityTier, UnitTeamKind.Ally);
            accentImg.raycastTarget = false;
            var accentRt = accentImg.rectTransform;
            accentRt.anchorMin = accentRt.anchorMax = new Vector2(0.5f, 1f);
            accentRt.pivot = new Vector2(0.5f, 1f);
            accentRt.anchoredPosition = new Vector2(0f, -168f);
            accentRt.sizeDelta = new Vector2(920f, 12f);

            CreateText(canvasGo.transform, "Title", $"Upgrade Detail - L{_lineIndex} {_rarityTier}", 36, new Vector2(0f, -120f), new Vector2(980f, 100f));
            _resourceHud = new MetaResourceHudBar();
            _resourceHud.Build(
                canvasGo.transform,
                _font,
                new Vector2(MetaHudTheme.SafeEdgeX, -MetaHudTheme.SafeEdgeYTop));
            MetaDebugGrantPanel.TryAttach(canvasGo.transform, _font, RefreshUi);

            var portraitGo = new GameObject("DetailPortrait");
            portraitGo.transform.SetParent(canvasGo.transform, false);
            _portraitImage = portraitGo.AddComponent<Image>();
            _portraitImage.raycastTarget = false;
            _portraitImage.preserveAspect = true;
            portraitGo.AddComponent<Animator>();
            _portraitAnimatorBridge = portraitGo.AddComponent<AllyUiAnimatorBridge>();
            var portRt = _portraitImage.rectTransform;
            portRt.anchorMin = portRt.anchorMax = new Vector2(0.5f, 1f);
            portRt.pivot = new Vector2(0.5f, 1f);
            portRt.anchoredPosition = new Vector2(0f, -258f);
            portRt.sizeDelta = new Vector2(128f, 128f);
            _portraitImage.sprite = UnitSpriteFactory.GetSprite(UnitBodyShape.Square);
            _portraitImage.color = RarityPalette.UnitTint(_rarityTier, UnitTeamKind.Ally);

            _detailText = CreateText(canvasGo.transform, "Detail", string.Empty, 22, new Vector2(0f, -418f), new Vector2(1020f, 600f));
            _detailText.alignment = TextAnchor.UpperLeft;
            _detailText.supportRichText = true;
            _detailText.lineSpacing = 1.08f;
            _statusText = CreateText(canvasGo.transform, "Status", string.Empty, 24, new Vector2(0f, -880f), new Vector2(980f, 120f));
            _statusText.alignment = TextAnchor.MiddleCenter;

            _upgradeButton = CreateButton(
                canvasGo.transform,
                "Upgrade",
                new Vector2(0f, -1050f),
                new Color(0.2f, 0.58f, 0.34f, 0.95f),
                OnUpgradeClicked);
            var upgradeImg = _upgradeButton.GetComponent<Image>();
            HudUiSprites.ApplyIcon(upgradeImg, HudUiSprites.MetaGreenButton, new Color(0.2f, 0.58f, 0.34f, 0.95f));
            HudUiButtonHelper.SyncLabelVisibility(_upgradeButton.GetComponentInChildren<Text>(), HudUiSprites.MetaGreenButton, "Upgrade");
            MetaHudTheme.CreateFooterBackButton(canvasGo.transform, _font, BackToList);
        }

        void OnUpgradeClicked()
        {
            if (!_hasValidSelection)
                return;
            var upgraded = MetaProgressionStore.TryUpgradeLine(_lineIndex, _rarityTier);
            if (upgraded)
                _portraitAnimatorBridge?.PlayUpgrade();
            RefreshUi();
        }

        void RefreshUi()
        {
            _resourceHud?.Refresh();
            if (!_hasValidSelection)
            {
                _detailText.text = "Invalid selection context.\nOpen this page from the Upgrade card list.";
                _statusText.text = "Status: InvalidSelection";
                _upgradeButton.interactable = false;
                _playedRevealOnce = false;
                if (_portraitImage != null)
                    _portraitImage.gameObject.SetActive(false);
                return;
            }

            if (_portraitImage != null)
                _portraitImage.gameObject.SetActive(true);

            var snap = MetaProgressionStore.GetLineUpgradeSnapshot(_lineIndex, _rarityTier);
            var id = AllyLineCatalog.UnitIdForLine(_lineIndex, _rarityTier);
            var allyName = id;
            var allyShape = "Unknown";
            UnitDefinition def = null;
            if (_unitCatalog != null && _unitCatalog.TryGet(id, out var foundDef))
            {
                def = foundDef;
                if (!string.IsNullOrWhiteSpace(def.DisplayName))
                    allyName = def.DisplayName;
                allyShape = def.BodyShape.ToString();
            }

            var (baseHp, baseAtk) = AllyStatScaling.ScaleStats(def, _rarityTier);
            var baseInterval = def != null ? def.RangedShotIntervalSeconds : 0.52f;
            var baseAps = Mathf.Max(0.01f, 1f / Mathf.Max(0.01f, baseInterval));
            var totalHp = baseHp * (1f + snap.TotalHpPct);
            var totalAtk = baseAtk * (1f + snap.TotalDmgPct);
            var totalAps = baseAps * (1f + snap.TotalAtkSpeedPct);
            var nextTotalHp = baseHp * (1f + snap.NextTotalHpPct);
            var nextTotalAtk = baseAtk * (1f + snap.NextTotalDmgPct);
            var nextTotalAps = baseAps * (1f + snap.NextTotalAtkSpeedPct);

            if (_portraitImage != null)
            {
                var shape = def != null ? def.BodyShape : UnitBodyShape.Square;
                var resolvedPortrait = (Sprite)null;
                if (def != null)
                {
                    var allowCustomVisualAtThisTier = _rarityTier >= def.Rarity;
                    if (allowCustomVisualAtThisTier)
                        resolvedPortrait = def.PortraitSprite != null ? def.PortraitSprite : def.CardSprite;
                }
                if (resolvedPortrait != null)
                {
                    _portraitImage.sprite = resolvedPortrait;
                    _portraitImage.color = Color.white;
                }
                else
                {
                    _portraitImage.sprite = UnitSpriteFactory.GetSprite(shape);
                    _portraitImage.color = RarityPalette.UnitTint(_rarityTier, UnitTeamKind.Ally);
                }
            }

            _portraitAnimatorBridge?.ConfigureForUnit(def, _rarityTier);
            if (!_playedRevealOnce)
            {
                _portraitAnimatorBridge?.PlayReveal();
                _playedRevealOnce = true;
            }
            else
            {
                _portraitAnimatorBridge?.PlayIdle();
            }

            _detailText.text = BuildDetailLayout(
                allyName,
                id,
                allyShape,
                snap,
                totalHp,
                totalAtk,
                totalAps,
                nextTotalHp,
                nextTotalAtk,
                nextTotalAps);
            _statusText.text = snap.Status switch
            {
                LineUpgradeStatus.MaxLevel => $"Status: MaxLevel (Lv{MetaProgressionStore.MaxUpgradeLevel})",
                LineUpgradeStatus.NeedGold => $"Status: NeedGold ({snap.MissingGold} more)",
                _ => $"Status: CanUpgrade (Cost {snap.Cost} Gold)"
            };
            _upgradeButton.interactable = snap.Status == LineUpgradeStatus.CanUpgrade;
        }

        const string DeltaGreen = "#5EE87A";
        const string MutedCap = "#8A9AAA";

        string BuildDetailLayout(
            string allyName,
            string unitId,
            string allyShape,
            LineUpgradeSnapshot snap,
            float totalHp,
            float totalAtk,
            float totalAps,
            float nextHp,
            float nextAtk,
            float nextAps)
        {
            var sb = new StringBuilder(900);
            sb.AppendLine($"<size=26><color=#FFFFFF><b>{allyName}</b></color></size>");
            sb.AppendLine($"<color=#C5D8EC><b>{unitId}</b>   ·   {allyShape}   ·   L{_lineIndex}   ·   {_rarityTier}</color>");
            sb.AppendLine($"Level  <color=#FFF0C0><b>{snap.Level}</b></color> / {snap.MaxLevel}\n");

            sb.AppendLine("<color=#B0C4D8>Current (meta)          After next level</color>");
            sb.AppendLine("<color=#6E8AAA>────────────────────────────────────────────</color>");

            var dHp = nextHp - totalHp;
            var dAtk = nextAtk - totalAtk;
            var dAps = nextAps - totalAps;
            var atCap = snap.Status == LineUpgradeStatus.MaxLevel;

            sb.AppendLine(
                $"<color=#B8E0FF><b>HP</b></color>     " +
                $"<color=#FFFFFF><size=22><b>{totalHp:0.#}</b></size></color>          " +
                $"{FormatLevelDelta(dHp, "HP", atCap)}");
            sb.AppendLine(
                $"<color=#FFD6A8><b>ATK</b></color>    " +
                $"<color=#FFFFFF><size=22><b>{totalAtk:0.#}</b></size></color>          " +
                $"{FormatLevelDelta(dAtk, "ATK", atCap)}");
            sb.AppendLine(
                $"<color=#D4C8FF><b>ASPD</b></color>   " +
                $"<color=#FFFFFF><size=22><b>{totalAps:0.###}/s</b></size></color>      " +
                $"{FormatLevelDelta(dAps, "/s", atCap, isRate: true)}");

            return sb.ToString();
        }

        static string FormatLevelDelta(float delta, string unitSuffix, bool atCap, bool isRate = false)
        {
            if (atCap)
                return $"<color={MutedCap}>—</color>";

            if (isRate)
            {
                var sign = delta >= 0f ? "+" : string.Empty;
                return $"<color={DeltaGreen}><b>{sign}{delta:0.###}{unitSuffix}</b></color>";
            }

            var s = delta >= 0f ? "+" : string.Empty;
            return $"<color={DeltaGreen}><b>{s}{delta:0.#} {unitSuffix}</b></color>";
        }

        void BackToList()
        {
            if (Application.CanStreamedLevelBeLoaded(listSceneName))
                SceneManager.LoadScene(listSceneName);
            else
            {
                _statusText.text = $"Cannot load scene: {listSceneName}";
                Debug.LogWarning("[UpgradeDetail] Cannot load list scene: " + listSceneName);
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
