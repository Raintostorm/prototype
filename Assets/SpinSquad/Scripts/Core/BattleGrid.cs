using UnityEngine;

namespace SpinSquad.Core
{
    /// <summary>
    /// Hai lưới 4×4: allies bên trái, enemies bên phải. Đơn vị spawn trong ô rồi bước ra phía đối thủ.
    /// </summary>
    public static class BattleGrid
    {
        public const int GridSize = 4;
        public const float Cell = 0.42f;

        /// <summary>
        /// Góc dưới-trái lưới ally. Căn trong ~±2.2 world (camera ortho 5, portrait ~0.46 aspect)
        /// để cả 4×4 không bị cắt mép Game view.
        /// </summary>
        public static readonly Vector3 AllyOrigin = new(-2.2f, -0.84f, 0f);

        /// <summary>Góc dưới-trái lưới enemy; col 0 là hàng gần ally nhất.</summary>
        public static readonly Vector3 EnemyOrigin = new(0.52f, -0.84f, 0f);

        /// <summary>Ô mặc định cho stack 3: hàng thứ 3 / cột thứ 4 (gốc lưới dưới-trái, row tăng lên).</summary>
        public const int DefaultAllyRow = 2;
        public const int DefaultAllyCol = 3;

        /// <summary>Cùng tọa độ ô trên lưới enemy để so layout stack 3 với ally.</summary>
        public const int DefaultEnemyRow = 2;
        public const int DefaultEnemyCol = 3;

        public static Vector3 GetAllyCellCenter(int row, int col)
        {
            row = Mathf.Clamp(row, 0, GridSize - 1);
            col = Mathf.Clamp(col, 0, GridSize - 1);
            return AllyOrigin + new Vector3((col + 0.5f) * Cell, (row + 0.5f) * Cell, 0f);
        }

        public static Vector3 GetEnemyCellCenter(int row, int col)
        {
            row = Mathf.Clamp(row, 0, GridSize - 1);
            col = Mathf.Clamp(col, 0, GridSize - 1);
            return EnemyOrigin + new Vector3((col + 0.5f) * Cell, (row + 0.5f) * Cell, 0f);
        }

        public static Vector3 DefaultAllySpawn => GetAllyCellCenter(DefaultAllyRow, DefaultAllyCol);
        public static Vector3 DefaultEnemySpawn => GetEnemyCellCenter(DefaultEnemyRow, DefaultEnemyCol);

        public static void WorldToAllyCell(Vector2 world, out int row, out int col)
        {
            var ox = AllyOrigin.x;
            var oy = AllyOrigin.y;
            col = Mathf.Clamp(Mathf.FloorToInt((world.x - ox) / Cell), 0, GridSize - 1);
            row = Mathf.Clamp(Mathf.FloorToInt((world.y - oy) / Cell), 0, GridSize - 1);
        }

        public static void WorldToEnemyCell(Vector2 world, out int row, out int col)
        {
            var ox = EnemyOrigin.x;
            var oy = EnemyOrigin.y;
            col = Mathf.Clamp(Mathf.FloorToInt((world.x - ox) / Cell), 0, GridSize - 1);
            row = Mathf.Clamp(Mathf.FloorToInt((world.y - oy) / Cell), 0, GridSize - 1);
        }

        /// <summary>Chặn world vào hình chữ nhật lưới ally rồi trả tâm ô gần nhất.</summary>
        public static Vector3 SnapWorldToAllyGrid(Vector2 world)
        {
            var ox = AllyOrigin.x;
            var oy = AllyOrigin.y;
            var maxX = ox + GridSize * Cell;
            var maxY = oy + GridSize * Cell;
            var wx = Mathf.Clamp(world.x, ox + 0.001f, maxX - 0.001f);
            var wy = Mathf.Clamp(world.y, oy + 0.001f, maxY - 0.001f);
            var col = Mathf.Clamp(Mathf.FloorToInt((wx - ox) / Cell), 0, GridSize - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt((wy - oy) / Cell), 0, GridSize - 1);
            return GetAllyCellCenter(row, col);
        }

        /// <summary>Chặn world vào hình chữ nhật lưới enemy rồi trả tâm ô gần nhất.</summary>
        public static Vector3 SnapWorldToEnemyGrid(Vector2 world)
        {
            var ox = EnemyOrigin.x;
            var oy = EnemyOrigin.y;
            var maxX = ox + GridSize * Cell;
            var maxY = oy + GridSize * Cell;
            var wx = Mathf.Clamp(world.x, ox + 0.001f, maxX - 0.001f);
            var wy = Mathf.Clamp(world.y, oy + 0.001f, maxY - 0.001f);
            var col = Mathf.Clamp(Mathf.FloorToInt((wx - ox) / Cell), 0, GridSize - 1);
            var row = Mathf.Clamp(Mathf.FloorToInt((wy - oy) / Cell), 0, GridSize - 1);
            return GetEnemyCellCenter(row, col);
        }

        /// <summary>Vẽ viền và lưới 4×4 hai bên (LineRenderer).</summary>
        public static void BuildVisuals(Transform parent)
        {
            var root = new GameObject("BattleGrids");
            root.transform.SetParent(parent, false);

            BuildOneGrid(root.transform, "AllyGrid", AllyOrigin, new Color(0.45f, 0.75f, 1f, 0.55f));
            BuildOneGrid(root.transform, "EnemyGrid", EnemyOrigin, new Color(1f, 0.45f, 0.45f, 0.55f));
        }

        static void BuildOneGrid(Transform parent, string name, Vector3 origin, Color color)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var w = GridSize * Cell;
            var h = GridSize * Cell;

            for (var i = 0; i <= GridSize; i++)
            {
                var lineGo = new GameObject($"Line_V_{i}");
                lineGo.transform.SetParent(go.transform, false);
                var lr = lineGo.AddComponent<LineRenderer>();
                SetupLine(lr, color);
                var x = origin.x + i * Cell;
                lr.positionCount = 2;
                lr.SetPosition(0, new Vector3(x, origin.y, 0.02f));
                lr.SetPosition(1, new Vector3(x, origin.y + h, 0.02f));
            }

            for (var i = 0; i <= GridSize; i++)
            {
                var lineGo = new GameObject($"Line_H_{i}");
                lineGo.transform.SetParent(go.transform, false);
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
            lr.sortingOrder = -2;
            var shader = Shader.Find("Unlit/Color");
            if (shader != null)
                lr.sharedMaterial = new Material(shader);
        }
    }
}
