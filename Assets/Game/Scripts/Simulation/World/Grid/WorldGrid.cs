using System.Collections.Generic;
using UnityEngine;

namespace Simulation
{
    /// <summary>
    /// 世界空间数据库：存储每个格子的状态信息。
    ///
    /// 核心职责：
    ///   1. 空间查询：给定格子坐标，返回该格子的状态（是否可行走、被谁占据、是什么类型）
    ///   2. 状态更新：设置/修改格子的标记和实体 ID
    ///   3. 边界管理：定义世界的有效范围，边界外视为不可行走
    ///
    /// 存储方案：Dictionary<GridPos, Cell>
    ///   优点：按需分配，支持任意坐标（包括负数），内存友好
    ///   缺点：比一维数组慢（哈希查找 vs 直接索引）
    ///   适用场景：格子数量 < 百万级（65536 个格子完全没问题）
    ///   如果后续 A* 寻路成为瓶颈，可在 P4 优化为一维数组
    ///
    /// 默认值约定：
    ///   边界内的格子（未显式设置过）：Flags = Walkable，OccupantId = 0，TypeId = None
    ///   边界外的格子：Flags = None（不可行走），OccupantId = 0，TypeId = None
    ///
    /// P0 泛化说明：
    ///   本类不感知具体实体种类，只负责「格子 → (OccupantId, TypeId, Flags)」。
    ///   新增实体类型无需改动这里。
    /// </summary>
    public class WorldGrid
    {
        private readonly Dictionary<GridPos, Cell> _cells;
        private readonly GridPos _minPos;
        private readonly GridPos _maxPos;

        /// <summary>世界 X 方向的最小格子索引。</summary>
        public GridPos MinPos => _minPos;

        /// <summary>世界 X 方向的最大格子索引。</summary>
        public GridPos MaxPos => _maxPos;

        /// <summary>
        /// 创建有边界的世界。
        /// 边界由 Unity Terrain 的尺寸决定，通常在 TerrainDataReader 初始化时调用。
        /// </summary>
        /// <param name="minPos">世界左下角格子坐标（含）。</param>
        /// <param name="maxPos">世界右上角格子坐标（含）。</param>
        public WorldGrid(GridPos minPos, GridPos maxPos)
        {
            _minPos = minPos;
            _maxPos = maxPos;

            int width = maxPos.X - minPos.X + 1;
            int height = maxPos.Z - minPos.Z + 1;
            // 预估容量：假设 10% 的格子会被设置，减少 Dictionary 扩容开销
            _cells = new Dictionary<GridPos, Cell>((int)(width * height * 0.1f));
        }

        /// <summary>
        /// 根据 Unity Terrain 自动计算世界边界。
        /// 将 Terrain 的世界尺寸除以 CellSize，得到格子范围。
        /// </summary>
        public WorldGrid(Terrain terrain)
        {
            Vector3 size = terrain.terrainData.size;
            int width = Mathf.CeilToInt(size.x / CoordinateUtility.CellSize);
            int height = Mathf.CeilToInt(size.z / CoordinateUtility.CellSize);
            _minPos = new GridPos(0, 0);
            _maxPos = new GridPos(width - 1, height - 1);
            _cells = new Dictionary<GridPos, Cell>((int)(width * height * 0.1f));
        }

        // ---- 边界检查 ----

        /// <summary>检查格子坐标是否在世界边界内。</summary>
        public bool IsInBounds(GridPos pos)
        {
            return pos.X >= _minPos.X && pos.X <= _maxPos.X
                && pos.Z >= _minPos.Z && pos.Z <= _maxPos.Z;
        }

        // ---- 核心查询 ----

        /// <summary>
        /// 获取指定格子的状态。
        /// 边界内未设置的格子返回默认值（Walkable，OccupantId = 0）。
        /// 边界外返回默认值（不可行走，OccupantId = 0）。
        /// </summary>
        public Cell GetCell(GridPos pos)
        {
            if (!IsInBounds(pos))
                return default; // 边界外：Flags = None（不可行走）

            return _cells.TryGetValue(pos, out var cell) ? cell : new Cell { Flags = CellFlags.Walkable };
        }

        /// <summary>
        /// 设置指定格子的状态。
        /// 如果格子已存在，则覆盖；如果不存在，则新增。
        /// 边界外的格子无法设置（静默忽略）。
        /// </summary>
        public void SetCell(GridPos pos, Cell cell)
        {
            if (!IsInBounds(pos))
                return;

            _cells[pos] = cell;
        }

        /// <summary>检查指定格子是否可行走。</summary>
        public bool IsWalkable(GridPos pos)
        {
            return GetCell(pos).IsWalkable;
        }

        /// <summary>检查指定格子是否被任意实体占据。</summary>
        public bool IsOccupied(GridPos pos)
        {
            return GetCell(pos).IsOccupied;
        }

        /// <summary>获取指定格子上的主实体 ID；无实体返回 0。</summary>
        public int GetOccupantAt(GridPos pos)
        {
            return GetCell(pos).OccupantId;
        }

        /// <summary>获取指定格子上主实体的类型 ID；无实体返回 EntityTypes.None。</summary>
        public int GetTypeIdAt(GridPos pos)
        {
            return GetCell(pos).TypeId;
        }

        /// <summary>
        /// 获取指定格子上指定类型的实体 ID。
        /// 返回 0 表示该格子没有该类型的实体。
        /// P0 起 typeId 为开放 int（见 EntityTypes），不再依赖封闭枚举。
        /// </summary>
        public int GetEntityAt(GridPos pos, int typeId)
        {
            return GetCell(pos).GetEntityId(typeId);
        }

        // ---- 格子修改辅助方法 ----
        // 修改格子状态有两种常用模式：
        //
        // 1. 只改标记（最常用）：
        //      grid.SetFlag(pos, CellFlags.HasTree, true);
        //
        // 2. 改多个字段（用 SetOccupant 一次性写 ID + 类型 + 标记）：
        //      var cell = grid.GetCell(pos);
        //      cell.SetOccupant(treeId, EntityTypes.Tree, CellFlags.HasTree | CellFlags.Occupied);
        //      grid.SetCell(pos, cell);

        /// <summary>
        /// 设置/清除指定格子的标记位。
        /// 这是最常用的修改操作，封装了 GetCell → SetFlag/ClearFlag → SetCell。
        /// </summary>
        /// <param name="pos">格子坐标。</param>
        /// <param name="flag">要操作的标记。</param>
        /// <param name="value">true 添加标记，false 移除标记。</param>
        public void SetFlag(GridPos pos, CellFlags flag, bool value)
        {
            if (!IsInBounds(pos))
                return;

            var cell = _cells.TryGetValue(pos, out var existing) ? existing : new Cell { Flags = CellFlags.Walkable };
            if (value)
                cell.SetFlag(flag);
            else
                cell.ClearFlag(flag);
            _cells[pos] = cell;
        }

        // ---- 调试辅助 ----

        /// <summary>返回当前已存储的格子数量（用于调试和性能监控）。</summary>
        public int CellCount => _cells.Count;
    }
}
