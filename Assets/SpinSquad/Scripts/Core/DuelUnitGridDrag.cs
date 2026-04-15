using UnityEngine;
using UnityEngine.EventSystems;

namespace SpinSquad.Core
{
    /// <summary>
    /// Kéo unit trên lưới 4×4 đúng phe (ally chỉ lưới trái, enemy chỉ lưới phải). Chỉ khi chưa bấm Bắt đầu.
    /// Ally / enemy leader: nhấn ngắn = chọn ô (merge); giữ hoặc kéo = di chuyển stack. Follower: chạm = chọn ô stack.
    /// </summary>
    [RequireComponent(typeof(CombatHealth))]
    [RequireComponent(typeof(Collider2D))]
    public sealed class DuelUnitGridDrag : MonoBehaviour
    {
        const float DragThresholdWorld = 0.11f;

        [Tooltip("Ally leader: giữ bấy lâu thì vào kéo dù chưa di chuyển đủ slop.")]
        [SerializeField] float _allyDragLongPressSeconds = 0.28f;

        DuelDirector _director;
        CombatHealth _health;
        Collider2D _col;
        bool _dragging;
        bool _pending;
        bool _pendingEnemy;
        bool _pendingEnemyTapSelect;
        Vector2 _pendingStartWorld;
        Vector2 _pendingEnemyStartWorld;
        float _allyLeaderPendingUnscaledTime;
        int _dragStartRow;
        int _dragStartCol;
        Vector3 _dragStartLeaderWorld;

        public void Init(DuelDirector director)
        {
            _director = director;
        }

        void Awake()
        {
            _health = GetComponent<CombatHealth>();
            _col = GetComponent<Collider2D>();
        }

        void OnDisable()
        {
            if (_dragging && _director != null)
            {
                _dragging = false;
                _director.SetDraggingGridUnit(false);
            }

            _pending = false;
            _pendingEnemy = false;
            _pendingEnemyTapSelect = false;
        }

        void Update()
        {
            if (_director == null || _director.CombatStarted || _director.BattleEnded)
            {
                if (_dragging)
                    EndDrag();
                _pending = false;
                _pendingEnemy = false;
                _pendingEnemyTapSelect = false;
                return;
            }

            if (PrepGridPointer.WasPressedThisFrame())
            {
                if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                    return;

                if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var pressScreen))
                    return;

                var w = ScreenToWorldOnPlane(pressScreen);
                if (!OverlapPointTouchesCollider(w, _col))
                    return;

                if (_health.Faction == CombatFaction.Enemy)
                {
                    var enemySpec = GetComponent<EnemyInstanceSpec>();
                    if (enemySpec != null && !enemySpec.StackLeader)
                    {
                        _pendingEnemyTapSelect = true;
                        return;
                    }

                    _pendingEnemy = true;
                    _pendingEnemyStartWorld = w;
                    var snappedE = BattleGrid.SnapWorldToEnemyGrid((Vector2)DuelDirector.EnemyGridSampleWorld(_health));
                    BattleGrid.WorldToEnemyCell(snappedE, out _dragStartRow, out _dragStartCol);
                    _dragStartLeaderWorld = DuelDirector.EnemyGridSampleWorld(_health);
                    return;
                }

                _pending = true;
                _allyLeaderPendingUnscaledTime = Time.unscaledTime;
                _pendingStartWorld = w;
                var snapped = BattleGrid.SnapWorldToAllyGrid((Vector2)transform.position);
                BattleGrid.WorldToAllyCell(snapped, out _dragStartRow, out _dragStartCol);
                _dragStartLeaderWorld = transform.position;
            }
            else if (_pendingEnemy && PrepGridPointer.IsPressed() && !_dragging)
            {
                if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var moveScreen))
                    return;

                var w = ScreenToWorldOnPlane(moveScreen);
                if ((w - _pendingEnemyStartWorld).sqrMagnitude > DragThresholdWorld * DragThresholdWorld)
                {
                    BeginDrag();
                    _pendingEnemy = false;
                }
            }
            else if (_pending && PrepGridPointer.IsPressed() && !_dragging)
            {
                if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var moveScreen))
                    return;

                var w = ScreenToWorldOnPlane(moveScreen);
                if (_health.Faction == CombatFaction.Ally)
                {
                    if (Time.unscaledTime - _allyLeaderPendingUnscaledTime >= _allyDragLongPressSeconds)
                    {
                        BeginDrag();
                        _pending = false;
                    }
                    else if ((w - _pendingStartWorld).sqrMagnitude > DragThresholdWorld * DragThresholdWorld)
                    {
                        BeginDrag();
                        _pending = false;
                    }
                }
            }
            else if (_dragging && PrepGridPointer.IsPressed())
            {
                if (!PrepGridPointer.TryGetPrimaryScreenPosition(out var dragScreen))
                    return;

                var w = ScreenToWorldOnPlane(dragScreen);
                DragTo(w);
            }
            else if (PrepGridPointer.WasReleasedThisFrame())
            {
                if (_pendingEnemyTapSelect)
                {
                    _director.NotifyEnemyShortTap(_health);
                    _pendingEnemyTapSelect = false;
                }
                else if (_pendingEnemy && !_dragging)
                {
                    _director.NotifyEnemyShortTap(_health);
                    _pendingEnemy = false;
                }
                else if (_pending && !_dragging)
                {
                    _pending = false;
                }
                else
                {
                    EndDrag();
                }
            }
        }

        void BeginDrag()
        {
            if (_health.Faction == CombatFaction.Ally)
                _director.CancelPendingAllyCellContextTap();

            _dragging = true;
            _director.SetDraggingGridUnit(true);
        }

        void DragTo(Vector2 world)
        {
            Vector3 snap;
            if (_health.Faction == CombatFaction.Ally)
                snap = BattleGrid.SnapWorldToAllyGrid(world);
            else
                snap = BattleGrid.SnapWorldToEnemyGrid(world);

            var p = transform.position;
            p.x = snap.x;
            p.y = snap.y;
            transform.position = p;
        }

        void EndDrag()
        {
            if (_health.Faction == CombatFaction.Enemy)
            {
                if (!_dragging)
                    return;

                _dragging = false;
                _director.SetDraggingGridUnit(false);

                var enemyPos = (Vector2)transform.position;
                var enemySnap = BattleGrid.SnapWorldToEnemyGrid(enemyPos);
                BattleGrid.WorldToEnemyCell(enemySnap, out var enemyToRow, out var enemyToCol);
                _director.TryRelocateEnemyStack(
                    _health,
                    _dragStartRow,
                    _dragStartCol,
                    enemyToRow,
                    enemyToCol,
                    _dragStartLeaderWorld);
                return;
            }

            if (!_dragging)
                return;

            _dragging = false;
            _director.SetDraggingGridUnit(false);

            var allyPos = (Vector2)transform.position;
            var allySnap = BattleGrid.SnapWorldToAllyGrid(allyPos);
            BattleGrid.WorldToAllyCell(allySnap, out var allyToRow, out var allyToCol);
            _director.TryRelocateAllyStack(
                _health,
                _dragStartRow,
                _dragStartCol,
                allyToRow,
                allyToCol,
                _dragStartLeaderWorld);
        }

        static Vector2 ScreenToWorldOnPlane(Vector2 screen)
        {
            var cam = Camera.main;
            if (cam == null)
                return Vector2.zero;

            var depth = Mathf.Abs(cam.transform.position.z);
            var w = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, depth));
            return new Vector2(w.x, w.y);
        }

        static bool OverlapPointTouchesCollider(Vector2 world, Collider2D col)
        {
            if (col == null)
                return false;
            foreach (var h in Physics2D.OverlapPointAll(world))
            {
                if (h == col)
                    return true;
            }

            return false;
        }
    }
}
