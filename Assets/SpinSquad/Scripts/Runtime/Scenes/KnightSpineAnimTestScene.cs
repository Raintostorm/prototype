using Spine;
using Spine.Unity;
using SpinSquad.Core;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace SpinSquad.Scenes
{
    /// <summary>Màn Editor-only: spawn Knight Spine prefab và bấm thử idle / walk / attack / died.</summary>
    public sealed class KnightSpineAnimTestScene : MonoBehaviour
    {
        const string IdleName = "idle";
        const string WalkName = "walk";
        const string AttackName = "attack";
        const string DiedName = "died";

        [SerializeField] string resourcesPrefabPath = "Battle/KnightBattleVisual";
        [SerializeField] string homepageSceneName = "Homepage";
        [Tooltip("Skeleton export lớn; scale world để fit camera orthographic ~5.")]
        [SerializeField] float worldScale = 1f;
        [SerializeField] Vector3 spawnPosition = new(0f, -2.2f, 0f);
        [Tooltip("Tâm mong muốn của nhân vật sau khi canh theo Renderer.bounds (giúp hết lệch góc do pivot/root).")]
        [SerializeField] Vector3 desiredWorldCenter = new(0f, -2.2f, 0f);
        [SerializeField] Color backdropColor = new(0.12f, 0.14f, 0.18f, 1f);

        SkeletonAnimation _skel;
        Spine.AnimationState _state;
        Text _statusText;
        TrackEntry _pendingComplete;

        void Start()
        {
            EnsureEventSystem();
            BuildBackdrop();
            var prefab = Resources.Load<GameObject>(resourcesPrefabPath);
            if (prefab == null)
            {
                Debug.LogError($"[KnightSpineAnimTest] Không Load được Resources.Load(\"{resourcesPrefabPath}\"). Kiểm tra prefab và tên Resources.");
                return;
            }

            var go = Instantiate(prefab, spawnPosition, Quaternion.identity);
            go.name = resourcesPrefabPath + " (clone)";
            go.transform.localScale = Vector3.one * worldScale;

            // Scene test tự điều khiển animation bằng button; tắt battle driver để tránh bị ép về idle.
            var battleDriver = go.GetComponent<SpineBattleAnimator>();
            if (battleDriver != null)
                battleDriver.enabled = false;

            _skel = go.GetComponent<SkeletonAnimation>();
            if (_skel == null)
                _skel = go.GetComponentInChildren<SkeletonAnimation>(true);
            if (_skel == null)
            {
                Debug.LogError("[KnightSpineAnimTest] Prefab không có SkeletonAnimation.", go);
                return;
            }

            BuildUi();
            _skel.Initialize(true);
            _state = _skel.AnimationState;
            AlignCharacterToDesiredCenter();
            if (!ValidateClips())
            {
                SetStatus("Loi SkeletonData/animation. Mo Console de xem chi tiet import Spine.");
                return;
            }

            ClearPendingHandler();
            SetLooping(IdleName);
            SetStatus("Da load Knight. Bam Idle/Walk/attack/died de test.");
        }

        void OnDestroy()
        {
            ClearPendingHandler();
        }

        bool ValidateClips()
        {
            if (_skel == null || _skel.Skeleton == null || _skel.Skeleton.Data == null)
            {
                Debug.LogError("[KnightSpineAnimTest] SkeletonData null. Kiểm tra console Spine import/runtime.");
                return false;
            }

            var data = _skel.Skeleton.Data;
            var ok = true;
            foreach (var n in new[] { IdleName, WalkName, AttackName, DiedName })
            {
                if (data.FindAnimation(n) == null)
                {
                    Debug.LogError($"[KnightSpineAnimTest] Skeleton không có animation '{n}'.");
                    ok = false;
                }
            }
            return ok;
        }

        void AlignCharacterToDesiredCenter()
        {
            if (_skel == null)
                return;

            var rend = _skel.GetComponent<Renderer>();
            if (rend == null)
                return;

            // Skeleton root/pivot thường lệch; canh theo visual bounds để luôn ra giữa.
            var center = rend.bounds.center;
            var delta = desiredWorldCenter - center;
            _skel.transform.position += delta;
        }

        void EnsureEventSystem()
        {
            if (FindAnyObjectByType<EventSystem>() != null)
                return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>();
        }

        void BuildBackdrop()
        {
            var cam = Camera.main;
            if (cam == null)
                return;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = backdropColor;
        }

        void BuildUi()
        {
            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
            var canvasGo = new GameObject("KnightAnimTest_UI");
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 400;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();

            _statusText = CreateText(canvas.transform, font, new Vector2(0, -80), 28,
                $"{resourcesPrefabPath} — adjust worldScale in the Inspector if the character is too large or small.");

            float y = -180f;
            float step = -95f;
            CreateButton(canvas.transform, font, "Idle (loop)", new Vector2(0, y), () => PlayLoop(IdleName));
            y += step;
            CreateButton(canvas.transform, font, "Walk (loop)", new Vector2(0, y), () => PlayLoop(WalkName));
            y += step;
            CreateButton(canvas.transform, font, string.Format("{0} (one-shot)", AttackName), new Vector2(0, y), PlayAttackOnce);
            y += step;
            CreateButton(canvas.transform, font, string.Format("{0} (one-shot)", DiedName), new Vector2(0, y), PlayDieOnce);

            CreateButton(canvas.transform, font, "Flip X", new Vector2(0, -560), ToggleFlip);
            CreateButton(canvas.transform, font, "BACK TO HOME", new Vector2(0, -660), BackToHomepage);
        }

        Text CreateText(Transform parent, Font font, Vector2 pos, int size, string msg)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(960, 80);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = Color.white;
            t.alignment = TextAnchor.UpperCenter;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.text = msg;
            return t;
        }

        Button CreateButton(Transform parent, Font font, string label, Vector2 pos, UnityEngine.Events.UnityAction onClick)
        {
            var go = new GameObject($"Btn_{label}");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = new Vector2(420, 72);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.45f, 0.72f, 0.95f);
            var btn = go.AddComponent<Button>();
            btn.targetGraphic = img;
            btn.onClick.AddListener(onClick);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var trt = textGo.AddComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = Vector2.zero;
            trt.offsetMax = Vector2.zero;
            var txt = textGo.AddComponent<Text>();
            txt.font = font;
            txt.fontSize = 28;
            txt.color = Color.white;
            txt.alignment = TextAnchor.MiddleCenter;
            txt.text = label;

            return btn;
        }

        void SetStatus(string msg)
        {
            if (_statusText != null)
                _statusText.text = msg;
            Debug.Log("[KnightSpineAnimTest] " + msg);
        }

        void ClearPendingHandler()
        {
            if (_pendingComplete == null || _state == null)
            {
                _pendingComplete = null;
                return;
            }

            _pendingComplete.Complete -= OnOneShotFinished;
            _pendingComplete = null;
        }

        void PlayLoop(string animName)
        {
            ClearPendingHandler();
            if (_state == null)
                return;
            var clip = _skel.Skeleton.Data.FindAnimation(animName);
            if (clip == null)
                return;
            _state.SetAnimation(0, animName, true);
            SetStatus($"Looping: {animName}");
        }

        void PlayAttackOnce()
        {
            OneShotThenIdle(AttackName);
        }

        void PlayDieOnce()
        {
            OneShotThenIdle(DiedName);
        }

        void OneShotThenIdle(string animName)
        {
            ClearPendingHandler();
            if (_state == null)
                return;
            if (_skel.Skeleton.Data.FindAnimation(animName) == null)
                return;
            _pendingComplete = _state.SetAnimation(0, animName, false);
            if (_pendingComplete != null)
            {
                _pendingComplete.Complete += OnOneShotFinished;
                SetStatus($"{animName} (one-shot) → idle");
            }
        }

        void OnOneShotFinished(TrackEntry entry)
        {
            if (entry != _pendingComplete)
                return;
            ClearPendingHandler();
            if (_skel != null && _skel.Skeleton != null)
                _skel.Skeleton.SetToSetupPose();
            SetLooping(IdleName);
            SetStatus("Xong clip → idle");
        }

        void SetLooping(string animName)
        {
            if (_state != null && _skel.Skeleton.Data.FindAnimation(animName) != null)
                _state.SetAnimation(0, animName, true);
        }

        void ToggleFlip()
        {
            if (_skel == null)
                return;
            var s = _skel.Skeleton;
            if (Mathf.Abs(s.ScaleX) < 1e-4f)
                s.ScaleX = 1f;
            else
                s.ScaleX *= -1f;
            SetStatus("Flip ScaleX → " + s.ScaleX.ToString("0.##"));
        }

        void BackToHomepage()
        {
            if (!Application.CanStreamedLevelBeLoaded(homepageSceneName))
            {
                SetStatus("Could not load scene: " + homepageSceneName);
                return;
            }
            SceneManager.LoadScene(homepageSceneName);
        }
    }
}
