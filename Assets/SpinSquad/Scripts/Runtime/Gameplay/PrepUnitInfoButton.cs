using SpinSquad.Data;
using UnityEngine;
using UnityEngine.EventSystems;

namespace SpinSquad.Core
{
    /// <summary>Nút ! trên unit — chỉ hiện lúc prep; mở panel chỉ số.</summary>
    [RequireComponent(typeof(CombatHealth))]
    public sealed class PrepUnitInfoButton : MonoBehaviour
    {
        const float BtnWorldSize = 0.2f;

        DuelDirector _director;
        CombatHealth _health;
        Transform _btnRoot;
        BoxCollider2D _btnCollider;

        public void Init(DuelDirector director)
        {
            _director = director;
            _health = GetComponent<CombatHealth>();
            if (_btnRoot == null)
                BuildButton();
        }

        void BuildButton()
        {
            _btnRoot = new GameObject("PrepInfoBtn").transform;
            _btnRoot.SetParent(transform, false);
            _btnRoot.localPosition = new Vector3(0.2f, 0.16f, -0.02f);
            _btnRoot.localScale = Vector3.one * BtnWorldSize;

            var bg = _btnRoot.gameObject.AddComponent<SpriteRenderer>();
            bg.sprite = UnitSpriteFactory.GetSprite(UnitBodyShape.Circle);
            bg.color = new Color(1f, 0.78f, 0.12f, 0.96f);
            bg.sortingOrder = 48;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(_btnRoot, false);
            labelGo.transform.localPosition = Vector3.zero;
            var tm = labelGo.AddComponent<TextMesh>();
            tm.text = "!";
            tm.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            tm.fontSize = 64;
            tm.characterSize = 0.12f;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.color = new Color(0.12f, 0.1f, 0.05f, 1f);
            tm.GetComponent<MeshRenderer>().sortingOrder = 49;

            _btnCollider = _btnRoot.gameObject.AddComponent<BoxCollider2D>();
            _btnCollider.isTrigger = true;
            _btnCollider.size = Vector2.one * 1.05f;
        }

        void LateUpdate()
        {
            if (_btnRoot == null)
                return;

            var show = _director != null && _director.SandboxCanMutateUnits && _health != null && !_health.IsDead;
            _btnRoot.gameObject.SetActive(show);
        }

        void Update()
        {
            if (_btnRoot == null || !_btnRoot.gameObject.activeInHierarchy || _director == null)
                return;
            if (!PrepGridPointer.WasPressedThisFrame())
                return;
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;
            if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var screen))
                return;

            var cam = Camera.main;
            if (cam == null)
                return;

            var depth = Mathf.Abs(cam.transform.position.z);
            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            var hit = Physics2D.OverlapPoint(new Vector2(world.x, world.y));
            if (hit == null || !hit.transform.IsChildOf(_btnRoot))
                return;

            _director.ShowPrepUnitStatInspect(_health);
        }
    }
}
