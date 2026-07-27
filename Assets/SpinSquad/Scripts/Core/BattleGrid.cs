using SpinSquad.UI;
using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Hai lưới ally/enemy: mặc định 2 cột × 4 hàng. Đơn vị spawn trong ô rồi bước ra phía đối thủ.
    /// </summary>
    public static class BattleGrid
    {
        public const int Rows = 4;
        public const int Cols = 2;
        public const int CellCount = Rows * Cols;
        public const float Cell = 0.62f;

        /// <summary>Bench 2×8 dưới lưới ally (cùng bề ngang 2 cột).</summary>
        public const int InventoryRows = 8;
        public const int InventoryCols = 2;
        public const int InventorySlotCount = InventoryRows * InventoryCols;
        public const float InventoryCell = Cell * 0.5f;

        const float InventoryGapBelowAllyGrid = 0.14f;

        static Transform _allyGridLinesRoot;
        static Transform _enemyGridLinesRoot;
        static Transform _allyPrepMarkersRoot;
        static Transform _enemyPrepMarkersRoot;
        static Transform _inventoryPrepMarkersRoot;
        static Transform _inventoryBenchPanelRoot;
        static SpriteRenderer _battleBackgroundSr;

        public static Vector3 AllyOrigin { get; private set; } = new(-2.05f, -0.84f, 0f);
        public static Vector3 EnemyOrigin { get; private set; } = new(0.18f, -0.84f, 0f);
        public static Vector3 InventoryOrigin { get; private set; } = new(-2.05f, -1.55f, 0f);

        /// <summary>Đẩy sprite ally xuống trong ô — khung lưới vẫn ngang với enemy.</summary>
        public const float AllyUnitAnchorYOffset = -0.14f;

        public static Vector3 AllyUnitAnchorOffset => new(0f, AllyUnitAnchorYOffset, 0f);

        public static void ConfigureForCenteredPair(Camera cam, float gapBetweenGrids, float baseY, float extraBaseYOffset = 0f)
        {
            if (cam == null || !cam.orthographic)
                return;

            var halfW = cam.orthographicSize * cam.aspect;
            var halfH = cam.orthographicSize;
            var w = Cols * Cell;
            var h = Rows * Cell;

            var leftX = -gapBetweenGrids * 0.5f - w;
            var rightX = gapBetweenGrids * 0.5f;

            var margin = 0.06f;
            leftX = Mathf.Max(leftX, -halfW + margin);
            rightX = Mathf.Min(rightX, halfW - margin - w);

            var benchBelow = InventoryRows * InventoryCell + InventoryGapBelowAllyGrid;
            var y = baseY + extraBaseYOffset;
            var yMin = -halfH + margin + benchBelow;
            var yMax = halfH - margin - h;
            y = Mathf.Clamp(y, yMin, yMax);

            AllyOrigin = new Vector3(leftX, y, 0f);
            EnemyOrigin = new Vector3(rightX, y, 0f);
            ConfigureInventoryOrigin();
        }

        static void ConfigureInventoryOrigin()
        {
            var allyW = Cols * Cell;
            var invW = InventoryCols * InventoryCell;
            var invX = AllyOrigin.x + (allyW - invW) * 0.5f;
            var invY = AllyOrigin.y - Rows * Cell - InventoryGapBelowAllyGrid - InventoryRows * InventoryCell;
            InventoryOrigin = new Vector3(invX, invY, 0f);
        }

        public static Vector3 GetInventoryPanelTopCenter()
        {
            var w = InventoryCols * InventoryCell;
            var h = InventoryRows * InventoryCell;
            return InventoryOrigin + new Vector3(w * 0.5f, h, 0f);
        }

        public static Vector2 GetInventoryPanelWorldSize() =>
            new Vector2(InventoryCols * InventoryCell, InventoryRows * InventoryCell);

        public const int DefaultAllyRow = 2;
        public const int DefaultAllyCol = 1;
        public const int DefaultEnemyRow = 2;
        public const int DefaultEnemyCol = 1;

        public static Vector3 GetAllyCellCenter(int row, int col)
        {
            row = Mathf.Clamp(row, 0, Rows - 1);
            col = Mathf.Clamp(col, 0, Cols - 1);
            return AllyOrigin + new Vector3((col + 0.5f) * Cell, (row + 0.5f) * Cell, 0f);
        }

        public static Vector3 GetAllyUnitAnchor(int row, int col) =>
            GetAllyCellCenter(row, col) + AllyUnitAnchorOffset;

        public static Vector3 GetEnemyCellCenter(int row, int col)
        {
            row = Mathf.Clamp(row, 0, Rows - 1);
            col = Mathf.Clamp(col, 0, Cols - 1);
            return EnemyOrigin + new Vector3((col + 0.5f) * Cell, (row + 0.5f) * Cell, 0f);
        }

        public static Vector3 DefaultAllySpawn => GetAllyUnitAnchor(DefaultAllyRow, DefaultAllyCol);
        public static Vector3 DefaultEnemySpawn => GetEnemyCellCenter(DefaultEnemyRow, DefaultEnemyCol);

        public static int InventorySlotToRow(int slotIndex) => Mathf.Clamp(slotIndex / InventoryCols, 0, InventoryRows - 1);

        public static int InventorySlotToCol(int slotIndex) => Mathf.Clamp(slotIndex % InventoryCols, 0, InventoryCols - 1);

        public static Vector3 GetInventorySlotCenter(int slotIndex)
        {
            var row = InventorySlotToRow(slotIndex);
            var col = InventorySlotToCol(slotIndex);
            return InventoryOrigin + new Vector3((col + 0.5f) * InventoryCell, (row + 0.5f) * InventoryCell, 0f);
        }

        public static void WorldToAllyCell(Vector2 world, out int row, out int col)
        {
            var ox = AllyOrigin.x;
            var oy = AllyOrigin.y;
            col = Mathf.Clamp(Mathf.FloorToInt((world.x - ox) / Cell), 0, Cols - 1);
            row = Mathf.Clamp(Mathf.FloorToInt((world.y - oy) / Cell), 0, Rows - 1);
        }

        public static void WorldToEnemyCell(Vector2 world, out int row, out int col)
        {
            var ox = EnemyOrigin.x;
            var oy = EnemyOrigin.y;
            col = Mathf.Clamp(Mathf.FloorToInt((world.x - ox) / Cell), 0, Cols - 1);
            row = Mathf.Clamp(Mathf.FloorToInt((world.y - oy) / Cell), 0, Rows - 1);
        }

        public static Vector3 SnapWorldToAllyGrid(Vector2 world)
        {
            var ox = AllyOrigin.x;
            var oy = AllyOrigin.y;
            var maxX = ox + Cols * Cell;
            var maxY = oy + Rows * Cell;
            var wx = Mathf.Clamp(world.x, ox + 0.001f, maxX - 0.001f);
            var wy = Mathf.Clamp(world.y, oy + 0.001f, maxY - 0.001f);
            var col = Mathf.Clamp(Mathf.FloorToInt((wx - ox) / Cell), 0, Cols - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt((wy - oy) / Cell), 0, Rows - 1);
            return GetAllyCellCenter(row, col);
        }

        public static Vector3 SnapWorldToEnemyGrid(Vector2 world)
        {
            var ox = EnemyOrigin.x;
            var oy = EnemyOrigin.y;
            var maxX = ox + Cols * Cell;
            var maxY = oy + Rows * Cell;
            var wx = Mathf.Clamp(world.x, ox + 0.001f, maxX - 0.001f);
            var wy = Mathf.Clamp(world.y, oy + 0.001f, maxY - 0.001f);
            var col = Mathf.Clamp(Mathf.FloorToInt((wx - ox) / Cell), 0, Cols - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt((wy - oy) / Cell), 0, Rows - 1);
            return GetEnemyCellCenter(row, col);
        }

        public static void BuildVisuals(Transform parent)
        {
            var root = new GameObject("BattleGrids");
            root.transform.SetParent(parent, false);

            BuildBattleBackground(root.transform);
            BuildPrepCellMarkers(root.transform, "AllyPrepCellMarkers", GetAllyCellCenter, out _allyPrepMarkersRoot);
            BuildPrepCellMarkers(root.transform, "EnemyPrepCellMarkers", GetEnemyCellCenter, out _enemyPrepMarkersRoot);
            BuildInventoryBenchPanel(root.transform);
            BuildInventoryPrepCellMarkers(root.transform);

            var allyLines = new GameObject("AllyGridLines");
            allyLines.transform.SetParent(root.transform, false);
            _allyGridLinesRoot = allyLines.transform;
            BuildLineGrid(_allyGridLinesRoot, AllyOrigin, new Color(0.45f, 0.75f, 1f, 0.55f));

            var enemyLines = new GameObject("EnemyGridLines");
            enemyLines.transform.SetParent(root.transform, false);
            _enemyGridLinesRoot = enemyLines.transform;
            BuildLineGrid(_enemyGridLinesRoot, EnemyOrigin, new Color(1f, 0.45f, 0.45f, 0.55f));

            SetPrepPhaseGridVisuals(true);
            SetBattleBackgroundVisible(false);
        }

        /// <summary>Prep: ô Place_holder hai bên. In battle: ẩn ô prep và không hiện viền lưới trắng.</summary>
        public static void SetPrepPhaseGridVisuals(bool prepPhase)
        {
            if (_allyPrepMarkersRoot != null)
                _allyPrepMarkersRoot.gameObject.SetActive(prepPhase);
            if (_enemyPrepMarkersRoot != null)
                _enemyPrepMarkersRoot.gameObject.SetActive(prepPhase);
            if (_allyGridLinesRoot != null)
                _allyGridLinesRoot.gameObject.SetActive(false);
            if (_enemyGridLinesRoot != null)
                _enemyGridLinesRoot.gameObject.SetActive(false);
        }

        /// <summary>Ẩn/hiện khung inventory 4×4 (prep).</summary>
        public static void SetInventorySectionVisible(bool visible)
        {
            if (_inventoryPrepMarkersRoot != null)
                _inventoryPrepMarkersRoot.gameObject.SetActive(visible);
            if (_inventoryBenchPanelRoot != null)
                _inventoryBenchPanelRoot.gameObject.SetActive(visible);
        }

        static void BuildInventoryBenchPanel(Transform parent)
        {
            var rootGo = new GameObject("InventoryBenchPanel");
            rootGo.transform.SetParent(parent, false);
            _inventoryBenchPanelRoot = rootGo.transform;

            var size = GetInventoryPanelWorldSize();
            var center = InventoryOrigin + new Vector3(size.x * 0.5f, size.y * 0.5f, 0.03f);
            rootGo.transform.position = center;

            var sprite = BattleUiSprites.PlaceHolderCell;
            var sr = rootGo.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = 2;
            sr.color = new Color(0.08f, 0.1f, 0.16f, 0.72f);
            if (sprite != null)
            {
                var bounds = sprite.bounds.size;
                var scaleX = size.x / Mathf.Max(0.01f, bounds.x) * 1.04f;
                var scaleY = size.y / Mathf.Max(0.01f, bounds.y) * 1.04f;
                rootGo.transform.localScale = new Vector3(scaleX, scaleY, 1f);
            }
        }

        /// <summary>Nền Background_battle_1 chỉ khi đang combat (in battle).</summary>
        public static void SetBattleBackgroundVisible(bool visible)
        {
            if (_battleBackgroundSr != null)
                _battleBackgroundSr.gameObject.SetActive(visible);
        }

        static void BuildBattleBackground(Transform parent)
        {
            var go = new GameObject("BattleBackground");
            go.transform.SetParent(parent, false);
            _battleBackgroundSr = go.AddComponent<SpriteRenderer>();
            _battleBackgroundSr.sortingOrder = -20;
            _battleBackgroundSr.gameObject.SetActive(false);
            ApplyBattleBackgroundSprite(BattleUiSprites.BackgroundBattle);
        }

        /// <summary>Đổi nền combat (theo level) và scale full màn.</summary>
        public static void ApplyBattleBackgroundForLevel(int level, float brightness = 1f)
        {
            ApplyBattleBackgroundSprite(BattleUiSprites.GetBackgroundBattleForLevel(level), brightness);
        }

        public static void ApplyPrepBackground(float brightness = 0.92f)
        {
            ApplyBattleBackgroundSprite(BattleUiSprites.GetBackgroundPrep(), brightness);
        }

        static void ApplyBattleBackgroundSprite(Sprite sprite, float brightness = 1f)
        {
            if (_battleBackgroundSr == null || sprite == null)
                return;

            _battleBackgroundSr.sprite = sprite;
            var b = Mathf.Clamp01(brightness);
            _battleBackgroundSr.color = new Color(b, b, b, 1f);

            var cam = Camera.main;
            if (cam == null || !cam.orthographic)
            {
                _battleBackgroundSr.transform.position = new Vector3(0f, 0f, 0.5f);
                _battleBackgroundSr.transform.localScale = Vector3.one * 8f;
                return;
            }

            var h = cam.orthographicSize * 2f;
            var w = h * cam.aspect;
            var allyMid = AllyOrigin + new Vector3(Cols * Cell * 0.5f, Rows * Cell * 0.5f, 0f);
            var enemyMid = EnemyOrigin + new Vector3(Cols * Cell * 0.5f, Rows * Cell * 0.5f, 0f);
            var center = (allyMid + enemyMid) * 0.5f;
            _battleBackgroundSr.transform.position = new Vector3(center.x, center.y, 0.5f);

            var bounds = sprite.bounds.size;
            var scaleX = w / Mathf.Max(0.01f, bounds.x) * 1.12f;
            var scaleY = h / Mathf.Max(0.01f, bounds.y) * 1.12f;
            var scale = Mathf.Max(scaleX, scaleY);
            _battleBackgroundSr.transform.localScale = new Vector3(scale, scale, 1f);
        }

        static void BuildInventoryPrepCellMarkers(Transform parent)
        {
            var sprite = BattleUiSprites.PlaceHolderCell;
            var rootGo = new GameObject("InventoryPrepCellMarkers");
            rootGo.transform.SetParent(parent, false);
            _inventoryPrepMarkersRoot = rootGo.transform;

            for (var i = 0; i < InventorySlotCount; i++)
            {
                var slot = new GameObject($"InvSlot_{i}");
                slot.transform.SetParent(_inventoryPrepMarkersRoot, false);
                var center = GetInventorySlotCenter(i);
                slot.transform.position = new Vector3(center.x, center.y, 0.035f);

                var sr = slot.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.sortingOrder = 3;
                sr.color = new Color(1f, 1f, 1f, 0.82f);

                if (sprite != null)
                {
                    var bounds = sprite.bounds.size;
                    var target = InventoryCell * 1.02f;
                    var scale = target / Mathf.Max(bounds.x, bounds.y);
                    slot.transform.localScale = Vector3.one * scale;
                }
            }
        }

        static void BuildPrepCellMarkers(
            Transform parent,
            string rootName,
            System.Func<int, int, Vector3> getCenter,
            out Transform markersRoot)
        {
            var sprite = BattleUiSprites.PlaceHolderCell;
            var rootGo = new GameObject(rootName);
            rootGo.transform.SetParent(parent, false);
            markersRoot = rootGo.transform;

            for (var r = 0; r < Rows; r++)
            {
                for (var c = 0; c < Cols; c++)
                {
                    var slot = new GameObject($"Slot_{r}_{c}");
                    slot.transform.SetParent(markersRoot, false);
                    var center = getCenter(r, c);
                    slot.transform.position = new Vector3(center.x, center.y, 0.04f);

                    var sr = slot.AddComponent<SpriteRenderer>();
                    sr.sprite = sprite;
                    sr.sortingOrder = 4;
                    sr.color = Color.white;

                    if (sprite != null)
                    {
                        var bounds = sprite.bounds.size;
                        var target = Cell * 1.08f;
                        var scale = target / Mathf.Max(bounds.x, bounds.y);
                        slot.transform.localScale = Vector3.one * scale;
                    }
                }
            }
        }

        static void BuildLineGrid(Transform parent, Vector3 origin, Color color)
        {
            var w = Cols * Cell;
            var h = Rows * Cell;

            for (var i = 0; i <= Cols; i++)
            {
                var lineGo = new GameObject($"Line_V_{i}");
                lineGo.transform.SetParent(parent, false);
                var lr = lineGo.AddComponent<LineRenderer>();
                SetupLine(lr, color);
                var x = origin.x + i * Cell;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(x, origin.y, 0.02f));
                lr.SetPosition(1, new Vector3(x, origin.y + h, 0.02f));
            }

            for (var i = 0; i <= Rows; i++)
            {
                var lineGo = new GameObject($"Line_H_{i}");
                lineGo.transform.SetParent(parent, false);
                var lr = lineGo.AddComponent<LineRenderer>();
                SetupLine(lr, color);
                var y = origin.y + i * Cell;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(origin.x, y, 0.02f));
                lr.SetPosition(1, new Vector3(origin.x + w, y, 0.02f));
            }
        }

        static void SetupLine(LineRenderer lr, Color color)
        {
            lr.loop = false;
            lr.useWorldSpace = true;
            lr.startWidth = lr.endWidth = 0.028f;
            lr.startColor = lr.endColor = color;
            lr.numCapVertices = 0;
            lr.sortingOrder = 10;
            var shader = Shader.Find("Unlit/Color");
            if (shader != null)
                lr.sharedMaterial = new Material(shader);
        }
    }
}
