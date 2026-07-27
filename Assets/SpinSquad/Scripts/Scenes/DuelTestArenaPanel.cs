using SpinSquad.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpinSquad.Scenes
{
    /// <summary>
    /// Scene sandbox: preset ally/enemy + reset; thêm/xóa từng loại (melee/ranged × ally/enemy) khi prep.
    /// </summary>
    public sealed class DuelTestArenaPanel : MonoBehaviour
    {
        const string AllyMeleeId = "unit_slip_slinger";
        const string AllyRangedId = "unit_ally_ranged";
        /// <summary>Same Spine knight as ally <c>common_melee</c>; enemy faction for facing/HP tests.</summary>
        const string EnemyMeleeId = "unit_enemy_knight";
        const string EnemyRangedId = "unit_enemy_ranged";

        [SerializeField] private string hubSceneName = "Homepage";
        [SerializeField] private string panelTitle = "Test combat";
        [SerializeField] private string panelHint = "Preset: chọn loại mặc định rồi «Áp dụng & reset trận». Sandbox: thêm/xóa từng loại (chỉ khi chưa bấm Bắt đầu).";

        DuelDirector _director;
        string _allyId = AllyMeleeId;
        string _enemyId = EnemyMeleeId;

        Image _allyMeleeBg;
        Image _allyRangedBg;
        Image _enemyMeleeBg;
        Image _enemyRangedBg;

        void Start()
        {
            EnsureEventSystem();
            _director = FindAnyObjectByType<DuelDirector>();
            if (_director != null)
            {
                _allyId = _director.CurrentAllyUnitId;
                _enemyId = _director.CurrentEnemyUnitId;
            }

            BuildUi();
            RefreshSelectionColors();
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;

            var go = new GameObject("EventSystem");
            go.AddComponent<EventSystem>();
            go.AddComponent<InputSystemUIInputModule>();
        }

        void BuildUi()
        {
            var canvasGo = new GameObject("TestArenaUI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 80;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            canvasGo.AddComponent<GraphicRaycaster>();

            var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");

            CreateText(canvasGo.transform, font, panelTitle, 36, new Vector2(0f, -56f), new Vector2(960f, 56f)).color = Color.white;

            CreateText(canvasGo.transform, font, panelHint, 20,
                new Vector2(0f, -108f), new Vector2(1000f, 96f)).color = new Color(0.82f, 0.88f, 1f, 1f);

            CreateText(canvasGo.transform, font, "Preset — Ally", 24, new Vector2(0f, -200f), new Vector2(900f, 36f)).color = new Color(0.7f, 0.92f, 1f, 1f);
            _allyMeleeBg = CreateChoiceButton(canvasGo.transform, font, "Ally cận", new Vector2(-230f, -262f),
                () => SetAlly(AllyMeleeId));
            _allyRangedBg = CreateChoiceButton(canvasGo.transform, font, "Ally xa", new Vector2(230f, -262f),
                () => SetAlly(AllyRangedId));

            CreateText(canvasGo.transform, font, "Preset — Enemy", 24, new Vector2(0f, -322f), new Vector2(900f, 36f)).color = new Color(1f, 0.72f, 0.68f, 1f);
            _enemyMeleeBg = CreateChoiceButton(canvasGo.transform, font, "Enemy cận", new Vector2(-230f, -384f),
                () => SetEnemy(EnemyMeleeId));
            _enemyRangedBg = CreateChoiceButton(canvasGo.transform, font, "Enemy xa", new Vector2(230f, -384f),
                () => SetEnemy(EnemyRangedId));

            CreateSolidButton(canvasGo.transform, font, "Áp dụng & reset trận", new Vector2(0f, -478f),
                new Color(0.18f, 0.5f, 0.36f, 0.98f), ApplyAndRestart);

            CreateText(canvasGo.transform, font, "Thêm (ô trống ngẫu nhiên)", 22, new Vector2(0f, -548f), new Vector2(920f, 32f)).color = new Color(0.9f, 0.95f, 1f, 1f);
            const float addY = -598f;
            CreateCompactButton(canvasGo.transform, font, "+ Ally cận", new Vector2(-324f, addY), () => SandboxAddAlly(AllyMeleeId));
            CreateCompactButton(canvasGo.transform, font, "+ Ally xa", new Vector2(-108f, addY), () => SandboxAddAlly(AllyRangedId));
            CreateCompactButton(canvasGo.transform, font, "+ Enemy cận", new Vector2(108f, addY), () => SandboxAddEnemy(EnemyMeleeId));
            CreateCompactButton(canvasGo.transform, font, "+ Enemy xa", new Vector2(324f, addY), () => SandboxAddEnemy(EnemyRangedId));

            CreateText(canvasGo.transform, font, "Xóa (ngẫu nhiên đúng loại)", 22, new Vector2(0f, -658f), new Vector2(920f, 32f)).color = new Color(0.95f, 0.88f, 0.88f, 1f);
            const float rmY = -708f;
            var rmCol = new Color(0.52f, 0.22f, 0.22f, 0.92f);
            CreateCompactButton(canvasGo.transform, font, "− Ally cận", new Vector2(-324f, rmY), () => SandboxRemove(false, false), rmCol);
            CreateCompactButton(canvasGo.transform, font, "− Ally xa", new Vector2(-108f, rmY), () => SandboxRemove(false, true), rmCol);
            CreateCompactButton(canvasGo.transform, font, "− Enemy cận", new Vector2(108f, rmY), () => SandboxRemove(true, false), rmCol);
            CreateCompactButton(canvasGo.transform, font, "− Enemy xa", new Vector2(324f, rmY), () => SandboxRemove(true, true), rmCol);

            CreateSolidButton(canvasGo.transform, font, "Về Homepage", new Vector2(0f, -818f),
                new Color(0.22f, 0.28f, 0.42f, 0.96f), LoadHub);
        }

        void SandboxAddAlly(string catalogId)
        {
            if (_director == null)
            {
                Debug.LogWarning("[DuelTestArena] Không tìm thấy DuelDirector.");
                return;
            }

            if (!_director.SandboxTryAddAlly(catalogId))
                Debug.Log("[DuelTestArena] Không thêm ally (đang combat / thua, hết ô, hoặc id không phải ally).");
        }

        void SandboxAddEnemy(string catalogId)
        {
            if (_director == null)
            {
                Debug.LogWarning("[DuelTestArena] Không tìm thấy DuelDirector.");
                return;
            }

            if (!_director.SandboxTryAddEnemy(catalogId))
                Debug.Log("[DuelTestArena] Không thêm enemy (đang combat / thua, hết ô, hoặc id không phải enemy).");
        }

        void SandboxRemove(bool isEnemy, bool ranged)
        {
            if (_director == null)
            {
                Debug.LogWarning("[DuelTestArena] Không tìm thấy DuelDirector.");
                return;
            }

            if (!_director.SandboxTryRemoveOne(isEnemy, ranged))
                Debug.Log("[DuelTestArena] Không xóa được (đang combat, hoặc không còn đơn vị đúng loại).");
        }

        void SetAlly(string id)
        {
            _allyId = id;
            RefreshSelectionColors();
        }

        void SetEnemy(string id)
        {
            _enemyId = id;
            RefreshSelectionColors();
        }

        void RefreshSelectionColors()
        {
            SetPair(_allyMeleeBg, _allyRangedBg, _allyId == AllyMeleeId);
            SetPair(_enemyMeleeBg, _enemyRangedBg, _enemyId == EnemyMeleeId);
        }

        static void SetPair(Image melee, Image ranged, bool meleeSelected)
        {
            if (melee != null)
                melee.color = meleeSelected ? new Color(0.22f, 0.55f, 0.42f, 0.98f) : new Color(0.18f, 0.2f, 0.24f, 0.55f);
            if (ranged != null)
                ranged.color = !meleeSelected ? new Color(0.22f, 0.48f, 0.62f, 0.98f) : new Color(0.18f, 0.2f, 0.24f, 0.55f);
        }

        void ApplyAndRestart()
        {
            if (_director == null)
            {
                Debug.LogWarning("[DuelTestArena] Không tìm thấy DuelDirector.");
                return;
            }

            _director.ApplyTestLoadout(_allyId, _enemyId);
        }

        void LoadHub()
        {
            if (!Application.CanStreamedLevelBeLoaded(hubSceneName))
            {
                Debug.LogWarning("[DuelTestArena] Không load được scene: " + hubSceneName);
                return;
            }

            SceneManager.LoadScene(hubSceneName);
        }

        static Text CreateText(Transform parent, Font font, string msg, int size, Vector2 pos, Vector2 sizeDelta)
        {
            var go = new GameObject("Text");
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.text = msg;
            t.fontSize = size;
            t.alignment = TextAnchor.MiddleCenter;
            t.color = Color.white;
            t.raycastTarget = false;
            var rt = t.rectTransform;
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            return t;
        }

        Image CreateChoiceButton(Transform parent, Font font, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(label);
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = new Color(0.2f, 0.22f, 0.28f, 0.75f);
            img.raycastTarget = true;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 26;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 1f);
            btnRt.pivot = new Vector2(0.5f, 1f);
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = new Vector2(400f, 72f);
            return img;
        }

        static void CreateCompactButton(Transform parent, Font font, string label, Vector2 pos,
            UnityEngine.Events.UnityAction onClick, Color? bg = null)
        {
            var btnGo = new GameObject(label);
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = bg ?? new Color(0.24f, 0.32f, 0.42f, 0.92f);
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 22;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 1f);
            btnRt.pivot = new Vector2(0.5f, 1f);
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = new Vector2(196f, 56f);
        }

        static void CreateSolidButton(Transform parent, Font font, string label, Vector2 pos, Color bg,
            UnityEngine.Events.UnityAction onClick)
        {
            var btnGo = new GameObject(label);
            btnGo.transform.SetParent(parent, false);
            var img = btnGo.AddComponent<Image>();
            img.sprite = WhiteSprite();
            img.color = bg;
            var btn = btnGo.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(btnGo.transform, false);
            var text = labelGo.AddComponent<Text>();
            text.font = font;
            text.text = label;
            text.fontSize = 30;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            var labelRt = text.rectTransform;
            labelRt.anchorMin = Vector2.zero;
            labelRt.anchorMax = Vector2.one;
            labelRt.offsetMin = Vector2.zero;
            labelRt.offsetMax = Vector2.zero;

            var btnRt = btnGo.GetComponent<RectTransform>();
            btnRt.anchorMin = btnRt.anchorMax = new Vector2(0.5f, 1f);
            btnRt.pivot = new Vector2(0.5f, 1f);
            btnRt.anchoredPosition = pos;
            btnRt.sizeDelta = new Vector2(520f, 84f);
        }

        static Sprite WhiteSprite()
        {
            var tex = Texture2D.whiteTexture;
            return Sprite.Create(tex, new Rect(0f, 0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
        }
    }
}
