using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace SpinSquad.Core
{
    /// <summary>Mũi tên trên ô lưới prep: bấm để merge hoặc combine (Mythic).</summary>
    public sealed class WorldMergeAffordance : MonoBehaviour
    {
        DuelDirector _director;
        bool _allyGrid;
        Collider2D _col;

        public void Bind(DuelDirector director, bool allyGrid)
        {
            _director = director;
            _allyGrid = allyGrid;
            _col = GetComponent<Collider2D>();
        }

        void Update()
        {
            if (_director == null || !_director.SandboxCanMutateUnits || !gameObject.activeSelf)
                return;

            var mouse = Mouse.current;
            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            var cam = Camera.main;
            if (cam == null || _col == null)
                return;

            var depth = Mathf.Abs(cam.transform.position.z);
            var w = cam.ScreenToWorldPoint(new Vector3(mouse.position.ReadValue().x, mouse.position.ReadValue().y, depth));
            var hit = Physics2D.OverlapPoint(w);
            if (hit == null || hit != _col)
                return;

            _director.OnWorldMergeAffordanceClicked(_allyGrid);
        }
    }
}
